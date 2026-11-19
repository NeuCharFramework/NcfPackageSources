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
    ///Legacy comment translated to English.
    /// </summary>
    /// <remarks>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </remarks>
    public interface IEventBusRequestClient
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        Task<TResponse> RequestAsync<TResponse>(
            IIntegrationRequest<TResponse> request,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            where TResponse : class, IIntegrationResponse;
    }
}
