/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellSnapshotService.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

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

    public async Task<bool> HasAvailableProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var providers = await _providerCatalog.GetAvailableProvidersAsync(cancellationToken)
            .ConfigureAwait(false);
        return providers.Count > 0;
    }

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
