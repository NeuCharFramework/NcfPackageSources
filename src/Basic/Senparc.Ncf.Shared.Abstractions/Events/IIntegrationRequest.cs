/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IIntegrationRequest.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.3.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    ///Legacy comment translated to English.
    public interface IIntegrationRequest<out TResponse> : IIntegrationEvent
        where TResponse : class, IIntegrationResponse
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Guid RequestId { get; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IIntegrationResponse : IIntegrationEvent
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Guid RequestId { get; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    ///Legacy comment translated to English.
    public abstract record IntegrationRequest<TResponse> : IntegrationEvent, IIntegrationRequest<TResponse>
        where TResponse : class, IIntegrationResponse
    {
        public Guid RequestId { get; init; } = Guid.NewGuid();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    ///Legacy comment translated to English.
    public abstract record IntegrationResponse(Guid RequestId) : IntegrationEvent, IIntegrationResponse;
}
