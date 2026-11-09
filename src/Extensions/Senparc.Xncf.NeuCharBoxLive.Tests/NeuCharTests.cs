/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharTests.cs
    文件功能描述：测试 ConnectRetryHandler：连接失败与响应前连接重置的重试次数与不重试的情形


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 只保留 ConnectRetryHandler 的测试，加入响应前连接重置的重试测试（ncb/live 90321e1）；回环测试改走 AiKernelChatClients

----------------------------------------------------------------*/

using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.AI;
using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.NeuCharBoxLive.Api.Model;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// The model transport's retry handler (<see cref="ConnectRetryHandler"/>) without the network: every handler here is a
/// fake, except the loopback tests at the end, which talk to a server on 127.0.0.1 only. The key is a made-up one.
/// </summary>
[TestClass]
public class NeuCharTests
{
    const string FakeKey = "sk-SECRET123";

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

    /// <summary>
    /// What SocketsHttpHandler's SendAsync throws when the peer resets (10054) or aborts (10053) the connection before the
    /// response arrived, as seen from neuchar.com on 2026-11-08: no status code, an IOException from the read, the socket error.
    /// </summary>
    static Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> Reset(SocketError code = SocketError.ConnectionReset) =>
        (_, _) => throw ResetError(code);

    static HttpRequestException ResetError(SocketError code = SocketError.ConnectionReset) =>
        new(HttpRequestError.Unknown, "An error occurred while sending the request.",
            new IOException("Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.",
                new SocketException((int)code)));

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
        // The connection was up, so the request may have reached the server: never send it twice. The one exception is
        // a reset or abort before the response (below).
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

    [TestMethod]
    public async Task Retry_ResetBeforeResponse_UpTo2()
    {
        var (once, onceInner, onceDelays) = Retrying(Reset(), Status(HttpStatusCode.OK));
        using (var r = await Send(once)) Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
        Assert.AreEqual(2, onceInner.Attempts);
        CollectionAssert.AreEqual(Backoff[..1], onceDelays);

        var (twice, twiceInner, twiceDelays) = Retrying(Reset(SocketError.ConnectionAborted), Reset(), Status(HttpStatusCode.OK));
        using (var r = await Send(twice)) Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
        Assert.AreEqual(3, twiceInner.Attempts);
        CollectionAssert.AreEqual(Backoff[..2], twiceDelays);

        var (fail, failInner, failDelays) = Retrying(Reset());
        var e = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Send(fail));
        Assert.AreEqual(SocketError.ConnectionReset, ((SocketException)e.InnerException!.InnerException!).SocketErrorCode, "the last reset is passed on as is");
        Assert.AreEqual(3, failInner.Attempts, "two retries, then the third reset fails the call");
        CollectionAssert.AreEqual(Backoff[..2], failDelays);
    }

    [TestMethod]
    public async Task Retry_ResetsAndConnectFailures_CountSeparately()
    {
        // Connect failures are free (nothing was sent) and keep their 3 retries here; resets keep their own 2.
        var (inv, inner, delays) = Retrying(Reset(), Throw(HttpRequestError.ConnectionError), Reset(), Throw(HttpRequestError.ConnectionError),
            Status(HttpStatusCode.OK));
        using (var r = await Send(inv)) Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
        Assert.AreEqual(5, inner.Attempts);
        CollectionAssert.AreEqual(new[] { Backoff[0], Backoff[0], Backoff[1], Backoff[1] }, delays);

        // A reset during the TCP connect or the TLS handshake is a connect failure: its retries end at 3, not 3 + 2.
        foreach (var error in new[] { HttpRequestError.ConnectionError, HttpRequestError.SecureConnectionError })
        {
            var (c, cInner, _) = Retrying((_, _) => throw new HttpRequestException(error, "connect reset", ResetError().InnerException));
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Send(c), error.ToString());
            Assert.AreEqual(4, cInner.Attempts, error.ToString());
        }
    }

    [TestMethod]
    public async Task Retry_OtherTransportFailures_AreNotRetried()
    {
        var cases = new (string Name, Exception Error)[]
        {
            ("EOF, no socket error", new HttpRequestException(HttpRequestError.Unknown, "send failed",
                new IOException("Received an unexpected EOF or 0 bytes from the transport stream."))),
            ("socket timeout, not a reset", new HttpRequestException(HttpRequestError.Unknown, "send failed",
                new IOException("timed out", new SocketException((int)SocketError.TimedOut)))),
            ("a status code means a response came", new HttpRequestException("Response status code does not indicate success: 502.",
                ResetError().InnerException, HttpStatusCode.BadGateway)),
            ("a bare IOException is not SocketsHttpHandler's shape", new IOException("reset", new SocketException((int)SocketError.ConnectionReset))),
        };
        foreach (var (name, error) in cases)
        {
            var (inv, inner, delays) = Retrying((_, _) => throw error, Status(HttpStatusCode.OK));
            try { using var _ = await Send(inv); Assert.Fail(name + ": expected the failure to pass through"); }
            catch (Exception e) when (e is not AssertFailedException) { Assert.AreSame(error, e, name); }
            Assert.AreEqual(1, inner.Attempts, name);
            Assert.AreEqual(0, delays.Count, name);
        }
    }

    [TestMethod]
    public async Task Retry_ResetAfterCallerCancel_IsNotRetried()
    {
        // The runner's work budget and HttpClient.Timeout cancel the token: a reset that lands after that is not retried.
        using var cts = new CancellationTokenSource();
        var (inv, inner, delays) = Retrying((_, _) => { cts.Cancel(); throw ResetError(); }, Status(HttpStatusCode.OK));
        await Assert.ThrowsExceptionAsync<HttpRequestException>(() => Send(inv, cts.Token));
        Assert.AreEqual(1, inner.Attempts);
        Assert.AreEqual(0, delays.Count);
    }

    // ---- the real transport on loopback: SocketsHttpHandler, the SDK, and a server that resets ----

    /// <summary>
    /// A plain-HTTP/1.1 server on 127.0.0.1. Connection n runs script n (the last one repeats) once the whole request has
    /// been read, so a reset lands while the client waits for the response, as in the 2026-11-08 failures.
    /// </summary>
    sealed class LoopbackServer : IAsyncDisposable
    {
        readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        readonly Func<Socket, Task>[] _scripts;
        readonly CancellationTokenSource _stop = new();
        readonly Task _loop;
        int _connections;

        public List<string> Bodies { get; } = new();
        public int Connections => Volatile.Read(ref _connections);
        public string Endpoint => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/19194/";

        public LoopbackServer(params Func<Socket, Task>[] scripts)
        {
            _scripts = scripts;
            _listener.Start();
            _loop = LoopAsync();
        }

        async Task LoopAsync()
        {
            try
            {
                while (true)
                {
                    var s = await _listener.AcceptSocketAsync(_stop.Token);
                    var i = Interlocked.Increment(ref _connections) - 1;
                    var body = await ReadRequestAsync(s, _stop.Token);
                    lock (Bodies) Bodies.Add(body);
                    await _scripts[Math.Min(i, _scripts.Length - 1)](s);
                }
            }
            catch (OperationCanceledException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        /// <summary>The body of one request (Content-Length or chunked, kept raw).</summary>
        static async Task<string> ReadRequestAsync(Socket s, CancellationToken ct)
        {
            var data = new List<byte>();
            var buf = new byte[8192];
            int headerEnd;
            while ((headerEnd = IndexOf(data, "\r\n\r\n"u8.ToArray())) < 0) await ReadMore();
            var head = Encoding.ASCII.GetString(data.GetRange(0, headerEnd).ToArray());
            var bodyStart = headerEnd + 4;
            var length = head.Split("\r\n").Select(l => l.Split(':', 2)).Where(p => p.Length == 2 && p[0].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                .Select(p => (int?)int.Parse(p[1].Trim())).FirstOrDefault();
            if (length is int n) { while (data.Count - bodyStart < n) await ReadMore(); }
            else if (head.Contains("chunked", StringComparison.OrdinalIgnoreCase)) { while (IndexOf(data, "0\r\n\r\n"u8.ToArray(), bodyStart) < 0) await ReadMore(); }
            return Encoding.UTF8.GetString(data.GetRange(bodyStart, data.Count - bodyStart).ToArray());

            async Task ReadMore()
            {
                var got = await s.ReceiveAsync(buf, SocketFlags.None, ct);
                if (got == 0) throw new SocketException((int)SocketError.ConnectionAborted);
                data.AddRange(buf.AsSpan(0, got).ToArray());
            }
        }

        static int IndexOf(List<byte> data, byte[] needle, int from = 0)
        {
            for (var i = from; i + needle.Length <= data.Count; i++)
                if (data.Skip(i).Take(needle.Length).SequenceEqual(needle)) return i;
            return -1;
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();
            await _loop;
            _stop.Dispose();
        }
    }

    /// <summary>An abortive close: Windows sends RST, and the client's read fails with 10054.</summary>
    static Task ResetConnection(Socket s)
    {
        s.LingerState = new LingerOption(true, 0);
        s.Close();
        return Task.CompletedTask;
    }

    static string ResponseHead(string json) =>
        $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {Encoding.UTF8.GetByteCount(json)}\r\nConnection: close\r\n\r\n";

    static Func<Socket, Task> Answer(string json) => async s =>
    {
        await s.SendAsync(Encoding.UTF8.GetBytes(ResponseHead(json) + json), SocketFlags.None);
        s.Shutdown(SocketShutdown.Both);
        s.Close();
    };

    /// <summary>The status line, the headers and half the body, then a reset: the response had started.</summary>
    static Func<Socket, Task> HalfThenReset(string json) => async s =>
    {
        var body = Encoding.UTF8.GetBytes(json);
        await s.SendAsync(Encoding.UTF8.GetBytes(ResponseHead(json)).Concat(body[..(body.Length / 2)]).ToArray(), SocketFlags.None);
        await Task.Delay(500); // let the client read the headers and start on the body before the reset
        await ResetConnection(s);
    };

    /// <summary>A NeuCharAI chat model as AI Kernel stores it, at <paramref name="endpoint"/>, on the real transport.</summary>
    static IChatClient LoopbackClient(string endpoint) =>
        new AiKernelChatClients().Resolve(new AIModelDto
        {
            Id = 1, AiPlatform = AiPlatform.NeuCharAI, ConfigModelType = ConfigModelType.Chat, Endpoint = endpoint,
            DeploymentName = "gpt-5-chat", ModelId = "gpt-5-chat", ApiKey = FakeKey, ApiVersion = "2025-04-01-preview",
        }).Model!.Client;

    [TestMethod]
    public async Task Factory_ResetBeforeResponse_IsRetriedWithTheSameRequest()
    {
        await using var server = new LoopbackServer(ResetConnection, Answer(Completion));
        var client = LoopbackClient(server.Endpoint);

        var resp = await client.GetResponseAsync("hi");

        Assert.AreEqual("ok", resp.Text);
        Assert.AreEqual(2, server.Connections);
        Assert.AreEqual(2, server.Bodies.Count);
        StringAssert.Contains(server.Bodies[0], "\"hi\"", "the first attempt carried the prompt");
        Assert.AreEqual(server.Bodies[0], server.Bodies[1], "the retry resends the same body");
    }

    [TestMethod]
    public async Task Factory_ResetAfterTheResponseStarted_IsNotRetried()
    {
        await using var server = new LoopbackServer(HalfThenReset(Completion), Answer(Completion));
        var client = LoopbackClient(server.Endpoint);

        Exception? failed = null;
        try { await client.GetResponseAsync("hi"); }
        catch (Exception e) { failed = e; }

        Assert.IsNotNull(failed, "a reset mid-body fails the call");
        Assert.AreEqual(1, server.Connections, "never resent once response bytes came");
        StringAssert.DoesNotMatch(failed.ToString(), new System.Text.RegularExpressions.Regex("SECRET123"));
    }
}
