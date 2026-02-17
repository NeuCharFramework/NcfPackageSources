using Senparc.Ncf.Core.Events; // 假设 IIntegrationEvent 在这里
using Senparc.Ncf.Shared.Abstractions.Models; // 如果 ActionType 是通用的，放 Shared；如果是私有的，放这里

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