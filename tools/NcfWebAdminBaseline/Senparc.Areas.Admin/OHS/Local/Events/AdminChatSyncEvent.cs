/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSyncEvent.cs
    File description: Related implementation.

    Created by: Senparc - 20260726

    Modified by: Senparc - 20260726
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System.Globalization;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Areas.Admin.OHS.Local.Events;

/// <summary>
/// Describes an authorized synchronization event for a changed administrator chat session.
/// The event carries the administrator owner and session resource identifiers used by
/// downstream authorization checks.
/// </summary>
public sealed record AdminChatSyncEvent(
    int AdminUserId,
    int SessionId,
    string Action) : IntegrationEvent, IAuthorizedIntegrationSyncEvent
{
    /// <summary>
    /// Gets the synchronization channel used for administrator chat events.
    /// </summary>
    public string Channel => "admin-chat";

    /// <summary>
    /// Gets the administrator identifier used as the event owner.
    /// </summary>
    public string OwnerId => AdminUserId.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the chat session identifier used as the protected resource.
    /// </summary>
    public string ResourceId => SessionId.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the authorization policy required to consume the event.
    /// </summary>
    public string RequiredPolicy => NcfAuthorizationPolicyNames.AdminOnly;

    /// <inheritdoc/>
    public override string GetEventSummary()
    {
        return $"Admin Chat resource changed: SessionId={SessionId}, Action={Action}";
    }
}
