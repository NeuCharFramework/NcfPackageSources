/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfPreviewStateStore.cs
    File description: Related implementation.

    Created by: Senparc - 20260802

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.40.0-preview10 Updated module behavior and related support.

    Modified by: Senparc - 20261028
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.41.7 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Preview
{
    public sealed class XncfPreviewPersistenceSnapshot
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int TenantId { get; init; }

        public string SessionId { get; init; }

        public string ModuleProjectName { get; init; }

        public string SolutionFilePath { get; init; }

        public XncfPreviewStage Stage { get; init; }

        public int ProgressPercent { get; init; }

        public string StatusMessage { get; init; }

        public string ErrorMessage { get; init; }

        public string SourceFingerprint { get; init; }

        public string ModuleAssemblySha256 { get; init; }

        public string RecentOutput { get; init; }

        public DateTimeOffset StartedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }

        public DateTimeOffset? CompletedAt { get; init; }

        public bool HasHost { get; init; }

        public string Url { get; init; }

        public int ProcessId { get; init; }

        public string EnvironmentName { get; init; }

        public string PublishDirectory { get; init; }

        public XncfPreviewHostStatus HostStatus { get; init; }

        public string HostStatusMessage { get; init; }

        public DateTimeOffset? ProcessStartedAt { get; init; }

        public DateTimeOffset? HealthyAt { get; init; }

        public DateTimeOffset? StoppedAt { get; init; }

        public int? ExitCode { get; init; }
    }

    public interface IXncfPreviewStateStore
    {
        Task<IReadOnlyList<XncfPreviewPersistenceSnapshot>> LoadRecentAndInterruptAsync(
            int maxCount,
            DateTimeOffset interruptedAt,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            XncfPreviewPersistenceSnapshot snapshot,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The preview service is a singleton because it owns child processes. This store creates a
    /// short-lived scope for every database operation so scoped repositories never escape a scope.
    /// Multi-tenant deployments additionally restore the snapshot's tenant into each new scope so
    /// tenant-filtered queries and TenantId auto-stamping stay within the owning tenant.
    /// </summary>
    public sealed class XncfPreviewStateStore : IXncfPreviewStateStore
    {
        private const string RestartInterruptedTaskMessage =
            "主站重新启动，之前未完成的预览任务已中断。";
        private const string RestartInterruptedHostMessage =
            "主站重新启动，无法安全重新绑定之前的预览进程。";

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IBackgroundTenantScopeFactory? _tenantScopeFactory;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public XncfPreviewStateStore(
            IServiceScopeFactory serviceScopeFactory,
            IBackgroundTenantScopeFactory? tenantScopeFactory = null)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _tenantScopeFactory = tenantScopeFactory;
        }

        public async Task<IReadOnlyList<XncfPreviewPersistenceSnapshot>> LoadRecentAndInterruptAsync(
            int maxCount,
            DateTimeOffset interruptedAt,
            CancellationToken cancellationToken = default)
        {
            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                //Legacy comment translated to English.
                var tenantScopeFactory = _tenantScopeFactory
                    ?? throw new NcfTenantException("多租户预览持久化需要 IBackgroundTenantScopeFactory。");
                await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var snapshots = new List<XncfPreviewPersistenceSnapshot>();
                    await tenantScopeFactory.ForEachEnabledTenantAsync(
                        async (tenantServiceProvider, tenantCancellationToken) =>
                        {
                            var tenantInfo = tenantServiceProvider.GetRequiredService<RequestTenantInfo>();
                            var tenantSnapshots = await LoadAndInterruptCoreAsync(
                                    tenantServiceProvider,
                                    tenantInfo.Id,
                                    maxCount,
                                    interruptedAt,
                                    tenantCancellationToken)
                                .ConfigureAwait(false);
                            snapshots.AddRange(tenantSnapshots);
                        },
                        cancellationToken).ConfigureAwait(false);
                    //Legacy comment translated to English.
                    await using var platformScope = _serviceScopeFactory.CreateAsyncScope();
                    ApplyTenantContext(platformScope, 0);
                    snapshots.AddRange(await LoadAndInterruptCoreAsync(
                        platformScope.ServiceProvider,
                        tenantId: 0,
                        maxCount,
                        interruptedAt,
                        cancellationToken).ConfigureAwait(false));
                    return snapshots.ToArray();
                }
                finally
                {
                    _writeLock.Release();
                }
            }

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                return await LoadAndInterruptCoreAsync(
                    scope.ServiceProvider,
                    tenantId: 0,
                    maxCount,
                    interruptedAt,
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private static async Task<IReadOnlyList<XncfPreviewPersistenceSnapshot>> LoadAndInterruptCoreAsync(
            IServiceProvider tenantServiceProvider,
            int tenantId,
            int maxCount,
            DateTimeOffset interruptedAt,
            CancellationToken cancellationToken)
        {
            var taskRepository = tenantServiceProvider.GetRequiredService<IRepositoryBase<XncfPreviewTask>>();
            var hostRepository = tenantServiceProvider.GetRequiredService<IRepositoryBase<XncfPreviewHost>>();

            cancellationToken.ThrowIfCancellationRequested();
            var unfinishedTasks = await taskRepository.GetObjectListAsync(
                    task => !task.Flag && task.Stage <= XncfPreviewStage.Stopping,
                    task => task.Id,
                    OrderingType.Ascending,
                    0,
                    0)
                .ConfigureAwait(false);
            foreach (var task in unfinishedTasks)
            {
                task.MarkInterrupted(interruptedAt, RestartInterruptedTaskMessage);
            }
            if (unfinishedTasks.Count > 0)
            {
                await taskRepository.SaveObjectListAsync(unfinishedTasks).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var unfinishedHosts = await hostRepository.GetObjectListAsync(
                    host => !host.Flag
                            && host.Status >= XncfPreviewHostStatus.Starting
                            && host.Status <= XncfPreviewHostStatus.Stopping,
                    host => host.Id,
                    OrderingType.Ascending,
                    0,
                    0)
                .ConfigureAwait(false);
            foreach (var host in unfinishedHosts)
            {
                host.MarkInterrupted(interruptedAt, RestartInterruptedHostMessage);
            }
            if (unfinishedHosts.Count > 0)
            {
                await hostRepository.SaveObjectListAsync(unfinishedHosts).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var recentTasks = await taskRepository.GetObjectListAsync(
                    task => !task.Flag,
                    task => task.StartedAtUtc,
                    OrderingType.Descending,
                    1,
                    maxCount)
                .ConfigureAwait(false);
            if (recentTasks.Count == 0)
            {
                return Array.Empty<XncfPreviewPersistenceSnapshot>();
            }

            var sessionIds = recentTasks.Select(task => task.SessionId).ToArray();
            var hosts = await hostRepository.GetObjectListAsync(
                    host => !host.Flag && Enumerable.Contains(sessionIds, host.SessionId),
                    host => host.UpdatedAtUtc,
                    OrderingType.Descending,
                    0,
                    0)
                .ConfigureAwait(false);
            var hostsBySessionId = hosts
                .GroupBy(host => host.SessionId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            return recentTasks
                .Select(task => ToSnapshot(
                    task,
                    hostsBySessionId.TryGetValue(task.SessionId, out var host) ? host : null,
                    tenantId))
                .ToArray();
        }

        public async Task SaveAsync(
            XncfPreviewPersistenceSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            if (string.IsNullOrWhiteSpace(snapshot.SessionId))
            {
                throw new ArgumentException("预览持久化快照缺少 SessionId。", nameof(snapshot));
            }

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                ApplyTenantContext(scope, snapshot.TenantId);
                var taskRepository = scope.ServiceProvider.GetRequiredService<IRepositoryBase<XncfPreviewTask>>();
                var task = await taskRepository
                    .GetFirstOrDefaultObjectAsync(item => !item.Flag && item.SessionId == snapshot.SessionId)
                    .ConfigureAwait(false);
                task ??= new XncfPreviewTask(snapshot);
                task.Apply(snapshot);
                await taskRepository.SaveAsync(task).ConfigureAwait(false);

                if (snapshot.HasHost)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var hostRepository = scope.ServiceProvider.GetRequiredService<IRepositoryBase<XncfPreviewHost>>();
                    var host = await hostRepository
                        .GetFirstOrDefaultObjectAsync(item => !item.Flag && item.SessionId == snapshot.SessionId)
                        .ConfigureAwait(false);
                    host ??= new XncfPreviewHost(snapshot);
                    host.Apply(snapshot);
                    await hostRepository.SaveAsync(host).ConfigureAwait(false);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private static XncfPreviewPersistenceSnapshot ToSnapshot(
            XncfPreviewTask task,
            XncfPreviewHost host,
            int tenantId)
        {
            return new XncfPreviewPersistenceSnapshot
            {
                TenantId = tenantId,
                SessionId = task.SessionId,
                ModuleProjectName = task.ModuleProjectName,
                SolutionFilePath = task.SolutionFilePath,
                Stage = task.Stage,
                ProgressPercent = task.ProgressPercent,
                StatusMessage = task.StatusMessage,
                ErrorMessage = task.ErrorMessage,
                SourceFingerprint = task.SourceFingerprint,
                ModuleAssemblySha256 = task.ModuleAssemblySha256,
                RecentOutput = task.RecentOutput,
                StartedAt = ToUtcOffset(task.StartedAtUtc),
                UpdatedAt = ToUtcOffset(task.UpdatedAtUtc),
                CompletedAt = ToUtcOffset(task.CompletedAtUtc),
                HasHost = host != null,
                Url = host?.Url,
                ProcessId = host?.ProcessId ?? 0,
                EnvironmentName = host?.EnvironmentName,
                PublishDirectory = host?.PublishDirectory,
                HostStatus = host?.Status ?? XncfPreviewHostStatus.NotCreated,
                HostStatusMessage = host?.StatusMessage,
                ProcessStartedAt = ToUtcOffset(host?.ProcessStartedAtUtc),
                HealthyAt = ToUtcOffset(host?.HealthyAtUtc),
                StoppedAt = ToUtcOffset(host?.StoppedAtUtc),
                ExitCode = host?.ExitCode
            };
        }

        private static DateTimeOffset ToUtcOffset(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }

        private static DateTimeOffset? ToUtcOffset(DateTime? value)
        {
            return value.HasValue ? ToUtcOffset(value.Value) : null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        private static void ApplyTenantContext(IServiceScope scope, int tenantId)
        {
            if (!SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                return;
            }

            var requestTenantInfo = scope.ServiceProvider.GetRequiredService<RequestTenantInfo>();
            requestTenantInfo.Id = tenantId;
            requestTenantInfo.TryMatch(true);
        }
    }
}
