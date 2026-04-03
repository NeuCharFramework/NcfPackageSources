using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    /// <summary>
    ///Prompt optimization request event
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
    ///Prompt optimizes response events
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
    /// Optimization context (parameters of the current Prompt)
    /// </summary>
    public record OptimizationContext(
        int ModelId,
        float CurrentTemperature,
        float CurrentTopP,
        int CurrentMaxTokens,
        float CurrentFrequencyPenalty,
        float CurrentPresencePenalty,
        bool AutoShootAfterOptimize = true,      // 🆕 Target shooting immediately after creation (default true)
        bool AutoAIGradeAfterShoot = false       // 🆕 AI scoring after target practice (default false)
    );

    /// <summary>
    ///Optimized parameters
    /// </summary>
    public record OptimizedParameters(
        float Temperature,
        float TopP,
        int MaxTokens,
        float FrequencyPenalty,
        float PresencePenalty
    );
}
