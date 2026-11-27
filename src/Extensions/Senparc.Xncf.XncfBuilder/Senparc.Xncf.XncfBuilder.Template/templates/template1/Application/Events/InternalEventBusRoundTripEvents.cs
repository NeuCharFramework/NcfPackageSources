/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InternalEventBusRoundTripEvents.cs
    File description: template internal EventBus request-response round-trip events

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v1.1.16 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;

namespace Template_OrgName.Xncf.Template_XncfName.Application.Events
{
    /// <summary>
    /// Requests a template-module EventBus round trip and carries the UTC send timestamp.
    /// </summary>
    /// <param name="SentAtUtc">The UTC timestamp at which the request was created.</param>
    public sealed record InternalEventBusRoundTripRequest(DateTime SentAtUtc)
        : IntegrationRequest<InternalEventBusRoundTripResponse>;

    /// <summary>
    /// Reports the request send time and the UTC timestamp at which the template handler processed it.
    /// </summary>
    /// <param name="RequestId">The correlated request identifier.</param>
    /// <param name="SentAtUtc">The original UTC send timestamp.</param>
    /// <param name="HandledAtUtc">The UTC handler-completion timestamp.</param>
    public sealed record InternalEventBusRoundTripResponse(
        Guid RequestId,
        DateTime SentAtUtc,
        DateTime HandledAtUtc)
        : IntegrationResponse(RequestId);
}
