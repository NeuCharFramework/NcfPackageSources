/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：InternalEventBusRoundTripRequestHandler.cs
    文件功能描述：模板内部 EventBus 回环请求处理器


    创建标识：Senparc - 20260725

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Template_OrgName.Xncf.Template_XncfName.Application.Events;

namespace Template_OrgName.Xncf.Template_XncfName.Application.EventHandlers
{
    public sealed class InternalEventBusRoundTripRequestHandler
        : IIntegrationEventHandler<InternalEventBusRoundTripRequest>
    {
        private readonly IEventBus _eventBus;

        public InternalEventBusRoundTripRequestHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task Handle(
            InternalEventBusRoundTripRequest @event,
            CancellationToken cancellationToken)
        {
            var response = new InternalEventBusRoundTripResponse(
                @event.RequestId,
                @event.SentAtUtc,
                DateTime.UtcNow);

            await _eventBus.PublishDerivedAsync(response, @event, cancellationToken);
        }
    }
}
