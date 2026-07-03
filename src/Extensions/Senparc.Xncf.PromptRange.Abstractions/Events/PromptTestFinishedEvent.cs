/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptTestFinishedEvent.cs
    文件功能描述：PromptTestFinishedEvent 相关实现
    
    
    创建标识：Senparc - 20260218
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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