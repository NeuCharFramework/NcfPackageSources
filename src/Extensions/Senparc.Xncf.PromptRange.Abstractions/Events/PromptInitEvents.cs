using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    /// <summary>
    /// Prompt 初始化请求事件（支持自定义 Model）
    /// </summary>
    public record PromptInitRequestEvent(
        string RequestId,
        int? ModelId = null  // 可选：用户选择的 Model ID
    ) : IntegrationEvent;

    /// <summary>
    /// Prompt 初始化响应事件
    /// </summary>
    public record PromptInitResponseEvent(
        string RequestId,
        string PromptCode,
        bool Success,
        string ErrorMessage
    ) : IntegrationEvent;
}
