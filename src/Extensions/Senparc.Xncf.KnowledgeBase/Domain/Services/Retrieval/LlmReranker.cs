/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlmReranker.cs
    File description: Related implementation.
    Legacy comment translated to English.
    Legacy comment translated to English.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval
{
    /// <summary>Represents an LLM reranking result and whether fallback order was used.</summary>
    public sealed class LlmRerankOutcome
    {
        /// <summary>Gets the zero-based candidate order returned by the reranker or fallback.</summary>
        public int[] Order { get; init; }

        /// <summary>Gets a value indicating whether original retrieval order was used.</summary>
        public bool Fallback { get; init; }

        /// <summary>Gets the fallback reason, when <see cref="Fallback"/> is <see langword="true"/>.</summary>
        public string FallbackReason { get; init; }

        /// <summary>Creates a successful reranking outcome.</summary>
        /// <param name="order">The zero-based candidate order.</param>
        /// <returns>A non-fallback outcome.</returns>
        public static LlmRerankOutcome Succeeded(int[] order) => new() { Order = order, Fallback = false };

        /// <summary>Creates a fallback outcome preserving the original candidate order.</summary>
        /// <param name="reason">The fallback reason.</param>
        /// <param name="originalOrder">The original zero-based candidate order.</param>
        /// <returns>A fallback outcome.</returns>
        public static LlmRerankOutcome Failed(string reason, int[] originalOrder) =>
            new() { Order = originalOrder, Fallback = true, FallbackReason = reason };
    }

    /// <summary>Uses a configured chat model to rerank retrieval candidates.</summary>
    public sealed class LlmReranker
    {
        private static readonly TimeSpan RerankTimeout = TimeSpan.FromSeconds(45);
        private const int MaxSnippetChars = 500;
        private const int MaxPromptQueryChars = 800;

        private static readonly Regex OrderArrayRegex = new(
            @"\[\s*([0-9]{1,4}(?:\s*,\s*[0-9]{1,4}){0,99})\s*\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly AIModelService _aiModelService;

        /// <summary>
        /// Initializes the LLM reranker.
        /// </summary>
        /// <param name="aiModelService">The service used to resolve the configured chat model.</param>
        public LlmReranker(AIModelService aiModelService)
        {
            _aiModelService = aiModelService;
        }

        /// <summary>
        /// Asks the configured chat model to order candidates by relevance and falls back
        /// to the original order for missing configuration, timeout, or model failures.
        /// </summary>
        /// <param name="query">The user query.</param>
        /// <param name="candidates">Candidate text fragments.</param>
        /// <param name="modelId">The configured chat model identifier.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The model order or a fallback outcome.</returns>
        public async Task<LlmRerankOutcome> RankAsync(
            string query,
            IReadOnlyList<string> candidates,
            int modelId,
            CancellationToken cancellationToken = default)
        {
            var originalOrder = candidates.Select((_, index) => index).ToArray();
            if (candidates.Count <= 1)
            {
                return LlmRerankOutcome.Succeeded(originalOrder);
            }
            if (modelId <= 0)
            {
                return LlmRerankOutcome.Failed("未配置可用的重排对话模型。", originalOrder);
            }

            var model = await _aiModelService.GetObjectAsync(z => z.Id == modelId).ConfigureAwait(false);
            if (model == null)
            {
                return LlmRerankOutcome.Failed($"重排对话模型不存在：{modelId}", originalOrder);
            }

            var modelDto = _aiModelService.Mapper.Map<AIModelDto>(model);
            var setting = _aiModelService.BuildSenparcAiSetting(modelDto);

            var handler = new AgentAiHandler(setting);
            var runner = await handler
                .IWantTo(setting)
                .ConfigChatModel("NcfKnowledgeBase_Rerank", new Microsoft.Agents.AI.ChatClientAgentOptions
                {
                    Name = "NcfKnowledgeBase_Rerank",
                    Description = "知识库检索结果重排",
                    ChatOptions = new Microsoft.Extensions.AI.ChatOptions
                    {
                        Instructions = "你是一个精确的检索结果重排引擎。只输出 JSON 对象，禁止输出任何解释、前后缀或 Markdown。",
                        Temperature = 0,
                        MaxOutputTokens = 512
                    }
                })
                .BuildKernelWithAgentSessionAsync()
                .ConfigureAwait(false);

            var prompt = BuildPrompt(query, candidates);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(RerankTimeout);

            string output;
            try
            {
                var response = await runner
                    .RunChatResponseAsync(prompt, runner.Kernel.AgentSession, null, timeoutSource.Token)
                    .ConfigureAwait(false);
                output = response?.Text;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return LlmRerankOutcome.Failed($"重排模型调用超时（>{(int)RerankTimeout.TotalSeconds} 秒）。", originalOrder);
            }
            catch (Exception ex)
            {
                return LlmRerankOutcome.Failed($"重排模型调用失败：{ex.Message}", originalOrder);
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return LlmRerankOutcome.Failed("重排模型没有返回内容。", originalOrder);
            }

            return ParseOrder(output, candidates.Count, originalOrder);
        }

        /// <summary>
        /// Parses a one-based integer order array from model output and appends omitted
        /// candidate indexes in their original order.
        /// </summary>
        /// <param name="output">The raw model output.</param>
        /// <param name="candidateCount">The number of candidates.</param>
        /// <param name="originalOrder">The original zero-based order.</param>
        /// <returns>A successful or fallback outcome.</returns>
        public static LlmRerankOutcome ParseOrder(string output, int candidateCount, int[] originalOrder)
        {
            var match = OrderArrayRegex.Match(output ?? string.Empty);
            if (!match.Success)
            {
                return LlmRerankOutcome.Failed("模型输出中没有可解析的排序数组。", originalOrder);
            }

            var seen = new HashSet<int>();
            var order = new List<int>(candidateCount);
            foreach (var part in match.Groups[1].Value.Split(','))
            {
                if (!int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                {
                    continue;
                }
                var index = number - 1; // modeloutputfrom 1 start
                if (index < 0 || index >= candidateCount || seen.Contains(index))
                {
                    continue;
                }
                seen.Add(index);
                order.Add(index);
            }

            if (order.Count == 0)
            {
                return LlmRerankOutcome.Failed("排序数组为空或编号全部越界。", originalOrder);
            }

            // Append candidates omitted by the model so the result remains a complete
            // permutation of the original candidate set.
            foreach (var index in originalOrder)
            {
                if (!seen.Contains(index))
                {
                    order.Add(index);
                }
            }

            return LlmRerankOutcome.Succeeded(order.ToArray());
        }

        /// <summary>
        /// Builds the bounded prompt instructing the model to return only a candidate order.
        /// </summary>
        /// <param name="query">The user query.</param>
        /// <param name="candidates">The candidate snippets.</param>
        /// <returns>The reranking prompt.</returns>
        private static string BuildPrompt(string query, IReadOnlyList<string> candidates)
        {
            var builder = new StringBuilder();
            builder.Append("相关性重排任务。\n");
            builder.Append("用户问题：").Append(query.Length > MaxPromptQueryChars ? query.Substring(0, MaxPromptQueryChars) + "…" : query).Append("\n\n");
            builder.Append("下面按编号给出候选知识片段，请按“回答用户问题的有用程度”从高到低排序。\n");
            for (var i = 0; i < candidates.Count; i++)
            {
                var text = candidates[i] ?? string.Empty;
                if (text.Length > MaxSnippetChars)
                {
                    text = text.Substring(0, MaxSnippetChars) + "…";
                }
                builder.Append('[').Append(i + 1).Append("] ").Append(text.Replace('\n', ' ')).Append('\n');
            }

            builder.Append("\n输出要求：只输出一个 JSON 对象，例如 {\"order\":[2,1,3]}；");
            builder.Append("order 必须包含 1 到 ").Append(candidates.Count).Append(" 的全部编号，按相关度从高到低排列。不要输出任何其他内容。");
            return builder.ToString();
        }
    }
}
