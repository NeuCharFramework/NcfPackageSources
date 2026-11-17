/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RemoteA2AHttpMessageHandlerBuilderFilter.cs
    File description: Related implementation.

    Created by: Senparc - 20260814

    Modified by: Senparc - 20260815
    Change description: v0.15.0-preview20 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.Extensions.Http;
using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    internal sealed class RemoteA2AHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private const string ResilienceHandlerTypeName =
            "Microsoft.Extensions.Http.Resilience.ResilienceHandler";

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
