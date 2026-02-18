using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    // 这个事件定义属于 PromptRange 的契约
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