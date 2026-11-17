/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSyncEvent.cs
    File description: Related implementation.

    Created by: Senparc - 20260726

    Modified by: Senparc - 20260726
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

----------------------------------------------------------------*/

using System.Globalization;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Areas.Admin.OHS.Local.Events;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed record AdminChatSyncEvent(
    int AdminUserId,
    int SessionId,
    string Action) : IntegrationEvent, IAuthorizedIntegrationSyncEvent
{
    public string Channel => "admin-chat";

    public string OwnerId => AdminUserId.ToString(CultureInfo.InvariantCulture);

    public string ResourceId => SessionId.ToString(CultureInfo.InvariantCulture);

    public string RequiredPolicy => NcfAuthorizationPolicyNames.AdminOnly;

    public override string GetEventSummary()
    {
        return $"Admin Chat resource changed: SessionId={SessionId}, Action={Action}";
    }
}
