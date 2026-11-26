/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellSnapshotService.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class NeuBellSnapshotService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(5);
    private readonly NeuBellProviderCatalog _providerCatalog;
    private readonly NeuBellChangeNotifier _changeNotifier;
    private readonly ILogger<NeuBellSnapshotService> _logger;

    public NeuBellSnapshotService(
        NeuBellProviderCatalog providerCatalog,
        NeuBellChangeNotifier changeNotifier,
        ILogger<NeuBellSnapshotService> logger)
    {
        _providerCatalog = providerCatalog;
        _changeNotifier = changeNotifier;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether at least one NeuBell provider belongs to an open XNCF module.
    /// </summary>
    /// <param name="cancellationToken">Token used to query module availability.</param>
    /// <returns><see langword="true"/> when one or more providers are available.</returns>
    public async Task<bool> HasAvailableProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var providers = await _providerCatalog.GetAvailableProvidersAsync(cancellationToken)
            .ConfigureAwait(false);
        return providers.Count > 0;
    }

    /// <summary>
    /// Loads provider snapshots concurrently, using revision-aware short-lived caching
    /// and ignoring providers that fail or return mismatched module metadata.
    /// </summary>
    /// <param name="context">The tenant/user context passed to providers.</param>
    /// <param name="cancellationToken">Token used to cancel snapshot loading.</param>
    /// <returns>The successfully retrieved provider snapshots.</returns>
    public async Task<IReadOnlyList<NeuBellSnapshot>> GetSnapshotsAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var providers = await _providerCatalog.GetAvailableProvidersAsync(cancellationToken)
            .ConfigureAwait(false);
        if (providers.Count == 0)
        {
            return Array.Empty<NeuBellSnapshot>();
        }

        var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
        //Legacy comment translated to English.
        var snapshotTasks = providers
            .Select(provider => GetSnapshotAsync(provider, context, cache, cancellationToken))
            .ToArray();
        var snapshots = await Task.WhenAll(snapshotTasks).ConfigureAwait(false);
        return snapshots.Where(snapshot => snapshot != null).ToArray();
    }

    /// <summary>
    /// Reads one provider snapshot from revision-keyed cache or the provider with
    /// a bounded timeout, validating the returned module UID.
    /// </summary>
    /// <param name="provider">The provider to query.</param>
    /// <param name="context">The tenant/user request context.</param>
    /// <param name="cache">The object cache.</param>
    /// <param name="cancellationToken">Token used to cancel the provider call.</param>
    /// <returns>The valid snapshot, or <see langword="null"/> when unavailable.</returns>
    private async Task<NeuBellSnapshot> GetSnapshotAsync(
        INeuBellProvider provider,
        NeuBellRequestContext context,
        IBaseObjectCacheStrategy cache,
        CancellationToken cancellationToken)
    {
        var revision = _changeNotifier.GetRevision(provider.ProviderId);
        var cacheKey = $"NCF:NeuBell:{context.TenantId ?? "default"}:{context.UserId}:{provider.ProviderId}:{revision}";

        try
        {
            var snapshot = cache.Get<NeuBellSnapshot>(cacheKey);
            if (snapshot == null)
            {
                //Legacy comment translated to English.
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ProviderTimeout);
                snapshot = await provider.GetSnapshotAsync(context, timeout.Token).ConfigureAwait(false);
                if (snapshot != null)
                {
                    cache.Set(cacheKey, snapshot, CacheDuration);
                }
            }

            if (snapshot != null
                && !string.Equals(snapshot.ModuleUid, provider.ModuleUid, StringComparison.OrdinalIgnoreCase))
            {
                //Legacy comment translated to English.
                _logger.LogWarning(
                    "纽铃 Provider {ProviderId} 返回了不匹配的 ModuleUid {SnapshotModuleUid}，已忽略。",
                    provider.ProviderId,
                    snapshot.ModuleUid);
                return null;
            }

            return snapshot;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "纽铃 Provider {ProviderId} 快照获取失败。", provider.ProviderId);
            return null;
        }
    }
}
