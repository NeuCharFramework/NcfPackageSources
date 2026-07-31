/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeClient.cs
    文件功能描述：DesktopBridge 能力探测和容错 SSE 客户端

    创建标识：Senparc - 20260725
----------------------------------------------------------------*/

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NcfDesktopApp.GUI.Models;

namespace NcfDesktopApp.GUI.Services;

public sealed class DesktopBridgeClient : IAsyncDisposable
{
    public const int SupportedProtocolVersion = 1;
    public const string TokenHeaderName = "X-Ncf-Desktop-Token";
    private const string CapabilitiesPath = "/api/Senparc.Xncf.DesktopBridge/capabilities";
    private const string PairingRequestsPath = "/api/Senparc.Xncf.DesktopBridge/pairing/requests";
    private const string PairingPollPath = "/api/Senparc.Xncf.DesktopBridge/pairing/poll";
    private const string DefaultEventsPath = "/api/Senparc.Xncf.DesktopBridge/events";
    private const string DefaultAuthorizedSyncPath = "/api/Senparc.Xncf.DesktopBridge/authorized-sync/events";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly SemaphoreSlim _authorizedSyncLifecycleLock = new(1, 1);
    private CancellationTokenSource? _listenCancellation;
    private Task? _listenTask;
    private CancellationTokenSource? _authorizedSyncCancellation;
    private Task? _authorizedSyncTask;
    private long _lastSequence;
    private long _lastAuthorizedSyncSequence;

    public DesktopBridgeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public event Action<DesktopBridgeProbeResult>? AvailabilityChanged;

    public event Action<DesktopActivityMessage>? ActivityReceived;

    public event Action<DesktopAuthorizedSyncMessage>? AuthorizedSyncReceived;

    public event Action<string>? AuthorizedSyncAuthorizationFailed;

    public event Action<string>? SessionRevoked;

    public async Task<DesktopBridgePairingCreateResponse> CreatePairingRequestAsync(
        string siteUrl,
        string clientName,
        CancellationToken cancellationToken = default)
    {
        if (!SiteEndpointPolicy.TryCreateEndpoint(
                siteUrl,
                PairingRequestsPath,
                out var endpoint,
                out var endpointError))
        {
            throw new InvalidOperationException(endpointError);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = CreateJsonContent(new { clientName })
        };
        using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException("当前站点的 DesktopBridge 版本不支持管理员配对，请先更新模块或手动填写令牌。");
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("配对请求过于频繁，请等待 30 秒后重试。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("远程配对被拒绝：请使用 HTTPS，或通过 localhost/SSH 隧道连接。");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DesktopBridge 创建配对请求失败（HTTP {(int)response.StatusCode}）。");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var result = DeserializePairingResponse<DesktopBridgePairingCreateResponse>(json);
        if (result == null || result.RequestId == Guid.Empty ||
            string.IsNullOrWhiteSpace(result.DeviceCode) ||
            string.IsNullOrWhiteSpace(result.PollSecret) ||
            string.IsNullOrWhiteSpace(result.VerificationPath))
        {
            throw new InvalidOperationException("DesktopBridge 返回了无效的配对信息，请更新服务端模块。");
        }

        return result;
    }

    public async Task<DesktopBridgePairingPollResponse> PollPairingAsync(
        string siteUrl,
        Guid requestId,
        string pollSecret,
        CancellationToken cancellationToken = default)
    {
        if (!SiteEndpointPolicy.TryCreateEndpoint(
                siteUrl,
                PairingPollPath,
                out var endpoint,
                out var endpointError))
        {
            throw new InvalidOperationException(endpointError);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = CreateJsonContent(new { requestId, pollSecret })
        };
        using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("DesktopBridge 配对凭据无效或安全传输要求未满足，请重新发起配对。");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DesktopBridge 查询配对状态失败（HTTP {(int)response.StatusCode}）。");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var result = DeserializePairingResponse<DesktopBridgePairingPollResponse>(json);
        if (result == null || string.IsNullOrWhiteSpace(result.Status))
        {
            throw new InvalidOperationException("DesktopBridge 返回了无效的配对状态。");
        }

        return result;
    }

    public async Task<DesktopBridgeProbeResult> ProbeAsync(
        string siteUrl,
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (!SiteEndpointPolicy.TryCreateEndpoint(siteUrl, CapabilitiesPath, out var endpoint, out var endpointError))
        {
            return new DesktopBridgeProbeResult(
                DesktopBridgeAvailability.Unavailable,
                $"{endpointError} 已使用兼容模式。");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(4));
            using var request = CreateRequest(HttpMethod.Get, endpoint, sessionToken);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new DesktopBridgeProbeResult(
                    DesktopBridgeAvailability.NotInstalled,
                    "当前 NCF 站点未安装 DesktopBridge，桌面机器人已切换到兼容模式。");
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new DesktopBridgeProbeResult(
                    DesktopBridgeAvailability.Unauthorized,
                    "DesktopBridge 会话认证失败，桌面机器人已切换到兼容模式。");
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return new DesktopBridgeProbeResult(
                    DesktopBridgeAvailability.Inactive,
                    "DesktopBridge 已安装但本次会话未启用，请从桌面应用重新启动 NCF。");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new DesktopBridgeProbeResult(
                    DesktopBridgeAvailability.Unavailable,
                    $"DesktopBridge 返回 HTTP {(int)response.StatusCode}，桌面机器人已切换到兼容模式。");
            }

            var json = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            DesktopBridgeCapabilities? capabilities;
            try
            {
                capabilities = JsonSerializer.Deserialize<DesktopBridgeCapabilities>(json, JsonOptions);
            }
            catch (JsonException)
            {
                capabilities = null;
            }

            if (capabilities == null ||
                capabilities.ProtocolVersion != SupportedProtocolVersion ||
                !capabilities.SupportsSse)
            {
                return new DesktopBridgeProbeResult(
                    DesktopBridgeAvailability.Incompatible,
                    "DesktopBridge 协议版本不兼容，请更新该模块；当前已安全降级。");
            }

            return new DesktopBridgeProbeResult(
                DesktopBridgeAvailability.Available,
                $"DesktopBridge {capabilities.BridgeVersion} 已连接。",
                capabilities);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DesktopBridgeProbeResult(
                DesktopBridgeAvailability.Unavailable,
                "DesktopBridge 探测超时，桌面机器人已切换到兼容模式。");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            return new DesktopBridgeProbeResult(
                DesktopBridgeAvailability.Unavailable,
                $"DesktopBridge 暂不可用（{ex.Message}），桌面机器人已切换到兼容模式。");
        }
    }

    public async Task<DesktopBridgeProbeResult> ConnectAsync(
        string siteUrl,
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        await StopAsync().ConfigureAwait(false);
        var probe = await ProbeAsync(siteUrl, sessionToken, cancellationToken).ConfigureAwait(false);
        NotifyAvailability(probe);
        if (!probe.IsAvailable && probe.Availability != DesktopBridgeAvailability.Unavailable)
        {
            return probe;
        }

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _lastSequence = 0;
            _listenCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listenTask = probe.IsAvailable
                ? ListenLoopAsync(
                    siteUrl,
                    sessionToken,
                    probe.Capabilities?.EventEndpoint ?? DefaultEventsPath,
                    _listenCancellation.Token)
                : ProbeUntilAvailableAsync(siteUrl, sessionToken, _listenCancellation.Token);
        }
        finally
        {
            _lifecycleLock.Release();
        }

        return probe;
    }

    private async Task ProbeUntilAvailableAsync(
        string siteUrl,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        var retryDelay = TimeSpan.FromSeconds(2);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var probe = await ProbeAsync(siteUrl, sessionToken, cancellationToken).ConfigureAwait(false);
            NotifyAvailability(probe);
            if (probe.IsAvailable)
            {
                await ListenLoopAsync(
                        siteUrl,
                        sessionToken,
                        probe.Capabilities?.EventEndpoint ?? DefaultEventsPath,
                        cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            // 404、认证失败、未启用和协议不兼容都需要用户操作，继续轮询没有意义。
            if (probe.Availability != DesktopBridgeAvailability.Unavailable)
            {
                return;
            }

            retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 10));
        }
    }

    public async Task StopAsync()
    {
        await StopAuthorizedSyncAsync().ConfigureAwait(false);

        CancellationTokenSource? cancellation;
        Task? listenTask;

        await _lifecycleLock.WaitAsync().ConfigureAwait(false);
        try
        {
            cancellation = _listenCancellation;
            listenTask = _listenTask;
            _listenCancellation = null;
            _listenTask = null;
        }
        finally
        {
            _lifecycleLock.Release();
        }

        if (cancellation == null)
        {
            return;
        }

        cancellation.Cancel();
        try
        {
            if (listenTask != null)
            {
                await listenTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // 停止桥接不能妨碍 NCF 进程关闭。
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _lifecycleLock.Dispose();
        _authorizedSyncLifecycleLock.Dispose();
    }

    public async Task StartAuthorizedSyncAsync(
        string siteUrl,
        string sessionToken,
        string accessToken,
        string? eventPath = null,
        CancellationToken cancellationToken = default)
    {
        await StopAuthorizedSyncAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Admin access token is required.", nameof(accessToken));
        }

        await _authorizedSyncLifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _lastAuthorizedSyncSequence = 0;
            _authorizedSyncCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _authorizedSyncTask = ListenAuthorizedSyncLoopAsync(
                siteUrl,
                sessionToken,
                eventPath ?? DefaultAuthorizedSyncPath,
                accessToken,
                _authorizedSyncCancellation.Token);
        }
        finally
        {
            _authorizedSyncLifecycleLock.Release();
        }
    }

    public async Task StopAuthorizedSyncAsync()
    {
        CancellationTokenSource? cancellation;
        Task? listenTask;

        await _authorizedSyncLifecycleLock.WaitAsync().ConfigureAwait(false);
        try
        {
            cancellation = _authorizedSyncCancellation;
            listenTask = _authorizedSyncTask;
            _authorizedSyncCancellation = null;
            _authorizedSyncTask = null;
        }
        finally
        {
            _authorizedSyncLifecycleLock.Release();
        }

        if (cancellation == null)
        {
            return;
        }

        cancellation.Cancel();
        try
        {
            if (listenTask != null)
            {
                await listenTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // 注销和停止站点时不传播同步流的网络异常。
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    private async Task ListenLoopAsync(
        string siteUrl,
        string sessionToken,
        string eventPath,
        CancellationToken cancellationToken)
    {
        var retryDelay = TimeSpan.FromSeconds(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!SiteEndpointPolicy.TryCreateEndpoint(siteUrl, eventPath, out var endpoint, out _))
            {
                NotifyAvailability(new DesktopBridgeProbeResult(
                    DesktopBridgeAvailability.Incompatible,
                    "DesktopBridge 返回了无效的事件地址，已安全降级。"));
                return;
            }

            try
            {
                using var request = CreateRequest(HttpMethod.Get, endpoint, sessionToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                using var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    NotifyAvailability(new DesktopBridgeProbeResult(
                        DesktopBridgeAvailability.NotInstalled,
                        "DesktopBridge 事件接口不存在，桌面机器人已切换到兼容模式。"));
                    return;
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    var revoked = new DesktopBridgeProbeResult(
                        DesktopBridgeAvailability.Unauthorized,
                        "DesktopBridge 会话已被撤销，正在关闭当前工作台。");
                    NotifyAvailability(revoked);
                    NotifySessionRevoked(revoked.Message);
                    return;
                }

                response.EnsureSuccessStatusCode();
                NotifyAvailability(new DesktopBridgeProbeResult(
                    DesktopBridgeAvailability.Available,
                    "DesktopBridge 实时事件流已连接。"));
                retryDelay = TimeSpan.FromSeconds(1);

                await ReadEventStreamAsync(response, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (ex is
                       OperationCanceledException or HttpRequestException or IOException or JsonException or InvalidOperationException)
            {
                if (await CheckSessionRevokedAsync(siteUrl, sessionToken, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                NotifyAvailability(new DesktopBridgeProbeResult(
                    DesktopBridgeAvailability.Unavailable,
                    $"DesktopBridge 连接中断（{ex.Message}），正在后台重连。"));
            }

            try
            {
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 10));
        }
    }

    private async Task<bool> CheckSessionRevokedAsync(
        string siteUrl,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        var probe = await ProbeAsync(siteUrl, sessionToken, cancellationToken).ConfigureAwait(false);
        if (probe.Availability != DesktopBridgeAvailability.Unauthorized)
        {
            return false;
        }

        var revoked = probe with { Message = "DesktopBridge 会话已被管理员撤销，正在关闭当前工作台。" };
        NotifyAvailability(revoked);
        NotifySessionRevoked(revoked.Message);
        return true;
    }

    private async Task ReadEventStreamAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var data = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                throw new IOException("DesktopBridge 事件流已结束");
            }

            if (line.Length == 0)
            {
                if (data.Length > 0)
                {
                    var activity = JsonSerializer.Deserialize<DesktopActivityMessage>(data.ToString(), JsonOptions);
                    data.Clear();
                    if (activity != null && activity.Sequence > Interlocked.Read(ref _lastSequence))
                    {
                        Interlocked.Exchange(ref _lastSequence, activity.Sequence);
                        NotifyActivity(activity);
                    }
                }

                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (data.Length > 0)
                {
                    data.Append('\n');
                }

                data.Append(line.AsSpan("data:".Length).TrimStart());
            }
        }
    }

    private async Task ListenAuthorizedSyncLoopAsync(
        string siteUrl,
        string sessionToken,
        string eventPath,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var retryDelay = TimeSpan.FromSeconds(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!SiteEndpointPolicy.TryCreateEndpoint(siteUrl, eventPath, out var endpoint, out _))
            {
                NotifyAuthorizedSyncAuthorizationFailed("DesktopBridge 返回了无效的授权同步地址。");
                return;
            }

            try
            {
                using var request = CreateRequest(HttpMethod.Get, endpoint, sessionToken, accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                using var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    NotifyAuthorizedSyncAuthorizationFailed("管理员登录已过期或不具备 AdminOnly 权限。");
                    return;
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    NotifyAuthorizedSyncAuthorizationFailed("当前 DesktopBridge 版本不支持 Admin Chat 同步，请更新模块。");
                    return;
                }

                response.EnsureSuccessStatusCode();
                retryDelay = TimeSpan.FromSeconds(1);
                await ReadAuthorizedSyncStreamAsync(response, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidOperationException)
            {
                // 短暂断线保留登录状态并重连；只有明确的 401/403 才注销。
            }

            try
            {
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 10));
        }
    }

    private async Task ReadAuthorizedSyncStreamAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var data = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                throw new IOException("DesktopBridge 授权同步流已结束");
            }

            if (line.Length == 0)
            {
                if (data.Length > 0)
                {
                    var message = JsonSerializer.Deserialize<DesktopAuthorizedSyncMessage>(data.ToString(), JsonOptions);
                    data.Clear();
                    if (message != null && message.Sequence > Interlocked.Read(ref _lastAuthorizedSyncSequence))
                    {
                        Interlocked.Exchange(ref _lastAuthorizedSyncSequence, message.Sequence);
                        NotifyAuthorizedSync(message);
                    }
                }

                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (data.Length > 0)
                {
                    data.Append('\n');
                }

                data.Append(line.AsSpan("data:".Length).TrimStart());
            }
        }
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        Uri endpoint,
        string sessionToken,
        string? accessToken = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.TryAddWithoutValidation(TokenHeaderName, sessionToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return request;
    }

    private static StringContent CreateJsonContent<T>(T value)
    {
        return new StringContent(
            JsonSerializer.Serialize(value, JsonOptions),
            Encoding.UTF8,
            "application/json");
    }

    private static T? DeserializePairingResponse<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private void NotifyAvailability(DesktopBridgeProbeResult result)
    {
        foreach (var handler in AvailabilityChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            try
            {
                ((Action<DesktopBridgeProbeResult>)handler)(result);
            }
            catch
            {
                // UI 订阅者异常不能终止重连循环。
            }
        }
    }

    private void NotifyActivity(DesktopActivityMessage activity)
    {
        foreach (var handler in ActivityReceived?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            try
            {
                ((Action<DesktopActivityMessage>)handler)(activity);
            }
            catch
            {
                // UI 订阅者异常不能终止 SSE 读取。
            }
        }
    }

    private void NotifyAuthorizedSync(DesktopAuthorizedSyncMessage message)
    {
        foreach (var handler in AuthorizedSyncReceived?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            try
            {
                ((Action<DesktopAuthorizedSyncMessage>)handler)(message);
            }
            catch
            {
                // UI 订阅者异常不能终止 SSE 读取。
            }
        }
    }

    private void NotifyAuthorizedSyncAuthorizationFailed(string message)
    {
        foreach (var handler in AuthorizedSyncAuthorizationFailed?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            try
            {
                ((Action<string>)handler)(message);
            }
            catch
            {
                // UI 订阅者异常不能终止同步流。
            }
        }
    }

    private void NotifySessionRevoked(string message)
    {
        foreach (var handler in SessionRevoked?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            try
            {
                ((Action<string>)handler)(message);
            }
            catch
            {
                // 会话撤销必须继续结束监听，不能被单个 UI 订阅者阻断。
            }
        }
    }
}
