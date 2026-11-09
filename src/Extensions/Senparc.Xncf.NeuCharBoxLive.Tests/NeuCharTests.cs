/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharTests.cs
    文件功能描述：NeuCharBox Live 测试：NeuCharTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using Senparc.Xncf.NeuCharBoxLive.NeuChar;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// The NeuChar client without the network: every handler here is a fake, and the key is a made-up one. Nothing reads
/// NEUCHAR_API_KEY.
/// </summary>
[TestClass]
public class NeuCharTests
{
    const string FakeKey = "sk-SECRET123";
    static readonly NeuCharOptions Opt = new("https://www.neuchar.com/19194/", "gpt-5-chat");

    static readonly TimeSpan[] Backoff = { TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };

    const string Completion = """
        {"id":"chatcmpl-1","object":"chat.completion","created":1762500000,"model":"gpt-5-chat",
         "choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}],
         "usage":{"prompt_tokens":3,"completion_tokens":1,"total_tokens":4}}
        """;

    /// <summary>Each attempt takes the next step (the last one repeats). A step throws to act as a failed connect.</summary>
    sealed class StepHandler(params Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>[] steps) : HttpMessageHandler
    {
        int _attempts;
        public int Attempts => Volatile.Read(ref _attempts);
        public List<Uri?> Uris { get; } = new();
        public List<bool> HadKeyHeader { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var i = Interlocked.Increment(ref _attempts) - 1;
            lock (Uris)
            {
                Uris.Add(request.RequestUri);
                HadKeyHeader.Add(request.Headers.Contains("api-key")); // presence only: the value is never read
            }
            try { return Task.FromResult(steps[Math.Min(i, steps.Length - 1)](request, ct)); }
            catch (Exception e) { return Task.FromException<HttpResponseMessage>(e); }
        }
    }

    static Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> Throw(HttpRequestError error) =>
        (_, _) => throw new HttpRequestException(error, $"fake {error}");

    static Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> Status(HttpStatusCode code, string body = "{}") =>
        (_, _) => new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>SocketsHttpHandler's ConnectTimeout: a TaskCanceledException over a TimeoutException, the caller's token untouched.</summary>
    static readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> ConnectTimeout =
        (_, _) => throw new TaskCanceledException("connect timed out", new TimeoutException("A connection could not be established within the configured ConnectTimeout."));

    static (HttpMessageInvoker Invoker, StepHandler Inner, List<TimeSpan> Delays) Retrying(params Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>[] steps)
    {
        var inner = new StepHandler(steps);
        var delays = new List<TimeSpan>();
        var handler = new ConnectRetryHandler(3, (d, _) => { delays.Add(d); return Task.CompletedTask; }) { InnerHandler = inner };
        return (new HttpMessageInvoker(handler), inner, delays);
    }

    static Task<HttpResponseMessage> Send(HttpMessageInvoker invoker, CancellationToken ct = default) =>
        invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://www.neuchar.com/19194/x") { Content = new StringContent("{}") }, ct);

    // ---- ConnectRetryHandler ----

    [TestMethod]
    public async Task Retry_ConnectFailures_UpTo3()
    {
        var (ok, okInner, okDelays) = Retrying(Throw(HttpRequestError.ConnectionError), Throw(HttpRequestError.ConnectionError), Status(HttpStatusCode.OK));
        using (var r = await Send(ok)) Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
        Assert.AreEqual(3, okInner.Attempts);
        CollectionAssert.AreEqual(Backoff[..2], okDelays);

        var (fail, failInner, failDelays) = Retrying(Throw(HttpRequestError.ConnectionError));
        var e = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Send(fail));
        Assert.AreEqual(HttpRequestError.ConnectionError, e.HttpRequestError);
        Assert.AreEqual(4, failInner.Attempts);
        CollectionAssert.AreEqual(Backoff, failDelays);
    }

    [TestMethod]
    public async Task Retry_HandshakeDnsAndConnectTimeout_AreRetried()
    {
        var (inv, inner, delays) = Retrying(Throw(HttpRequestError.SecureConnectionError), Throw(HttpRequestError.NameResolutionError),
            ConnectTimeout, Status(HttpStatusCode.OK));
        using (var r = await Send(inv)) Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
        Assert.AreEqual(4, inner.Attempts);
        CollectionAssert.AreEqual(Backoff, delays);
    }

    [TestMethod]
    public async Task Retry_NeverAfterResponse()
    {
        foreach (var code in new[] { HttpStatusCode.InternalServerError, HttpStatusCode.Forbidden, HttpStatusCode.ServiceUnavailable, HttpStatusCode.TooManyRequests })
        {
            var (inv, inner, delays) = Retrying(Status(code, "AI 应用不可用或已暂时停用"), Status(HttpStatusCode.OK));
            using var r = await Send(inv);
            Assert.AreEqual(code, r.StatusCode, "returned as is");
            Assert.AreEqual(1, inner.Attempts, code.ToString());
            Assert.AreEqual(0, delays.Count);
        }
    }

    [TestMethod]
    public async Task Retry_FailuresAfterTheRequestWasSent_AreNotRetried()
    {
        // The connection was up, so the request may have reached the server: never send it twice.
        foreach (var error in new[] { HttpRequestError.ResponseEnded, HttpRequestError.Unknown, HttpRequestError.HttpProtocolError, HttpRequestError.InvalidResponse })
        {
            var (inv, inner, delays) = Retrying(Throw(error), Status(HttpStatusCode.OK));
            var e = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Send(inv), error.ToString());
            Assert.AreEqual(error, e.HttpRequestError);
            Assert.AreEqual(1, inner.Attempts, error.ToString());
            Assert.AreEqual(0, delays.Count);
        }

        var (io, ioInner, _) = Retrying((_, _) => throw new IOException("reset mid-response"), Status(HttpStatusCode.OK));
        await Assert.ThrowsExceptionAsync<IOException>(() => Send(io));
        Assert.AreEqual(1, ioInner.Attempts);
    }

    [TestMethod]
    public async Task Retry_CallerCancelOrClientTimeout_IsNotRetried()
    {
        // HttpClient.Timeout and the runner's budget cancel the token they pass in; only the connect timeout is retried.
        using var cts = new CancellationTokenSource();
        var (inv, inner, delays) = Retrying((_, ct) =>
        {
            cts.Cancel();
            throw new TaskCanceledException("cancelled", new TimeoutException("HttpClient.Timeout"), ct);
        }, Status(HttpStatusCode.OK));
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => Send(inv, cts.Token));
        Assert.AreEqual(1, inner.Attempts);
        Assert.AreEqual(0, delays.Count);

        var (plain, plainInner, _) = Retrying((_, _) => throw new TaskCanceledException("no inner timeout"), Status(HttpStatusCode.OK));
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => Send(plain));
        Assert.AreEqual(1, plainInner.Attempts);
    }

    // ---- ApiKeySource ----

    [TestMethod]
    public void ApiKey_ProcessFirst_UserFallbackOnlyOnWindows()
    {
        Assert.AreEqual("p", ApiKeySource.Read(() => "p", () => "u"));
        Assert.AreEqual("p", ApiKeySource.Read(() => " p\n", () => "u"), "trimmed");
        Assert.AreEqual(OperatingSystem.IsWindows() ? "u" : null, ApiKeySource.Read(() => null, () => "u"));
        Assert.AreEqual(OperatingSystem.IsWindows() ? "u" : null, ApiKeySource.Read(() => "  ", () => "u"));
        Assert.IsNull(ApiKeySource.Read(() => null, () => ""));
        Assert.IsNull(ApiKeySource.Read(() => null, null));
    }

    // ---- NeuCharChatClientFactory ----

    [TestMethod]
    public void Factory_NoKey_ReturnsNull()
    {
        Assert.IsNull(NeuCharChatClientFactory.Create(Opt, null));
        Assert.IsNull(NeuCharChatClientFactory.Create(Opt, ""));
        Assert.IsNull(NeuCharChatClientFactory.Create(Opt, "   "));
    }

    [TestMethod]
    public void Factory_ErrorsNeverCarryKey()
    {
        var bad = new[]
        {
            new NeuCharOptions("not a url", "gpt-5-chat"),
            new NeuCharOptions("", "gpt-5-chat"),
            new NeuCharOptions("/19194/", "gpt-5-chat"),
            new NeuCharOptions("ftp://www.neuchar.com/19194/", "gpt-5-chat"),
            new NeuCharOptions("https://www.neuchar.com/19194/", ""),
        };
        foreach (var o in bad)
        {
            var e = Assert.ThrowsException<ArgumentException>(() => NeuCharChatClientFactory.Create(o, FakeKey), o.ToString());
            StringAssert.DoesNotMatch(e.ToString(), new System.Text.RegularExpressions.Regex("SECRET123"), o.ToString());
        }
        Assert.IsFalse(Opt.ToString().Contains("SECRET"), "the options have no key member");
    }

    [TestMethod]
    public async Task Factory_BuildsDeploymentUrl()
    {
        foreach (var endpoint in new[] { "https://www.neuchar.com/19194/", "https://www.neuchar.com/19194" })
        {
            var inner = new StepHandler(Status(HttpStatusCode.OK, Completion));
            var client = NeuCharChatClientFactory.Create(Opt with { Endpoint = endpoint }, FakeKey, inner);
            Assert.IsNotNull(client);
            var resp = await client.GetResponseAsync("hi");
            Assert.AreEqual("ok", resp.Text);
            Assert.AreEqual(1, inner.Attempts);
            Assert.AreEqual("https://www.neuchar.com/19194/openai/deployments/gpt-5-chat/chat/completions?api-version=2025-04-01-preview",
                inner.Uris[0]?.ToString(), endpoint);
            Assert.IsTrue(inner.HadKeyHeader[0], "api-key header present");
        }
    }

    [TestMethod]
    public async Task Factory_ServerErrors_AreNeverRetried()
    {
        // The SDK's own retry policy would resend a 429 or 5xx: it is off, so a request that reached NeuChar goes once.
        foreach (var code in new[] { HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable, HttpStatusCode.TooManyRequests, HttpStatusCode.Forbidden })
        {
            var inner = new StepHandler(Status(code, """{"error":{"message":"AI 应用不可用或已暂时停用"}}"""), Status(HttpStatusCode.OK, Completion));
            var client = NeuCharChatClientFactory.Create(Opt, FakeKey, inner)!;
            var e = await Assert.ThrowsExceptionAsync<System.ClientModel.ClientResultException>(() => client.GetResponseAsync("hi"), code.ToString());
            Assert.AreEqual((int)code, e.Status);
            Assert.AreEqual(1, inner.Attempts, code.ToString());
            StringAssert.DoesNotMatch(e.ToString(), new System.Text.RegularExpressions.Regex("SECRET123"));
        }
    }

    [TestMethod]
    public void Factory_Transport_ConnectTimeoutAndKeepAlive()
    {
        using var s = NeuCharChatClientFactory.NewSockets();
        // About half the handshakes to neuchar.com stall; good ones finish in under 1.5 s. Seven tries of 6 s plus the
        // 9.5 s of backoff stay well inside the 90 s request timeout.
        Assert.AreEqual(TimeSpan.FromSeconds(6), s.ConnectTimeout);
        Assert.AreEqual(6, NeuCharChatClientFactory.ConnectRetries);
        Assert.IsTrue(NeuCharChatClientFactory.ConnectTimeout * (NeuCharChatClientFactory.ConnectRetries + 1)
            + TimeSpan.FromSeconds(9.5) < NeuCharChatClientFactory.RequestTimeout - TimeSpan.FromSeconds(30), "room left for the model");
        Assert.AreEqual(TimeSpan.FromSeconds(90), s.PooledConnectionIdleTimeout, "idle connections stay open for reuse");
        Assert.AreEqual(TimeSpan.FromMinutes(10), s.PooledConnectionLifetime);
        Assert.IsTrue(s.MaxConnectionsPerServer > 1);
    }

    [TestMethod]
    public void Factory_Transport_NeverResumesTls()
    {
        // Resumed TLS sessions to neuchar.com stall until the timeout about half the time (2026-11-07: 7 of 12 resumed
        // requests stalled, 0 of 6 with resumption off, 0 of ~40 from curl, which never resumes).
        using var s = NeuCharChatClientFactory.NewSockets();
        Assert.IsNotNull(s.SslOptions);
        Assert.IsFalse(s.SslOptions.AllowTlsResume, "every connection to NeuChar does a full TLS handshake");
    }
}
