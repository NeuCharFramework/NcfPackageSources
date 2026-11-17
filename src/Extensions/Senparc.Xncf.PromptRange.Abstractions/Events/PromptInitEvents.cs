/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptInitEvents.cs
    File description: PromptInitEvents related implementation

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
    public record PromptInitRequestEvent(
        string RequestId,
        int? ModelId = null  //Legacy comment translated to English.
    ) : IntegrationEvent;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public record PromptInitResponseEvent(
        string RequestId,
        string PromptCode,
        bool Success,
        string ErrorMessage
    ) : IntegrationEvent;
}
