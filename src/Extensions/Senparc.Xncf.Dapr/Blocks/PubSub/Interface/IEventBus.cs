namespace Senparc.Xncf.Dapr.Blocks.PubSub.Interface;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent);
}
