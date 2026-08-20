/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptInitEvents.cs
    文件功能描述：PromptInitEvents 相关实现
    
    
    创建标识：Senparc - 20260306
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
