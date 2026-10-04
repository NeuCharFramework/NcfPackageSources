/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawMessageDispatcher.cs
    文件功能描述：WeixinClawMessageDispatcher.cs implementation and project behavior.


    创建标识：Senparc - 20260920

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

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
