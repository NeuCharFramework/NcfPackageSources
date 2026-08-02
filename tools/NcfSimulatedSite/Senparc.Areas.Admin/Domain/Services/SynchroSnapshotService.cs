/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SynchroSnapshotService.cs
    文件功能描述：聚合 XNCF Synchro Provider 并使用 NCF 全局缓存策略缓存快照

    创建标识：Senparc - 20260802
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Shared.Abstractions.Synchro;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class SynchroSnapshotService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
    private readonly IReadOnlyList<ISynchroProvider> _providers;
    private readonly SynchroChangeNotifier _changeNotifier;
    private readonly ILogger<SynchroSnapshotService> _logger;

    public SynchroSnapshotService(
        IEnumerable<ISynchroProvider> providers,
        SynchroChangeNotifier changeNotifier,
        ILogger<SynchroSnapshotService> logger)
    {
        _providers = providers.OrderBy(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase).ToArray();
        _changeNotifier = changeNotifier;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SynchroSnapshot>> GetSnapshotsAsync(
        SynchroRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshots = new List<SynchroSnapshot>(_providers.Count);
        var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();

        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var revision = _changeNotifier.GetRevision(provider.ProviderId);
            var cacheKey = $"NCF:Synchro:{context.TenantId ?? "default"}:{context.UserId}:{provider.ProviderId}:{revision}";

            try
            {
                var snapshot = cache.Get<SynchroSnapshot>(cacheKey);
                if (snapshot == null)
                {
                    snapshot = await provider.GetSnapshotAsync(context, cancellationToken).ConfigureAwait(false);
                    cache.Set(cacheKey, snapshot, CacheDuration);
                }

                if (snapshot != null)
                {
                    snapshots.Add(snapshot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Synchro Provider {ProviderId} 快照获取失败。", provider.ProviderId);
            }
        }

        return snapshots;
    }
}
