/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptOptimizationEvents.cs
    File description: PromptOptimizationEvents related implementation
    
    
    Created by: Senparc - 20260306
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    /// <summary>
    ///Legacy comment translated to English.
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
    ///Legacy comment translated to English.
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
    ///Legacy comment translated to English.
    /// </summary>
    public record OptimizationContext(
        int ModelId,
        float CurrentTemperature,
        float CurrentTopP,
        int CurrentMaxTokens,
        float CurrentFrequencyPenalty,
        float CurrentPresencePenalty,
        bool AutoShootAfterOptimize = true,      //Legacy comment translated to English.
        bool AutoAIGradeAfterShoot = false       //Legacy comment translated to English.
    );

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public record OptimizedParameters(
        float Temperature,
        float TopP,
        int MaxTokens,
        float FrequencyPenalty,
        float PresencePenalty
    );
}
