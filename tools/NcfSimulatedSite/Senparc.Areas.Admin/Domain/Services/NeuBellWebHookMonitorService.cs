/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellWebHookMonitorService.cs
    File description: Related implementation.
    Legacy comment translated to English.
    Legacy comment translated to English.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class NeuBellWebHookMonitorService : IHostedService, IDisposable
{
    public const string SectionName = "NeuBellWebHook";

    private const int DefaultPollingIntervalSeconds = 30;
    private const int MinPollingIntervalSeconds = 5;

    private readonly IServiceProvider _serviceProvider;
    private readonly NeuBellChangeNotifier _changeNotifier;
    private readonly NeuBellWebHookDispatcher _dispatcher;
    private readonly ILogger<NeuBellWebHookMonitorService> _logger;
    private readonly CancellationTokenSource _stoppingCts = new();
    private Task _runningTask;

    public NeuBellWebHookMonitorService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        NeuBellChangeNotifier changeNotifier,
        NeuBellWebHookDispatcher dispatcher,
        ILogger<NeuBellWebHookMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _changeNotifier = changeNotifier;
        _dispatcher = dispatcher;
        _logger = logger;
        PollingInterval = TimeSpan.FromSeconds(Math.Max(
            MinPollingIntervalSeconds,
            configuration.GetValue($"{SectionName}:PollingIntervalSeconds", DefaultPollingIntervalSeconds)));
    }

    public TimeSpan PollingInterval { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runningTask = Task.Run(() => RunAsync(_stoppingCts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingCts.Cancel();
        if (_runningTask != null)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await _runningTask.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                //Legacy comment translated to English.
            }
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ObserveOnceAsync(stoppingToken).ConfigureAwait(false);
                await WaitNextTriggerAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            //Legacy comment translated to English.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NeuBell WebHook 监测循环异常退出。");
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    private async Task WaitNextTriggerAsync(CancellationToken stoppingToken)
    {
        using var triggerCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var delayTask = Task.Delay(PollingInterval, triggerCts.Token);
        var changeTask = WaitForChangeAsync(triggerCts.Token);

        await Task.WhenAny(delayTask, changeTask).ConfigureAwait(false);
        triggerCts.Cancel();
        try
        {
            await Task.WhenAll(delayTask, changeTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (triggerCts.IsCancellationRequested)
        {
        }
    }

    private async Task WaitForChangeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var _ in _changeNotifier.SubscribeAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "NeuBell 变更事件订阅异常，回退为纯轮询模式。");
        }
    }

    private async Task ObserveOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var catalog = scope.ServiceProvider.GetRequiredService<NeuBellProviderCatalog>();
            var snapshotService = scope.ServiceProvider.GetRequiredService<NeuBellSnapshotService>();

            //Legacy comment translated to English.
            var context = new NeuBellRequestContext("neubell-webhook-monitor");
            var availableProviders = await catalog.GetAvailableProvidersAsync(cancellationToken).ConfigureAwait(false);
            var expectedProviderIds = availableProviders
                .Select(provider => provider.ProviderId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var snapshots = await snapshotService.GetSnapshotsAsync(context, cancellationToken).ConfigureAwait(false);
            await _dispatcher.ObserveAsync(snapshots, expectedProviderIds, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            //Legacy comment translated to English.
            _logger.LogWarning(ex, "NeuBell WebHook 监测轮询失败。");
        }
    }

    public void Dispose()
    {
        _stoppingCts.Dispose();
    }
}
