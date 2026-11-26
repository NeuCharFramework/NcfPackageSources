/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptOptimizationEvents.cs
    File description: PromptOptimizationEvents related implementation

    Created by: Senparc - 20260306

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.3-preview8 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    /// <summary>
    /// Requests AI-assisted optimization of one PromptRange prompt.
    /// </summary>
    /// <param name="RequestId">The optimization correlation identifier.</param>
    /// <param name="PromptCode">The source PromptCode.</param>
    /// <param name="PromptContent">The current prompt content.</param>
    /// <param name="UserRequirement">The requested improvement goal.</param>
    /// <param name="Context">The current model and automatic-evaluation settings.</param>
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
    /// Reports the optimized prompt, parameters, score, and evaluation outcome.
    /// </summary>
    /// <param name="RequestId">The request correlation identifier.</param>
    /// <param name="NewPromptCode">The generated PromptCode.</param>
    /// <param name="NewPromptContent">The generated prompt content.</param>
    /// <param name="Parameters">The generated model parameters.</param>
    /// <param name="Score">The evaluation score.</param>
    /// <param name="EvaluationReason">The evaluator's explanation.</param>
    /// <param name="Success">Whether optimization completed successfully.</param>
    /// <param name="ErrorMessage">The failure message when unsuccessful.</param>
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
    /// Captures the model configuration and automatic evaluation flags used for optimization.
    /// </summary>
    /// <param name="ModelId">The model identifier.</param>
    /// <param name="CurrentTemperature">The current temperature.</param>
    /// <param name="CurrentTopP">The current top-p value.</param>
    /// <param name="CurrentMaxTokens">The current output-token limit.</param>
    /// <param name="CurrentFrequencyPenalty">The current frequency penalty.</param>
    /// <param name="CurrentPresencePenalty">The current presence penalty.</param>
    /// <param name="AutoShootAfterOptimize">Whether to run a shoot test automatically.</param>
    /// <param name="AutoAIGradeAfterShoot">Whether to grade the shoot result automatically.</param>
    public record OptimizationContext(
        int ModelId,
        float CurrentTemperature,
        float CurrentTopP,
        int CurrentMaxTokens,
        float CurrentFrequencyPenalty,
        float CurrentPresencePenalty,
        bool AutoShootAfterOptimize = true,
        bool AutoAIGradeAfterShoot = false
    );

    /// <summary>
    /// Contains the model-generation parameters proposed by optimization.
    /// </summary>
    /// <param name="Temperature">The proposed temperature.</param>
    /// <param name="TopP">The proposed top-p value.</param>
    /// <param name="MaxTokens">The proposed output-token limit.</param>
    /// <param name="FrequencyPenalty">The proposed frequency penalty.</param>
    /// <param name="PresencePenalty">The proposed presence penalty.</param>
    public record OptimizedParameters(
        float Temperature,
        float TopP,
        int MaxTokens,
        float FrequencyPenalty,
        float PresencePenalty
    );
}
