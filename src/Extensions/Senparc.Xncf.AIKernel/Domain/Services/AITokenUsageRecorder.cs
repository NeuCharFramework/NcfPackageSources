/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenUsageRecorder.cs
    File description: Related implementation.

    Created by: Senparc - 20261011

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261028
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// <para>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </para>
    /// </summary>
    public sealed class AITokenUsageRecorder : IAITokenUsageRecorder, IHostedService, IDisposable
    {
        private readonly AITokenMonitorService _monitor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AITokenUsageRecorder> _logger;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly Channel<PendingTokenUsageRecord> _records = Channel.CreateUnbounded<PendingTokenUsageRecord>(
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
            ILogger<AITokenUsageRecorder> logger,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _monitor = monitor;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _workerTask = ProcessAsync();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _records.Writer.TryComplete();
            if (_workerTask == null || _workerTask.IsCompleted)
            {
                return;
            }

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var drainTimeout = TimeSpan.FromSeconds(1);
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await _workerTask.WaitAsync(drainTimeout).ConfigureAwait(false);
                    return;
                }

                await _workerTask.WaitAsync(drainTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
            {
                _logger.LogInformation("Token 用量记录尚未全部落库，主站继续退出。");
            }
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

            if (!_records.Writer.TryWrite(new PendingTokenUsageRecord(normalized, CaptureCurrentTenant())))
            {
                _logger.LogError(
                    "Token 用量记录队列已关闭，无法持久化记录。Source={Source}, Model={Model}",
                    normalized.Source,
                    normalized.ModelAlias);
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private RequestTenantSnapshot CaptureCurrentTenant()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            return httpContext == null
                ? RequestTenantSnapshot.Unmatched
                : RequestTenantSnapshot.Capture(httpContext.RequestServices);
        }

        private async Task ProcessAsync()
        {
            await foreach (var pending in _records.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await PersistAsync(pending).ConfigureAwait(false);
            }
        }

        private sealed class PendingTokenUsageRecord
        {
            public PendingTokenUsageRecord(AITokenUsageRecord record, RequestTenantSnapshot tenant)
            {
                Record = record;
                Tenant = tenant;
            }

            public AITokenUsageRecord Record { get; }
            public RequestTenantSnapshot Tenant { get; }
        }

        private async Task PersistAsync(PendingTokenUsageRecord pending)
        {
            var record = pending.Record;
            const int MaxAttempts = 3;
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    pending.Tenant.Restore(scope.ServiceProvider);
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
