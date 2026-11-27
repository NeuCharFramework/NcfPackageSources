/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RemoteA2AHttpMessageHandlerBuilderFilter.cs
    File description: Related implementation.

    Created by: Senparc - 20260814

    Modified by: Senparc - 20260815
    Change description: v0.15.0-preview20 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Microsoft.Extensions.Http;
using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// Removes the global resilience handler from the dedicated Remote A2A HTTP client,
    /// allowing the remote-agent factory to apply its own timeout and retry policy.
    /// </summary>
    internal sealed class RemoteA2AHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private const string ResilienceHandlerTypeName =
            "Microsoft.Extensions.Http.Resilience.ResilienceHandler";

        /// <summary>
        /// Returns a handler-builder callback that removes the global resilience handler
        /// from the dedicated Remote A2A client.
        /// </summary>
        /// <param name="next">The next handler-builder callback in the filter chain.</param>
        /// <returns>The filtered handler-builder callback.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="next"/> is null.</exception>
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            ArgumentNullException.ThrowIfNull(next);

            return builder =>
            {
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                next(builder);

                if (!string.Equals(
                        builder.Name,
                        RemoteA2AAgentFactory.HttpClientName,
                        StringComparison.Ordinal))
                {
                    return;
                }

                for (var index = builder.AdditionalHandlers.Count - 1; index >= 0; index--)
                {
                    if (string.Equals(
                            builder.AdditionalHandlers[index].GetType().FullName,
                            ResilienceHandlerTypeName,
                            StringComparison.Ordinal))
                    {
                        builder.AdditionalHandlers.RemoveAt(index);
                    }
                }
            };
        }
    }
}
