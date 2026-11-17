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

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.NeuBell
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed record NeuBellRequestContext(string UserId, string TenantId = null);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed record NeuBellItem(
        string Id,
        string Title,
        string Summary,
        int Count,
        string Severity,
        string DetailUrl,
        DateTimeOffset UpdatedAt);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed record NeuBellSnapshot(
        string ProviderId,
        string ModuleUid,
        string DisplayName,
        string Icon,
        bool DefaultVisible,
        IReadOnlyList<NeuBellItem> Items);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface INeuBellProvider
    {
        string ProviderId { get; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        string ModuleUid => null;

        ValueTask<NeuBellSnapshot> GetSnapshotAsync(
            NeuBellRequestContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public interface INeuBellConsumableProvider
    {
        ///Legacy comment translated to English.
        ValueTask<int> ConsumeItemAsync(
            NeuBellRequestContext context,
            string itemId,
            CancellationToken cancellationToken = default);

        ///Legacy comment translated to English.
        ValueTask<int> ConsumeAllAsync(
            NeuBellRequestContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface INeuBellPublisher
    {
        ValueTask NotifyChangedAsync(string providerId, CancellationToken cancellationToken = default);
    }
}
