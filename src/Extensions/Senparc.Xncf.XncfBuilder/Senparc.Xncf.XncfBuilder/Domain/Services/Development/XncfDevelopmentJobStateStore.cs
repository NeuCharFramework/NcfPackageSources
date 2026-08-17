/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentJobStateStore.cs
    文件功能描述：XNCF 隔离开发任务数据库状态存储

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Enums;
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
        Task SaveAsync(XncfDevelopmentJobSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<XncfDevelopmentJobSnapshot> GetAsync(string jobId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<XncfDevelopmentJobSnapshot>> GetRecentAsync(int maxCount, CancellationToken cancellationToken = default);
        XncfDevelopmentPersistenceInfo GetPersistenceStatus();
    }

    /// <summary>
    /// Creates a scope per operation because the development workflow is singleton and owns
    /// per-job locks. Repository instances are scoped and must never be retained by it.
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

        public async Task<XncfDevelopmentJobSnapshot> GetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return null;
            }

            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
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

        public async Task<IReadOnlyList<XncfDevelopmentJobSnapshot>> GetRecentAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
        {
            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            try
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
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

        private static XncfDevelopmentJobSnapshot ToSnapshot(XncfDevelopmentJob job)
        {
            return new XncfDevelopmentJobSnapshot
            {
                JobId = job.JobId,
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

        private static DateTimeOffset ToUtcOffset(DateTime value) =>
            new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

        private static DateTimeOffset? ToUtcOffset(DateTime? value) =>
            value.HasValue ? ToUtcOffset(value.Value) : null;
    }
}
