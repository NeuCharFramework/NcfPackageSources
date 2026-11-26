/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IAuthorizedIntegrationSyncEvent.cs
    File description: Related implementation.

    Created by: Senparc - 20260726

    Modified by: Senparc - 20260726
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// Identifies an integration synchronization event that must be checked against
    /// an owner and policy before it is applied.
    /// </summary>
    /// <remarks>
    /// Consumers should treat the channel, owner, resource, action, and policy values
    /// as authorization inputs rather than display-only metadata.
    /// </remarks>
    public interface IAuthorizedIntegrationSyncEvent : IIntegrationEvent
    {
        /// <summary>
        /// Gets the external channel or integration source.
        /// </summary>
        string Channel { get; }

        /// <summary>
        /// Gets the owner identifier whose resource is being synchronized.
        /// </summary>
        string OwnerId { get; }

        /// <summary>
        /// Gets the external resource identifier.
        /// </summary>
        string ResourceId { get; }

        /// <summary>
        /// Gets the requested synchronization action.
        /// </summary>
        string Action { get; }

        /// <summary>
        /// Gets the policy key required to authorize the action.
        /// </summary>
        string RequiredPolicy { get; }
    }
}
