/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfDevelopmentJobStateStore.cs
    File description: Related implementation.

    Created by: Senparc - 20260814

    Modified by: Senparc - 20260815
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.41.0 Updated module behavior and related support.

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
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Development
{
    public sealed class XncfDevelopmentJobSnapshot
    {
        public string JobId { get; set; }
        /// <summary>Gets or sets the tenant owning the development job.</summary>
        public int TenantId { get; set; }
        public int OwnerAdminUserId { get; set; }
        public XncfDevelopmentJobMode Mode { get; set; }
        public string ModuleProjectName { get; set; }
        public string TargetSolutionFilePath { get; set; }
        public string WorkspaceRootPath { get; set; }
        public string WorkspaceSolutionFilePath { get; set; }
        public string Requirement { get; set; }
        public XncfDevelopmentJobStage Stage { get; set; }
        public string StatusMessage { get; set; }
        public string ErrorMessage { get; set; }
        public string TargetModuleFingerprint { get; set; }
        public string WorkspaceModuleFingerprint { get; set; }
        public string ValidationSummary { get; set; }
        public string DiffSummary { get; set; }
        public string PreviewSessionId { get; set; }
        public string SandboxSessionId { get; set; }
        public string PreviewUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset? MergeRequestedAt { get; set; }
        public DateTimeOffset? AppliedAt { get; set; }
    }

    public interface IXncfDevelopmentJobStateStore
    {
        /// <summary>
        /// Persists a development-job snapshot for its owning tenant.
        /// </summary>
        /// <param name="snapshot">The job state to persist.</param>
        /// <param name="cancellationToken">Token used to cancel persistence.</param>
        Task SaveAsync(XncfDevelopmentJobSnapshot snapshot, CancellationToken cancellationToken = default);
        /// <summary>
        /// Loads one non-deleted development job for a tenant.
        /// </summary>
        /// <param name="jobId">The development job identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">Token used to cancel lookup.</param>
        /// <returns>The snapshot, or <see langword="null"/> when absent.</returns>
        Task<XncfDevelopmentJobSnapshot> GetAsync(string jobId, int tenantId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Loads recent non-deleted development jobs for a tenant.
        /// </summary>
        /// <param name="maxCount">The maximum number of snapshots to return.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">Token used to cancel lookup.</param>
        /// <returns>The recent snapshots ordered by update time.</returns>
        Task<IReadOnlyList<XncfDevelopmentJobSnapshot>> GetRecentAsync(int maxCount, int tenantId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets the current persistence availability and diagnostic status.
        /// </summary>
        /// <returns>The persistence status snapshot.</returns>
        XncfDevelopmentPersistenceInfo GetPersistenceStatus();
    }

    /// <summary>
    /// Creates a scope per operation because the development workflow is singleton and owns
    /// per-job locks. Repository instances are scoped and must never be retained by it.
    /// Multi-tenant deployments additionally restore the job's tenant into each new scope so
    /// tenant-filtered queries and TenantId auto-stamping stay within the owning tenant.
    /// </summary>
    public sealed class XncfDevelopmentJobStateStore : IXncfDevelopmentJobStateStore
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly object _persistenceStatusLock = new();
        private bool _persistenceAvailable = true;
        private string _persistenceStatusMessage = "隔离开发任务数据库持久化可用。";
        private string _persistenceErrorMessage;
        private DateTimeOffset _persistenceStatusUpdatedAt = DateTimeOffset.UtcNow;
        private DateTimeOffset? _retryAfter;

        public XncfDevelopmentJobStateStore(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <inheritdoc />
        public async Task SaveAsync(XncfDevelopmentJobSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            if (string.IsNullOrWhiteSpace(snapshot.JobId))
            {
                throw new ArgumentException("开发任务缺少 JobId。", nameof(snapshot));
            }

            try
            {
                await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await using var scope = _serviceScopeFactory.CreateAsyncScope();
                    ApplyTenantContext(scope, snapshot.TenantId);
                    var repository = scope.ServiceProvider.GetRequiredService<IRepositoryBase<XncfDevelopmentJob>>();
                    var entity = await repository.GetFirstOrDefaultObjectAsync(
                            job => !job.Flag && job.JobId == snapshot.JobId)
                        .ConfigureAwait(false);
                    entity ??= new XncfDevelopmentJob(snapshot);
                    entity.Apply(snapshot);
                    await repository.SaveAsync(entity).ConfigureAwait(false);
                    MarkPersistenceAvailable();
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (Exception ex)
            {
                MarkPersistenceUnavailable(ex);
                throw;
            }
        }

        /// <summary>
        /// Loads one non-deleted development job under the supplied tenant scope.
        /// </summary>
        /// <param name="jobId">The development job identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">Token used to cancel database access.</param>
        /// <returns>The snapshot, or <see langword="null"/> when absent.</returns>
        public async Task<XncfDevelopmentJobSnapshot> GetAsync(string jobId, int tenantId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return null;
            }

            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                ApplyTenantContext(scope, tenantId);
                var repository = scope.ServiceProvider.GetRequiredService<IRepositoryBase<XncfDevelopmentJob>>();
                var entity = await repository.GetFirstOrDefaultObjectAsync(
                        job => !job.Flag && job.JobId == jobId.Trim())
                    .ConfigureAwait(false);
                MarkPersistenceAvailable();
                return entity == null ? null : ToSnapshot(entity);
            }
            catch (Exception ex)
            {
                MarkPersistenceUnavailable(ex);
                throw;
            }
        }

        /// <summary>
        /// Loads recent non-deleted development jobs ordered by update time.
        /// </summary>
        /// <param name="maxCount">The maximum number of jobs to return.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">Token used to cancel database access.</param>
        /// <returns>The recent job snapshots.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxCount"/> is not positive.</exception>
        public async Task<IReadOnlyList<XncfDevelopmentJobSnapshot>> GetRecentAsync(
            int maxCount,
            int tenantId,
            CancellationToken cancellationToken = default)
        {
            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                ApplyTenantContext(scope, tenantId);
                var repository = scope.ServiceProvider.GetRequiredService<IRepositoryBase<XncfDevelopmentJob>>();
                var jobs = await repository.GetObjectListAsync(
                        job => !job.Flag,
                        job => job.UpdatedAtUtc,
                        OrderingType.Descending,
                        1,
                        maxCount)
                    .ConfigureAwait(false);
                MarkPersistenceAvailable();
                return jobs.Select(ToSnapshot).ToArray();
            }
            catch (Exception ex)
            {
                MarkPersistenceUnavailable(ex);
                throw;
            }
        }

        /// <summary>
        /// Returns the last known state of development-job persistence.
        /// </summary>
        /// <returns>A thread-safe persistence status snapshot.</returns>
        public XncfDevelopmentPersistenceInfo GetPersistenceStatus()
        {
            lock (_persistenceStatusLock)
            {
                return new XncfDevelopmentPersistenceInfo
                {
                    IsAvailable = _persistenceAvailable,
                    StatusMessage = _persistenceStatusMessage,
                    ErrorMessage = _persistenceErrorMessage,
                    UpdatedAt = _persistenceStatusUpdatedAt,
                    RetryAfter = _retryAfter
                };
            }
        }

        /// <summary>
        /// Records a successful persistence operation and clears retry state.
        /// </summary>
        private void MarkPersistenceAvailable()
        {
            lock (_persistenceStatusLock)
            {
                _persistenceAvailable = true;
                _persistenceStatusMessage = "隔离开发任务数据库持久化可用。";
                _persistenceErrorMessage = null;
                _persistenceStatusUpdatedAt = DateTimeOffset.UtcNow;
                _retryAfter = null;
            }
        }

        /// <summary>
        /// Records a persistence failure and schedules a bounded retry time.
        /// </summary>
        /// <param name="exception">The persistence exception.</param>
        private void MarkPersistenceUnavailable(Exception exception)
        {
            lock (_persistenceStatusLock)
            {
                _persistenceAvailable = false;
                _persistenceStatusMessage = "隔离开发任务表不可用；已暂缓状态查询，等待数据库迁移完成后重试。";
                _persistenceErrorMessage = exception.Message;
                _persistenceStatusUpdatedAt = DateTimeOffset.UtcNow;
                _retryAfter = _persistenceStatusUpdatedAt.AddSeconds(30);
            }
        }

        /// <summary>
        /// Converts a persisted development job entity to its immutable snapshot contract.
        /// </summary>
        /// <param name="job">The persisted job entity.</param>
        /// <returns>The snapshot contract.</returns>
        private static XncfDevelopmentJobSnapshot ToSnapshot(XncfDevelopmentJob job)
        {
            return new XncfDevelopmentJobSnapshot
            {
                JobId = job.JobId,
                TenantId = job.TenantId,
                OwnerAdminUserId = job.OwnerAdminUserId,
                Mode = job.Mode,
                ModuleProjectName = job.ModuleProjectName,
                TargetSolutionFilePath = job.TargetSolutionFilePath,
                WorkspaceRootPath = job.WorkspaceRootPath,
                WorkspaceSolutionFilePath = job.WorkspaceSolutionFilePath,
                Requirement = job.Requirement,
                Stage = job.Stage,
                StatusMessage = job.StatusMessage,
                ErrorMessage = job.ErrorMessage,
                TargetModuleFingerprint = job.TargetModuleFingerprint,
                WorkspaceModuleFingerprint = job.WorkspaceModuleFingerprint,
                ValidationSummary = job.ValidationSummary,
                DiffSummary = job.DiffSummary,
                PreviewSessionId = job.PreviewSessionId,
                SandboxSessionId = job.SandboxSessionId,
                PreviewUrl = job.PreviewUrl,
                CreatedAt = ToUtcOffset(job.CreatedAtUtc),
                UpdatedAt = ToUtcOffset(job.UpdatedAtUtc),
                CompletedAt = ToUtcOffset(job.CompletedAtUtc),
                MergeRequestedAt = ToUtcOffset(job.MergeRequestedAtUtc),
                AppliedAt = ToUtcOffset(job.AppliedAtUtc)
            };
        }

        /// <summary>
        /// Converts a stored DateTime to a UTC DateTimeOffset.
        /// </summary>
        /// <param name="value">The stored timestamp.</param>
        /// <returns>The UTC offset timestamp.</returns>
        private static DateTimeOffset ToUtcOffset(DateTime value) =>
            new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

        /// <summary>
        /// Converts an optional stored DateTime to an optional UTC DateTimeOffset.
        /// </summary>
        /// <param name="value">The optional stored timestamp.</param>
        /// <returns>The UTC offset timestamp or <see langword="null"/>.</returns>
        private static DateTimeOffset? ToUtcOffset(DateTime? value) =>
            value.HasValue ? ToUtcOffset(value.Value) : null;

        /// <summary>
        /// Restores the tenant context in a persistence scope when multi-tenancy is enabled.
        /// </summary>
        /// <param name="scope">The tenant-scoped service scope.</param>
        /// <param name="tenantId">The tenant identifier to match.</param>
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
