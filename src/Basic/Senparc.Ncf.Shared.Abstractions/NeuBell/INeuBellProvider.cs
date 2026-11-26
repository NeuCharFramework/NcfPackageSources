/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: INeuBellProvider.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.NeuBell
{
    /// <summary>
    /// Identifies the administrator and tenant for a NeuBell snapshot or consumption operation.
    /// </summary>
    /// <param name="UserId">The authenticated user identifier.</param>
    /// <param name="TenantId">The optional tenant identifier.</param>
    public sealed record NeuBellRequestContext(string UserId, string TenantId = null);

    /// <summary>
    /// Represents one notification item shown in the NeuBell surface.
    /// </summary>
    /// <param name="Id">The provider-local item identifier.</param>
    /// <param name="Title">The notification title.</param>
    /// <param name="Summary">The notification summary.</param>
    /// <param name="Count">The number represented by the item.</param>
    /// <param name="Severity">The display severity.</param>
    /// <param name="DetailUrl">The optional detail URL.</param>
    /// <param name="UpdatedAt">The UTC timestamp of the latest update.</param>
    public sealed record NeuBellItem(
        string Id,
        string Title,
        string Summary,
        int Count,
        string Severity,
        string DetailUrl,
        DateTimeOffset UpdatedAt);

    /// <summary>
    /// Contains the notification items exposed by one NeuBell provider.
    /// </summary>
    /// <param name="ProviderId">The stable provider identifier.</param>
    /// <param name="ModuleUid">The owning module UID.</param>
    /// <param name="DisplayName">The provider display name.</param>
    /// <param name="Icon">The provider icon identifier.</param>
    /// <param name="DefaultVisible">Whether the provider is visible by default.</param>
    /// <param name="Items">The current notification items.</param>
    public sealed record NeuBellSnapshot(
        string ProviderId,
        string ModuleUid,
        string DisplayName,
        string Icon,
        bool DefaultVisible,
        IReadOnlyList<NeuBellItem> Items);

    /// <summary>
    /// Provides notification snapshots for the NeuBell aggregation surface.
    /// </summary>
    public interface INeuBellProvider
    {
        /// <summary>
        /// Gets the stable provider identifier.
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Gets the owning module UID, when the provider is associated with a module.
        /// </summary>
        string ModuleUid => null;

        /// <summary>
        /// Builds the current notification snapshot for a user and tenant.
        /// </summary>
        /// <param name="context">The user and tenant scope.</param>
        /// <param name="cancellationToken">Token used to cancel snapshot creation.</param>
        /// <returns>The provider notification snapshot.</returns>
        ValueTask<NeuBellSnapshot> GetSnapshotAsync(
            NeuBellRequestContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Defines optional consumption operations for NeuBell notification providers.
    /// </summary>
    public interface INeuBellConsumableProvider
    {
        /// <summary>
        /// Consumes one notification item for the requesting user.
        /// </summary>
        /// <param name="context">The user and tenant scope.</param>
        /// <param name="itemId">The provider item identifier.</param>
        /// <param name="cancellationToken">Token used to cancel consumption.</param>
        /// <returns>The number of consumed items.</returns>
        ValueTask<int> ConsumeItemAsync(
            NeuBellRequestContext context,
            string itemId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Consumes all eligible notification items for the requesting user.
        /// </summary>
        /// <param name="context">The user and tenant scope.</param>
        /// <param name="cancellationToken">Token used to cancel consumption.</param>
        /// <returns>The number of consumed items.</returns>
        ValueTask<int> ConsumeAllAsync(
            NeuBellRequestContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Notifies the NeuBell aggregation layer that a provider snapshot changed.
    /// </summary>
    public interface INeuBellPublisher
    {
        /// <summary>
        /// Publishes a provider-change notification.
        /// </summary>
        /// <param name="providerId">The provider whose snapshot changed.</param>
        /// <param name="cancellationToken">Token used to cancel notification.</param>
        ValueTask NotifyChangedAsync(string providerId, CancellationToken cancellationToken = default);
    }
}
