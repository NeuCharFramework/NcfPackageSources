/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SessionTests.cs
    文件功能描述：测试会话的创建、上限、过期、重置与忙碌规则


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;
using Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;
using static Senparc.Xncf.NeuCharBoxLive.Tests.Fakes.ScriptedChatClient;
using ScriptTurn = System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, Microsoft.Extensions.AI.ChatResponse>;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// SessionManager against real AccessService, TokenMeter and in-memory stores, with a scripted model on a
/// FakeTimeProvider. Requests run in the background; every wait is bounded by a real-time guard.
/// </summary>
[TestClass]
public class SessionTests
{
    static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);

    readonly FakeTimeProvider Fake = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    readonly LiveOptions Opt = new();
    readonly ListLogger<RequestRunner> Log = new();
    readonly ListLogger<SessionManager> ManagerLog = new();
    readonly InMemoryLiveStore Store;
    readonly AccessService Access;
    readonly SessionManager M;
    /// <summary>The runtime each request reads its snapshot from; Start and WhatIf set its chat client.</summary>
    readonly StaticLiveRuntime Runtime = new(Snapshots.On(new ScriptedChatClient()));
    ILogger<RequestRunner> _runnerLog;
    InviteCode Code = null!;

    public SessionTests()
    {
        _runnerLog = Log;
        Store = new InMemoryLiveStore(Fake);
        var meter = new TokenMeter(Store, Runtime, Fake);
        Access = new AccessService(Store, Store, meter, Runtime, UsageDay.Utc, Fake);
        // Each runner keeps the logger it was built with; each request, the snapshot it started on.
        M = new SessionManager(Opt, Fake, Access,
            () => new RequestRunner(Opt, new InstantPacer(Fake.Advance), Fake, Store, meter, UsageDay.Utc, _runnerLog),
            Runtime, UsageDay.Utc, ManagerLog);
    }

    /// <summary>Throws on every warning or error, so a runner's own catch block throws past its done.</summary>
    sealed class ThrowingLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning) throw new InvalidOperationException("the logger broke");
        }
    }

    [TestInitialize]
    public async Task Init() => Code = await Store.CreateAsync("visitor@example.com", 20);

    // ---- scripts ----

    static ScriptedChatClient Script(params ChatResponse[] responses) => new(responses.Select(r => (ScriptTurn)((_, _) => r)).ToArray());

    static ChatResponse LampPlan() => Tool("submit_plan", new { steps = new[] { new { id = "s1", text = "Dim the floor lamp to 20%", devices = new[] { "lamp" } } } });

    static ChatResponse SetLamp20() => Tool("set_lamp", new { brightness = 20, step = "s1" });

    static ScriptedChatClient LampOk() => Script(LampPlan(), SetLamp20(), Text("DONE"), Text("I dimmed the floor lamp to 20%."));

    /// <summary>A what-if replays the approved plan without planning: the run, then the summary.</summary>
    static ScriptedChatClient LampWhatIf() => Script(SetLamp20(), Text("DONE"), Text("The floor lamp didn't respond, so I couldn't dim it."));

    static ScriptedChatClient CannotDo() => Script(Tool("cannot_do", new { reason = "I can't order food here.", suggestions = new[] { "Dim the lamp" } }));

    static ScriptedChatClient Throws(params ChatResponse[] first) =>
        new(first.Select(r => (ScriptTurn)((_, _) => r)).Append((_, _) => throw new HttpRequestException("503 Service Unavailable")).ToArray());

    // ---- helpers ----

    DateOnly Day => UsageDay.Utc.For(Fake.GetUtcNow());

    LiveSession NewSession(InviteCode? c = null)
    {
        var r = M.Create(c ?? Code, "home");
        Assert.IsNull(r.Error, r.Error);
        return r.Session!;
    }

    async Task<LiveEvent> WaitFor(LiveSession s, string kind, string rid)
    {
        using var guard = new CancellationTokenSource(Guard);
        long after = 0;
        try
        {
            while (true)
            {
                var batch = await s.Events.WaitAfterAsync(after, TimeSpan.FromDays(1), guard.Token);
                if (batch.FirstOrDefault(e => e.Kind == kind && e.Rid == rid) is { } hit) return hit;
                if (batch.Count > 0) after = batch[^1].Seq;
            }
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"no '{kind}' event for {rid}");
            throw;
        }
    }

    async Task<string> Outcome(LiveSession s, string rid) => (string)(await WaitFor(s, "done", rid)).Data["outcome"]!;

    static async Task<IReadOnlyList<LiveEvent>> Of(LiveSession s, string rid) =>
        (await s.Events.WaitAfterAsync(0, TimeSpan.Zero, CancellationToken.None)).Where(e => e.Rid == rid).ToList();

    async Task<string> Start(LiveSession s, IChatClient chat, string text = "Dim the lamp")
    {
        Runtime.SetChat(chat);
        var r = await M.StartRequestAsync(s, text);
        Assert.IsNull(r.Error, r.Error);
        Assert.IsNotNull(r.Rid);
        return r.Rid;
    }

    async Task Decide(LiveSession s, string rid, Decision d)
    {
        await WaitFor(s, "awaiting_approval", rid);
        Assert.IsNull(s.TryDecide(rid, d));
    }

    async Task<string> RunOk(LiveSession s)
    {
        var rid = await Start(s, LampOk());
        await Decide(s, rid, Decision.Approve);
        Assert.AreEqual("ok", await Outcome(s, rid));
        return rid;
    }

    async Task<string> RunDeclined(LiveSession s)
    {
        var rid = await Start(s, Script(LampPlan()));
        await Decide(s, rid, Decision.Decline);
        Assert.AreEqual("declined", await Outcome(s, rid));
        return rid;
    }

    async Task<string> WhatIf(LiveSession s, string rid, string device = "lamp")
    {
        Runtime.SetChat(LampWhatIf());
        var w = await M.StartWhatIfAsync(s, rid, device);
        Assert.IsNull(w.Error, w.Error);
        Assert.IsNotNull(w.Rid);
        await WaitFor(s, "done", w.Rid);
        return w.Rid;
    }

    /// <summary>Waits until the store holds n runs, all finished (the runner logs the end after done).</summary>
    async Task<IReadOnlyList<RunRow>> Logged(int n)
    {
        var until = DateTime.UtcNow + Guard;
        while (true)
        {
            var rows = await Store.ListAsync(1000);
            if (rows.Count == n && rows.All(r => r.End is not null)) return rows;
            if (DateTime.UtcNow > until) Assert.Fail($"expected {n} finished runs, found {rows.Count} ({rows.Count(r => r.End is null)} running)");
            await Task.Delay(10);
        }
    }

    async Task Seed(int n)
    {
        for (var i = 0; i < n; i++)
            await Store.StartAsync(new RunStart(Code.Code, "home", "earlier", false, null, Snapshots.Model, Day, Fake.GetUtcNow()));
    }

    // ---- sessions ----

    [TestMethod]
    public void Session_ExpiresAfterSessionIdle()
    {
        var s = NewSession();
        Assert.AreSame(s, M.Get(s.Id, Code));
        Assert.IsTrue(s.Id.Length >= 32, "an unguessable id");
        Assert.AreNotEqual(s.Id, NewSession().Id);

        var almost = Opt.SessionIdle - TimeSpan.FromMinutes(1); // 44 of the default 45 minutes
        Fake.Advance(almost);
        Assert.AreSame(s, M.Get(s.Id, Code), "a minute short of SessionIdle");
        Fake.Advance(almost);
        Assert.AreSame(s, M.Get(s.Id, Code), "every call restarts the idle clock");
        Fake.Advance(Opt.SessionIdle);
        Assert.IsNull(M.Get(s.Id, Code), "SessionIdle idle");
        Assert.IsNull(M.Get(s.Id, Code));
    }

    [TestMethod]
    public void Custom_RebuildsNeverHitCodeLimit()
    {
        var room = CustomRoom.Build(new[] { new RoomDevice("floor_lamp", "floor_lamp") });
        for (var i = 0; i < 6; i++)
        {
            var r = M.Create(Code, room);
            Assert.IsNull(r.Error, $"rebuild {i + 1}");
            Assert.IsNotNull(r.Session);
            Assert.IsTrue(M.Count <= 5, $"count {M.Count}");
        }
        Assert.AreEqual(5, M.Count);
    }

    [TestMethod]
    public void Sweep_RemovesIdleSessionsOnly()
    {
        var idle = NewSession();
        var used = NewSession();
        Assert.AreEqual(2, M.Count);
        Fake.Advance(Opt.SessionIdle - TimeSpan.FromMinutes(10));
        Assert.IsNotNull(M.Get(used.Id, Code));
        Fake.Advance(TimeSpan.FromMinutes(10));

        M.Sweep();

        Assert.AreEqual(1, M.Count);
        Assert.IsNull(M.Get(idle.Id, Code));
        Assert.AreSame(used, M.Get(used.Id, Code));
    }

    [TestMethod]
    public async Task Sweep_ExpiredSession_CancelsItsRequest()
    {
        // Shorter than MaxWork + ApprovalWait, to stand for a stuck request: valid options can't idle out a live one (with
        // these options AddNcbLive would keep live off; the manager built by hand runs anyway).
        Opt.SessionIdle = TimeSpan.FromMinutes(5);
        var s = NewSession();
        var rid = await Start(s, Script(LampPlan()));
        await WaitFor(s, "awaiting_approval", rid);

        Fake.Advance(TimeSpan.FromMinutes(5));
        M.Sweep();

        Assert.AreEqual("failed", await Outcome(s, rid), "the background request is cancelled with its session");
        Assert.AreEqual(0, M.Count);
        Assert.AreEqual("failed", (await Logged(1)).Single().End!.Outcome, "and still logged");
        Assert.IsNull(s.Active);
    }

    [TestMethod]
    public async Task Session_OtherCodeCantSeeIt()
    {
        var other = await Store.CreateAsync("other@example.com", 20);
        var s = NewSession();

        Assert.IsNull(M.Get(s.Id, other));
        Assert.IsNull(M.Get("no-such-session", Code));
        Assert.AreSame(s, M.Get(s.Id, Code));
        Assert.AreSame(Code, s.Code, "a refused Get leaves the session's code alone");
    }

    [TestMethod]
    public async Task Session_GetRefreshesTheCode()
    {
        var s = NewSession();
        await Store.SetLimitAsync(Code.Id, 3);
        var fresh = (await Store.FindAsync(InviteCodes.Normalize(Code.Code)))!;

        Assert.AreSame(s, M.Get(s.Id, fresh));
        Assert.AreEqual(3, s.Code.DailyLimit, "the runner's remainingToday uses the code's current limit");
    }

    [TestMethod]
    public void Session_UnknownScene_Throws() =>
        Assert.ThrowsException<KeyNotFoundException>(() => M.Create(Code, "moon-base"));

    // ---- session caps (final review I2) ----

    /// <summary>Five sessions on Code, a minute apart, oldest first.</summary>
    List<LiveSession> FiveSessions()
    {
        var list = new List<LiveSession>();
        for (var i = 0; i < SessionManager.SessionsPerCode; i++)
        {
            list.Add(NewSession());
            Fake.Advance(TimeSpan.FromMinutes(1));
        }
        return list;
    }

    [TestMethod]
    public void Session_SixthForACode_EndsItsLeastRecentlyActiveIdleSession()
    {
        Assert.AreEqual(5, SessionManager.SessionsPerCode);
        var five = FiveSessions();
        Assert.IsNotNull(M.Get(five[0].Id, Code), "the oldest is touched: the second is now the least recently active");
        var expiry = M.ExpiryOf(five[1])!.Value;

        var sixth = NewSession();

        Assert.IsNull(M.Get(five[1].Id, Code), "its tab now gets no_session");
        Assert.IsNull(M.ExpiryOf(five[1]));
        Assert.IsTrue(expiry.IsCancellationRequested, "its SSE stream ends");
        foreach (var s in five.Where(s => s != five[1]).Append(sixth)) Assert.AreSame(s, M.Get(s.Id, Code));
        Assert.AreEqual(5, M.Count);
    }

    [TestMethod]
    public async Task Session_SixthForACode_SkipsBusySessions_AndAllBusyIsCodeLimit()
    {
        var five = FiveSessions();
        // five[0] waits for approval and is then the least recently active: busy, so the oldest idle one goes instead.
        var rid0 = await Start(five[0], Script(LampPlan()));
        await WaitFor(five[0], "awaiting_approval", rid0);
        foreach (var s in five.Skip(1))
        {
            Fake.Advance(TimeSpan.FromSeconds(1));
            Assert.IsNotNull(M.Get(s.Id, Code));
        }
        var sixth = NewSession();
        Assert.IsNull(M.Get(five[1].Id, Code), "the least recently active idle session ended");
        Assert.IsNotNull(M.ExpiryOf(five[0]), "the busy one is kept, though it was less recently active");

        // Every session of the code waiting for approval: nothing to end, so the code is refused.
        var busy = five.Skip(2).Append(sixth).ToList();
        var rids = new List<string>();
        foreach (var s in busy)
        {
            var rid = await Start(s, Script(LampPlan()));
            await WaitFor(s, "awaiting_approval", rid);
            rids.Add(rid);
        }
        var refused = M.Create(Code, "home");
        Assert.AreEqual(new SessionResult(null, "code_limit"), refused);
        Assert.AreEqual(5, M.Count, "nothing ended, nothing added");

        // Another code is not affected by this code's cap.
        var other = await Store.CreateAsync("other@example.com", 20);
        Assert.IsNull(M.Create(other, "home").Error);

        // Once one of them finishes, it is idle again and can be ended for a new one.
        Assert.IsNull(busy[1].TryDecide(rids[1], Decision.Decline));
        Assert.AreEqual("declined", await Outcome(busy[1], rids[1]));
        Assert.IsNull(M.Create(Code, "home").Error);
        Assert.IsNull(M.ExpiryOf(busy[1]), "the only idle one ended");

        Assert.IsNull(five[0].TryDecide(rid0, Decision.Decline));
        for (var i = 0; i < busy.Count; i++)
            if (i != 1) Assert.IsNull(busy[i].TryDecide(rids[i], Decision.Decline));
    }

    [TestMethod]
    public async Task Session_GlobalCap_GlobalLimit_ExpiredSessionsDontCount()
    {
        Opt.MaxSessions = 3;
        var codes = new List<InviteCode>();
        for (var i = 0; i < 4; i++) codes.Add(await Store.CreateAsync($"v{i}@example.com", 20));
        var held = codes.Take(3).Select(c => NewSession(c)).ToList();

        Assert.AreEqual(new SessionResult(null, "global_limit"), M.Create(codes[3], "home"));
        Assert.AreEqual(new SessionResult(null, "global_limit"), M.Create(codes[0], "home"), "codes[0] holds 1, not 5: none of its own to end");
        Assert.AreEqual(3, M.Count);

        // Sessions that have expired (and are not swept yet) don't hold a place: Create removes them and ends them.
        var expiry = M.ExpiryOf(held[0])!.Value;
        Fake.Advance(Opt.SessionIdle);
        Assert.IsNull(M.Create(codes[3], "home").Error, "the idle ones expired");
        Assert.AreEqual(1, M.Count);
        Assert.IsTrue(expiry.IsCancellationRequested);
    }

    [TestMethod]
    public async Task Session_CodeAtItsCap_WhenTheServerIsFull_EndsItsOwnSession()
    {
        var other = await Store.CreateAsync("other@example.com", 20);
        Opt.MaxSessions = SessionManager.SessionsPerCode + 1;
        var five = FiveSessions();
        NewSession(other);
        Assert.AreEqual(new SessionResult(null, "global_limit"), M.Create(await Store.CreateAsync("third@example.com", 20), "home"));

        var sixth = NewSession();
        Assert.IsNull(M.ExpiryOf(five[0]), "its own least recently active session made room");
        Assert.AreEqual(Opt.MaxSessions, M.Count);
        Assert.AreSame(sixth, M.Get(sixth.Id, Code));
    }

    [TestMethod]
    public void Options_MaxSessionsAndAccessCheckRateMustBePositive()
    {
        Assert.AreEqual(200, new LiveOptions().MaxSessions);
        Assert.AreEqual(30, new LiveOptions().AccessCheckPerMinute);
        Assert.AreEqual(0, new LiveOptions { MaxSessions = 1, AccessCheckPerMinute = 1 }.Problems().Count);
        CollectionAssert.AreEqual(new[] { "MaxSessions" }, new LiveOptions { MaxSessions = 0 }.Problems().ToList());
        CollectionAssert.AreEqual(new[] { "AccessCheckPerMinute" }, new LiveOptions { AccessCheckPerMinute = 0 }.Problems().ToList());
    }

    [TestMethod]
    public void Manifest_ParsedOncePerScene_SessionsShareIt_RoomsStayTheirOwn()
    {
        Assert.AreSame(ManifestLoader.Load("home"), ManifestLoader.Load("home"), "cached, not parsed per session");
        var a = NewSession();
        var b = NewSession();
        Assert.AreSame(a.Scene, b.Scene);

        a.Room.Set("lamp.brightness", 0.5);
        a.PostSetup.Set("lamp.on", true);
        Assert.AreEqual(0.0, (double)b.Room.Get("lamp.brightness")!, "each session's room is its own copy");
        Assert.AreEqual(false, (bool)b.PostSetup.Get("lamp.on")!);
        Assert.AreEqual(false, (bool)ManifestLoader.Load("home").Device("lamp")!.Initial["on"]!, "the shared manifest is untouched");
        Assert.AreEqual(0, (int)ManifestLoader.Load("home").Device("lamp")!.Initial["brightness"]!);

        // Read from many threads at once (sessions on different requests): every reader sees the same, whole manifest.
        var m = ManifestLoader.Load("home");
        var texts = Enumerable.Range(0, 16).AsParallel().Select(_ => string.Join("|", m.Devices.SelectMany(d =>
            d.Actions.Select(x => x.Name + x.Parameters.ToJsonString() + (x.SafeWhen?.ToJsonString() ?? "")).Append(d.Initial.ToJsonString())))).Distinct().ToList();
        Assert.AreEqual(1, texts.Count);
    }

    [TestMethod]
    public async Task Session_HistoryKeepsLast6Turns()
    {
        var s = NewSession();
        for (var i = 1; i <= 7; i++)
        {
            var rid = await Start(s, CannotDo(), $"Order pizza {i}");
            Assert.AreEqual("cannot_do", await Outcome(s, rid));
        }

        Assert.AreEqual(Opt.HistoryTurns, s.History.Count);
        CollectionAssert.AreEqual(Enumerable.Range(2, 6).Select(i => $"Order pizza {i}").ToList(), s.History.Select(t => t.User).ToList());
    }

    [TestMethod]
    public async Task Session_ResetRestoresPostSetupAndRefusesMidRequest()
    {
        var s = NewSession();
        await RunOk(s);
        Assert.AreEqual(0.2, (double)s.Room.Get("lamp.brightness")!);

        Assert.IsTrue(M.Reset(s));
        Assert.AreEqual(s.PostSetup.ToWire().ToJsonString(), s.Room.ToWire().ToJsonString());
        Assert.AreNotSame(s.PostSetup, s.Room, "a copy, so the next request can't change the snapshot");
        s.Room.Set("lamp.brightness", 0.5);
        Assert.AreEqual(0.0, (double)s.PostSetup.Get("lamp.brightness")!);

        var rid = await Start(s, Script(LampPlan()));
        await WaitFor(s, "awaiting_approval", rid);
        Assert.IsFalse(M.Reset(s), "mid-request");
        Assert.AreEqual(0.5, (double)s.Room.Get("lamp.brightness")!, "a refused reset changes nothing");
        Assert.IsNull(s.TryDecide(rid, Decision.Decline));
        Assert.AreEqual("declined", await Outcome(s, rid));
        Assert.IsTrue(M.Reset(s));
    }

    [TestMethod]
    public async Task Session_ResetEmitsNoEvent()
    {
        var s = NewSession();
        await RunOk(s);
        var seq = s.Events.LastSeq;

        Assert.IsTrue(M.Reset(s));

        Assert.AreEqual(seq, s.Events.LastSeq);
    }

    // ---- requests ----

    [TestMethod]
    public async Task Session_OneActiveRequest()
    {
        var s = NewSession();
        Runtime.SetChat(Script(LampPlan()));
        var both = await Task.WhenAll(Task.Run(() => M.StartRequestAsync(s, "Dim the lamp")), Task.Run(() => M.StartRequestAsync(s, "Close the blinds")));

        var won = both.Single(r => r.Error is null);
        Assert.IsNotNull(won.Rid);
        Assert.AreEqual(new StartResult(null, "busy"), both.Single(r => r.Error is not null), "two tabs on one session");
        await WaitFor(s, "awaiting_approval", won.Rid);
        Assert.AreEqual(new StartResult(null, "busy"), await M.StartRequestAsync(s, "Close the blinds"), "second StartRequestAsync while Active != null");
        Assert.AreEqual(1, await Store.CountTodayAsync(Day, Code.Code), "busy reserves nothing");

        Assert.IsNull(s.TryDecide(won.Rid, Decision.Decline));
        Assert.AreEqual("declined", await Outcome(s, won.Rid));
        Assert.IsNull((await M.StartRequestAsync(s, "Close the blinds")).Error, "free again once it's done");
    }

    [TestMethod]
    public async Task Session_RequestIsTrackedBeforeStartReturns()
    {
        var s = NewSession();
        var release = new ManualResetEventSlim();
        Runtime.SetChat(new ScriptedChatClient((_, _) => { release.Wait(Guard); return LampPlan(); }));

        var r = await M.StartRequestAsync(s, "  Dim the lamp  ");

        Assert.IsNotNull(r.Rid);
        Assert.AreSame(s.Find(r.Rid), s.Active, "active before the 202, so an instant approve or a second request sees it");
        Assert.AreEqual("not_waiting", s.TryDecide(r.Rid, Decision.Approve), "known (not no_request); no card open yet");
        Assert.AreEqual("Dim the lamp", s.Find(r.Rid)!.Text, "trimmed");
        Assert.IsNotNull(s.Find(r.Rid)!.RunId, "admission's run id");
        release.Set();
        await Decide(s, r.Rid, Decision.Decline);
        Assert.AreEqual("declined", await Outcome(s, r.Rid));

        var row = (await Logged(1)).Single();
        Assert.AreEqual(s.Find(r.Rid)!.RunId, row.Id, "the runner logs the end on admission's row, not a second one");
        Assert.AreEqual("Dim the lamp", row.Start.Text);
        Assert.IsFalse(row.Start.WhatIf);
        Assert.AreEqual("declined", row.End!.Outcome);
    }

    [TestMethod]
    public async Task Session_LimitsPassThroughAndBusyComesFirst()
    {
        var s = NewSession();
        await Seed(19);
        var rid = await Start(s, Script(LampPlan()));
        await WaitFor(s, "awaiting_approval", rid);
        Assert.AreEqual("busy", (await M.StartRequestAsync(s, "Close the blinds")).Error, "busy before code_limit");
        Assert.IsNull(s.TryDecide(rid, Decision.Decline));
        await Outcome(s, rid);

        var seq = s.Events.LastSeq;
        Assert.AreEqual(new StartResult(null, "code_limit"), await M.StartRequestAsync(s, "Close the blinds"));
        Assert.IsNull(s.Active);
        Assert.AreEqual(seq, s.Events.LastSeq, "a refused request emits nothing");
        Assert.AreEqual(20, await Store.CountTodayAsync(Day, Code.Code));
    }

    [TestMethod]
    public async Task Session_TwoTabsAtLimit_OnlyOneStarts()
    {
        await Seed(19);
        var tab1 = NewSession();
        var tab2 = NewSession();
        Runtime.SetChat(Script(LampPlan())); // only one request gets to plan

        var both = await Task.WhenAll(Task.Run(() => M.StartRequestAsync(tab1, "Dim the lamp")), Task.Run(() => M.StartRequestAsync(tab2, "Dim the lamp")));

        Assert.AreEqual(1, both.Count(r => r.Rid is not null && r.Error is null));
        Assert.AreEqual(1, both.Count(r => r.Error == "code_limit" && r.Rid is null));
        Assert.AreEqual(20, await Store.CountTodayAsync(Day, Code.Code));
        var winner = both[0].Rid is not null ? tab1 : tab2;
        var rid = both.Single(r => r.Rid is not null).Rid!;
        await Decide(winner, rid, Decision.Decline);
        await Outcome(winner, rid);
    }

    // ---- what-ifs ----

    [TestMethod]
    public async Task Session_WhatIfRidChecks()
    {
        var s = NewSession();
        Assert.AreEqual(new StartResult(null, "no_request"), await M.StartWhatIfAsync(s, "r-unknown", "lamp"));

        var a = await Start(s, LampOk());
        await WaitFor(s, "awaiting_approval", a);
        Assert.AreEqual(new StartResult(null, "not_waiting"), await M.StartWhatIfAsync(s, a, "lamp"), "the active request's rid");
        Assert.IsNull(s.TryDecide(a, Decision.Approve));
        Assert.AreEqual("ok", await Outcome(s, a));

        var b = await Start(s, Script(LampPlan()));
        await WaitFor(s, "awaiting_approval", b);
        Assert.AreEqual(new StartResult(null, "busy"), await M.StartWhatIfAsync(s, a, "lamp"), "mid-request");
        Assert.IsNull(s.TryDecide(b, Decision.Decline));
        await Outcome(s, b);

        var seq = s.Events.LastSeq;
        Assert.AreEqual(new StartResult(null, "bad_request"), await M.StartWhatIfAsync(s, a, "toaster"), "not a device in the scene");
        Assert.AreEqual(new StartResult(null, "bad_request"), await M.StartWhatIfAsync(s, a, ""));
        Assert.AreEqual(new StartResult(null, "not_waiting"), await M.StartWhatIfAsync(s, b, "lamp"));
        Assert.AreEqual(seq, s.Events.LastSeq, "refusals emit nothing");
        Assert.IsNull(s.Active);

        var w = await WhatIf(s, a);
        Assert.AreNotEqual(a, w);
        Assert.AreEqual("lamp", s.Find(w)!.WhatIfDevice);
        Assert.AreEqual(new StartResult(null, "not_waiting"), await M.StartWhatIfAsync(s, w, "lamp"), "a what-if's own rid");

        var rows = await Logged(3);
        Assert.AreEqual(2, await Store.CountTodayAsync(Day, Code.Code), "the what-if skipped admission and doesn't count");
        Assert.IsTrue(rows.Single(r => r.Start.WhatIf).Start.WhatIfDevice == "lamp");
        Assert.AreEqual(2, (await Store.FindAsync(InviteCodes.Normalize(Code.Code)))!.UsesTotal, "nor does it touch the code");
    }

    [TestMethod]
    public async Task WhatIf_AfterDeclinedNext_TargetsEarlierRun()
    {
        var s = NewSession();
        var a = await RunOk(s);
        var aPlan = (await Of(s, a)).Single(e => e.Kind == "plan").Data.ToJsonString();

        var declined = await RunDeclined(s);
        var cannot = await Start(s, CannotDo(), "Order a pizza");
        Assert.AreEqual("cannot_do", await Outcome(s, cannot));
        var expired = await Start(s, Script(LampPlan()));
        await WaitFor(s, "awaiting_approval", expired);
        Fake.Advance(Opt.ApprovalWait);
        Assert.AreEqual("expired", await Outcome(s, expired));
        Opt.MaxCalls = 2; // plan + one run call, then limit {calls}: approved, but not replayable
        var limited = await Start(s, Script(LampPlan(), SetLamp20(), Text("DONE")));
        await Decide(s, limited, Decision.Approve);
        Assert.AreEqual("limit", await Outcome(s, limited));
        Opt.MaxCalls = new LiveOptions().MaxCalls;
        Assert.IsNotNull(s.Find(limited)!.Plan);

        foreach (var rid in new[] { declined, cannot, expired, limited })
            Assert.AreEqual(new StartResult(null, "not_waiting"), await M.StartWhatIfAsync(s, rid, "lamp"), s.Find(rid)!.Outcome);
        Assert.AreSame(s.Find(a), s.LastReplayable);
        Assert.AreSame(s.Find(limited), s.LastFinished);

        var w = await WhatIf(s, a);
        Assert.AreEqual(aPlan, (await Of(s, w)).Single(e => e.Kind == "plan").Data.ToJsonString(), "a's approved plan replays");
    }

    [TestMethod]
    public async Task WhatIf_PlanningOnlyFailure_NotWaiting()
    {
        var s = NewSession();
        var p = await Start(s, Throws());
        Assert.AreEqual("failed", await Outcome(s, p));
        Assert.IsNull(s.Find(p)!.Plan);
        Assert.AreEqual(new StartResult(null, "not_waiting"), await M.StartWhatIfAsync(s, p, "lamp"), "nothing was approved");

        var a = await RunOk(s);
        var q = await Start(s, Throws());
        Assert.AreEqual("failed", await Outcome(s, q));
        Assert.AreEqual(new StartResult(null, "not_waiting"), await M.StartWhatIfAsync(s, q, "lamp"));
        Assert.AreSame(s.Find(a), s.LastReplayable, "a planning-only failure doesn't replace the target");

        // A request that failed after its plan was approved is replayable.
        var f = await Start(s, Throws(LampPlan(), SetLamp20()));
        await Decide(s, f, Decision.Approve);
        Assert.AreEqual("failed", await Outcome(s, f));
        Assert.AreSame(s.Find(f), s.LastReplayable);
        Assert.AreEqual(new StartResult(null, "not_waiting"), await M.StartWhatIfAsync(s, a, "lamp"), "a is no longer the latest");
        await WhatIf(s, f);
    }

    [TestMethod]
    public async Task WhatIf_Twice_SameRid_ReplaysOriginal()
    {
        var s = NewSession();
        var a = await RunOk(s);

        var w1 = await WhatIf(s, a);
        var w2 = await WhatIf(s, a);

        var room1 = (await Of(s, w1))[0];
        var room2 = (await Of(s, w2))[0];
        Assert.AreEqual("room", room1.Kind);
        Assert.AreEqual("room", room2.Kind);
        Assert.AreEqual(room1.Data.ToJsonString(), room2.Data.ToJsonString(), "both replay a from its own Before");
        Assert.AreEqual(0.0, (double)room2.Data["state"]!["lamp"]!["brightness"]!, "the room just before a");
        Assert.AreSame(s.Find(a), s.LastReplayable);
        Assert.AreSame(s.Find(a), s.LastFinished);
    }

    [TestMethod]
    public async Task WhatIf_BudgetSpent_Refused()
    {
        var s = NewSession();
        var a = await RunOk(s);
        Runtime.SetLimits(l => l with { DailyTokenBudget = 1 }); // the run's tokens alone spend it

        Assert.AreEqual(new StartResult(null, "no_request"), await M.StartWhatIfAsync(s, "r-unknown", "lamp"), "the rid checks come first");
        Assert.AreEqual(new StartResult(null, "bad_request"), await M.StartWhatIfAsync(s, a, "toaster"));
        var seq = s.Events.LastSeq;
        Assert.AreEqual(new StartResult(null, "budget"), await M.StartWhatIfAsync(s, a, "lamp"));
        Assert.AreEqual(new StartResult(null, "budget"), await M.StartRequestAsync(s, "Close the blinds"));
        Assert.AreEqual(seq, s.Events.LastSeq, "nothing started");
        Assert.IsNull(s.Active);
        Assert.AreEqual(1, (await Logged(1)).Count, "no what-if run was logged");

        Runtime.SetLimits(_ => LiveLimits.Defaults);
        await WhatIf(s, a);
    }

    /// <summary>
    /// The runtime turned off after the route checked it: a request or what-if reads its own snapshot and is refused with
    /// live_off, reserving nothing. Busy and the what-if target checks still come first.
    /// </summary>
    [TestMethod]
    public async Task OffSnapshot_RefusesRequestsAndWhatIfs()
    {
        var s = NewSession();
        var a = await RunOk(s);
        var on = Runtime.Current;
        Runtime.Current = on with { OffReason = LiveOff.Disabled };

        var seq = s.Events.LastSeq;
        Assert.AreEqual(new StartResult(null, "live_off"), await M.StartRequestAsync(s, "Close the blinds"));
        Assert.AreEqual(new StartResult(null, "no_request"), await M.StartWhatIfAsync(s, "r-unknown", "lamp"), "the rid checks come first");
        Assert.AreEqual(new StartResult(null, "live_off"), await M.StartWhatIfAsync(s, a, "lamp"));
        Runtime.Current = LiveSnapshot.Off(LiveOff.NoModel, LiveHost.Origins);
        Assert.AreEqual(new StartResult(null, "live_off"), await M.StartRequestAsync(s, "Close the blinds"));
        Assert.AreEqual(seq, s.Events.LastSeq, "nothing started");
        Assert.IsNull(s.Active);
        Assert.AreEqual(1, (await Logged(1)).Count, "nothing was reserved or logged");

        Runtime.Current = on;
        await WhatIf(s, a);
    }

    // ---- reset and the what-if target ----

    [TestMethod]
    public async Task Reset_ClearsWhatIfTargetAndAddsATurn()
    {
        var s = NewSession();
        var a = await RunOk(s);
        Assert.AreSame(s.Find(a), s.LastReplayable);

        Assert.IsTrue(M.Reset(s));

        Assert.IsNull(s.LastReplayable);
        Assert.AreEqual(new StartResult(null, "not_waiting"), await M.StartWhatIfAsync(s, a, "lamp"), "a what-if after a reset");
        Assert.AreEqual(2, s.History.Count);
        Assert.AreEqual(new Turn(Prompts.ResetUser, Prompts.ResetAssistant), s.History[^1]);
        StringAssert.Contains(Prompts.ResetAssistant, "schedules and rules are gone");

        // The next request sees that turn, and once it ends ok it is the new target.
        var chat = LampOk();
        var b = await Start(s, chat);
        await Decide(s, b, Decision.Approve);
        Assert.AreEqual("ok", await Outcome(s, b));
        CollectionAssert.Contains(chat.Calls[0].Messages.Select(m => m.Text).ToList(), Prompts.ResetAssistant);
        Assert.AreSame(s.Find(b), s.LastReplayable);
    }

    // ---- options, expiry token, a runner that breaks ----

    [TestMethod]
    public void Options_SessionIdleMustCoverWorkAndApproval()
    {
        Assert.AreEqual(0, new LiveOptions().Problems().Count);
        Assert.AreEqual(0, new LiveOptions { SessionIdle = TimeSpan.FromMinutes(40) }.Problems().Count); // exactly MaxWork + ApprovalWait

        var bad = new LiveOptions { SessionIdle = TimeSpan.FromMinutes(40) - TimeSpan.FromSeconds(1) };
        CollectionAssert.AreEqual(new[] { "SessionIdle" }, bad.Problems().ToList());
        var longWait = new LiveOptions { ApprovalWait = TimeSpan.FromMinutes(40) };
        CollectionAssert.AreEqual(new[] { "SessionIdle" }, longWait.Problems().ToList());
        // Neither stops the manager any more: AddNcbLive keeps live off instead (RuntimeTests).
        foreach (var o in new[] { bad, longWait })
            Assert.IsNotNull(new SessionManager(o, Fake, Access, () => throw new NotSupportedException(), Runtime, UsageDay.Utc));
    }

    [TestMethod]
    public void ExpiryOf_CancelledWhenTheSessionExpires()
    {
        var s = NewSession();
        var token = M.ExpiryOf(s);
        Assert.IsNotNull(token);
        Assert.IsFalse(token.Value.IsCancellationRequested);

        Fake.Advance(Opt.SessionIdle);
        M.Sweep();

        Assert.IsTrue(token.Value.IsCancellationRequested, "an SSE loop on it ends");
        Assert.IsNull(M.ExpiryOf(s), "gone");

        var t = NewSession();
        var other = M.ExpiryOf(t)!.Value;
        Fake.Advance(Opt.SessionIdle);
        Assert.IsNull(M.Get(t.Id, Code));
        Assert.IsTrue(other.IsCancellationRequested, "Get finding it idle ends it too");
    }

    [TestMethod]
    public async Task RunnerCrash_LogsAndEndsTheRequest()
    {
        var s = NewSession();
        _runnerLog = new ThrowingLogger<RequestRunner>(); // the runner's catch for the model error throws past its done
        var rid = await Start(s, Throws());

        Assert.AreEqual("failed", await Outcome(s, rid), "the visitor gets a done");
        var ev = await Of(s, rid);
        CollectionAssert.AreEqual(new[] { "error", "done" }, ev.TakeLast(2).Select(e => e.Kind).ToList());
        Assert.AreEqual("internal", (string)ev[^2].Data["kind"]!);
        Assert.AreEqual(1, ev.Count(e => e.Kind == "done"));
        Assert.AreEqual(19, (int)ev[^1].Data["remainingToday"]!);
        Assert.IsNull(s.Active);
        Assert.AreEqual("failed", s.Find(rid)!.Outcome);

        var until = DateTime.UtcNow + Guard;
        while (ManagerLog.Entries.Count == 0 && DateTime.UtcNow < until) await Task.Delay(10);
        var entry = ManagerLog.Entries.Single();
        Assert.AreEqual(LogLevel.Error, entry.Level);
        Assert.AreEqual(SessionManager.RunnerCrashed, entry.Id);

        _runnerLog = Log;
        await RunOk(s); // not left busy
    }
}
