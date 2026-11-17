/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IEventBus.cs
    File description: IEventBus related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

namespace Senparc.Xncf.Dapr.Blocks.PubSub.Interface;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent);
}
