/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IIntegrationEventHandler.cs
    File description: IIntegrationEventHandler related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

namespace Senparc.Xncf.Dapr.Blocks.PubSub.Interface;

public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
}

public interface IIntegrationEventHandler
{
}
