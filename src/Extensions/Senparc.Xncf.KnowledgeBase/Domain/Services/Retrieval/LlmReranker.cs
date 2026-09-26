/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LlmReranker.cs
    文件功能描述：LLM 重排器。复用 Senparc.AI.AgentKernel 的对话通道，让对话模型对
    向量召回的候选片段按与问题的相关度排序；输出解析失败或调用异常时回退（Fallback），
    由调用方决定降级策略，保证召回链路不会因重排失败而整体失败。


    创建标识：Senparc - 20260926

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
    /// <summary>
    /// LLM 重排结果。
    /// </summary>
    public sealed class LlmRerankOutcome
    {
        /// <summary>
        /// 0 基候选下标数组，按相关度从高到低排列（包含全部候选）。
        /// </summary>
        public int[] Order { get; init; }

        /// <summary>
        /// true 表示模型输出不可用，Order 为原始顺序（调用方应降级处理）。
        /// </summary>
        public bool Fallback { get; init; }

        /// <summary>
        /// 回退原因（用于日志/诊断）。
        /// </summary>
        public string FallbackReason { get; init; }

        public static LlmRerankOutcome Succeeded(int[] order) => new() { Order = order, Fallback = false };

        public static LlmRerankOutcome Failed(string reason, int[] originalOrder) =>
            new() { Order = originalOrder, Fallback = true, FallbackReason = reason };
    }

    /// <summary>
    /// 基于对话模型的重排器（无新增依赖：仅使用 AgentKernel 既有通道）。
    /// </summary>
    public sealed class LlmReranker
    {
        private static readonly TimeSpan RerankTimeout = TimeSpan.FromSeconds(45);
        private const int MaxSnippetChars = 500;
        private const int MaxPromptQueryChars = 800;

        private static readonly Regex OrderArrayRegex = new(
            @"\[\s*([0-9]{1,4}(?:\s*,\s*[0-9]{1,4}){0,99})\s*\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly AIModelService _aiModelService;

        public LlmReranker(AIModelService aiModelService)
        {
            _aiModelService = aiModelService;
        }

        /// <summary>
        /// 让对话模型对候选片段排序。
        /// </summary>
        /// <param name="query">用户问题</param>
        /// <param name="candidates">候选片段文本（与向量召回顺序一致）</param>
        /// <param name="modelId">重排使用的对话模型 Id（必须 &gt; 0）</param>
        /// <param name="cancellationToken">取消令牌</param>
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
        /// 解析模型输出中的排序数组。容错：忽略 JSON 外壳/解释文字，缺失编号按原顺序补在末尾。
        /// </summary>
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
                var index = number - 1; // 模型输出从 1 开始
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

            // 模型遗漏的候选按原始顺序（即向量排序）补在末尾
            foreach (var index in originalOrder)
            {
                if (!seen.Contains(index))
                {
                    order.Add(index);
                }
            }

            return LlmRerankOutcome.Succeeded(order.ToArray());
        }

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
