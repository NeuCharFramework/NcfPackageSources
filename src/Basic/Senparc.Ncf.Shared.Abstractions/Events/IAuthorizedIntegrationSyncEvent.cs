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
    ///Legacy comment translated to English.
    /// </summary>
    /// <remarks>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </remarks>
    public interface IAuthorizedIntegrationSyncEvent : IIntegrationEvent
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string Channel { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string OwnerId { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string ResourceId { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string Action { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string RequiredPolicy { get; }
    }
}
