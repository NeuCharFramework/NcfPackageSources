/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentJobStateStore.cs
    文件功能描述：XNCF 隔离开发任务数据库状态存储

    创建标识：Senparc - 20260814

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
        public string JobId { get; init; }
        public XncfDevelopmentJobMode Mode { get; init; }
        public string ModuleProjectName { get; init; }
        public string TargetSolutionFilePath { get; init; }
        public string WorkspaceRootPath { get; init; }
        public string WorkspaceSolutionFilePath { get; init; }
        public string Requirement { get; init; }
        public XncfDevelopmentJobStage Stage { get; init; }
        public string StatusMessage { get; init; }
        public string ErrorMessage { get; init; }
        public string TargetModuleFingerprint { get; init; }
        public string WorkspaceModuleFingerprint { get; init; }
        public string ValidationSummary { get; init; }
        public string DiffSummary { get; init; }
        public string PreviewSessionId { get; init; }
        public string SandboxSessionId { get; init; }
        public string PreviewUrl { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
        public DateTimeOffset? MergeRequestedAt { get; init; }
        public DateTimeOffset? AppliedAt { get; init; }
    }

    public interface IXncfDevelopmentJobStateStore
    {
        Task SaveAsync(XncfDevelopmentJobSnapshot snapshot, CancellationToken cancellationToken = default);
        Task<XncfDevelopmentJobSnapshot> GetAsync(string jobId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<XncfDevelopmentJobSnapshot>> GetRecentAsync(int maxCount, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Creates a scope per operation because the development workflow is singleton and owns
    /// per-job locks. Repository instances are scoped and must never be retained by it.
    /// </summary>
    public sealed class XncfDevelopmentJobStateStore : IXncfDevelopmentJobStateStore
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

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
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task<XncfDevelopmentJobSnapshot> GetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return null;
            }

            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepositoryBase<XncfDevelopmentJob>>();
            var entity = await repository.GetFirstOrDefaultObjectAsync(
                    job => !job.Flag && job.JobId == jobId.Trim())
                .ConfigureAwait(false);
            return entity == null ? null : ToSnapshot(entity);
        }

        public async Task<IReadOnlyList<XncfDevelopmentJobSnapshot>> GetRecentAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
        {
            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepositoryBase<XncfDevelopmentJob>>();
            var jobs = await repository.GetObjectListAsync(
                    job => !job.Flag,
                    job => job.UpdatedAtUtc,
                    OrderingType.Descending,
                    1,
                    maxCount)
                .ConfigureAwait(false);
            return jobs.Select(ToSnapshot).ToArray();
        }

        private static XncfDevelopmentJobSnapshot ToSnapshot(XncfDevelopmentJob job)
        {
            return new XncfDevelopmentJobSnapshot
            {
                JobId = job.JobId,
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
