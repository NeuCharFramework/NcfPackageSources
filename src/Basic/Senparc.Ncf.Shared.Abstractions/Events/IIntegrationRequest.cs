/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IIntegrationRequest.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.7.1.2-preview3 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// Defines an event-bus request that expects a strongly typed integration response.
    /// </summary>
    /// <typeparam name="TResponse">The response type associated with the request.</typeparam>
    public interface IIntegrationRequest<out TResponse> : IIntegrationEvent
        where TResponse : class, IIntegrationResponse
    {
        /// <summary>
        /// Gets the identifier used to correlate the request with its response.
        /// </summary>
        Guid RequestId { get; }
    }

    /// <summary>
    /// Defines an integration response correlated with one request.
    /// </summary>
    public interface IIntegrationResponse : IIntegrationEvent
    {
        /// <summary>
        /// Gets the request identifier that produced this response.
        /// </summary>
        Guid RequestId { get; }
    }

    /// <summary>
    /// Base implementation for request events with generated correlation identity.
    /// </summary>
    /// <typeparam name="TResponse">The response type associated with the request.</typeparam>
    public abstract record IntegrationRequest<TResponse> : IntegrationEvent, IIntegrationRequest<TResponse>
        where TResponse : class, IIntegrationResponse
    {
        /// <summary>
        /// Gets or initializes the request correlation identifier.
        /// </summary>
        public Guid RequestId { get; init; } = Guid.NewGuid();
    }

    /// <summary>
    /// Base implementation for responses carrying the originating request identifier.
    /// </summary>
    /// <param name="RequestId">The request correlation identifier.</param>
    public abstract record IntegrationResponse(Guid RequestId) : IntegrationEvent, IIntegrationResponse;
}
