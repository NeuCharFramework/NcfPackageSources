using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    /// <summary>
    /// Prompt 优化请求事件
    /// </summary>
    public record PromptOptimizationRequestEvent(
        string RequestId,
        string PromptCode,
        string PromptContent,
        string UserRequirement,
        OptimizationContext Context
    ) : IntegrationEvent
    {
        public override string GetEventSummary()
        {
            return $"PromptOptimizationRequest[{RequestId}] Code={PromptCode}";
        }
    };

    /// <summary>
    /// Prompt 优化响应事件
    /// </summary>
    public record PromptOptimizationResponseEvent(
        string RequestId,
        string NewPromptCode,
        string NewPromptContent,
        OptimizedParameters Parameters,
        double Score,
        string EvaluationReason,
        bool Success = true,
        string ErrorMessage = null
    ) : IntegrationEvent
    {
        public override string GetEventSummary()
        {
            return $"PromptOptimizationResponse[{RequestId}] NewCode={NewPromptCode}, Score={Score:F2}";
        }
    };

    /// <summary>
    /// 优化上下文（当前 Prompt 的参数）
    /// </summary>
    public record OptimizationContext(
        int ModelId,
        float CurrentTemperature,
        float CurrentTopP,
        int CurrentMaxTokens,
        float CurrentFrequencyPenalty,
        float CurrentPresencePenalty,
        bool AutoShootAfterOptimize = true,      // 🆕 创建后立即打靶（默认 true）
        bool AutoAIGradeAfterShoot = false       // 🆕 打靶后 AI 评分（默认 false）
    );

    /// <summary>
    /// 优化后的参数
    /// </summary>
    public record OptimizedParameters(
        float Temperature,
        float TopP,
        int MaxTokens,
        float FrequencyPenalty,
        float PresencePenalty
    );
}
