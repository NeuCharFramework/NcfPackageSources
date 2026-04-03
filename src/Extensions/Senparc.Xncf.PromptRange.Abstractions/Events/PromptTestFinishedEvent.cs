using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    // This event definition belongs to the PromptRange contract
    public record PromptTestFinishedEvent(
        string PromptCode,
        string OriginalPrompt,
        string UserInput,
        string ModelOutput,
        double Score,
        string EvaluationReason,
        string ModelId,
        double Temperature
    ) : IntegrationEvent;


}