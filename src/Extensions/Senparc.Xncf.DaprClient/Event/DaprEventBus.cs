using Microsoft.Extensions.Logging;
using Senparc.Xncf.DaprClient.EventBus.Interface;

namespace Senparc.Xncf.DaprClient.EventBus;

public class DaprEventBus : IEventBus
{
    private const string PubSubName = "ncf-pubsub";

    private readonly DaprClient _dapr;
    private readonly ILogger _logger;

    public DaprEventBus(DaprClient dapr, ILogger<DaprEventBus> logger)
    {
        _dapr = dapr;
        _logger = logger;
    }

    public async Task PublishAsync(IntegrationEvent integrationEvent)
    {
        var topicName = integrationEvent.GetType().Name;

        _logger.LogInformation(
            "发布事件 {@Event} 到 {PubsubName}.{TopicName}",
            integrationEvent,
            PubSubName,
            topicName);

        await _dapr.PublishEventAsync(PubSubName, topicName, integrationEvent);
    }
}
