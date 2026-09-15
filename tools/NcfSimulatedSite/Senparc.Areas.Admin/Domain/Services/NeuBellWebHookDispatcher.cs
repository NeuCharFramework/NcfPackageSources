/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookDispatcher.cs
    文件功能描述：NeuBell WebHook 调度器：对比相邻快照的条目差异（added/removed），
    将变更以异步方式（fire-and-forget）POST 到匹配的 WebHook 地址

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260914
    修改描述：v0.7.1 WebHook 增强：支持 GET/POST/PUT 请求方式、请求体模板与 {{占位符}}
    渲染（与 Workflow 文本模板同格式）、发送前对渲染后地址二次校验、
    修复变更通知日志在成功后仍被标记“请求未完成”的问题

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 单个 Provider 的一次条目变化（新增/移除）
/// </summary>
public sealed class NeuBellWebHookChange
{
    public string ProviderId { get; init; }
    public string ModuleUid { get; init; }
    public string DisplayName { get; init; }
    public string TenantId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public IReadOnlyList<NeuBellItem> Added { get; init; } = Array.Empty<NeuBellItem>();
    public IReadOnlyList<NeuBellItem> Removed { get; init; } = Array.Empty<NeuBellItem>();

    public bool HasAdded => Added is { Count: > 0 };
    public bool HasRemoved => Removed is { Count: > 0 };
}

/// <summary>
/// 纽铃 WebHook 调度器（Singleton）。
/// 每次 <see cref="ObserveAsync"/> 会以 Provider 为单位对比条目集合，
/// 得出新增（added）与移除（removed）的条目，并按 WebHook 设置异步通知。
/// 首次观测只建立基线、不触发通知，避免进程启动时误报全量“新增”。
/// </summary>
public sealed class NeuBellWebHookDispatcher
{
    public const string HttpClientName = "NeuBellWebHook";

    public const string EventKindItemCreated = "item-created";
    public const string EventKindItemsChanged = "items-changed";
    public const string EventKindTest = "test";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, ProviderObservation> _observations
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NeuBellWebHookDispatcher> _logger;

    // 可选站点根地址：把条目的相对 DetailUrl（{{link}}）拼成绝对链接
    private readonly string _linkBaseUrl;

    // 限制并发出站请求，避免 Provider 抖动时打爆下游 WebAPI
    private readonly SemaphoreSlim _outboundGate = new(4, 4);

    public NeuBellWebHookDispatcher(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<NeuBellWebHookDispatcher> logger,
        string linkBaseUrl = null)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _linkBaseUrl = (linkBaseUrl ?? string.Empty).Trim();
    }

    /// <summary>
    /// 观测一轮快照，计算各 Provider 的 added/removed 并触发 WebHook 通知（异步、不阻塞调用方）
    /// </summary>
    public async Task<IReadOnlyList<NeuBellWebHookChange>> ObserveAsync(
        IReadOnlyList<NeuBellSnapshot> snapshots,
        IReadOnlyCollection<string> expectedProviderIds = null,
        string tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var changes = new List<NeuBellWebHookChange>();
        var now = DateTimeOffset.Now;
        var seenProviderKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var snapshot in snapshots ?? Array.Empty<NeuBellSnapshot>())
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.ProviderId))
            {
                continue;
            }

            seenProviderKeys.Add(snapshot.ProviderId);
            var itemIndex = (snapshot.Items ?? Array.Empty<NeuBellItem>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

            var observation = _observations.GetOrAdd(snapshot.ProviderId, _ => new ProviderObservation());
            var (added, removed) = observation.Diff(itemIndex);

            // 首次观测：只建立基线，不触发通知（进程重启后不能把现存条目误报为“新增”）
            if (!observation.HasBaseline)
            {
                observation.SetBaseline(itemIndex);
                continue;
            }

            if (added.Count == 0 && removed.Count == 0)
            {
                continue;
            }

            var change = new NeuBellWebHookChange
            {
                ProviderId = snapshot.ProviderId,
                ModuleUid = snapshot.ModuleUid,
                DisplayName = snapshot.DisplayName,
                TenantId = tenantId,
                OccurredAt = now,
                Added = added,
                Removed = removed
            };
            changes.Add(change);

            observation.SetBaseline(itemIndex);
            _ = DispatchAsync(change, cancellationToken);
        }

        // 本轮“缺席”的 Provider 分两种情况：
        // 1. 模块未开放/已关闭（不在 expectedProviderIds 中）：存量条目按“移除”通知；
        // 2. 模块仍开放但本轮快照获取失败（在 expectedProviderIds 中）：保留基线等待下轮，避免误报。
        // expectedProviderIds 为 null（无法判定模块开放状态）时，一律不做移除判断。
        if (expectedProviderIds != null)
        {
            var expected = new HashSet<string>(expectedProviderIds, StringComparer.OrdinalIgnoreCase);
            foreach (var key in _observations.Keys.Where(key => !seenProviderKeys.Contains(key)).ToList())
            {
                if (expected.Contains(key))
                {
                    continue;
                }

                var observation = _observations[key];
                if (!observation.HasBaseline)
                {
                    continue;
                }

                var removed = observation.Items.Values.ToList();
                if (removed.Count == 0)
                {
                    continue;
                }

                var change = new NeuBellWebHookChange
                {
                    ProviderId = key,
                    ModuleUid = null,
                    DisplayName = null,
                    TenantId = tenantId,
                    OccurredAt = now,
                    Added = Array.Empty<NeuBellItem>(),
                    Removed = removed
                };
                changes.Add(change);

                observation.SetBaseline(new Dictionary<string, NeuBellItem>(StringComparer.OrdinalIgnoreCase));
                _ = DispatchAsync(change, cancellationToken);
            }
        }

        return changes;
    }

    /// <summary>
    /// 测试发送：向指定 WebHook 发送一条 test 事件（管理页“测试”按钮用，同步等待结果）。
    /// 地址与请求体支持 {{占位符}}（渲染后的真实数据记入日志）。
    /// 请求数据与结果会记录到 NeuBellWebHookLog 列表。
    /// </summary>
    public async Task<(bool success, string message)> SendTestAsync(
        NeuBellWebHook webHook,
        int adminUserId = 0,
        CancellationToken cancellationToken = default)
    {
        if (webHook == null)
        {
            return (false, "WebHook 不存在");
        }
        if (!NeuBellWebHook.TryValidateUrlTemplate(webHook.WebHookUrl, out var templateError))
        {
            return (false, templateError);
        }

        var method = NeuBellWebHook.NormalizeHttpMethod(webHook.HttpMethod);
        var payload = BuildPayload(new NeuBellWebHookChange
        {
            ProviderId = webHook.ProviderFilter,
            ModuleUid = null,
            DisplayName = null,
            TenantId = null,
            OccurredAt = DateTimeOffset.Now,
            Added = Array.Empty<NeuBellItem>(),
            Removed = Array.Empty<NeuBellItem>()
        }, test: true);

        var tokens = BuildTestTokens(webHook, payload);
        var renderedUrl = NeuBellWebHookTemplate.RenderUrl(webHook.WebHookUrl, tokens);
        if (!NeuBellWebHook.TryValidateUrl(renderedUrl, out var urlError))
        {
            return (false, urlError);
        }

        var (body, contentType) = BuildRequestBody(method, webHook.BodyTemplate, tokens, payload);
        var logId = await CreatePendingLogAsync(
            EventKindTest, method, TruncateLogUrl(renderedUrl), webHook.ProviderFilter, webHook.Name,
            body ?? string.Empty, adminUserId)
            .ConfigureAwait(false);

        var (success, message, statusCode, elapsed) = await SendCoreAsync(
            renderedUrl, webHook.Secret, body, method, contentType, cancellationToken)
            .ConfigureAwait(false);

        if (logId != null)
        {
            await CompleteLogAsync(logId.Value, success, message, statusCode, elapsed).ConfigureAwait(false);
        }
        return (success, message);
    }

    /// <summary>
    /// 异步分发（fire-and-forget）：加载启用中的 WebHook，按 Provider 过滤后逐个 POST
    /// </summary>
    private async Task DispatchAsync(NeuBellWebHookChange change, CancellationToken cancellationToken)
    {
        List<NeuBellWebHook> webHooks;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<INeuBellWebHookService>();
            webHooks = (await service.GetEnabledListAsync().ConfigureAwait(false))
                .Where(webHook => webHook.MatchesProvider(change.ProviderId))
                .Where(webHook =>
                    (!change.HasAdded || webHook.NotifyOnAdd)
                    && (!change.HasRemoved || webHook.NotifyOnRemove))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载 NeuBell WebHook 设置失败，跳过本轮通知。");
            return;
        }

        if (webHooks.Count == 0)
        {
            return;
        }

        var payload = BuildPayload(change, test: false);
        var tokens = BuildChangeTokens(change, payload);
        foreach (var webHook in webHooks)
        {
            await _outboundGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            _ = Task.Run(async () =>
            {
                int? logId = null;
                var completed = false;
                try
                {
                    var method = NeuBellWebHook.NormalizeHttpMethod(webHook.HttpMethod);
                    var renderedUrl = NeuBellWebHookTemplate.RenderUrl(webHook.WebHookUrl, tokens);

                    // 渲染后的真实地址必须仍是合法的 http/https 绝对地址，否则只记失败日志、不发送
                    if (!NeuBellWebHook.TryValidateUrl(renderedUrl, out var urlError))
                    {
                        logId = await CreatePendingLogAsync(
                            EventKindItemsChanged, method, TruncateLogUrl(renderedUrl), change.ProviderId,
                            webHook.Name, string.Empty, 0).ConfigureAwait(false);
                        if (logId != null)
                        {
                            await CompleteLogAsync(logId.Value, false, urlError, null, 0).ConfigureAwait(false);
                        }
                        completed = true;
                        _logger.LogWarning(
                            "NeuBell WebHook [{Name}] 渲染后的地址无效，已跳过：{Message}", webHook.Name, urlError);
                        return;
                    }

                    var (body, contentType) = BuildRequestBody(method, webHook.BodyTemplate, tokens, payload);
                    logId = await CreatePendingLogAsync(
                        EventKindItemsChanged, method, TruncateLogUrl(renderedUrl), change.ProviderId,
                        webHook.Name, body ?? string.Empty, 0)
                        .ConfigureAwait(false);

                    var (success, message, statusCode, elapsed) = await SendCoreAsync(
                        renderedUrl, webHook.Secret, body, method, contentType, CancellationToken.None)
                        .ConfigureAwait(false);
                    if (logId != null)
                    {
                        await CompleteLogAsync(logId.Value, success, message, statusCode, elapsed)
                            .ConfigureAwait(false);
                        completed = true;
                    }
                    if (!success)
                    {
                        _logger.LogWarning(
                            "NeuBell WebHook [{Name}] 通知失败：{Message}", webHook.Name, message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "NeuBell WebHook [{Name}] 通知异常。", webHook.Name);
                }
                finally
                {
                    // 仅在日志已创建但请求未走到“完成”路径（异常/被中断）时兜底标记失败
                    if (logId != null && !completed)
                    {
                        await CompleteLogAsync(logId.Value, false, "请求未完成", null, 0).ConfigureAwait(false);
                    }
                    _outboundGate.Release();
                }
            }, CancellationToken.None);
        }
    }

    /// <summary>
    /// 实际发送（返回 HTTP 状态码与耗时），不记录日志。
    /// GET 请求不携带请求体与签名；其他方法携带 body（默认 application/json）。
    /// </summary>
    private async Task<(bool success, string message, int? statusCode, long elapsedMilliseconds)> SendCoreAsync(
        string url,
        string secret,
        string body,
        string method,
        string contentType,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            var hasBody = !string.IsNullOrEmpty(body)
                && !string.Equals(method, NeuBellWebHook.MethodGet, StringComparison.OrdinalIgnoreCase);
            if (hasBody)
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "application/json");
                // 签名覆盖实际发出的请求体字节
                if (!string.IsNullOrWhiteSpace(secret))
                {
                    request.Headers.TryAddWithoutValidation("X-NeuBell-Signature", SignPayload(secret, body));
                }
            }

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            if (response.IsSuccessStatusCode)
            {
                return (true, $"HTTP {(int)response.StatusCode}", (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
            }

            return (false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}", (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return (false, "已取消", null, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return (false, ex.Message, null, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 创建“发送中”日志；失败时返回 null（日志失败不得阻塞通知本身）
    /// </summary>
    private async Task<int?> CreatePendingLogAsync(
        string eventKind,
        string httpMethod,
        string webHookUrl,
        string providerId,
        string title,
        string payload,
        int adminUserId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<INeuBellWebHookLogService>();
            var log = await logService.CreatePendingAsync(
                eventKind, httpMethod, webHookUrl, providerId, title, payload, adminUserId)
                .ConfigureAwait(false);
            return log?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "创建 NeuBell WebHook 请求日志失败（不影响本次通知）。");
            return null;
        }
    }

    /// <summary>
    /// 完成日志记录；失败只记警告
    /// </summary>
    private async Task CompleteLogAsync(
        int logId,
        bool success,
        string result,
        int? statusCode,
        long elapsedMilliseconds)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<INeuBellWebHookLogService>();
            await logService.CompleteAsync(logId, success, result, statusCode, elapsedMilliseconds).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 NeuBell WebHook 请求日志失败（日志 Id={LogId}）。", logId);
        }
    }

    /// <summary>
    /// 创建 NeuBell 时按调用方传入的 WebHook 地址发送一次通知（item-created）。
    /// 地址支持 {{占位符}}（渲染值来自传入的条目）；本方法只负责快速返回，
    /// 实际 HTTP 请求在后台 fire-and-forget 执行，不阻塞调用方（Function 响应）。
    /// </summary>
    public Task<(bool queued, string message)> NotifyItemCreatedAsync(
        string webHookUrl,
        string httpMethod,
        NeuBellItem item,
        string providerName,
        int adminUserId)
    {
        return Task.Run(async () =>
        {
            var method = NeuBellWebHook.NormalizeHttpMethod(httpMethod);
            var urlTemplate = (webHookUrl ?? string.Empty).Trim();
            var payload = BuildItemCreatedPayload(item, providerName);

            // 模板地址本身不合法（占位符之外的部分无法解析）：不发送，仅记录失败日志
            if (!NeuBellWebHook.TryValidateUrlTemplate(urlTemplate, out var templateError))
            {
                var failedLogId = await CreatePendingLogAsync(
                    EventKindItemCreated, method, TruncateLogUrl(urlTemplate), providerName,
                    item?.Title ?? string.Empty, payload, adminUserId).ConfigureAwait(false);
                if (failedLogId != null)
                {
                    await CompleteLogAsync(failedLogId.Value, false, templateError, null, 0).ConfigureAwait(false);
                }
                return (false, templateError);
            }

            var tokens = BuildItemCreatedTokens(item, providerName, payload);
            var renderedUrl = NeuBellWebHookTemplate.RenderUrl(urlTemplate, tokens);

            // 渲染后的真实地址必须仍是合法的 http/https 绝对地址，否则只记失败日志、不发送
            if (!NeuBellWebHook.TryValidateUrl(renderedUrl, out var urlError))
            {
                var (failedBody, _) = BuildRequestBody(method, null, tokens, payload);
                var failedLogId = await CreatePendingLogAsync(
                    EventKindItemCreated, method, TruncateLogUrl(renderedUrl), providerName,
                    item?.Title ?? string.Empty, failedBody ?? string.Empty, adminUserId).ConfigureAwait(false);
                if (failedLogId != null)
                {
                    await CompleteLogAsync(failedLogId.Value, false, urlError, null, 0).ConfigureAwait(false);
                }
                return (false, urlError);
            }

            var (body, contentType) = BuildRequestBody(method, null, tokens, payload);
            var logId = await CreatePendingLogAsync(
                EventKindItemCreated, method, TruncateLogUrl(renderedUrl), providerName,
                item?.Title ?? string.Empty, body ?? string.Empty, adminUserId).ConfigureAwait(false);

            await _outboundGate.WaitAsync().ConfigureAwait(false);
            try
            {
                var (success, message, statusCode, elapsed) = await SendCoreAsync(
                    renderedUrl, null, body, method, contentType, CancellationToken.None).ConfigureAwait(false);
                if (logId != null)
                {
                    await CompleteLogAsync(logId.Value, success, message, statusCode, elapsed).ConfigureAwait(false);
                }
                if (!success)
                {
                    _logger.LogWarning("NeuBell WebHook 创建通知失败：{Message}（{Url}）", message, renderedUrl);
                }
                return (true, "已发送");
            }
            finally
            {
                _outboundGate.Release();
            }
        }, CancellationToken.None);
    }

    /// <summary>
    /// 构建 item-created 报文（创建 NeuBell 时按参数触发的单次通知）
    /// </summary>
    private static string BuildItemCreatedPayload(NeuBellItem item, string providerName)
    {
        return JsonSerializer.Serialize(new
        {
            source = "NeuBell",
            kind = EventKindItemCreated,
            providerId = providerName,
            occurredAt = DateTimeOffset.Now,
            item = new
            {
                id = item?.Id,
                title = item?.Title,
                summary = item?.Summary,
                link = item?.DetailUrl,
                status = item?.Severity,
                count = item?.Count ?? 0,
                updated = item?.UpdatedAt
            }
        }, JsonOptions);
    }

    /// <summary>
    /// 按请求方式与模板计算实际发送的请求体（GET 返回 null 表示不发送请求体）。
    /// bodyTemplate 非空时走模板渲染，否则使用默认结构化 JSON 报文。
    /// </summary>
    private static (string body, string contentType) BuildRequestBody(
        string method,
        string bodyTemplate,
        IReadOnlyDictionary<string, string> tokens,
        string defaultPayload)
    {
        if (string.Equals(method, NeuBellWebHook.MethodGet, StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }
        if (string.IsNullOrWhiteSpace(bodyTemplate))
        {
            return (defaultPayload, "application/json");
        }
        return (NeuBellWebHookTemplate.RenderBody(bodyTemplate, tokens, out var contentType), contentType);
    }

    /// <summary>
    /// 日志中的地址超过 1000 字符（MaxLength）时截断
    /// </summary>
    private static string TruncateLogUrl(string url)
    {
        if (string.IsNullOrEmpty(url) || url.Length <= 1000)
        {
            return url ?? string.Empty;
        }
        return url[..997] + "…";
    }

    /// <summary>
    /// 构建 items-changed 事件的占位符数据
    /// </summary>
    private Dictionary<string, string> BuildChangeTokens(
        NeuBellWebHookChange change,
        string defaultPayload)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [NeuBellWebHookTemplate.TokenPayload] = defaultPayload,
            ["kind"] = EventKindItemsChanged,
            ["provider"] = change.ProviderId ?? string.Empty,
            ["providerName"] = string.IsNullOrWhiteSpace(change.DisplayName)
                ? change.ProviderId ?? string.Empty
                : change.DisplayName,
            ["time"] = change.OccurredAt.ToString("o"),
            ["addedCount"] = change.Added.Count.ToString(),
            ["removedCount"] = change.Removed.Count.ToString(),
            ["addedTitles"] = string.Join("、", change.Added.Select(item => item.Title)),
            ["removedTitles"] = string.Join("、", change.Removed.Select(item => item.Title))
        };
        // 条目占位符取第一条新增（无新增时取第一条移除）
        var item = change.Added.FirstOrDefault() ?? change.Removed.FirstOrDefault();
        AddItemTokens(tokens, item);
        return tokens;
    }

    /// <summary>
    /// 构建 item-created 事件的占位符数据
    /// </summary>
    private Dictionary<string, string> BuildItemCreatedTokens(
        NeuBellItem item,
        string providerName,
        string defaultPayload)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [NeuBellWebHookTemplate.TokenPayload] = defaultPayload,
            ["kind"] = EventKindItemCreated,
            ["provider"] = providerName ?? string.Empty,
            ["providerName"] = providerName ?? string.Empty,
            ["time"] = DateTimeOffset.Now.ToString("o"),
            ["count"] = "1"
        };
        AddItemTokens(tokens, item);
        return tokens;
    }

    /// <summary>
    /// 构建 test 事件的占位符数据
    /// </summary>
    private static Dictionary<string, string> BuildTestTokens(
        NeuBellWebHook webHook,
        string defaultPayload)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [NeuBellWebHookTemplate.TokenPayload] = defaultPayload,
            ["kind"] = EventKindTest,
            ["provider"] = webHook.ProviderFilter ?? string.Empty,
            ["providerName"] = webHook.Name ?? string.Empty,
            ["time"] = DateTimeOffset.Now.ToString("o")
        };
    }

    private void AddItemTokens(
        Dictionary<string, string> tokens,
        NeuBellItem item)
    {
        if (item == null)
        {
            return;
        }
        tokens["id"] = item.Id ?? string.Empty;
        tokens["title"] = item.Title ?? string.Empty;
        tokens["summary"] = item.Summary ?? string.Empty;
        tokens["link"] = ToAbsoluteLink(item.DetailUrl);
        tokens["status"] = item.Severity ?? string.Empty;
        tokens["count"] = (item.Count).ToString();
        tokens["updated"] = item.UpdatedAt.ToString("o");
    }

    /// <summary>
    /// 把条目的相对链接拼成绝对链接（配置了站点根地址时）
    /// </summary>
    private string ToAbsoluteLink(string detailUrl)
    {
        var url = detailUrl ?? string.Empty;
        if (url.Length == 0 || _linkBaseUrl.Length == 0 || !url.StartsWith("/", StringComparison.Ordinal))
        {
            return url;
        }
        return _linkBaseUrl.TrimEnd('/') + url;
    }

    /// <summary>
    /// 构建通知报文（test=true 时为测试事件）
    /// </summary>
    public static string BuildPayload(NeuBellWebHookChange change, bool test)
    {
        return JsonSerializer.Serialize(new
        {
            source = "NeuBell",
            kind = test ? EventKindTest : EventKindItemsChanged,
            providerId = change.ProviderId,
            moduleUid = change.ModuleUid,
            displayName = change.DisplayName,
            tenantId = change.TenantId,
            occurredAt = change.OccurredAt,
            added = (IReadOnlyList<NeuBellItem>)(test ? Array.Empty<NeuBellItem>() : change.Added),
            removed = (IReadOnlyList<NeuBellItem>)(test ? Array.Empty<NeuBellItem>() : change.Removed)
        }, JsonOptions);
    }

    /// <summary>
    /// HMAC-SHA256 签名：X-NeuBell-Signature: t=&lt;unix&gt;,v1=&lt;hex&gt;（v1 = HMAC(secret, t + "." + body)）
    /// </summary>
    public static string SignPayload(string secret, string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var message = Encoding.UTF8.GetBytes(timestamp + "." + payload);
        var key = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(key);
        var signature = Convert.ToHexString(hmac.ComputeHash(message)).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    private sealed class ProviderObservation
    {
        private readonly object _syncRoot = new();
        private Dictionary<string, NeuBellItem> _items;
        public bool HasBaseline { get; private set; }

        public IReadOnlyDictionary<string, NeuBellItem> Items
        {
            get { lock (_syncRoot) { return _items ?? new Dictionary<string, NeuBellItem>(StringComparer.OrdinalIgnoreCase); } }
        }

        public (List<NeuBellItem> added, List<NeuBellItem> removed) Diff(
            Dictionary<string, NeuBellItem> current)
        {
            lock (_syncRoot)
            {
                var baseline = _items ?? new Dictionary<string, NeuBellItem>(StringComparer.OrdinalIgnoreCase);
                var added = current.Values
                    .Where(item => !baseline.ContainsKey(item.Id))
                    .ToList();
                var removed = baseline.Values
                    .Where(item => !current.ContainsKey(item.Id))
                    .ToList();
                return (added, removed);
            }
        }

        public void SetBaseline(Dictionary<string, NeuBellItem> items)
        {
            lock (_syncRoot)
            {
                _items = new Dictionary<string, NeuBellItem>(items, StringComparer.OrdinalIgnoreCase);
                HasBaseline = true;
            }
        }
    }
}
