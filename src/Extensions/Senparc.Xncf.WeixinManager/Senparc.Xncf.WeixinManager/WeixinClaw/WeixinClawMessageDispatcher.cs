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
