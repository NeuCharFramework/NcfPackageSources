/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveHost.cs
    文件功能描述：测试替身：在内存中搭起带 Live 服务的 ASP.NET Core 测试宿主


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 按 NCF 顺序搭建：LiveApiPipeline 在 UseRouting 之前；移除隧道守卫、API 兜底与 “fell through” 终端中间件

----------------------------------------------------------------*/

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;
using Senparc.Xncf.NeuCharBoxLive.Api;

namespace Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;

/// <summary>A chat client a test can re-script between requests: every call goes to <see cref="Inner"/>.</summary>
public sealed class SwitchChatClient : IChatClient
{
    public IChatClient Inner { get; set; } = new ScriptedChatClient();

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Inner.GetResponseAsync(messages, options, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Inner.GetStreamingResponseAsync(messages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

/// <summary>One parsed SSE event.</summary>
public record SseEvent(long Id, string Kind, JsonObject Data)
{
    public string? Rid => (string?)Data["rid"];
}

/// <summary>
/// An open SSE response. A pump reads its lines into a channel, so a test can wait for a line with a timeout without
/// cancelling a read on the stream itself.
/// </summary>
public sealed class SseStream : IAsyncDisposable
{
    readonly Channel<string> _lines = Channel.CreateUnbounded<string>();
    readonly Task _pump;

    public SseStream(HttpResponseMessage response)
    {
        Response = response;
        _pump = Task.Run(async () =>
        {
            try
            {
                using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
                while (await reader.ReadLineAsync() is { } line) _lines.Writer.TryWrite(line);
                _lines.Writer.TryComplete();
            }
            catch (Exception e) { _lines.Writer.TryComplete(e); }
        });
    }

    public HttpResponseMessage Response { get; }

    /// <summary>Every line read so far, pings included.</summary>
    public List<string> Raw { get; } = new();

    public int Pings { get; private set; }

    /// <summary>The next line; null at the end of the stream. Fails the test after <paramref name="timeout"/> (default the guard).</summary>
    public async Task<string?> LineAsync(TimeSpan? timeout = null)
    {
        var line = await TryLineAsync(timeout ?? LiveHost.Guard);
        if (line is Pending) Assert.Fail("no SSE line in time");
        return line;
    }

    /// <summary>Marks "nothing arrived in time" for <see cref="TryLineAsync"/>.</summary>
    public const string Pending = "\0pending";

    /// <summary>The next line, null at the end of the stream, or <see cref="Pending"/> when none arrives within the wait.</summary>
    public async Task<string?> TryLineAsync(TimeSpan wait)
    {
        using var cts = new CancellationTokenSource(wait);
        try
        {
            if (!await _lines.Reader.WaitToReadAsync(cts.Token)) return null;
        }
        catch (OperationCanceledException) { return Pending; }
        catch (Exception) { return null; } // the stream broke: same as its end
        if (!_lines.Reader.TryRead(out var line)) return Pending;
        Raw.Add(line);
        return line;
    }

    /// <summary>The next event, skipping pings. Fails at the end of the stream.</summary>
    public async Task<SseEvent> NextAsync()
    {
        long id = -1;
        string? kind = null, data = null;
        while (true)
        {
            var line = await LineAsync();
            if (line is null) Assert.Fail("the SSE stream ended");
            if (line!.Length == 0)
            {
                if (kind is null && data is null) continue; // the blank line after a ping
                Assert.IsTrue(id > 0, "an event without an id");
                return new SseEvent(id, kind!, JsonNode.Parse(data!)!.AsObject());
            }
            if (line.StartsWith(':'))
            {
                Assert.AreEqual(": ping", line);
                Pings++;
                continue;
            }
            if (line.StartsWith("id: ")) id = long.Parse(line[4..]);
            else if (line.StartsWith("event: ")) kind = line[7..];
            else if (line.StartsWith("data: ")) data = line[6..];
            else Assert.Fail($"unexpected SSE line '{line}'");
        }
    }

    /// <summary>Events up to and including the first of that kind.</summary>
    public async Task<List<SseEvent>> UntilAsync(string kind)
    {
        var list = new List<SseEvent>();
        while (true)
        {
            var e = await NextAsync();
            list.Add(e);
            if (e.Kind == kind) return list;
        }
    }

    /// <summary>
    /// Moves the fake clock 15 s at a time until a line arrives (the SSE loop may not have armed its ping timer yet),
    /// and returns that line.
    /// </summary>
    public async Task<string?> AdvanceUntilLineAsync(FakeTimeProvider fake)
    {
        for (var i = 0; i < 20; i++)
        {
            var early = await TryLineAsync(TimeSpan.FromMilliseconds(100));
            if (early is not Pending) return early;
            fake.Advance(TimeSpan.FromSeconds(15));
            var line = await TryLineAsync(TimeSpan.FromMilliseconds(400));
            if (line is not Pending) return line;
        }
        Assert.Fail("no SSE line after moving the clock");
        return null;
    }

    /// <summary>True when the stream ends within the guard; pings before the end are skipped.</summary>
    public async Task<bool> EndsAsync()
    {
        while (true)
        {
            var line = await TryLineAsync(LiveHost.Guard);
            if (line is null) return true;
            if (line is Pending) return false;
            if (line.Length != 0 && line != ": ping") Assert.Fail($"an event before the end: '{line}'");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Response.Dispose();
        try { await _pump.WaitAsync(LiveHost.Guard); } catch { /* the pump ends with the response */ }
    }
}

/// <summary>
/// A TestServer host: AddNcbLive with a <see cref="StaticLiveRuntime"/> whose snapshot is on with a scripted model
/// (<see cref="Chat"/>), an instant pacer on a fake clock and the default in-memory stores; then, in NCF's order, the
/// module's /live/api branch (<see cref="LiveApiPipeline.Use"/>, as Register.UseXncfModule adds it), routing and the
/// controllers. PipelineTests builds the full NCF-ordered site through the module's UseXncfModule itself.
/// </summary>
public sealed class LiveHost : IAsyncDisposable
{
    /// <summary>The longest a test waits for anything (an SSE line, a condition, the host to stop).</summary>
    public static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);

    /// <summary>The model name the default snapshot plans and runs on.</summary>
    public const string Model = Snapshots.Model;

    /// <summary>The built-in origins plus the local demo (http://localhost:8765), which the CORS tests exercise.</summary>
    public static IReadOnlyList<string> Origins { get; } = LiveCors.BuiltIns.Concat(LiveCors.Development).ToArray();

    public FakeTimeProvider Fake { get; } = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    public SwitchChatClient Chat { get; } = new();

    /// <summary>The runtime handed to UseRuntime (null when the test skipped it); set Current to change the snapshot.</summary>
    public StaticLiveRuntime Runtime { get; private set; } = null!;

    public IHost Host { get; private set; } = null!;
    public TestServer Server { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    /// <summary>An on snapshot: Plan and Run both <paramref name="model"/> on <paramref name="chat"/> (default <see cref="Chat"/>).</summary>
    public LiveSnapshot OnSnapshot(IChatClient? chat = null, LiveLimits? limits = null, IReadOnlyList<string>? origins = null,
        string model = Model) =>
        Snapshots.On(chat ?? Chat, limits, model) with { Origins = origins ?? Origins };

    /// <summary>Changes the current snapshot's limits, keeping its models and on/off state.</summary>
    public void SetLimits(Func<LiveLimits, LiveLimits> change) => Runtime.SetLimits(change);

    /// <summary>Turns live off (reason disabled) or back on, keeping the snapshot's models and limits.</summary>
    public void SetLive(bool on) => Runtime.Current = Runtime.Current with { OffReason = on ? null : LiveOff.Disabled };

    /// <param name="snapshot">The runtime's first snapshot: <see cref="OnSnapshot"/> by default; null skips UseRuntime
    /// (AddNcbLive's default, off with no_model).</param>
    /// <param name="builder">More NcbLiveBuilder calls, e.g. UseStores.</param>
    /// <param name="beforeRouting">Middleware between the module's branch and UseRouting, where the NCF host has its own
    /// (static files, maintenance mode, cookie policy).</param>
    public static async Task<LiveHost> StartAsync(Func<LiveHost, LiveSnapshot?>? snapshot = null,
        Dictionary<string, string?>? config = null, Action<LiveHost, IServiceCollection>? services = null,
        Action<NcbLiveBuilder>? builder = null, Action<IApplicationBuilder>? beforeRouting = null)
    {
        var h = new LiveHost();
        var first = snapshot is null ? h.OnSnapshot() : snapshot(h);
        if (first is not null) h.Runtime = new StaticLiveRuntime(first);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(config ?? new()).Build();
        h.Host = new HostBuilder()
            .ConfigureLogging(l => l.ClearProviders())
            .ConfigureWebHost(w => w
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddNcbLive(cfg, b =>
                    {
                        if (first is not null) b.UseRuntime(h.Runtime);
                        b.UsePacer(new InstantPacer(h.Fake.Advance)).UseTimeProvider(h.Fake);
                        builder?.Invoke(b);
                    });
                    services?.Invoke(h, s);
                })
                .Configure(app =>
                {
                    LiveApiPipeline.Use(app);
                    beforeRouting?.Invoke(app);
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapControllers());
                }))
            .Build();
        await h.Host.StartAsync();
        h.Server = h.Host.GetTestServer();
        h.Client = h.Server.CreateClient();
        return h;
    }

    public T Get<T>() where T : notnull => Server.Services.GetRequiredService<T>();
    public LiveOptions Opt => Get<LiveOptions>();
    public InMemoryLiveStore Store => (InMemoryLiveStore)Get<IInviteStore>();

    public async Task<InviteCode> NewCodeAsync(int limit = 20) => await Get<IInviteStore>().CreateAsync("visitor@example.com", limit);

    // ---- HTTP ----

    public record Reply(HttpStatusCode Status, JsonNode? Body, HttpResponseMessage Message)
    {
        public int Code => (int)Status;
        public string? Error => (string?)Body?["error"];
    }

    /// <param name="body">A string is sent as raw JSON text; anything else as JSON.</param>
    public async Task<Reply> SendAsync(HttpMethod method, string path, object? body = null, string? code = null,
        Action<HttpRequestMessage>? edit = null)
    {
        var req = new HttpRequestMessage(method, path.StartsWith('/') ? path : "/live/api/" + path);
        if (code is not null) req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + code);
        if (body is string raw) req.Content = new StringContent(raw, Encoding.UTF8, "application/json");
        else if (body is not null) req.Content = JsonContent.Create(body);
        edit?.Invoke(req);
        var resp = await Client.SendAsync(req);
        var text = await resp.Content.ReadAsStringAsync();
        JsonNode? json = null;
        if (text.Length > 0 && resp.Content.Headers.ContentType?.MediaType == "application/json") json = JsonNode.Parse(text);
        return new Reply(resp.StatusCode, json, resp);
    }

    public Task<Reply> PostAsync(string path, object? body, string? code = null, Action<HttpRequestMessage>? edit = null) =>
        SendAsync(HttpMethod.Post, path, body, code, edit);

    public Task<Reply> GetAsync(string path, string? code = null, Action<HttpRequestMessage>? edit = null) =>
        SendAsync(HttpMethod.Get, path, null, code, edit);

    public async Task<string> SessionAsync(InviteCode c)
    {
        var r = await PostAsync("sessions", new { scene = "home" }, c.Code);
        Assert.AreEqual(201, r.Code, r.Body?.ToJsonString());
        return (string)r.Body!["sessionId"]!;
    }

    public async Task<string> RequestAsync(InviteCode c, string sid, string text = "Dim the lamp")
    {
        var r = await PostAsync($"sessions/{sid}/requests", new { text }, c.Code);
        Assert.AreEqual(202, r.Code, r.Body?.ToJsonString());
        return (string)r.Body!["requestId"]!;
    }

    public async Task<SseStream> OpenSseAsync(InviteCode c, string sid, long? after = null, string? origin = null)
    {
        var path = $"/live/api/sessions/{sid}/events" + (after is { } a ? $"?after={a}" : "");
        var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + c.Code);
        req.Headers.TryAddWithoutValidation("Accept", "text/event-stream");
        req.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "1");
        if (origin is not null) req.Headers.TryAddWithoutValidation("Origin", origin);
        using var guard = new CancellationTokenSource(Guard);
        var resp = await Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, guard.Token);
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode, await Peek(resp));
        Assert.AreEqual("text/event-stream", resp.Content.Headers.ContentType?.MediaType);
        return new SseStream(resp);
    }

    static async Task<string> Peek(HttpResponseMessage resp) =>
        resp.StatusCode == HttpStatusCode.OK ? "" : await resp.Content.ReadAsStringAsync();

    /// <summary>
    /// A request with full control of the connection: the remote address, the Host header and any headers, as a tunnel
    /// agent (or a local browser) would send it. Returns the status and the body text.
    /// </summary>
    /// <param name="path">
    /// The decoded path exactly as Kestrel hands it on (it decodes %5C to '\' but keeps %2F). Set through the
    /// PathString constructor: the implicit conversion would URL-decode it again.
    /// </param>
    public async Task<(int Status, string Body)> RawAsync(string method, string path, string? remoteIp, string host,
        IDictionary<string, string>? headers = null, string? json = null, Stream? body = null)
    {
        var ctx = await Server.SendAsync(c =>
        {
            c.Request.Method = method;
            var q = path.IndexOf('?');
            c.Request.Path = new PathString(q < 0 ? path : path[..q]);
            if (q >= 0) c.Request.QueryString = new QueryString(path[q..]);
            c.Request.Host = new HostString(host);
            c.Connection.RemoteIpAddress = remoteIp is null ? null : IPAddress.Parse(remoteIp);
            foreach (var (k, v) in headers ?? new Dictionary<string, string>()) c.Request.Headers[k] = v;
            if (json is not null)
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                c.Request.ContentType = "application/json";
                c.Request.ContentLength = bytes.Length;
                c.Request.Body = new MemoryStream(bytes);
            }
            if (body is not null)
            {
                c.Request.ContentType = "application/json";
                c.Request.Body = body;
            }
        });
        using var reader = new StreamReader(ctx.Response.Body);
        return (ctx.Response.StatusCode, await reader.ReadToEndAsync());
    }

    /// <summary>Waits (real time, bounded by the guard) until the condition holds.</summary>
    public static async Task WaitUntil(Func<Task<bool>> cond, string what)
    {
        var until = DateTime.UtcNow + Guard;
        while (!await cond())
        {
            if (DateTime.UtcNow > until) Assert.Fail($"timed out waiting for {what}");
            await Task.Delay(20);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (Host is not null)
        {
            using var stop = new CancellationTokenSource(Guard);
            try { await Host.StopAsync(stop.Token); } catch (OperationCanceledException) { }
            Host.Dispose();
        }
    }
}
