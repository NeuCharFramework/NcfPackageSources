using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    /// <summary>
    /// Prompt initialization request event (supports custom Model)
    /// </summary>
    public record PromptInitRequestEvent(
        string RequestId,
        int? ModelId = null  // Optional: User-selected Model ID
    ) : IntegrationEvent;

    /// <summary>
    ///Prompt initializes response events
    /// </summary>
    public record PromptInitResponseEvent(
        string RequestId,
        string PromptCode,
        bool Success,
        string ErrorMessage
    ) : IntegrationEvent;
}
