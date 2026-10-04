/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawHttpMessageHandlerBuilderFilter.cs
    文件功能描述：WeixinClawHttpMessageHandlerBuilderFilter.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

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
