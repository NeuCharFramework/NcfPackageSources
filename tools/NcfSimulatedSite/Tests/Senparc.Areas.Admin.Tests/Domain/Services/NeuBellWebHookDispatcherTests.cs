/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookDispatcherTests.cs
    文件功能描述：NeuBell WebHook 分发器测试：
    基线建立、新增/移除检测、Provider 过滤、签名与测试事件

    创建标识：Senparc - 20260906

----------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuBellWebHookDispatcherTests
{
    [TestMethod]
    public async Task ObserveAsync_FirstObservation_ShouldEstablishBaselineWithoutDispatch()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new()
        {
            CreateHook("hook-a", "https://example.com/hook-a")
        });
        var snapshots = new[] { Snapshot("provider-a", ("item-1", "任务一")) };

        var changes = await dispatcher.ObserveAsync(snapshots, new[] { "provider-a" });

        Assert.AreEqual(0, changes.Count);
        await Task.Delay(200);
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task ObserveAsync_AddedItems_ShouldDispatchOnlyToMatchingHooks()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all", secret: "secret-a"),
            CreateHook("hook-other", "https://example.com/hook-other", providerFilter: "provider-b"),
            CreateHook("hook-no-add", "https://example.com/hook-no-add", providerFilter: "provider-a", notifyOnAdd: false)
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a") }, new[] { "provider-a" });
        var changes = await dispatcher.ObserveAsync(
            new[] { Snapshot("provider-a", ("item-1", "任务一"), ("item-2", "任务二")) },
            new[] { "provider-a" });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(2, changes[0].Added.Count);
        await handler.WaitUntilAsync(1);

        // 只有“全部 Provider + 允许新增”的 hook-all 收到通知
        CollectionAssert.Contains(
            handler.Requests.Select(request => request.Url).ToList(),
            "https://example.com/hook-all");
        Assert.AreEqual(1, handler.Requests.Count);

        var captured = handler.Requests.Single();
        Assert.IsTrue(captured.Body.Contains("items-changed"));
        Assert.IsTrue(captured.Body.Contains("item-1"));
        Assert.IsTrue(captured.Body.Contains("item-2"));
        Assert.IsNotNull(captured.Signature, "配置了 Secret 的 WebHook 必须携带签名头");
    }

    [TestMethod]
    public async Task ObserveAsync_RemovedItems_ShouldDispatchRemovalNotification()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });
        var changes = await dispatcher.ObserveAsync(new[] { Snapshot("provider-a") }, new[] { "provider-a" });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(1, changes[0].Removed.Count);
        Assert.AreEqual("item-1", changes[0].Removed[0].Id);
        await handler.WaitUntilAsync(1);
        Assert.IsTrue(handler.Requests.Single().Body.Contains("\"item-1\""));
    }

    [TestMethod]
    public async Task ObserveAsync_NoChange_ShouldNotDispatch()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });
        var changes = await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });

        Assert.AreEqual(0, changes.Count);
        await Task.Delay(200);
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task ObserveAsync_ExpectedProviderMissingThisRound_ShouldKeepBaseline()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });
        // 本轮快照获取失败（provider 仍在 expected 中）：保留基线，不误报移除
        var changes = await dispatcher.ObserveAsync(Array.Empty<NeuBellSnapshot>(), new[] { "provider-a" });

        Assert.AreEqual(0, changes.Count);
        await Task.Delay(200);
        Assert.AreEqual(0, handler.Requests.Count);

        // 恢复后只把真正的差集报为新增
        var recovered = await dispatcher.ObserveAsync(
            new[] { Snapshot("provider-a", ("item-1", "任务一"), ("item-2", "任务二")) },
            new[] { "provider-a" });
        CollectionAssert.AreEquivalent(
            new[] { "item-2" },
            recovered.SelectMany(change => change.Added).Select(item => item.Id).ToList());
    }

    [TestMethod]
    public async Task ObserveAsync_ProviderNoLongerExpected_ShouldFireRemoval()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });
        // 模块被关闭（不在 expected 中）：存量条目按移除通知
        var changes = await dispatcher.ObserveAsync(Array.Empty<NeuBellSnapshot>(), Array.Empty<string>());

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(1, changes[0].Removed.Count);
        await handler.WaitUntilAsync(1);
        Assert.IsTrue(handler.Requests.Single().Body.Contains("\"item-1\""));
    }

    [TestMethod]
    public async Task ObserveAsync_NullExpectedProviders_ShouldNotJudgeRemoval()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) });
        // expectedProviderIds 为 null（无法判定开放状态）：缺席一律不做移除判断
        var changes = await dispatcher.ObserveAsync(Array.Empty<NeuBellSnapshot>());

        Assert.AreEqual(0, changes.Count);
        await Task.Delay(200);
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task SendTestAsync_ShouldPostTestEventWithoutItems()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new());
        var webHook = CreateHook("hook-all", "https://example.com/hook-test", providerFilter: "provider-a", secret: "s3cr3t");

        var (success, message) = await dispatcher.SendTestAsync(webHook);

        Assert.IsTrue(success, message);
        Assert.AreEqual(1, handler.Requests.Count);
        var captured = handler.Requests.Single();
        Assert.AreEqual("https://example.com/hook-test", captured.Url);
        Assert.IsTrue(captured.Body.Contains("\"test\""));
        Assert.IsFalse(captured.Body.Contains("items-changed"));
        Assert.IsNotNull(captured.Signature);
    }

    [TestMethod]
    public async Task SendTestAsync_InvalidUrl_ShouldFailWithoutRequest()
    {
        var (dispatcher, handler, _) = CreateDispatcher(new());
        var webHook = CreateHook("bad", "ftp://example.com/hook");

        var (success, message) = await dispatcher.SendTestAsync(webHook);

        Assert.IsFalse(success);
        Assert.IsFalse(string.IsNullOrEmpty(message));
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public void SignPayload_ShouldProduceTimestampAndHmacSha256()
    {
        const string payload = "{\"source\":\"NeuBell\"}";
        const string secret = "unit-test-secret";

        var signature = NeuBellWebHookDispatcher.SignPayload(secret, payload);

        Match match = Regex.Match(
            signature, @"^t=(\d+),v1=([0-9a-f]{64})$");
        Assert.IsTrue(match.Success, $"签名格式不符合 t=<unix>,v1=<hex>：{signature}");

        var expected = Encoding.UTF8.GetBytes(match.Groups[1].Value + "." + payload);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedHex = Convert.ToHexString(hmac.ComputeHash(expected)).ToLowerInvariant();
        Assert.AreEqual(expectedHex, match.Groups[2].Value, ignoreCase: true);
    }

    [TestMethod]
    public void BuildPayload_TestEvent_ShouldUseTestKindAndEmptyLists()
    {
        var payload = NeuBellWebHookDispatcher.BuildPayload(new NeuBellWebHookChange
        {
            ProviderId = "provider-a",
            ModuleUid = "module-a",
            DisplayName = "测试",
            OccurredAt = DateTimeOffset.UtcNow,
            Added = new[] { new NeuBellItem("item-1", "任务一", "摘要", 1, "info", "/detail", DateTimeOffset.UtcNow) },
            Removed = Array.Empty<NeuBellItem>()
        }, test: true);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.AreEqual("NeuBell", root.GetProperty("source").GetString());
        Assert.AreEqual("test", root.GetProperty("kind").GetString());
        Assert.AreEqual(0, root.GetProperty("added").GetArrayLength());
        Assert.AreEqual(0, root.GetProperty("removed").GetArrayLength());
    }

    [TestMethod]
    public void TryValidateUrl_ShouldAcceptHttpAndHttpsOnly()
    {
        Assert.IsTrue(NeuBellWebHook.TryValidateUrl("https://example.com/api/hook", out _));
        Assert.IsTrue(NeuBellWebHook.TryValidateUrl("http://127.0.0.1:5000/hook", out _));

        Assert.IsFalse(NeuBellWebHook.TryValidateUrl("ftp://example.com/hook", out var ftpError));
        StringAssert.Contains(ftpError, "http/https");
        Assert.IsFalse(NeuBellWebHook.TryValidateUrl("/relative/path", out _));
        Assert.IsFalse(NeuBellWebHook.TryValidateUrl("  ", out _));
        Assert.IsFalse(NeuBellWebHook.TryValidateUrl(null, out _));
    }

    [TestMethod]
    public void MatchesProvider_ShouldMatchEmptyFilterToAll()
    {
        var allHooks = CreateHook("all", "https://example.com/hook");
        var filtered = CreateHook("filtered", "https://example.com/hook", providerFilter: "provider-a");

        Assert.IsTrue(allHooks.MatchesProvider("provider-a"));
        Assert.IsTrue(allHooks.MatchesProvider("provider-z"));
        Assert.IsTrue(filtered.MatchesProvider("provider-a"));
        Assert.IsTrue(filtered.MatchesProvider("PROVIDER-A"));
        Assert.IsFalse(filtered.MatchesProvider("provider-b"));
    }

    private static NeuBellWebHook CreateHook(
        string name,
        string url,
        string providerFilter = null,
        string secret = null,
        bool notifyOnAdd = true,
        bool notifyOnRemove = true)
    {
        var webHook = new NeuBellWebHook(name, url, adminUserId: 1);
        webHook.UpdateInfo(
            name,
            url,
            providerFilter ?? string.Empty,
            secret ?? string.Empty,
            notifyOnAdd,
            notifyOnRemove,
            isEnabled: true);
        return webHook;
    }

    private static NeuBellSnapshot Snapshot(string providerId, params (string id, string title)[] items)
    {
        return new NeuBellSnapshot(
            providerId,
            "module-test",
            providerId,
            "fa fa-bell",
            true,
            items
                .Select(item => new NeuBellItem(item.id, item.title, "摘要", 1, "info", "/detail", DateTimeOffset.UtcNow))
                .ToList());
    }

    private static (NeuBellWebHookDispatcher dispatcher, CaptureHandler handler, FakeNeuBellWebHookService fake)
        CreateDispatcher(List<NeuBellWebHook> webHooks)
    {
        var handler = new CaptureHandler();
        var fake = new FakeNeuBellWebHookService(webHooks);
        var services = new ServiceCollection();
        services.AddScoped<INeuBellWebHookService>(_ => fake);
        var provider = services.BuildServiceProvider();
        var dispatcher = new NeuBellWebHookDispatcher(
            provider,
            new FakeHttpClientFactory(handler),
            NullLogger<NeuBellWebHookDispatcher>.Instance);
        return (dispatcher, handler, fake);
    }

    private sealed class FakeNeuBellWebHookService : INeuBellWebHookService
    {
        private readonly List<NeuBellWebHook> _webHooks;

        public FakeNeuBellWebHookService(List<NeuBellWebHook> webHooks)
        {
            _webHooks = webHooks;
        }

        public Task<List<NeuBellWebHook>> GetEnabledListAsync()
        {
            return Task.FromResult(_webHooks.Where(webHook => webHook.IsEnabled).ToList());
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false);
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public sealed class CapturedRequest
        {
            public string Url { get; init; }
            public string Body { get; init; }
            public string Signature { get; init; }
        }

        private readonly ConcurrentQueue<CapturedRequest> _requests = new();

        public IReadOnlyCollection<CapturedRequest> Requests => _requests.ToArray();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _requests.Enqueue(new CapturedRequest
            {
                Url = request.RequestUri?.ToString(),
                Body = body,
                Signature = request.Headers.TryGetValues("X-NeuBell-Signature", out var values)
                    ? string.Join(",", values)
                    : null
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        }

        public async Task WaitUntilAsync(int expectedCount, int timeoutMilliseconds = 5000)
        {
            var start = Environment.TickCount64;
            while (Requests.Count < expectedCount)
            {
                if (Environment.TickCount64 - start > timeoutMilliseconds)
                {
                    break;
                }
                await Task.Delay(25).ConfigureAwait(false);
            }
        }
    }
}