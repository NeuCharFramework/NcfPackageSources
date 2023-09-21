namespace Senparc.Xncf.DaprClient.EventBus.Interface;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent);
}
