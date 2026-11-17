/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InternalEventBusRoundTripEvents.cs
    File description: template internal EventBus request-response round-trip events

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;

namespace Template_OrgName.Xncf.Template_XncfName.Application.Events
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed record InternalEventBusRoundTripRequest(DateTime SentAtUtc)
        : IntegrationRequest<InternalEventBusRoundTripResponse>;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed record InternalEventBusRoundTripResponse(
        Guid RequestId,
        DateTime SentAtUtc,
        DateTime HandledAtUtc)
        : IntegrationResponse(RequestId);
}
