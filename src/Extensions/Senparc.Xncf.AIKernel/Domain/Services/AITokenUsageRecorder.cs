/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AITokenUsageRecorder.cs
    文件功能描述：异步 Token 用量记录器

    创建标识：Senparc - 20261011

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    /// <summary>
    /// Token 用量统一记录器。
    /// <para>
    /// 记录入口只更新进程内实时聚合并写入无界队列，不等待数据库操作，
    /// 避免 Token 返回链路被监控持久化拖慢。
    /// </para>
    /// </summary>
    public sealed class AITokenUsageRecorder : IAITokenUsageRecorder, IHostedService, IDisposable
    {
        private readonly AITokenMonitorService _monitor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AITokenUsageRecorder> _logger;
        private readonly Channel<AITokenUsageRecord> _records = Channel.CreateUnbounded<AITokenUsageRecord>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        private Task _workerTask;

        public AITokenUsageRecorder(
            AITokenMonitorService monitor,
            IServiceScopeFactory scopeFactory,
            ILogger<AITokenUsageRecorder> logger)
        {
            _monitor = monitor;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _workerTask = ProcessAsync();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _records.Writer.TryComplete();
            if (_workerTask == null)
            {
                return;
            }

            await _workerTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Record(AITokenUsageRecord record)
        {
            if (record == null)
            {
                return;
            }

            var snapshot = record.Snapshot ?? AITokenUsageSnapshot.Empty;
            snapshot.Normalize();
            var normalized = new AITokenUsageRecord
            {
                ModelAlias = record.ModelAlias,
                ModelId = record.ModelId,
                DeploymentName = record.DeploymentName,
                AiPlatform = record.AiPlatform,
                ConfigModelType = record.ConfigModelType,
                Snapshot = snapshot,
                DurationMs = Math.Max(0, record.DurationMs),
                Success = record.Success,
                TenantId = record.TenantId,
                OccurredAt = record.OccurredAt == default ? DateTime.Now : record.OccurredAt,
                Source = record.Source,
                ErrorNote = record.ErrorNote
            };

            _monitor.Record(
                normalized.ModelAlias,
                normalized.Snapshot,
                normalized.DurationMs,
                normalized.Success,
                normalized.TenantId);

            if (!_records.Writer.TryWrite(normalized))
            {
                _logger.LogError(
                    "Token 用量记录队列已关闭，无法持久化记录。Source={Source}, Model={Model}",
                    normalized.Source,
                    normalized.ModelAlias);
            }
        }

        private async Task ProcessAsync()
        {
            await foreach (var record in _records.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await PersistAsync(record).ConfigureAwait(false);
            }
        }

        private async Task PersistAsync(AITokenUsageRecord record)
        {
            const int MaxAttempts = 3;
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var usageService = scope.ServiceProvider.GetRequiredService<AITokenUsageService>();
                    await usageService.RecordAsync(
                        record.ModelAlias,
                        record.ModelId,
                        record.DeploymentName,
                        record.AiPlatform,
                        record.ConfigModelType,
                        record.Snapshot,
                        record.DurationMs,
                        record.Success,
                        record.Source,
                        record.ErrorNote,
                        record.TenantId,
                        record.OccurredAt).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts)
                {
                    _logger.LogWarning(
                        ex,
                        "Token 用量记录持久化失败，将重试。Attempt={Attempt}, Source={Source}, Model={Model}, TenantId={TenantId}",
                        attempt,
                        record.Source,
                        record.ModelAlias,
                        record.TenantId);
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Token 用量记录持久化失败。Source={Source}, Model={Model}, TenantId={TenantId}",
                        record.Source,
                        record.ModelAlias,
                        record.TenantId);
                }
            }
        }

        public void Dispose()
        {
            _records.Writer.TryComplete();
        }
    }
}
