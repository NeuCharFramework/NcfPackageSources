/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptInitEvents.cs
    File description: PromptInitEvents related implementation

    Created by: Senparc - 20260306

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.3-preview8 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.3.3.2-preview9 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    /// <summary>
    /// Requests initialization of the default PromptCatalyzer PromptRange data.
    /// </summary>
    /// <param name="RequestId">The EventBus correlation identifier.</param>
    /// <param name="ModelId">Optional AI model identifier to use for the initial prompt.</param>
    public record PromptInitRequestEvent(
        string RequestId,
        int? ModelId = null
    ) : IntegrationEvent;

    /// <summary>
    /// Reports the result of PromptRange initialization.
    /// </summary>
    /// <param name="RequestId">The correlation identifier from the request.</param>
    /// <param name="PromptCode">The initialized PromptCode, when successful.</param>
    /// <param name="Success">Whether initialization completed successfully.</param>
    /// <param name="ErrorMessage">The failure message when initialization fails.</param>
    public record PromptInitResponseEvent(
        string RequestId,
        string PromptCode,
        bool Success,
        string ErrorMessage
    ) : IntegrationEvent;
}
