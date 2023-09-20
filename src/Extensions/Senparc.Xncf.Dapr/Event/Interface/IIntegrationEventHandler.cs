using Senparc.Xncf.DaprClient.EventBus;

namespace Senparc.Xncf.DaprClient.EventBus.Interface;

public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
}

public interface IIntegrationEventHandler
{
}
