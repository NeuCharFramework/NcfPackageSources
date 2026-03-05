using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    public record PromptOptimizationRequestEvent(
        string RequestId,
        string PromptCode,
        string UserRequirement
    ) : IntegrationEvent;

    public record PromptOptimizationResponseEvent(
        string RequestId,
        string NewPromptCode,
        double Score,
        string EvaluationReason
    ) : IntegrationEvent;
}
