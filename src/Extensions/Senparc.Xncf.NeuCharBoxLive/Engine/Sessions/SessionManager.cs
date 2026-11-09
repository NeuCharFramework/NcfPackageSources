/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SessionManager.cs
    文件功能描述：会话管理：创建、上限、请求、批准、重置、what-if、编辑与过期清理


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

/// <summary>
/// Rid is the new request's id, or Error is a contract kind: busy, no_request, not_waiting, bad_request, budget,
/// global_limit or code_limit (and no_session for a session that expired after the route found it, live_off for a
/// runtime that turned off after the route checked it).
/// </summary>
public record StartResult(string? Rid, string? Error);

/// <summary>The new session, or Error "code_limit" (the code's sessions are all busy) or "global_limit" (the server is full).</summary>
public record SessionResult(LiveSession? Session, string? Error);

/// <summary>The clock after an advance, or Error "busy" (a request or another advance owns the room).</summary>
public record AdvanceResult(string? Error, Clock? Clock);

/// <summary>
/// The live sessions: create (at most <see cref="SessionsPerCode"/> per code and <see cref="LiveOptions.MaxSessions"/> in
/// all), find by id and code, start requests and what-ifs, reset, and expire after
/// <see cref="LiveOptions.SessionIdle"/> with no call. Each request runs as a background task, not tied to the HTTP
/// call that started it; it is cancelled only when its session expires. A request (or what-if) reads the runtime's
/// snapshot once, when it starts, and runs on it to the end: its models, its limits and the model the run log names.
/// </summary>
public sealed class SessionManager
{
    readonly LiveOptions _opt;
    readonly TimeProvider _tp;
    readonly AccessService _access;
    readonly Func<RequestRunner> _runners;
    readonly ILiveRuntime _runtime;
    readonly UsageDay _day;
    readonly ILogger<SessionManager> _log;
    readonly IPacer _pacer;
    readonly object _gate = new();
    readonly Dictionary<string, Entry> _sessions = new(StringComparer.Ordinal);

    public static readonly EventId RunnerCrashed = new(7101, nameof(RunnerCrashed));

    /// <summary>Live sessions one invite code may hold (contract, server final review).</summary>
    public const int SessionsPerCode = 5;

    sealed class Entry(LiveSession session)
    {
        public LiveSession Session { get; } = session;

        /// <summary>Cancelled when the session expires; its requests run on this token.</summary>
        public CancellationTokenSource Expiry { get; } = new();
    }

    /// <param name="runners">A new runner per request (Task 10 builds it from the container).</param>
    /// <param name="runtime">Where each request reads its snapshot. Options with <see cref="LiveOptions.Problems"/> don't
    /// stop this constructor: AddNcbLive hands it a runtime that is always off.</param>
    /// <param name="day">The usage day requests are admitted and logged on.</param>
    public SessionManager(LiveOptions opt, TimeProvider tp, AccessService access, Func<RequestRunner> runners, ILiveRuntime runtime,
        UsageDay day, ILogger<SessionManager>? log = null, IPacer? pacer = null)
    {
        _opt = opt;
        _tp = tp;
        _access = access;
        _runners = runners;
        _runtime = runtime;
        _day = day;
        _log = log ?? NullLogger<SessionManager>.Instance;
        _pacer = pacer ?? new RealPacer();
    }

    /// <summary>Sessions not yet swept or found expired.</summary>
    public int Count { get { lock (_gate) return _sessions.Count; } }

    /// <summary>
    /// A new session on the scene's post-setup room, within the caps (contract, server final review). Expired sessions
    /// are removed first, so they hold no place. A code already holding <see cref="SessionsPerCode"/> sessions ends its
    /// least recently active idle one to make room (that tab then gets no_session, its stream ends); if every one is
    /// busy, Error is code_limit. Otherwise, at <see cref="LiveOptions.MaxSessions"/>, Error is global_limit.
    /// Throws KeyNotFoundException for an unknown scene.
    /// </summary>
    public SessionResult Create(InviteCode c, string scene) => Create(c, ManifestLoader.Load(scene));

    /// <summary>The same, on a manifest already built (a custom room).</summary>
    public SessionResult Create(InviteCode c, SceneManifest manifest)
    {
        var ended = new List<Entry>();
        try
        {
            lock (_gate)
            {
                var now = _tp.GetUtcNow();
                foreach (var e in _sessions.Values.Where(e => Idle(e.Session, now)).ToList())
                {
                    _sessions.Remove(e.Session.Id);
                    ended.Add(e);
                }
                var mine = _sessions.Values.Where(e => e.Session.Code.Id == c.Id).OrderBy(e => e.Session.LastActivity).ToList();
                if (mine.Count >= SessionsPerCode)
                {
                    if (mine.FirstOrDefault(TryEndIdle) is not { } evicted) return new SessionResult(null, "code_limit");
                    ended.Add(evicted);
                }
                else if (_sessions.Count >= _opt.MaxSessions) return new SessionResult(null, "global_limit");

                string id;
                do id = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
                while (_sessions.ContainsKey(id));
                var s = new LiveSession(id, c, manifest, new EventLog(_tp), now) { Log = _log };
                _sessions[id] = new Entry(s);
                return new SessionResult(s, null);
            }
        }
        finally
        {
            foreach (var e in ended) e.Expiry.Cancel(); // outside the lock, as Get and Sweep do
        }
    }

    /// <summary>
    /// Under _gate: removes the session if it is idle, meaning no active request and its Gate free (no request is being
    /// admitted or ended on it right now). Gate is only tried, never waited for, so this can't deadlock with a holder
    /// of Gate that asks for _gate (StartRequestAsync's ExpiryOf). A request admitted after this finds no session.
    /// </summary>
    bool TryEndIdle(Entry e)
    {
        if (e.Session.Busy || !e.Session.Gate.Wait(0)) return false;
        try
        {
            if (e.Session.Busy) return false;
            _sessions.Remove(e.Session.Id);
            return true;
        }
        finally { e.Session.Gate.Release(); }
    }

    /// <summary>
    /// The session, or null when it doesn't exist, has expired, or belongs to another code. A hit counts as activity,
    /// and refreshes the session's code from <paramref name="c"/> (the route's fresh check), so a new limit applies.
    /// </summary>
    public LiveSession? Get(string sid, InviteCode c)
    {
        Entry? expired;
        lock (_gate)
        {
            if (sid is null || !_sessions.TryGetValue(sid, out var e)) return null;
            var now = _tp.GetUtcNow();
            if (!Idle(e.Session, now))
            {
                if (e.Session.Code.Id != c.Id) return null;
                e.Session.Code = c;
                e.Session.LastActivity = now;
                return e.Session;
            }
            _sessions.Remove(sid);
            expired = e;
        }
        expired.Expiry.Cancel();
        return null;
    }

    /// <summary>
    /// Under the session's Gate: busy while a request is active, live_off when the snapshot taken now is off, then
    /// admission against that snapshot's limits (budget → global_limit → code_limit, with the run reserved under its
    /// Run model). The request is tracked and active before this returns, then runs in the background on the snapshot.
    /// </summary>
    public async Task<StartResult> StartRequestAsync(LiveSession s, string text)
    {
        var snap = _runtime.Current; // the request's one read: a change from here on applies to the next request
        var runner = _runners(); // first: if it can't be built, nothing is reserved
        text = (text ?? "").Trim();
        await s.Gate.WaitAsync(CancellationToken.None);
        try
        {
            if (s.Busy) return new StartResult(null, "busy");
            if (!snap.On) return new StartResult(null, "live_off");
            if (ExpiryOf(s) is not { } ct) return new StartResult(null, "no_session");
            var now = _tp.GetUtcNow();
            var admit = await _access.AdmitRequestAsync(s.Code,
                new RunStart(s.Code.Code, s.Scene.Id, text, false, null, snap.Run!.Name, _day.For(now), now), snap.Limits);
            if (admit.Error is not null) return new StartResult(null, admit.Error);
            var r = new RequestRecord(NewRid(s), text, s.Room, _tp) { RunId = admit.RunId };
            Launch(s, r, c => runner.RunAsync(s, r, snap, c), ct);
            return new StartResult(r.Rid, null);
        }
        finally { s.Gate.Release(); }
    }

    /// <summary>
    /// "What if something fails?" on <paramref name="rid"/>: no_request when the session never had it, not_waiting
    /// unless it is the session's what-if target, busy mid-request, bad_request for a device not in the scene, live_off
    /// when the snapshot taken now is off, budget once today's token budget (that snapshot's) is spent. The replay skips
    /// admission: what-ifs don't count against the request limits. It runs on that snapshot, like a request.
    /// </summary>
    public async Task<StartResult> StartWhatIfAsync(LiveSession s, string rid, string device)
    {
        var snap = _runtime.Current;
        var runner = _runners();
        await s.Gate.WaitAsync(CancellationToken.None);
        try
        {
            var target = rid is null ? null : s.Find(rid);
            if (target is null) return new StartResult(null, "no_request");
            var prior = s.LastReplayable;
            if (prior is null || !ReferenceEquals(target, prior) || prior.Plan is null) return new StartResult(null, "not_waiting");
            if (s.Busy) return new StartResult(null, "busy");
            if (string.IsNullOrEmpty(device) || s.Scene.Device(device) is null) return new StartResult(null, "bad_request");
            if (!snap.On) return new StartResult(null, "live_off");
            if (_access.BudgetSpent(_day.For(_tp.GetUtcNow()), snap.Limits)) return new StartResult(null, "budget");
            if (ExpiryOf(s) is not { } ct) return new StartResult(null, "no_session");
            var r = new RequestRecord(NewRid(s), prior.Text, s.Room, _tp) { WhatIfDevice = device };
            Launch(s, r, c => runner.RunWhatIfAsync(s, prior, device, r, snap, c), ct);
            return new StartResult(r.Rid, null);
        }
        finally { s.Gate.Release(); }
    }

    /// <summary>
    /// "Reset room": back to a copy of the post-setup room. The conversation stays, with a turn saying the room was reset,
    /// and nothing is replayable until the next request (a what-if now answers not_waiting). False mid-request (the
    /// route answers 409 busy). The route's 200 body carries the state, clock and (empty) automations; the only event is
    /// an <c>automations</c> list (no rid, src "edit"), and only when the room had automations to clear.
    /// </summary>
    public bool Reset(LiveSession s)
    {
        s.Gate.Wait();
        try
        {
            if (s.Busy) return false;
            s.Room = s.PostSetup.Clone();
            s.InvalidateDelayedEdits();
            EditApplier.EmitAutomations(s, s.Room, null, edit: true);
            s.ClearReplayable();
            s.AddTurn(new Turn(Prompts.ResetUser, Prompts.ResetAssistant), _opt.HistoryTurns);
            s.LastActivity = _tp.GetUtcNow();
            return true;
        }
        finally { s.Gate.Release(); }
    }

    /// <summary>
    /// A visitor's hand edit (path and value already validated by the route). Queued under Gate; when nothing owns the
    /// room (no request, no advance) it is applied at once, with no rid. Otherwise the owner applies it, and this waits
    /// up to 2 s for the answer (outside Gate), returning <see cref="EditOutcome.Queued"/> if it isn't there by then.
    /// An edit never calls the model, meters tokens or counts against a limit.
    /// </summary>
    public async Task<EditOutcome> EditAsync(LiveSession s, string path, JsonNode value)
    {
        Task<EditOutcome> task;
        await s.Gate.WaitAsync(CancellationToken.None);
        try
        {
            task = s.Edits.Enqueue(path, value);
            if (!s.Busy) EditApplier.ApplyAll(s, EditorFor(s), s.Room, null, _pacer);
        }
        finally { s.Gate.Release(); }
        try { return await task.WaitAsync(TimeSpan.FromSeconds(2), _tp); }
        catch (TimeoutException) { return EditOutcome.Queued; }
    }

    /// <summary>
    /// The visitor's on/off switch on automation <paramref name="id"/>: null when it was applied, or queued behind the
    /// room's owner and not yet applied after 2 s; "no_automation" for an id not in the room. Like a hand edit it goes
    /// through the edit queue under Gate, so it lands only where the room's owner applies edits (idle: here, at once; a
    /// request: its servicing points; an advance: the current tick), never while a fast-forward step evaluates its rules
    /// and schedules (a fault turn's model call or re-plan wait is a servicing point, after its step was evaluated). While the
    /// room is owned, an id is checked against the last automations list streamed (what the visitor sees); the owner
    /// checks it again when it applies the switch. Allowed mid-request; never calls the model or counts against a limit.
    /// </summary>
    public async Task<string?> ToggleAsync(LiveSession s, string id, bool enabled)
    {
        Task<EditOutcome> task;
        await s.Gate.WaitAsync(CancellationToken.None);
        try
        {
            if (s.Busy && !s.KnowsAutomation(id)) return "no_automation";
            task = s.Edits.EnqueueToggle(id, enabled);
            if (!s.Busy) EditApplier.ApplyAll(s, EditorFor(s), s.Room, null, _pacer);
        }
        finally { s.Gate.Release(); }
        try { return (await task.WaitAsync(TimeSpan.FromSeconds(2), _tp)).Error; }
        catch (TimeoutException) { return null; }
    }

    /// <summary>
    /// Moves the room's clock forward <paramref name="hours"/> (the route checks 0 &lt; hours ≤ 168), playing the gap like
    /// the AI's fast-forward, paced, with its events on the session stream: no rid, src "edit". Busy while a request or
    /// another advance owns the room. Gate is held only to claim and to release the room, not while it plays, so an edit
    /// made meanwhile queues and is applied at the current tick. Never calls the model, meters tokens or counts against a limit.
    /// </summary>
    public async Task<AdvanceResult> AdvanceAsync(LiveSession s, double hours)
    {
        await s.Gate.WaitAsync(CancellationToken.None);
        try
        {
            if (s.Busy) return new AdvanceResult("busy", null);
            s.Advancing = true;
            s.LastActivity = _tp.GetUtcNow();
        }
        finally { s.Gate.Release(); }

        Clock? end = null;
        try
        {
            var behavior = ManifestLoader.Behavior(s.Scene);
            var editor = EditorFor(s);
            var room = s.Room;
            var pacer = new EditServicingPacer(_pacer, s.Edits.WhenPending,
                () => EditApplier.ApplyAll(s, editor, room, null, _pacer, null, immediate: true));
            await new FastForward(new Simulator(s.Scene, behavior), behavior, pacer, _opt.Pacing)
                .RunAsync(room, hours, new AdvanceSink(s, room), ExpiryOf(s) ?? CancellationToken.None);
        }
        finally
        {
            await s.Gate.WaitAsync(CancellationToken.None);
            try
            {
                // An edit made after the last wait is applied now; any later one finds the room free and applies itself.
                try
                {
                    EditApplier.ApplyAll(s, EditorFor(s), s.Room, null, _pacer);
                    EditApplier.EmitAutomations(s, s.Room, null, edit: true); // a step the advance stopped in (expiry) may not have streamed its fires
                }
                finally
                {
                    // Read while the room is still this advance's: once Gate is free, a reset can replace it.
                    end = SimClock.At(s.Scene.StartDay, s.Room.Hour);
                    s.Advancing = false;
                    s.LastActivity = _tp.GetUtcNow();
                }
            }
            finally { s.Gate.Release(); }
        }
        return new AdvanceResult(null, end);
    }

    static RoomEditor EditorFor(LiveSession s) => new(s.Scene, new Simulator(s.Scene, ManifestLoader.Behavior(s.Scene)));

    /// <summary>Removes the expired sessions and cancels their requests. Task 10's SessionSweepService calls it every minute.</summary>
    public void Sweep()
    {
        List<Entry> expired;
        lock (_gate)
        {
            var now = _tp.GetUtcNow();
            expired = _sessions.Values.Where(e => Idle(e.Session, now)).ToList();
            foreach (var e in expired) _sessions.Remove(e.Session.Id);
        }
        foreach (var e in expired) e.Expiry.Cancel();
    }

    /// <summary>
    /// No call for SessionIdle. A running request touches the session when it starts and ends, and every approve or
    /// decline comes through Get. The longest quiet stretch inside a request is its work time plus one approval wait,
    /// and a SessionIdle shorter than that is one of <see cref="LiveOptions.Problems"/> (live stays off), so expiry only
    /// cancels a request that is stuck.
    /// </summary>
    bool Idle(LiveSession s, DateTimeOffset now) => now - s.LastActivity >= _opt.SessionIdle;

    /// <summary>
    /// Cancelled when the session expires (swept, or found idle by Get), so an SSE loop can end its stream; its requests
    /// run on it too. Null once the session is gone.
    /// </summary>
    public CancellationToken? ExpiryOf(LiveSession s)
    {
        lock (_gate)
            return _sessions.TryGetValue(s.Id, out var e) && ReferenceEquals(e.Session, s) ? e.Expiry.Token : null;
    }

    static string NewRid(LiveSession s)
    {
        string rid;
        do rid = "r" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        while (s.Find(rid) is not null);
        return rid;
    }

    /// <summary>
    /// Call under Gate: the request is active and findable before the 202, so an instant approve finds it and a second
    /// request gets busy. The run itself takes Gate, so it starts in the background and is never awaited here. It
    /// doesn't inherit the HTTP request's execution context, since it outlives that request.
    /// </summary>
    void Launch(LiveSession s, RequestRecord r, Func<CancellationToken, Task> run, CancellationToken ct)
    {
        s.Begin(r);
        s.LastActivity = _tp.GetUtcNow();
        using (ExecutionContext.SuppressFlow())
            _ = Task.Run(async () =>
            {
                try { await run(ct); }
                catch (Exception e)
                {
                    // The runner ends its own failures in done. This only stops a bug in it from leaving the session busy
                    // and the visitor waiting for good. The visitor's done goes out first, the log line after.
                    await EndCrashedAsync(s, r);
                    _log.LogError(RunnerCrashed, e, "Request {Rid} in session {Sid} escaped its runner", r.Rid, s.Id);
                }
            }, CancellationToken.None);
    }

    /// <summary>
    /// The runner threw before its own done: the edits still waiting are applied (as the runner's end would), then
    /// error {internal} and done {failed}, unless it already ended the request. The reserved run stays in the log
    /// without an end (it counted against the limits as a request).
    /// </summary>
    async Task EndCrashedAsync(LiveSession s, RequestRecord r)
    {
        var remaining = 0;
        try { remaining = (await _access.CheckAsync(s.Code.Code)).Remaining; }
        catch (Exception e) { _log.LogError(RunnerCrashed, e, "Couldn't count today's runs for request {Rid}", r.Rid); }
        await s.Gate.WaitAsync(CancellationToken.None);
        try
        {
            if (!ReferenceEquals(s.Active, r)) return;
            try { EditApplier.ApplyAll(s, EditorFor(s), s.Room, r.Rid, _pacer); }
            catch (Exception e) { _log.LogError(RunnerCrashed, e, "Couldn't apply the edits waiting on request {Rid}", r.Rid); }
            r.Outcome = "failed";
            s.End(r);
            s.LastActivity = _tp.GetUtcNow();
            var error = Ev.Error("internal", Prompts.Internal);
            s.Events.Append(error.kind, r.Rid, error.data);
            var done = Ev.Done("failed", r.Usage, remaining);
            s.Events.Append(done.kind, r.Rid, done.data);
        }
        finally { s.Gate.Release(); }
    }
}
