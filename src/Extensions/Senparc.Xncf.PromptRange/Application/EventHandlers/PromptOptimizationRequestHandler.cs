using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Senparc.AI.Kernel;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel.Handlers;
using Microsoft.SemanticKernel;
using System.Linq;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.Application.EventHandlers
{
    /// <summary>
    /// PromptOptimization 请求处理器
    /// 负责：
    /// 1. 获取当前 PromptItem 信息
    /// 2. 调用 AI 进行 Prompt 优化
    /// 3. 创建新版本的 PromptItem（标记为 AI 生成）
    /// 4. 发布响应事件
    /// 
    /// 注意：ChatTask 的创建由 AgentsManager 模块负责（在另一个 Handler 中）
    /// </summary>
    public class PromptOptimizationRequestHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
    {
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<PromptOptimizationRequestHandler> _logger;

        public PromptOptimizationRequestHandler(
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            IEventBus eventBus,
            ILogger<PromptOptimizationRequestHandler> logger)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("========== PromptOptimizationRequestHandler 开始 ==========");
            _logger.LogInformation("  RequestId: {@event.RequestId}", @event.RequestId);
            _logger.LogInformation("  Target PromptCode: {@event.PromptCode}", @event.PromptCode);
            _logger.LogInformation("  UserRequirement: {@event.UserRequirement}", @event.UserRequirement);

            try
            {
                // 【步骤1/5】获取原始 PromptItem 和 PromptRange
                _logger.LogInformation("【步骤1/5】获取原始 PromptItem...");
                
                var promptResult = await _promptItemService.GetWithVersionAsync(@event.PromptCode, isAvg: true);
                if (promptResult == null || promptResult.PromptItem == null)
                {
                    throw new Exception($"找不到 PromptCode: {@event.PromptCode}");
                }
                
                _logger.LogInformation("  ✅ 找到 PromptItem: {PromptItemId}, Content Length: {Length}",
                    promptResult.PromptItem.Id, promptResult.PromptItem.Content?.Length);

                // 【步骤2/5】调用 AI 进行 Prompt 优化
                _logger.LogInformation("【步骤2/5】调用 AI 进行 Prompt 优化...");
                
                // 使用 Senparc.AI 的 SemanticKernel 进行优化
                var senparcAiSetting = promptResult.SenparcAiSetting ?? Senparc.AI.Config.SenparcAiSetting;
                var semanticAiHandler = new SemanticAiHandler(senparcAiSetting);
                
                var iWantToRun = semanticAiHandler
                    .IWantTo(senparcAiSetting)
                    .ConfigModel(ConfigModel.Chat, "PromptOptimizer")
                    .BuildKernel();
                
                var kernel = iWantToRun.Kernel;
                
                // 构建优化 Prompt
                var optimizationPrompt = $@"你是一个专业的 Prompt 工程师，专注于优化 AI Prompt 的质量。

## 当前 Prompt 内容：
{@event.PromptContent}

## 用户优化需求：
{@event.UserRequirement}

## 当前参数：
- Temperature: {@event.Context.CurrentTemperature}
- TopP: {@event.Context.CurrentTopP}
- MaxTokens: {@event.Context.CurrentMaxTokens}
- FrequencyPenalty: {@event.Context.CurrentFrequencyPenalty}
- PresencePenalty: {@event.Context.CurrentPresencePenalty}

## 优化任务：
1. 分析当前 Prompt 的优缺点
2. 根据用户需求优化 Prompt 内容（保持原有结构，提升清晰度和效果）
3. 建议参数调整（如需要）
4. 预测优化后的效果评分（0-1，1为最佳）

请返回 JSON 格式：
{{
    ""optimizedContent"": ""优化后的 Prompt 内容"",
    ""temperature"": 建议的 Temperature（0.0-2.0）,
    ""topP"": 建议的 TopP（0.0-1.0）,
    ""maxTokens"": 建议的 MaxTokens（整数）,
    ""frequencyPenalty"": 建议的 FrequencyPenalty（-2.0 到 2.0）,
    ""presencePenalty"": 建议的 PresencePenalty（-2.0 到 2.0）,
    ""score"": 预测评分（0.0-1.0）,
    ""reason"": ""优化原因和预期效果说明""
}}";

                _logger.LogInformation("  调用 AI Kernel 进行优化...");
                var aiResponse = await kernel.InvokePromptAsync(optimizationPrompt, cancellationToken: cancellationToken);
                var aiResult = aiResponse.GetValue<string>();
                
                _logger.LogInformation("  ✅ AI 返回结果（前200字符）: {Result}", 
                    aiResult?.Length > 200 ? aiResult.Substring(0, 200) + "..." : aiResult);
                
                // 【步骤3/5】解析 AI 返回的 JSON
                _logger.LogInformation("【步骤3/5】解析 AI 优化结果...");
                
                // 简化的解析（实际应该用 System.Text.Json 严格解析）
                // 这里为了鲁棒性，使用简单的字符串提取
                var optimizedContent = ExtractJsonValue(aiResult, "optimizedContent") ?? @event.PromptContent;
                var newTemperature = (float)ParseDouble(ExtractJsonValue(aiResult, "temperature"), @event.Context.CurrentTemperature);
                var newTopP = (float)ParseDouble(ExtractJsonValue(aiResult, "topP"), @event.Context.CurrentTopP);
                var newMaxTokens = ParseInt(ExtractJsonValue(aiResult, "maxTokens"), @event.Context.CurrentMaxTokens);
                var newFrequencyPenalty = (float)ParseDouble(ExtractJsonValue(aiResult, "frequencyPenalty"), @event.Context.CurrentFrequencyPenalty);
                var newPresencePenalty = (float)ParseDouble(ExtractJsonValue(aiResult, "presencePenalty"), @event.Context.CurrentPresencePenalty);
                var predictedScore = ParseDouble(ExtractJsonValue(aiResult, "score"), 0.85);
                var optimizationReason = ExtractJsonValue(aiResult, "reason") ?? "AI 自动优化";
                
                _logger.LogInformation("  ✅ 解析完成：Score={Score}, Temperature={Temp}, TopP={TopP}", 
                    predictedScore, newTemperature, newTopP);

                // 【步骤4/5】创建新版本的 PromptItem
                _logger.LogInformation("【步骤4/5】创建新版本 PromptItem...");
                
                var originalItem = promptResult.PromptItem;
                var newPromptItemRequest = new PromptItem_AddRequest
                {
                    RangeId = originalItem.RangeId,
                    ModelId = @event.Context.ModelId,
                    Content = optimizedContent,
                    Temperature = newTemperature,
                    TopP = newTopP,
                    MaxToken = newMaxTokens,
                    FrequencyPenalty = newFrequencyPenalty,
                    PresencePenalty = newPresencePenalty,
                    StopSequences = originalItem.StopSequences,
                    NumsOfResults = 1,
                    IsTopTactic = false,
                    IsNewTactic = false,
                    IsNewSubTactic = false,
                    IsNewAiming = true,  // 新版本的 Aiming
                    IsDraft = false,
                    Note = "🤖AI-Generated", // 标记 AI 生成
                    ExpectedResultsJson = originalItem.ExpectedResultsJson ?? string.Empty,
                    Prefix = originalItem.Prefix ?? string.Empty,
                    Suffix = originalItem.Suffix ?? string.Empty,
                    VariableDictJson = originalItem.VariableDictJson ?? string.Empty,
                    isAIGrade = false
                };
                
                var newPromptItem = await _promptItemService.AddPromptItemAsync(newPromptItemRequest);
                var newPromptCode = newPromptItem.FullVersion;
                
                _logger.LogInformation("  ✅ 新 PromptItem 创建成功！NewPromptCode: {NewPromptCode}", newPromptCode);

                // 【步骤5/5】发布响应事件
                _logger.LogInformation("【步骤5/5】发布优化响应...");
                
                var responseEvent = new PromptOptimizationResponseEvent(
                    @event.RequestId,
                    newPromptCode,
                    optimizedContent,
                    new OptimizedParameters(
                        Temperature: newTemperature,
                        TopP: newTopP,
                        MaxTokens: newMaxTokens,
                        FrequencyPenalty: newFrequencyPenalty,
                        PresencePenalty: newPresencePenalty
                    ),
                    predictedScore,
                    optimizationReason,
                    true,
                    ""
                );

                await _eventBus.PublishDerivedAsync(responseEvent, @event);
                _logger.LogInformation("========== PromptOptimizationRequestHandler 完成 ==========");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Prompt 优化失败");
                
                // 发布错误响应事件
                var errorResponse = new PromptOptimizationResponseEvent(
                    @event.RequestId,
                    null,
                    null,
                    null,
                    0,
                    ex.Message,
                    false,
                    ex.Message
                );
                
                await _eventBus.PublishDerivedAsync(errorResponse, @event);
            }
        }
        
        // 辅助方法：从 JSON 字符串中提取值
        private string ExtractJsonValue(string json, string key)
        {
            try
            {
                var pattern = $"\"{key}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                
                // 尝试匹配数字类型（不带引号）
                pattern = $"\"{key}\"\\s*:\\s*([0-9.\\-]+)";
                match = System.Text.RegularExpressions.Regex.Match(json, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "提取 JSON 值失败: Key={Key}", key);
            }
            
            return null;
        }
        
        private double ParseDouble(string value, double defaultValue)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return double.TryParse(value, out var result) ? result : defaultValue;
        }
        
        private int ParseInt(string value, int defaultValue)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
