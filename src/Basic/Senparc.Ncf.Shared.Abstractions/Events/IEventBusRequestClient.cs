/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IEventBusRequestClient.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// Sends request/response messages through the integration event bus.
    /// </summary>
    /// <remarks>
    /// Implementations correlate the response by <see cref="IIntegrationRequest{TResponse}.RequestId"/>
    /// and stop waiting when the timeout or cancellation token is reached.
    /// </remarks>
    public interface IEventBusRequestClient
    {
        /// <summary>
        /// Sends a request and waits for its correlated response.
        /// </summary>
        /// <typeparam name="TResponse">The expected integration response type.</typeparam>
        /// <param name="request">The request event to publish.</param>
        /// <param name="timeout">The maximum duration to wait for the response.</param>
        /// <param name="cancellationToken">Token used to cancel publishing or waiting.</param>
        /// <returns>The correlated response.</returns>
        /// <exception cref="TimeoutException">Thrown when no response arrives before <paramref name="timeout"/>.</exception>
        Task<TResponse> RequestAsync<TResponse>(
            IIntegrationRequest<TResponse> request,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            where TResponse : class, IIntegrationResponse;
    }
}
