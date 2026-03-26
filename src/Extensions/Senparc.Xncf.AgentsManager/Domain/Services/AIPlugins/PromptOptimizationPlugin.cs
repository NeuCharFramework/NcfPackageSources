using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Senparc.CO2NET;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins
{
    /// <summary>
    /// Prompt 优化 Plugin
    /// Agent 通过调用这些 function 来完成 Prompt 优化任务
    /// </summary>
    public class PromptOptimizationPlugin
    {
        private readonly PromptOptimizationAgentBridge _bridge;
        private readonly ILogger<PromptOptimizationPlugin> _logger;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly PromptResultService _promptResultService;

        public PromptOptimizationPlugin(PromptOptimizationAgentBridge bridge, ILogger<PromptOptimizationPlugin> logger)
        {
            _bridge = bridge;
            _logger = logger;
            var serviceProvider = SenparcDI.GetServiceProvider();
            _promptItemService = serviceProvider.GetRequiredService<PromptItemService>();
            _promptRangeService = serviceProvider.GetRequiredService<PromptRangeService>();
            _promptResultService = serviceProvider.GetRequiredService<PromptResultService>();
        }

        private static string UnescapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            return value
                .Replace("\\\\", "\u0001")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\u0001", "\\");
        }

        /// <summary>
        /// 获取 Prompt 信息
        /// </summary>
        [KernelFunction, Description("Get detailed information about a specific prompt")]
        public async Task<string> GetPromptInfo(
            [Description("The prompt code (e.g., 2025.12.28.3-T3.1-A2)")] string promptCode
        )
        {
            try
            {
                var promptResult = await _promptItemService.GetWithVersionAsync(promptCode, isAvg: true);
                if (promptResult == null || promptResult.PromptItem == null)
                {
                    return $"Error: Prompt with code '{promptCode}' not found.";
                }

                var item = promptResult.PromptItem;
                return $@"Prompt Information:
- Code: {item.FullVersion}
- Content: {item.Content}
- Model ID: {item.ModelId}
- Temperature: {item.Temperature}
- TopP: {item.TopP}
- MaxTokens: {item.MaxToken}
- FrequencyPenalty: {item.FrequencyPenalty}
- PresencePenalty: {item.PresencePenalty}
- Average Score: {item.EvalAvgScore}
- Max Score: {item.EvalMaxScore}
- Range Name: {item.RangeName}
- Note: {item.Note}";
            }
            catch (Exception ex)
            {
                return $"Error getting prompt info: {ex.Message}";
            }
        }

        /// <summary>
        /// 分析 Range 中所有模型的历史评分
        /// </summary>
        [KernelFunction, Description("Analyze historical scores of all models in the current range")]
        public async Task<string> AnalyzeModelScores(
            [Description("The range name (e.g., 2025.12.28.3)")] string rangeName
        )
        {
            try
            {
                var scoredItems = await _promptItemService.GetFullListAsync(
                    p => p.RangeName == rangeName && p.EvalAvgScore > 0,
                    p => p.Id,
                    Senparc.Ncf.Core.Enums.OrderingType.Ascending
                );

                if (scoredItems.Count == 0)
                {
                    return "No scored items found in this range.";
                }

                var modelScores = scoredItems
                    .GroupBy(p => p.ModelId)
                    .Select(g => new
                    {
                        ModelId = g.Key,
                        AvgScore = g.Average(p => (double)p.EvalAvgScore),
                        MaxScore = g.Max(p => (double)p.EvalMaxScore),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.AvgScore)
                    .ToList();

                var analysis = "Model Performance Analysis:\n";
                foreach (var model in modelScores)
                {
                    analysis += $"- Model {model.ModelId}: Avg={model.AvgScore:F2}, Max={model.MaxScore:F2}, Count={model.Count}\n";
                }
                analysis += $"\nBest Model: {modelScores.First().ModelId}";
                return analysis;
            }
            catch (Exception ex)
            {
                return $"Error analyzing model scores: {ex.Message}";
            }
        }

        /// <summary>
        /// 创建优化后的新 Prompt 版本
        /// </summary>
        [KernelFunction, Description("Create exactly ONE new version per optimization task. Call at most once — duplicate calls are rejected without creating rows. First parameter may be empty.")]
        public async Task<string> CreateOptimizedPrompt(
            [Description("Optional: REQUEST_ID from task; leave empty if unsure — server uses active correlation id")] string optimizationRequestId,
            [Description("Base prompt code to optimize from")] string basePromptCode,
            [Description("Optimized prompt content")] string optimizedContent,
            [Description("Recommended model ID (based on historical scores)")] int modelId,
            [Description("Recommended temperature (0.0-2.0)")] float temperature,
            [Description("Recommended topP (0.0-1.0)")] float topP,
            [Description("Recommended maxTokens")] int maxTokens,
            [Description("Recommended frequencyPenalty (-2.0 to 2.0)")] float frequencyPenalty,
            [Description("Recommended presencePenalty (-2.0 to 2.0)")] float presencePenalty,
            [Description("Brief summary of what you improved (for the user)")] string improvementSummary = null
        )
        {
            try
            {
                var activeId = PromptOptimizationAgentBridge.TryGetActiveRequestId()?.Trim();
                var paramId = optimizationRequestId?.Trim();
                var rid = !string.IsNullOrEmpty(activeId) ? activeId : paramId;
                _logger.LogInformation(
                    "CreateOptimizedPrompt: rid={Rid} (active={ActiveId}, param={ParamId}), basePromptCode={Code}",
                    rid, activeId, paramId, basePromptCode);
                if (string.IsNullOrEmpty(rid))
                {
                    return "Error: No active optimization request id (internal). Pass optimizationRequestId from the task or retry from PromptRange.";
                }

                if (_bridge.TryGetRecordedPromptCode(rid, out var existingCode))
                {
                    _logger.LogWarning(
                        "CreateOptimizedPrompt skipped (duplicate): rid={Rid}, existing={Code}",
                        rid, existingCode);
                    return $"Skipped: this optimization request already created version {existingCode}. Do not call CreateOptimizedPrompt again — the task is complete.";
                }

                var content = UnescapeJsonString(optimizedContent ?? "");

                // 获取原始 PromptItem
                var promptResult = await _promptItemService.GetWithVersionAsync(basePromptCode, isAvg: true);
                if (promptResult == null || promptResult.PromptItem == null)
                {
                    return $"Error: Base prompt '{basePromptCode}' not found.";
                }

                var originalItem = promptResult.PromptItem;

                // 创建新版本请求
                var newPromptItemRequest = new PromptItem_AddRequest
                {
                    Id = originalItem.Id,
                    RangeId = originalItem.RangeId,
                    ModelId = modelId,
                    Content = content,
                    Temperature = temperature,
                    TopP = topP,
                    MaxToken = maxTokens,
                    FrequencyPenalty = frequencyPenalty,
                    PresencePenalty = presencePenalty,
                    StopSequences = originalItem.StopSequences,
                    NumsOfResults = 1,
                    IsTopTactic = false,
                    IsNewTactic = false,
                    IsNewSubTactic = false,
                    IsNewAiming = true,
                    IsDraft = false,
                    Note = "🤖AI-Agent-Generated",
                    ExpectedResultsJson = originalItem.ExpectedResultsJson ?? string.Empty,
                    Prefix = originalItem.Prefix ?? string.Empty,
                    Suffix = originalItem.Suffix ?? string.Empty,
                    VariableDictJson = originalItem.VariableDictJson ?? string.Empty,
                    isAIGrade = false
                };

                var newPromptItem = await _promptItemService.AddPromptItemAsync(newPromptItemRequest);

                _bridge.RecordCreatedPrompt(
                    rid,
                    newPromptItem.FullVersion,
                    newPromptItem.Content,
                    temperature,
                    topP,
                    maxTokens,
                    frequencyPenalty,
                    presencePenalty,
                    improvementSummary);

                return $"Success! New prompt created: {newPromptItem.FullVersion}. The UI will refresh; do not call shoot/grade tools — the page runs them.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateOptimizedPrompt failed for basePromptCode={Code}", basePromptCode);
                return $"Error creating optimized prompt: {ex.Message}";
            }
        }

        /// <summary>
        /// 可选：一般由 PromptRange 页面打靶；仅当任务明确要求时在 Agent 内调用
        /// </summary>
        [KernelFunction, Description("Execute a shoot test on the prompt")]
        public async Task<string> ExecuteShootTest(
            [Description("The prompt code to test")] string promptCode
        )
        {
            try
            {
                // 获取 PromptItem
                var promptResult = await _promptItemService.GetWithVersionAsync(promptCode, isAvg: true);
                if (promptResult == null || promptResult.PromptItem == null)
                {
                    return $"Error: Prompt '{promptCode}' not found.";
                }

                var promptItem = promptResult.PromptItem;

                // 执行打靶
                var shootResult = await _promptResultService.SenparcGenerateResultAsync(
                    _promptItemService.Mapper.Map<Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto.PromptItemDto>(promptItem),
                    userMessage: null,
                    chatHistory: null
                );

                return $"Shoot test completed! Result ID: {shootResult.Id}, Output length: {shootResult.ResultString?.Length ?? 0} characters.";
            }
            catch (Exception ex)
            {
                return $"Error executing shoot test: {ex.Message}";
            }
        }

        /// <summary>
        /// 执行 AI 评分
        /// </summary>
        [KernelFunction, Description("Execute AI scoring on the shoot result")]
        public async Task<string> ExecuteAIGrade(
            [Description("The prompt code to grade")] string promptCode
        )
        {
            try
            {
                // 获取 PromptItem
                var promptResult = await _promptItemService.GetWithVersionAsync(promptCode, isAvg: true);
                if (promptResult == null || promptResult.PromptItem == null)
                {
                    return $"Error: Prompt '{promptCode}' not found.";
                }

                var promptItem = promptResult.PromptItem;

                // 获取最新的 PromptResult
                var promptResults = await _promptResultService.GetByItemId(promptItem.Id);
                if (promptResults == null || promptResults.Count == 0)
                {
                    return "Error: No shoot results found for this prompt. Please execute a shoot test first.";
                }

                var latestResult = promptResults.OrderByDescending(r => r.Id).First();

                // 获取期望结果
                var expectedResultsJson = promptItem.ExpectedResultsJson;
                if (string.IsNullOrWhiteSpace(expectedResultsJson))
                {
                    return "Error: No expected results configured for this prompt.";
                }

                // 执行 AI 评分
                await _promptResultService.RobotScoringAsync(
                    latestResult.Id,
                    isRefresh: false,
                    expectedResultsJson
                );

                // 更新 PromptItem 的平均分
                await _promptResultService.UpdateEvalScoreAsync(promptItem.Id);

                // 重新获取更新后的 PromptItem
                var updatedPromptResult = await _promptItemService.GetWithVersionAsync(promptCode, isAvg: true);
                var updatedItem = updatedPromptResult.PromptItem;

                return $"AI grading completed! Average Score: {updatedItem.EvalAvgScore}, Max Score: {updatedItem.EvalMaxScore}";
            }
            catch (Exception ex)
            {
                return $"Error executing AI grade: {ex.Message}";
            }
        }
    }
}
