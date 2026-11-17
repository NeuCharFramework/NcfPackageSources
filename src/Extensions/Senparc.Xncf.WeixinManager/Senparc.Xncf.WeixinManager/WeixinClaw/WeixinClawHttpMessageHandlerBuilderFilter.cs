/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawHttpMessageHandlerBuilderFilter.cs
    File description: WeixinClawHttpMessageHandlerBuilderFilter.cs implementation and project behavior.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw integration with new message handling and UI improvements

----------------------------------------------------------------*/

using Microsoft.Extensions.Http;
using System;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

/// <summary>
/// Removes the host-wide Aspire resilience handler from the iLink client.
/// Long-poll requests have protocol-specific timeouts, and QR/send POSTs must
/// not be transparently retried by a generic handler.
/// </summary>
internal sealed class WeixinClawHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
    private const string ResilienceHandlerTypeName =
        "Microsoft.Extensions.Http.Resilience.ResilienceHandler";

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return builder =>
        {
            next(builder);
            if (!string.Equals(
                    builder.Name,
                    typeof(WeixinClawApi).FullName,
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
