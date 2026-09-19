using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeixinClawHostedService> _logger;
    private readonly ConcurrentDictionary<int, Task> _runningAccounts = new();

    public WeixinClawHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<WeixinClawHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var accountService = scope.ServiceProvider.GetRequiredService<WeixinClawAccountService>();
                var accounts = await accountService.GetFullListAsync(
                    z => z.Enabled && !string.IsNullOrWhiteSpace(z.BotTokenProtected),
                    z => z.Id,
                    Senparc.Ncf.Core.Enums.OrderingType.Ascending).ConfigureAwait(false);

                foreach (var account in accounts)
                {
                    if (_runningAccounts.TryAdd(account.Id, Task.CompletedTask))
                    {
                        var task = RunAccountAsync(account.Id, stoppingToken);
                        _runningAccounts[account.Id] = task;
                        _ = task.ContinueWith(
                            _ => _runningAccounts.TryRemove(account.Id, out _),
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
                    }
                }

                foreach (var item in _runningAccounts.Where(z => z.Value.IsCompleted).ToList())
                {
                    _runningAccounts.TryRemove(item.Key, out _);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描个人微信 Claw 账号失败。");
            }
        }
    }

    private async Task RunAccountAsync(int accountId, CancellationToken stoppingToken)
    {
        string baseUrl = null;
        string token = null;
        var notifiedStart = false;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var accountService = scope.ServiceProvider.GetRequiredService<WeixinClawAccountService>();
                var account = await accountService.GetObjectAsync(z => z.Id == accountId).ConfigureAwait(false);
                if (account == null || !account.Enabled)
                {
                    return;
                }

                baseUrl = account.BaseUrl;
                token = accountService.UnprotectToken(account);
                if (string.IsNullOrWhiteSpace(token))
                {
                    account.MarkError("bot token 无法解密，请重新扫码连接。");
                    await accountService.SaveObjectAsync(account).ConfigureAwait(false);
                    return;
                }

                var api = scope.ServiceProvider.GetRequiredService<WeixinClawApi>();
                if (!notifiedStart)
                {
                    try
                    {
                        await api.NotifyStartAsync(baseUrl, token, stoppingToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "通知个人微信 Claw 启动失败，继续尝试拉取消息。");
                    }
                    notifiedStart = true;
                }
                var response = await api.GetUpdatesAsync(
                    baseUrl,
                    token,
                    account.GetUpdatesBuf,
                    stoppingToken).ConfigureAwait(false);

                if (response.Ret != 0)
                {
                    account.MarkError($"{response.Errcode ?? response.Ret}: {response.Errmsg}");
                    await accountService.SaveObjectAsync(account).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                var receiptService = scope.ServiceProvider.GetRequiredService<WeixinClawMessageReceiptService>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<WeixinClawMessageDispatcher>();
                foreach (var message in response.Msgs ?? Enumerable.Empty<WeixinClawMessage>())
                {
                    var messageId = string.IsNullOrWhiteSpace(message.MessageId)
                        ? "seq:" + message.Seq
                        : message.MessageId;
                    if (await receiptService.ExistsAsync(account.Id, messageId, message.Seq).ConfigureAwait(false))
                    {
                        continue;
                    }

                    var text = string.Join(
                        Environment.NewLine,
                        (message.ItemList ?? new())
                            .Where(z => z.Type == 1 && !string.IsNullOrWhiteSpace(z.TextItem?.Text))
                            .Select(z => z.TextItem.Text));
                    if (message.MessageType == 1 && !string.IsNullOrWhiteSpace(text))
                    {
                        await dispatcher.DispatchAsync(new WeixinClawMessageReceivedContext(
                            account.Id,
                            account.Name,
                            messageId,
                            message.Seq,
                            message.FromUserId,
                            message.ToUserId,
                            message.GroupId,
                            message.ContextToken,
                            text,
                            message.CreateTimeMs > 0
                                ? DateTimeOffset.FromUnixTimeMilliseconds(message.CreateTimeMs)
                                : DateTimeOffset.UtcNow), stoppingToken).ConfigureAwait(false);
                    }

                    await receiptService.SaveObjectAsync(
                        new WeixinClawMessageReceipt(account.Id, messageId, message.Seq)).ConfigureAwait(false);
                }

                if (response.GetUpdatesBuf != null)
                {
                    account.SetCursor(response.GetUpdatesBuf);
                }
                account.MarkRunning();
                await accountService.SaveObjectAsync(account).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "个人微信 Claw 账号 {AccountId} 长轮询失败。", accountId);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var accountService = scope.ServiceProvider.GetRequiredService<WeixinClawAccountService>();
                var account = await accountService.GetObjectAsync(z => z.Id == accountId).ConfigureAwait(false);
                if (account != null)
                {
                    account.MarkError(ex.Message);
                    await accountService.SaveObjectAsync(account).ConfigureAwait(false);
                }
            }
            catch (Exception updateException)
            {
                _logger.LogDebug(updateException, "记录个人微信 Claw 错误状态失败。");
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var api = scope.ServiceProvider.GetRequiredService<WeixinClawApi>();
                    await api.NotifyStopAsync(baseUrl, token, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "通知个人微信 Claw 停止失败。");
                }
            }
        }
    }
}
