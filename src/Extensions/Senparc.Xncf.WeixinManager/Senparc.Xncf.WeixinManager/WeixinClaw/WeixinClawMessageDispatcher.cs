/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WeixinClawMessageDispatcher.cs
    File description: WeixinClawMessageDispatcher.cs implementation and project behavior.


    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Updated module behavior and related support.

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawMessageDispatcher
{
    private readonly IEnumerable<IWeixinClawMessageHandler> _handlers;

    public WeixinClawMessageDispatcher(IEnumerable<IWeixinClawMessageHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task DispatchAsync(
        WeixinClawMessageReceivedContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var handler in _handlers)
        {
            await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
