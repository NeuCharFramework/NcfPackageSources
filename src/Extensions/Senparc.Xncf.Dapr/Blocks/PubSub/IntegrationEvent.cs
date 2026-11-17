/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IntegrationEvent.cs
    File description: IntegrationEvent related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

namespace Senparc.Xncf.Dapr.Blocks.PubSub;
public record IntegrationEvent
{
    public Guid Id { get; }

    public DateTime CreationDate { get; }

    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }
}
