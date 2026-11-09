/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveApiController.cs
    文件功能描述：/live/api 控制器：契约中的全部路由，自行读写 JSON 正文与 SSE 事件流


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// The live API (contract: Routes). Bodies are read and written here, not by model binding, so every non-2xx carries
/// the contract's <c>{ error, message }</c> whatever the host's MVC settings; an unexpected exception is the
/// contract's 500 internal (<see cref="LiveExceptionFilter"/>), never the host's error page.
/// </summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("live/api")]
[TypeFilter(typeof(LiveExceptionFilter))]
public sealed class LiveApiController(SessionManager sessions, AccessService access, ApplyRateLimiter applyLimit,
    AccessCheckRateLimiter checkLimit, EditRateLimiter editLimit, IApplicationStore applications, LiveOptions opt, TimeProvider tp,
    ILiveRuntime runtime, UsageDay usageDay, IHostApplicationLifetime lifetime, ILogger<LiveApiController> log) : ControllerBase
{
    /// <summary>Far above any real body (text is at most 400 characters); a larger one is bad_request.</summary>
    public const int MaxBodyBytes = 16 * 1024;

    /// <summary>Apply field caps, after trim, matching the admin's columns (Task 13). Over a cap is bad_request.</summary>
    public const int MaxEmail = 200, MaxName = 120, MaxWouldTry = 400, MaxScene = 32;

    static readonly string[] Interests = { "home", "lab", "warehouse", "business", "other" };

    /// <summary>The parameter AccessService.AdmitRequestAsync names in its ArgumentException for a disabled code.</summary>
    const string AdmittedCodeParam = "c";

    public sealed record CodeBody(string? Code);
    public sealed record RoomDeviceBody(string? Type, string? Id);
    public sealed record SessionBody(string? Scene, List<RoomDeviceBody?>? Devices);
    public sealed record RequestBody(string? Text);
    public sealed record WhatIfBody(string? Device);
    public sealed record ApplyBody(string? Email, string? Name, string? Interest, string? WouldTry, string? Scene, string? Website);

    // ---- no code ----

    [HttpGet("health")]
    public IActionResult Health() => LiveJson.Result(200, new JsonObject
    {
        ["ok"] = true,
        ["liveEnabled"] = LiveOn && !BudgetSpent,
        ["scenes"] = new JsonArray(ManifestLoader.LiveScenes.Select(id => (JsonNode)JsonValue.Create(id)!).ToArray()),
    });

    /// <summary>AccessCheckPerMinute per IP (counted like apply) first, so a refused check reads nothing; then the code.</summary>
    [HttpPost("access/check")]
    public async Task<IActionResult> AccessCheck()
    {
        if (!checkLimit.TryAcquire(ClientIp())) return Error("rate");
        var (ok, body) = await ReadAsync<CodeBody>();
        if (!ok) return Error("bad_request");
        var a = await CheckValueAsync(body?.Code);
        return a.Ok ? LiveJson.Result(200, new JsonObject { ["ok"] = true, ["remainingToday"] = a.Remaining }) : Error(a.Error!);
    }

    /// <summary>The honeypot first (202, nothing stored), then 5 per IP per hour, then validation.</summary>
    [HttpPost("apply")]
    public async Task<IActionResult> Apply()
    {
        var (ok, f) = await ReadAsync<ApplyBody>();
        if (ok && !string.IsNullOrEmpty(f?.Website)) return Accepted202();
        if (!applyLimit.TryAcquire(ClientIp())) return Error("rate");
        var email = f?.Email?.Trim() ?? "";
        var name = f?.Name?.Trim() ?? "";
        var wouldTry = f?.WouldTry?.Trim() ?? "";
        var scene = f?.Scene?.Trim() ?? "";
        if (!ok || f is null || !email.Contains('@') || email.Length > MaxEmail || !Interests.Contains(f.Interest, StringComparer.Ordinal)
            || name.Length > MaxName || wouldTry.Length > MaxWouldTry || scene.Length > MaxScene)
            return Error("bad_request");
        try
        {
            await applications.AddAsync(new Application(0, email, name.Length == 0 ? null : name, f.Interest!, wouldTry, scene,
                tp.GetUtcNow(), "new", null));
        }
        catch (Exception e) when (e is not OperationCanceledException && BudgetSpent)
        {
            WarnStoreDown(e);
            return Error("live_off");
        }
        return Accepted202();
    }

    // ---- sessions ----

    /// <summary>
    /// bad_code → live_off → budget → bad_request (missing or unknown scene) → bad_room (custom only: a missing or invalid device list) → code_limit (the code's 5 sessions are
    /// all busy) or global_limit (the server holds MaxSessions). A 6th session for a code ends its least recently active
    /// idle one.
    /// </summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession()
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        if (!LiveOn) return Error("live_off");
        if (BudgetSpent) return Error("budget");
        var (ok, body) = await ReadAsync<SessionBody>();
        if (!ok || body?.Scene is not { } scene || !ManifestLoader.LiveScenes.Contains(scene)) return Error("bad_request");
        SessionResult created;
        if (scene == CustomRoom.SceneId)
        {
            if (body.Devices is null || body.Devices.Any(d => d?.Type is null || d.Id is null)) return Error("bad_room");
            var room = body.Devices.Select(d => new RoomDevice(d!.Type!, d.Id!)).ToList();
            if (CustomRoom.Validate(room) is { } bad) return Error(bad);
            created = sessions.Create(a.Code!, CustomRoom.Build(room));
        }
        else created = sessions.Create(a.Code!, scene);
        if (created.Error is { } kind) return Error(kind);
        var s = created.Session!;
        return LiveJson.Result(201, new JsonObject
        {
            ["sessionId"] = s.Id,
            ["state"] = s.Room.ToWire(),
            ["clock"] = ClockNode(s, s.Room),
            ["suggestions"] = new JsonArray(s.Scene.Suggestions.Take(4).Select(x => (JsonNode)JsonValue.Create(x)!).ToArray()),
            ["automations"] = Automations.List(s.Scene, s.Behavior, s.PostSetup), // a new room has none
        });
    }

    /// <summary>
    /// The session's events after <c>after</c> (default 0, from the start), then live, with a ping every 15 s. Ends when
    /// the client leaves, the session expires (the stream itself doesn't count as activity) or the host stops (so
    /// shutdown doesn't wait on open streams; the client resumes with ?after=).
    /// </summary>
    [HttpGet("sessions/{sid}/events")]
    public async Task<IActionResult> Events(string sid, [FromQuery] string? after)
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        var s = sessions.Get(sid, a.Code!);
        if (s is null) return Error("no_session");
        long from = 0;
        if (!string.IsNullOrEmpty(after) && !long.TryParse(after, NumberStyles.None, CultureInfo.InvariantCulture, out from))
            return Error("bad_request");
        if (sessions.ExpiryOf(s) is not { } expiry) return Error("no_session");

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted, expiry, lifetime.ApplicationStopping);
        var sse = new SseWriter(HttpContext);
        try { await sse.StartAsync(stop.Token); }
        catch (Exception e) when (stop.IsCancellationRequested || e is IOException) { return new EmptyResult(); }
        await sse.RunAsync(s.Events, from, stop.Token);
        return new EmptyResult();
    }

    /// <summary>
    /// bad_code → live_off → no_session → bad_request (missing or empty text) → too_long (trimmed, over MaxChars) →
    /// then busy → budget → global_limit → code_limit from StartRequestAsync, which reserves the run.
    /// </summary>
    [HttpPost("sessions/{sid}/requests")]
    public async Task<IActionResult> StartRequest(string sid)
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        if (!LiveOn) return Error("live_off");
        var s = sessions.Get(sid, a.Code!);
        if (s is null) return Error("no_session");
        var (ok, body) = await ReadAsync<RequestBody>();
        var text = body?.Text?.Trim();
        if (!ok || string.IsNullOrEmpty(text)) return Error("bad_request");
        if (text.Length > opt.MaxChars) return Error("too_long");
        StartResult r;
        try { r = await sessions.StartRequestAsync(s, text); }
        catch (ArgumentException e) when (e.GetType() == typeof(ArgumentException) && e.ParamName == AdmittedCodeParam)
        {
            // AccessService.AdmitRequestAsync's own guard: the code was disabled after this route's check. Nothing was
            // reserved. Any other ArgumentException (a store's, or the run-code mismatch, a bug) is a 500 internal.
            log.LogWarning(e, "A request in session {Sid} was refused at admission", s.Id);
            return Error("bad_code");
        }
        return r.Error is { } kind ? Error(kind) : LiveJson.Result(202, new JsonObject { ["requestId"] = r.Rid });
    }

    [HttpPost("sessions/{sid}/requests/{rid}/approve")]
    public Task<IActionResult> Approve(string sid, string rid) => DecideAsync(sid, rid, Decision.Approve);

    [HttpPost("sessions/{sid}/requests/{rid}/decline")]
    public Task<IActionResult> Decline(string sid, string rid) => DecideAsync(sid, rid, Decision.Decline);

    /// <summary>
    /// bad_code → live_off → no_session → bad_request (unreadable body); then StartWhatIfAsync: no_request (404),
    /// not_waiting or busy (409), bad_request for a device not in the scene (400), budget (429).
    /// </summary>
    [HttpPost("sessions/{sid}/requests/{rid}/whatif")]
    public async Task<IActionResult> WhatIf(string sid, string rid)
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        if (!LiveOn) return Error("live_off");
        var s = sessions.Get(sid, a.Code!);
        if (s is null) return Error("no_session");
        var (ok, body) = await ReadAsync<WhatIfBody>();
        if (!ok) return Error("bad_request");
        var r = await sessions.StartWhatIfAsync(s, rid, body?.Device ?? "");
        return r.Error is { } kind ? Error(kind) : LiveJson.Result(202, new JsonObject { ["requestId"] = r.Rid });
    }

    /// <summary>Back to the post-setup room; 409 busy mid-request. No SSE event: this body is the room.</summary>
    [HttpPost("sessions/{sid}/reset")]
    public async Task<IActionResult> Reset(string sid)
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        var s = sessions.Get(sid, a.Code!);
        if (s is null) return Error("no_session");
        if (!sessions.Reset(s)) return Error("busy");
        // Reset put a copy of PostSetup in place; reading PostSetup itself can't race a request started right after.
        return LiveJson.Result(200, new JsonObject
        {
            ["state"] = s.PostSetup.ToWire(), ["clock"] = ClockNode(s, s.PostSetup),
            ["automations"] = Automations.List(s.Scene, s.Behavior, s.PostSetup), // reset clears them all
        });
    }

    /// <summary>
    /// An automation card's on/off switch, <c>{ enabled }</c>. bad_code → no_session → rate (the edit limit, shared with set
    /// and advance, checked before the body is read) → bad_request (unreadable body, missing or non-bool enabled) →
    /// no_automation (404, an id not in the room). Allowed mid-request (never busy); not gated on LiveOn; counts against
    /// no limit. 202 when applied or queued behind the room's owner; an <c>automations</c> event follows.
    /// </summary>
    [HttpPost("sessions/{sid}/automations/{id}")]
    public async Task<IActionResult> Toggle(string sid, string id)
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        var s = sessions.Get(sid, a.Code!);
        if (s is null) return Error("no_session");
        if (!editLimit.TryAcquire(s.Id)) return Error("rate");
        var (ok, body) = await ReadAsync<JsonObject>();
        if (!ok || body?["enabled"] is not JsonValue ev || ev.GetValueKind() is not (JsonValueKind.True or JsonValueKind.False))
            return Error("bad_request");
        var error = await sessions.ToggleAsync(s, id, ev.GetValue<bool>());
        return error is { } kind ? Error(kind) : Accepted202();
    }

    /// <summary>
    /// A hand edit, <c>{ path, value }</c>. bad_code → no_session → rate (10 per 5 s per session, shared with advance) →
    /// bad_request (unreadable body, missing or non-string path, missing value) → bad_value → device_unavailable (409).
    /// Not gated on LiveOn: an edit never calls the model, meters tokens or counts against a limit. 202 when applied or queued.
    /// </summary>
    [HttpPost("sessions/{sid}/set")]
    public async Task<IActionResult> Set(string sid)
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        var s = sessions.Get(sid, a.Code!);
        if (s is null) return Error("no_session");
        if (!editLimit.TryAcquire(s.Id)) return Error("rate");
        var (ok, body) = await ReadAsync<JsonObject>();
        if (!ok || body is null || body["path"] is not JsonValue pv || !pv.TryGetValue<string>(out var path) || body["value"] is not { } value)
            return Error("bad_request");
        if (RoomEditor.Validate(s.Scene, path, value) is { } bad) return Error(bad);
        var outcome = await sessions.EditAsync(s, path, value);
        return outcome.Error is { } kind ? Error(kind) : Accepted202();
    }

    /// <summary>
    /// Moves the room's clock forward, <c>{ hours }</c>, 0 &lt; hours ≤ 168. bad_code → no_session → rate (shared with set) →
    /// bad_request (unreadable body, missing hours) → bad_value (not a finite number in range) → busy (409). Plays the gap
    /// before it answers 200 <c>{ clock }</c>. Not gated on LiveOn; counts against no limit.
    /// </summary>
    [HttpPost("sessions/{sid}/advance")]
    public async Task<IActionResult> Advance(string sid)
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        var s = sessions.Get(sid, a.Code!);
        if (s is null) return Error("no_session");
        if (!editLimit.TryAcquire(s.Id)) return Error("rate");
        var (ok, body) = await ReadAsync<JsonObject>();
        if (!ok || body?["hours"] is not JsonValue hv) return Error("bad_request");
        if (hv.GetValueKind() != JsonValueKind.Number || !hv.TryGetValue<double>(out var hours)
            || !double.IsFinite(hours) || !(hours > 0 && hours <= FastForward.MaxHours))
            return Error("bad_value");
        AdvanceResult r;
        try { r = await sessions.AdvanceAsync(s, hours); }
        catch (OperationCanceledException) { return Error("no_session"); } // the session expired while the gap played
        return r.Error is { } kind ? Error(kind) : LiveJson.Result(200, new JsonObject { ["clock"] = Ev.ClockEv(r.Clock!).data });
    }

    // ---- helpers ----

    async Task<IActionResult> DecideAsync(string sid, string rid, Decision d)
    {
        var a = await CheckCodeAsync();
        if (!a.Ok) return Error(a.Error!);
        var s = sessions.Get(sid, a.Code!);
        if (s is null) return Error("no_session");
        return s.TryDecide(rid, d) is { } kind ? Error(kind) : Accepted202();
    }

    /// <summary>Live is on: the runtime's current snapshot has both models and no off reason. The budget is separate.</summary>
    bool LiveOn => runtime.Current.On;

    /// <summary>Today's token budget (the current snapshot's) is spent.</summary>
    bool BudgetSpent => access.BudgetSpent(usageDay.For(tp.GetUtcNow()));

    /// <summary>
    /// The code check. If the store can't be read and the token meter has already failed closed for the same reason
    /// (the module isn't installed yet, or the database is down), the result is Error "live_off" (503), not a 500.
    /// A store failure while the meter reads fine stays an exception: 500 internal.
    /// </summary>
    Task<AccessResult> CheckCodeAsync() => CheckValueAsync(Request.Headers.Authorization.ToString());

    async Task<AccessResult> CheckValueAsync(string? value)
    {
        try { return await access.CheckAsync(value); }
        catch (Exception e) when (e is not OperationCanceledException && BudgetSpent)
        {
            WarnStoreDown(e);
            return new AccessResult(false, null, 0, "live_off");
        }
    }

    static long _storeWarnedAt;

    void WarnStoreDown(Exception e)
    {
        var now = Environment.TickCount64;
        var last = Interlocked.Read(ref _storeWarnedAt);
        if (last != 0 && now - last < 60_000) return;
        Interlocked.Exchange(ref _storeWarnedAt, now);
        log.LogWarning("The live store can't be read ({Reason}); answering live_off. Expected before the module is installed.", e.GetType().Name);
    }

    static IActionResult Error(string kind) => LiveErrors.Result(kind);

    static IActionResult Accepted202() => LiveJson.Result(202, new JsonObject());

    static JsonObject ClockNode(LiveSession s, RoomState room) => Ev.ClockEv(SimClock.At(s.Scene.StartDay, room.Hour)).data;

    /// <summary>
    /// The address the apply and access/check limits count. Through the tunnel the ngrok agent connects from loopback and appends the
    /// visitor's address to X-Forwarded-For, so the last entry is the one ngrok saw; earlier entries are whatever the
    /// client sent. Otherwise the remote address.
    /// </summary>
    string ClientIp()
    {
        var remote = HttpContext.Connection.RemoteIpAddress;
        if (remote is not null && IPAddress.IsLoopback(remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote))
        {
            var last = Request.Headers["X-Forwarded-For"].SelectMany(v => (v ?? "").Split(','))
                .Select(x => x.Trim()).LastOrDefault(x => x.Length > 0);
            if (last is not null) return last;
        }
        return remote?.ToString() ?? "";
    }

    /// <summary>
    /// The JSON body, case-insensitive camelCase. Ok is false for unreadable JSON, a wrong type, a body over
    /// <see cref="MaxBodyBytes"/> or one Kestrel couldn't read (BadHttpRequestException: a broken chunk, its own size
    /// limit); an empty body is ok with a null value.
    /// </summary>
    async Task<(bool Ok, T? Value)> ReadAsync<T>() where T : class
    {
        if (Request.ContentLength > MaxBodyBytes) return (false, null);
        using var buf = new MemoryStream();
        var chunk = new byte[4096];
        try
        {
            int n;
            while ((n = await Request.Body.ReadAsync(chunk, HttpContext.RequestAborted)) > 0)
            {
                if (buf.Length + n > MaxBodyBytes) return (false, null);
                buf.Write(chunk, 0, n);
            }
        }
        catch (BadHttpRequestException) { return (false, null); }
        if (buf.Length == 0) return (true, null);
        try { return (true, JsonSerializer.Deserialize<T>(buf.GetBuffer().AsSpan(0, (int)buf.Length), LiveJson.Options)); }
        catch (JsonException) { return (false, null); }
    }
}
