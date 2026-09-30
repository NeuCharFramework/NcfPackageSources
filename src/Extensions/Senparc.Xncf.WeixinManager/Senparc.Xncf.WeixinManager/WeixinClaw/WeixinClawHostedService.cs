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

public sealed class WeixinClawHostedService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeixinClawHostedService> _logger;
    private readonly ConcurrentDictionary<int, Task> _runningAccounts = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private Task _runningTask;

    public WeixinClawHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<WeixinClawHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runningTask = Task.Run(() => RunAsync(_stoppingCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingCts.Cancel();
        if (_runningTask == null)
        {
            return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await _runningTask.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("个人微信 Claw 长轮询已发出停止信号，主站继续退出。");
        }

        var accountTasks = _runningAccounts.Values
            .Where(task => !task.IsCompleted)
            .ToArray();
        if (accountTasks.Length == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(accountTasks).WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "个人微信 Claw 账号任务未在停止等待窗口内全部结束，主站继续退出：Count={Count}",
                accountTasks.Length);
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        _stoppingCts.Dispose();
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
                WeixinClawGetUpdatesResponse response;
                try
                {
                    response = await api.GetUpdatesAsync(
                        baseUrl,
                        token,
                        account.GetUpdatesBuf,
                        stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsExpectedLongPollTimeout(ex, stoppingToken))
                {
                    // The host may still have a generic 30-second Polly timeout in
                    // an already-running process. A canceled getupdates request is
                    // an empty poll, not an account failure.
                    _logger.LogDebug(
                        "个人微信 Claw 账号 {AccountId} 长轮询达到宿主超时边界，继续下一轮：{ExceptionType}",
                        account.Id,
                        ex.GetType().FullName);
                    account.MarkRunning();
                    await accountService.SaveObjectAsync(account).ConfigureAwait(false);
                    continue;
                }

                if (response.Ret != 0)
                {
                    account.MarkError($"{response.Errcode ?? response.Ret}: {response.Errmsg}");
                    await accountService.SaveObjectAsync(account).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                var receiptService = scope.ServiceProvider.GetRequiredService<WeixinClawMessageReceiptService>();
                var recordService = scope.ServiceProvider.GetRequiredService<WeixinClawMessageRecordService>();
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

                    _logger.LogInformation(
                        "个人微信 Claw 账号 {AccountId} 收到消息：MessageId={MessageId}, Seq={Seq}, MessageType={MessageType}, ItemCount={ItemCount}, HasContextToken={HasContextToken}",
                        account.Id,
                        messageId,
                        message.Seq,
                        message.MessageType,
                        message.ItemList?.Count ?? 0,
                        !string.IsNullOrWhiteSpace(message.ContextToken));

                    var text = string.Join(
                        Environment.NewLine,
                        (message.ItemList ?? new())
                            .Where(z => z.Type == 1 && !string.IsNullOrWhiteSpace(z.TextItem?.Text))
                            .Select(z => z.TextItem.Text));
                    if (message.MessageType == 1 && !string.IsNullOrWhiteSpace(text))
                    {
                        account.MarkMessageReceived(
                            message.FromUserId,
                            accountService.ProtectContextToken(message.ContextToken));
                        await accountService.SaveObjectAsync(account).ConfigureAwait(false);
                        await recordService.AddInboundAsync(
                            account.Id,
                            messageId,
                            message.Seq,
                            message.FromUserId,
                            message.ToUserId,
                            accountService.ProtectContextToken(message.ContextToken),
                            message.RunId,
                            message.MessageType,
                            message.MessageState,
                            text,
                            message.CreateTimeMs > 0
                                ? DateTimeOffset.FromUnixTimeMilliseconds(message.CreateTimeMs).UtcDateTime
                                : DateTime.UtcNow).ConfigureAwait(false);

                        await dispatcher.DispatchAsync(new WeixinClawMessageReceivedContext(
                            account.Id,
                            account.Name,
                            messageId,
                            message.Seq,
                            message.FromUserId,
                            message.ToUserId,
                            message.GroupId,
                            message.ContextToken,
                            message.RunId,
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
            if (!stoppingToken.IsCancellationRequested
                && !string.IsNullOrWhiteSpace(baseUrl)
                && !string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    using var notifyTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var scope = _scopeFactory.CreateScope();
                    var api = scope.ServiceProvider.GetRequiredService<WeixinClawApi>();
                    await api.NotifyStopAsync(baseUrl, token, notifyTimeout.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "通知个人微信 Claw 停止失败。");
                }
            }
        }
    }

    private static bool IsExpectedLongPollTimeout(
        Exception exception,
        CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return false;
        }

        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is TaskCanceledException
                || string.Equals(
                    current.GetType().FullName,
                    "Polly.Timeout.TimeoutRejectedException",
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
