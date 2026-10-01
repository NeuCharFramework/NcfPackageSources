using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.WeixinManager.Domain.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawHostedService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBackgroundTenantScopeFactory _tenantScopeFactory;
    private readonly ILogger<WeixinClawHostedService> _logger;
    private readonly ConcurrentDictionary<(int TenantId, int AccountId), RunningAccount> _runningAccounts = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private Task _runningTask;

    public WeixinClawHostedService(
        IServiceScopeFactory scopeFactory,
        IBackgroundTenantScopeFactory tenantScopeFactory,
        ILogger<WeixinClawHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _tenantScopeFactory = tenantScopeFactory;
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
            .Select(z => z.Task)
            .Where(task => task != null && !task.IsCompleted)
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
                    await ScanAccountsAsync(stoppingToken).ConfigureAwait(false);
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

    private async Task ScanAccountsAsync(CancellationToken stoppingToken)
    {
        var accounts = new List<(int TenantId, int AccountId)>();
        await _tenantScopeFactory.ForEachEnabledTenantAsync(async (services, cancellationToken) =>
        {
            var accountService = services.GetRequiredService<WeixinClawAccountService>();
            accounts.AddRange(await accountService.GetPollingAccountsAsync(cancellationToken).ConfigureAwait(false));
        }, stoppingToken).ConfigureAwait(false);

        var activeKeys = new HashSet<(int TenantId, int AccountId)>();
        foreach (var account in accounts)
        {
            var key = (account.TenantId, account.AccountId);
            activeKeys.Add(key);
            if (_runningAccounts.ContainsKey(key))
            {
                continue;
            }

            var accountState = new RunningAccount(stoppingToken);
            if (_runningAccounts.TryAdd(key, accountState))
            {
                accountState.Task = RunAccountAndCleanupAsync(key, accountState);
            }
            else
            {
                accountState.Dispose();
            }
        }

        CancelStaleAccounts(activeKeys);
    }

    private async Task RunAccountAndCleanupAsync((int TenantId, int AccountId) key, RunningAccount state)
    {
        try
        {
            await RunAccountAsync(key.TenantId, key.AccountId, state.Token).ConfigureAwait(false);
        }
        finally
        {
            _runningAccounts.TryRemove(new KeyValuePair<(int TenantId, int AccountId), RunningAccount>(key, state));
            state.Dispose();
        }
    }

    private async Task RunAccountAsync(
        int tenantId,
        int accountId,
        CancellationToken stoppingToken)
    {
        string baseUrl = null;
        string token = null;
        var notifiedStart = false;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = await _tenantScopeFactory.TryCreateScopeAsync(tenantId, stoppingToken).ConfigureAwait(false);
                if (scope == null)
                {
                    return;
                }
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
                        ex,
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
                var mediaService = scope.ServiceProvider.GetRequiredService<WeixinClawMediaService>();
                var handledMessage = false;
                foreach (var message in response.Msgs ?? Enumerable.Empty<WeixinClawMessage>())
                {
                    var hasMessageId = !string.IsNullOrWhiteSpace(message.MessageId);
                    var messageId = hasMessageId
                        ? message.MessageId
                        : "seq:" + message.Seq;
                    var receiptSeq = hasMessageId ? message.Seq : 0;
                    if (!await receiptService.TryCreateAsync(account.Id, messageId, receiptSeq).ConfigureAwait(false))
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

                    handledMessage = true;

                    var text = string.Join(
                        Environment.NewLine,
                        (message.ItemList ?? new())
                            .Where(z => z.Type == 1 && !string.IsNullOrWhiteSpace(z.TextItem?.Text))
                            .Select(z => z.TextItem.Text));
                    var mediaItems = message.MessageType == 1
                        ? await mediaService.DownloadInboundAsync(
                            tenantId,
                            account.Id,
                            messageId,
                            message.ItemList,
                            stoppingToken).ConfigureAwait(false)
                        : new List<WeixinClawStoredMedia>();
                    if (message.MessageType == 1
                        && (!string.IsNullOrWhiteSpace(text) || mediaItems.Count > 0))
                    {
                        try
                        {
                            var protectedContextToken = accountService.ProtectContextToken(message.ContextToken);
                            account.MarkMessageReceived(
                                message.FromUserId,
                                protectedContextToken);
                            await accountService.SaveObjectAsync(account).ConfigureAwait(false);
                            await recordService.AddInboundAsync(
                                account.Id,
                                messageId,
                                message.Seq,
                                message.FromUserId,
                                message.ToUserId,
                                protectedContextToken,
                                message.RunId,
                                message.MessageType,
                                message.MessageState,
                                WeixinClawMessageContent.Serialize(text, mediaItems),
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
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "个人微信 Claw 账号 {AccountId} 分发消息 {MessageId} 失败。",
                                account.Id,
                                messageId);
                        }
                    }
                }

                if (response.GetUpdatesBuf != null)
                {
                    account.SetCursor(response.GetUpdatesBuf);
                }
                if (handledMessage)
                {
                    account.MarkMessageReceived();
                }
                else
                {
                    account.MarkRunning();
                }
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
                using var scope = await _tenantScopeFactory.TryCreateScopeAsync(tenantId, stoppingToken).ConfigureAwait(false);
                if (scope != null)
                {
                    var accountService = scope.ServiceProvider.GetRequiredService<WeixinClawAccountService>();
                    var account = await accountService.GetObjectAsync(z => z.Id == accountId).ConfigureAwait(false);
                    if (account != null)
                    {
                        account.MarkError(ex.Message);
                        await accountService.SaveObjectAsync(account).ConfigureAwait(false);
                    }
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

    private void CancelStaleAccounts(HashSet<(int TenantId, int AccountId)> activeKeys)
    {
        foreach (var item in _runningAccounts)
        {
            if (item.Value.Task?.IsCompleted == true || !activeKeys.Contains(item.Key))
            {
                item.Value.Cancel();
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

    private sealed class RunningAccount : IDisposable
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationTokenRegistration _stoppingRegistration;
        private bool _disposeRequested;
        private int _cancelOperations;

        public RunningAccount(CancellationToken stoppingToken)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Token = _cancellationTokenSource.Token;
            _stoppingRegistration = stoppingToken.Register(Cancel);
        }

        public CancellationToken Token { get; }

        public Task Task { get; set; }

        public void Cancel()
        {
            lock (_sync)
            {
                if (_disposeRequested)
                {
                    return;
                }
                _cancelOperations++;
            }

            try
            {
                _cancellationTokenSource.Cancel();
            }
            finally
            {
                lock (_sync)
                {
                    _cancelOperations--;
                    if (_disposeRequested && _cancelOperations == 0)
                    {
                        _cancellationTokenSource.Dispose();
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposeRequested)
                {
                    return;
                }
                _disposeRequested = true;
                if (_cancelOperations == 0)
                {
                    _cancellationTokenSource.Dispose();
                }
            }
            // Shutdown callbacks may be waiting to enter Cancel(), so unregister outside the lock.
            _stoppingRegistration.Dispose();
        }
    }
}
