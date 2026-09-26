using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.WeixinClawIntegration;

/// <summary>
/// 将 NeuBell 的变更事件转发给已绑定的个人微信会话。
/// 变更通知只触发重新读取快照，不把业务正文放进底层发布事件。
/// </summary>
public sealed class WeixinClawNeuBellPushHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NeuBellChangeNotifier _changeNotifier;
    private readonly IOptions<WeixinClawAdminIntegrationOptions> _options;
    private readonly ILogger<WeixinClawNeuBellPushHostedService> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastPush = new(StringComparer.OrdinalIgnoreCase);

    public WeixinClawNeuBellPushHostedService(
        IServiceScopeFactory scopeFactory,
        NeuBellChangeNotifier changeNotifier,
        IOptions<WeixinClawAdminIntegrationOptions> options,
        ILogger<WeixinClawNeuBellPushHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _changeNotifier = changeNotifier;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled || !_options.Value.EnableNeuBell)
        {
            return;
        }

        await foreach (var providerId in _changeNotifier.SubscribeAsync(stoppingToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            if (_lastPush.TryGetValue(providerId, out var previous)
                && now - previous < TimeSpan.FromSeconds(2))
            {
                continue;
            }
            _lastPush[providerId] = now;

            try
            {
                await PushProviderAsync(providerId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NeuBell 变更转发到个人微信失败：ProviderId={ProviderId}", providerId);
            }
        }
    }

    private async Task PushProviderAsync(string providerId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var bindingService = scope.ServiceProvider.GetRequiredService<WeixinClawAdminBindingService>();
        var snapshotService = scope.ServiceProvider.GetRequiredService<NeuBellSnapshotService>();
        var sender = scope.ServiceProvider.GetRequiredService<Senparc.Xncf.WeixinManager.WeixinClaw.IWeixinClawMessageSender>();
        var bindings = await bindingService.GetNeuBellBindingsAsync().ConfigureAwait(false);

        foreach (var binding in bindings)
        {
            var snapshots = await snapshotService.GetSnapshotsAsync(
                new NeuBellRequestContext(binding.AdminUserId.ToString()),
                cancellationToken).ConfigureAwait(false);
            var snapshot = snapshots.FirstOrDefault(item =>
                string.Equals(item.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
            var items = snapshot?.Items
                ?.Where(item => item.Count > 0 || !string.Equals(item.Severity, "info", StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
            if (snapshot == null || items == null || items.Count == 0)
            {
                continue;
            }

            var lines = items.Select(item => $"{item.Title}：{item.Summary}{(item.Count > 0 ? $"（{item.Count}）" : string.Empty)}");
            var message = $"NeuBell「{snapshot.DisplayName}」有新的提醒：{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
            await sender.SendTextAsync(
                binding.AccountId,
                binding.FromUserId,
                message,
                binding.ContextToken,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
