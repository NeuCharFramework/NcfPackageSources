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

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

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
    /// Buffers normalized AI token-usage records, updates live monitoring state,
    /// and persists records asynchronously within the originating tenant scope.
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

        /// <summary>
        /// Initializes the asynchronous token-usage recorder.
        /// </summary>
        /// <param name="monitor">The token monitor used for recording.</param>
        /// <param name="scopeFactory">The scope factory used for persistence.</param>
        /// <param name="logger">The recorder logger.</param>
        /// <param name="httpContextAccessor">Optional request context accessor.</param>
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

        /// <summary>
        /// Starts the background record-processing loop.
        /// </summary>
        /// <param name="cancellationToken">Hosted-service startup cancellation token.</param>
        /// <returns>A completed task after scheduling processing.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _workerTask = ProcessAsync();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Completes the record channel and waits briefly for pending records to persist.
        /// </summary>
        /// <param name="cancellationToken">Token used to bound drain waiting.</param>
        /// <returns>A task that completes after the drain attempt.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _records.Writer.TryComplete();
            if (_workerTask == null || _workerTask.IsCompleted)
            {
                return;
            }

            // Allow a short drain window during host shutdown; token persistence must
            // not indefinitely delay application termination.
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

        /// <summary>
        /// Normalizes a token-usage record, updates the in-memory monitor, and queues
        /// a tenant-aware persistence operation.
        /// </summary>
        /// <param name="record">The provider usage record to record.</param>
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
        /// Captures the current request tenant so queued persistence can restore it later.
        /// </summary>
        /// <returns>The matched tenant snapshot, or an unmatched snapshot outside an HTTP request.</returns>
        private RequestTenantSnapshot CaptureCurrentTenant()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            return httpContext == null
                ? RequestTenantSnapshot.Unmatched
                : RequestTenantSnapshot.Capture(httpContext.RequestServices);
        }

        /// <summary>
        /// Drains queued usage records and persists them in order until the channel completes.
        /// </summary>
        private async Task ProcessAsync()
        {
            await foreach (var pending in _records.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await PersistAsync(pending).ConfigureAwait(false);
            }
        }

        private sealed class PendingTokenUsageRecord
        {
            /// <summary>
            /// Initializes a queued token-usage record together with its tenant snapshot.
            /// </summary>
            /// <param name="record">The normalized usage record.</param>
            /// <param name="tenant">The tenant context captured when the usage occurred.</param>
            public PendingTokenUsageRecord(AITokenUsageRecord record, RequestTenantSnapshot tenant)
            {
                Record = record;
                Tenant = tenant;
            }

            public AITokenUsageRecord Record { get; }
            public RequestTenantSnapshot Tenant { get; }
        }

        /// <summary>
        /// Persists one usage record with bounded retries and restored tenant scope.
        /// </summary>
        /// <param name="pending">The normalized record and captured tenant snapshot.</param>
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

        /// <summary>
        /// Completes the usage-record channel and releases recorder resources.
        /// </summary>
        public void Dispose()
        {
            _records.Writer.TryComplete();
        }
    }
}
