/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IntegrationEvent.cs
    File description: IntegrationEvent related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

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
