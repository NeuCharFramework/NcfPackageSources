namespace Senparc.Xncf.DaprClient.Blocks.PubSub.Interface;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent);
}
