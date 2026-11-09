/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SessionEditTests.cs
    文件功能描述：测试会话内的 set 编辑与自动化开关，以及排队应用


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
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
/// Hand edits through SessionManager, the applier behind them, and edits made while a request runs (scripted model,
/// FakeTimeProvider, controllable pacers; every wait is bounded by a real-time guard).
/// </summary>
[TestClass]
public class SessionEditTests
{
    static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);

    readonly FakeTimeProvider Fake = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    readonly LiveOptions Opt = new();
    readonly InMemoryLiveStore Store;
    readonly AccessService Access;
    readonly TokenMeter Meter;
    readonly ControlledPacer Pacer = new();
    readonly ListLogger<RequestRunner> Log = new();
    SessionManager M;
    InviteCode Code = null!;

    // What the next request's runner is built with (and the runtime its snapshot is read from).
    readonly StaticLiveRuntime Runtime = new(Snapshots.On(new ScriptedChatClient()));
    IPacer _runPacer = new InstantPacer();
    ILogger<RequestRunner> _runnerLog;

    public SessionEditTests()
    {
        Store = new InMemoryLiveStore(Fake);
        Meter = new TokenMeter(Store, Runtime, Fake);
        Access = new AccessService(Store, Store, Meter, Runtime, UsageDay.Utc, Fake);
        _runnerLog = Log;
        M = Manager(Pacer);
    }

    /// <summary>Edits made with no request running pace their delayed frames on <paramref name="edits"/>.</summary>
    SessionManager Manager(IPacer edits) => new(Opt, Fake, Access, () =>
    {
        var (pacer, log) = (_runPacer, _runnerLog); // each runner keeps what it was built with
        return new RequestRunner(Opt, pacer, Fake, Store, Meter, UsageDay.Utc, log);
    }, Runtime, UsageDay.Utc, pacer: edits);

    /// <summary>Each wait blocks until the test calls Release; Waits records the requested delays.</summary>
    sealed class ControlledPacer : IPacer
    {
        readonly SemaphoreSlim _gate = new(0);
        readonly List<TimeSpan> _waits = new();
        public IReadOnlyList<TimeSpan> Waits { get { lock (_waits) return _waits.ToList(); } }
        public void Release(int n = 1) => _gate.Release(n);

        public async Task WaitAsync(TimeSpan d, CancellationToken ct)
        {
            lock (_waits) _waits.Add(d);
            await _gate.WaitAsync(ct);
        }
    }

    /// <summary>
    /// Each wait ends when the test calls Next, and the waiter's continuation runs inside Next (no async continuations),
    /// so when Next returns, whatever the waiter did up to its next wait is done. Registered completes per wait started.
    /// </summary>
    sealed class StepPacer : IPacer
    {
        readonly Queue<TaskCompletionSource> _waits = new();
        readonly SemaphoreSlim _registered = new(0);

        public Task WaitAsync(TimeSpan d, CancellationToken ct)
        {
            var t = new TaskCompletionSource();
            lock (_waits) _waits.Enqueue(t);
            _registered.Release();
            return t.Task;
        }

        /// <summary>Waits for the next wait to start, then ends it.</summary>
        public async Task Next()
        {
            Assert.IsTrue(await _registered.WaitAsync(Guard), "no wait started");
            TaskCompletionSource t;
            lock (_waits) t = _waits.Dequeue();
            t.SetResult();
        }
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

    LiveSession NewSession()
    {
        var r = M.Create(Code, "home");
        Assert.IsNull(r.Error, r.Error);
        return r.Session!;
    }

    static RoomEditor EditorFor(LiveSession s) => new(s.Scene, new Simulator(s.Scene, ManifestLoader.Behavior(s.Scene.Id)));

    static IReadOnlyList<LiveEvent> All(LiveSession s) => s.Events.WaitAfterAsync(0, TimeSpan.Zero, CancellationToken.None).GetAwaiter().GetResult();

    static string PathValue(LiveEvent e) => $"{(string)e.Data["path"]!}={e.Data["value"]!.ToJsonString()}";

    static async Task Idle(LiveSession s)
    {
        using var guard = new CancellationTokenSource(Guard);
        while (s.Busy) await Task.Delay(1, guard.Token);
    }

    static bool IsState(LiveEvent e, string path, object value) =>
        e.Kind == "state" && (string)e.Data["path"]! == path && e.Data["value"]!.ToJsonString() == JsonValue.Create(value)!.ToJsonString();

    static async Task WaitUntil(LiveSession s, Func<IReadOnlyList<LiveEvent>, bool> done)
    {
        using var guard = new CancellationTokenSource(Guard);
        long after = 0;
        while (!done(All(s)))
        {
            var batch = await s.Events.WaitAfterAsync(after, TimeSpan.FromDays(1), guard.Token);
            if (batch.Count > 0) after = batch[^1].Seq;
        }
    }

    static Rule Water() =>
        new("r1", "s2", "soil.moisture", RuleOp.Below, JsonValue.Create(30.0),
            new ActionCall("run_pump", (JsonObject)JsonNode.Parse("{\"seconds\":6}")!, "s2"), null, 2);

    [TestMethod]
    public async Task Edit_Idle_EmitsStateWithSrcAndNoRid()
    {
        var s = NewSession();
        var o = await M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        Assert.IsNull(o.Error);
        var e = All(s).Single(x => IsState(x, "soil.moisture", 25));
        Assert.AreEqual("edit", (string)e.Data["src"]!);
        Assert.IsNull(e.Rid);
        Assert.AreEqual(25, (double)s.Room.Get("soil.moisture")!);
    }

    [TestMethod]
    public async Task Edit_Idle_RuleFireEmitsTimelineRow()
    {
        var s = NewSession();
        s.Room.Rules.Add(Water());
        var o = await M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        Assert.IsNull(o.Error);

        var row = All(s).Single(e => e.Kind == "timeline");
        Assert.AreEqual("rule", (string)row.Data["origin"]!);
        Assert.AreEqual("pump", (string)row.Data["device"]!);
        Assert.AreEqual("edit", (string)row.Data["src"]!);
        Assert.IsNull(row.Rid);

        Pacer.Release(10);
        await WaitUntil(s, ev => ev.Any(e => IsState(e, "pump.running", false)));
        Assert.IsTrue(All(s).Where(e => e.Kind == "state").All(e => (string)e.Data["src"]! == "edit" && e.Rid is null));
    }

    [TestMethod]
    public async Task CustomEdit_SetsFieldAndFiresRule()
    {
        var built = M.Create(Code, CustomRoom.Build(new[] { new RoomDevice("soil_sensor", "soil_sensor"), new RoomDevice("water_pump", "water_pump") }));
        Assert.IsNull(built.Error, built.Error);
        var s = built.Session!;
        s.Room.Rules.Add(new Rule("r1", "s2", "soil_sensor.moisture", RuleOp.Below, JsonValue.Create(30.0),
            new ActionCall("run_water_pump", (JsonObject)JsonNode.Parse("{\"seconds\":6}")!, "s2"), null, 2));

        // The catalog's edit fields are the custom room's: soil moisture 0 to 100, the pump's running switch.
        Assert.IsNull(RoomEditor.Validate(s.Scene, "soil_sensor.moisture", JsonValue.Create(25.0)));
        Assert.IsNull(RoomEditor.Validate(s.Scene, "water_pump.running", JsonValue.Create(false)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(s.Scene, "soil_sensor.moisture", JsonValue.Create(101)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(s.Scene, "soil_sensor.status", JsonValue.Create("fault")));
        Assert.AreEqual("bad_value", RoomEditor.Validate(s.Scene, "water_pump.seconds", JsonValue.Create(5)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(s.Scene, "soil.moisture", JsonValue.Create(25.0)), "Home's path is not this room's");

        var o = await M.EditAsync(s, "soil_sensor.moisture", JsonValue.Create(25.0));
        Assert.IsNull(o.Error);
        Assert.AreEqual(25, (double)s.Room.Get("soil_sensor.moisture")!);
        var row = All(s).Single(e => e.Kind == "timeline");
        Assert.AreEqual("rule", (string)row.Data["origin"]!);
        Assert.AreEqual("water_pump", (string)row.Data["device"]!);
        Assert.IsTrue(IsEdit(row) && row.Rid is null);
        Assert.IsTrue(All(s).Any(e => IsState(e, "water_pump.running", true) && IsEdit(e)), "the rule's run turns the pump on");
        Pacer.Release(10);
        await WaitUntil(s, ev => ev.Any(e => IsState(e, "water_pump.running", false)));

        // A hand edit in a custom room is a plain set: the pump switch is not Home's hand run.
        await Idle(s);
        var seq = All(s)[^1].Seq;
        Assert.IsNull((await M.EditAsync(s, "water_pump.running", JsonValue.Create(true))).Error);
        Assert.IsTrue((bool)s.Room.Get("water_pump.running")!);
        CollectionAssert.AreEqual(new[] { "water_pump.running=true" }, All(s).Where(e => e.Seq > seq).Select(PathValue).ToArray());
    }

    [TestMethod]
    public async Task Edit_FaultedDevice_ReturnsUnavailableAndReemitsTheRoomsValue()
    {
        var s = NewSession();
        s.Room.Set("lamp.status", JsonValue.Create("fault"));
        var o = await M.EditAsync(s, "lamp.brightness", JsonValue.Create(0.5));
        Assert.AreEqual("device_unavailable", o.Error);
        Assert.AreNotEqual(0.5, (double)s.Room.Get("lamp.brightness")!);
        // Nothing changed: the only events are the room's own values for the field, so the card goes back to them.
        var events = All(s);
        Assert.IsTrue(events.All(e => e.Kind == "state" && IsEdit(e) && e.Rid is null));
        CollectionAssert.AreEqual(new[] { "lamp.brightness=0", "lamp.on=false" }, events.Select(PathValue).ToArray());
    }

    [TestMethod]
    public async Task Edit_DoesNotMeterTokensOrCountRequests()
    {
        var s = NewSession();
        var day = UsageDay.Utc.For(Fake.GetUtcNow());
        var before = (await Access.CheckAsync(Code.Code)).Remaining;
        var spent = Meter.Spent(day);

        await M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        await M.EditAsync(s, "lamp.brightness", JsonValue.Create(0.5));

        Assert.AreEqual(before, (await Access.CheckAsync(Code.Code)).Remaining);
        Assert.AreEqual(spent, Meter.Spent(day));
    }

    [TestMethod]
    public async Task Edit_WhileBusy_IsQueuedUntilTheOwnerAppliesIt()
    {
        var s = NewSession();
        s.Advancing = true;
        Assert.IsTrue(s.Busy);

        var pending = M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        Assert.IsFalse(pending.IsCompleted);
        Assert.AreEqual(0, All(s).Count, "nothing is applied while the room is owned");

        Fake.Advance(TimeSpan.FromSeconds(2));
        var o = await pending;
        Assert.AreSame(EditOutcome.Queued, o);

        // The owner takes the edit later and applies it; the queued edit then reaches the room and the log.
        EditApplier.ApplyAll(s, EditorFor(s), s.Room, "r1", Pacer);
        Assert.AreEqual(25, (double)s.Room.Get("soil.moisture")!);
        Assert.AreEqual("r1", All(s).Single(e => IsState(e, "soil.moisture", 25)).Rid);
    }

    [TestMethod]
    public async Task EditQueue_WhenPendingCompletesOnEnqueueAndTakeAllEmpties()
    {
        var q = new EditQueue();
        var wait = q.WhenPending();
        Assert.IsFalse(wait.IsCompleted);
        _ = q.Enqueue("soil.moisture", JsonValue.Create(1));
        await wait.WaitAsync(Guard);
        Assert.IsTrue(q.WhenPending().IsCompleted);
        Assert.AreEqual(1, q.TakeAll().Count);
        Assert.AreEqual(0, q.TakeAll().Count);
        Assert.IsFalse(q.WhenPending().IsCompleted);
    }

    [TestMethod]
    public void ApplyAll_ErrorOutcomeCompletesTheEditAndReemitsTheRoomsValue()
    {
        var s = NewSession();
        s.Room.Set("lamp.status", JsonValue.Create("busy"));
        var t = s.Edits.Enqueue("lamp.brightness", JsonValue.Create(0.5));
        EditApplier.ApplyAll(s, EditorFor(s), s.Room, null, Pacer);
        Assert.AreEqual("device_unavailable", t.Result.Error);
        Assert.AreEqual(0, (double)s.Room.Get("lamp.brightness")!);
        CollectionAssert.AreEqual(new[] { "lamp.brightness=0", "lamp.on=false" }, All(s).Select(PathValue).ToArray());
        Assert.AreEqual(1, s.EditStamp("lamp.brightness"), "emitted as the newest value for the path");
        Assert.AreEqual(1, s.EditStamp("lamp.on"));
    }

    [TestMethod]
    public async Task Edit_QueuedThenFailed_ReemitsTheRoomsValueWithTheOwnersRid()
    {
        var s = NewSession();
        s.Advancing = true; // something owns the room
        var pending = M.EditAsync(s, "lamp.brightness", JsonValue.Create(0.5));
        Fake.Advance(TimeSpan.FromSeconds(2));
        Assert.AreSame(EditOutcome.Queued, await pending, "answered 202: the card keeps showing 50 %");

        // The lamp fails before the owner takes the edit, so the edit can't apply.
        s.Room.Set("lamp.status", JsonValue.Create("fault"));
        EditApplier.ApplyAll(s, EditorFor(s), s.Room, "r1", Pacer);

        var events = All(s);
        Assert.IsTrue(events.All(e => e.Kind == "state" && IsEdit(e) && e.Rid == "r1"));
        CollectionAssert.AreEqual(new[] { "lamp.brightness=0", "lamp.on=false" }, events.Select(PathValue).ToArray(),
            "the card goes back to the room's value");
        Assert.AreEqual(0, (double)s.Room.Get("lamp.brightness")!);
    }

    [TestMethod]
    public async Task Edit_TwoFiresOnOnePath_EndOnTheLaterValue()
    {
        var s = NewSession();
        var start = s.Room.ToWire();
        // Two rules on the soil: the first turns the lamp off (its lamp.on false would go out 1200 ms later), the
        // second sets it to 50 % at once. The room ends at the second's values, and so must the browser.
        s.Room.Rules.Add(new Rule("r1", "s1", "soil.moisture", RuleOp.Below, JsonValue.Create(30.0),
            new ActionCall("set_lamp", (JsonObject)JsonNode.Parse("{\"brightness\":0}")!, "s1"), null, 2));
        s.Room.Rules.Add(new Rule("r2", "s2", "soil.moisture", RuleOp.Below, JsonValue.Create(30.0),
            new ActionCall("set_lamp", (JsonObject)JsonNode.Parse("{\"brightness\":50}")!, "s2"), null, 2));
        Pacer.Release(10); // a delayed frame, if one were left, would go out at once

        var o = await M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        Assert.IsNull(o.Error);
        Assert.AreEqual(2, o.Rows.Count);
        for (var i = 0; i < 50; i++) await Task.Run(() => { }); // let any background emit run

        var lamp = All(s).Where(e => e.Kind == "state" && ((string)e.Data["path"]!).StartsWith("lamp.", StringComparison.Ordinal));
        CollectionAssert.AreEqual(new[] { "lamp.on=true", "lamp.brightness=0.5" }, lamp.Select(PathValue).ToArray());
        Assert.AreEqual(0, Pacer.Waits.Count, "nothing left to pace");
        Assert.AreEqual(true, (bool)s.Room.Get("lamp.on")!);
        AssertBrowserMatchesRoom(s, start);
    }

    [TestMethod]
    public void ApplyAll_Immediate_EmitsDelayedChangesAtOnce()
    {
        var s = NewSession();
        var t = s.Edits.Enqueue("pump.running", JsonValue.Create(true));
        EditApplier.ApplyAll(s, EditorFor(s), s.Room, "r9", Pacer, immediate: true); // Pacer never released: must not wait
        var o = t.Result;
        Assert.IsTrue(o.Changes.Any(c => c.DelayMs > 0));

        var states = All(s).Where(e => e.Kind == "state").ToList();
        Assert.AreEqual(o.Changes.Count, states.Count);
        CollectionAssert.AreEqual(o.Changes.Select(c => c.Path).ToList(), states.Select(e => (string)e.Data["path"]!).ToList());
        Assert.IsTrue(states.All(e => e.Rid == "r9" && (string)e.Data["src"]! == "edit"));
        Assert.AreEqual(0, Pacer.Waits.Count);
    }

    [TestMethod]
    public async Task ApplyAll_NewerEditOnSamePathSuppressesOlderDelayedEmit()
    {
        var s = NewSession();
        // A hand pump-on: pump.running true now, soil rises and the pump stops later.
        var first = await M.EditAsync(s, "pump.running", JsonValue.Create(true));
        var delayedSoil = first.Changes.Single(c => c.Path == "soil.moisture" && c.DelayMs > 0);
        var delayedPump = first.Changes.Single(c => c.Path == "pump.running" && c.DelayMs > 0);
        Assert.IsTrue(delayedSoil.DelayMs < delayedPump.DelayMs);

        // The visitor then sets soil by hand: the pending soil value is stale.
        await M.EditAsync(s, "soil.moisture", JsonValue.Create(50.0));

        Pacer.Release(2);
        await WaitUntil(s, ev => ev.Any(e => IsState(e, "pump.running", false)));

        var soilEvents = All(s).Where(e => e.Kind == "state" && (string)e.Data["path"]! == "soil.moisture").ToList();
        Assert.AreEqual(1, soilEvents.Count, "only the newer edit's soil value was emitted");
        Assert.AreEqual(50.0, (double)soilEvents[0].Data["value"]!);
        CollectionAssert.AreEqual(
            new[] { TimeSpan.FromMilliseconds(delayedSoil.DelayMs), TimeSpan.FromMilliseconds(delayedPump.DelayMs - delayedSoil.DelayMs) },
            Pacer.Waits.ToArray());
    }

    [TestMethod]
    public async Task Reset_DropsPendingDelayedEdits()
    {
        var s = NewSession();
        await M.EditAsync(s, "pump.running", JsonValue.Create(true));
        var before = All(s).Count;
        Assert.IsTrue(M.Reset(s));

        Pacer.Release(2);
        using var guard = new CancellationTokenSource(Guard);
        while (Pacer.Waits.Count < 2) await Task.Delay(1, guard.Token); // the soil emit was decided
        for (var i = 0; i < 50; i++) await Task.Run(() => { }); // let the pump emit be decided

        Assert.AreEqual(before, All(s).Count, "no edit event after the reset");
        Assert.IsFalse(All(s).Skip(before).Any());
    }

    [TestMethod]
    public async Task WhatIf_DropsPendingDelayedEdits()
    {
        var s = NewSession();
        var rid = await Start(s, Script(LampPlan(), SetLamp20(), Text("DONE"), Text(LampSummary)));
        await Approve(s, rid);
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);
        await Idle(s);

        // An idle hand pump-on: its soil rise (2000 ms) and pump-off (3000 ms) frames wait on Pacer.
        await Applied(M.EditAsync(s, "pump.running", JsonValue.Create(true)));
        Assert.AreEqual(34 + 7.2, (double)s.Room.Get("soil.moisture")!, 1e-9);

        // The what-if swaps in the room from before the request (soil 34): the pending frames belong to the old room.
        Runtime.SetChat(Script(SetLamp20(), Text("DONE"), Text(LampSummary)));
        var whatIf = await M.StartWhatIfAsync(s, rid, "camera");
        Assert.IsNull(whatIf.Error, whatIf.Error);
        var room = await WaitFor(s, "room", whatIf.Rid!);
        await WaitFor(s, "done", whatIf.Rid!);
        await Idle(s);

        Pacer.Release(2);
        using var guard = new CancellationTokenSource(Guard);
        while (Pacer.Waits.Count < 2) await Task.Delay(1, guard.Token); // the soil emit was decided
        for (var i = 0; i < 50; i++) await Task.Run(() => { }); // let the pump emit be decided

        var late = All(s).Where(e => e.Seq > room.Seq && e.Kind == "state" && IsEdit(e)).ToList();
        Assert.AreEqual(0, late.Count, string.Join(", ", late.Select(e => e.Data.ToJsonString())));
        Assert.AreEqual(34, (double)s.Room.Get("soil.moisture")!, 1e-9);
        AssertBrowserMatchesRoom(s, room);
    }

    [TestMethod]
    public async Task EditServicingPacer_ServesWhileWaitingAndOnceAfter()
    {
        var q = new EditQueue();
        var served = 0;
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Serve()
        {
            Interlocked.Add(ref served, q.TakeAll().Count);
            first.TrySetResult();
        }

        var blocked = new ControlledPacer();
        var wait = new EditServicingPacer(blocked, q.WhenPending, Serve).WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        _ = q.Enqueue("soil.moisture", JsonValue.Create(20.0));
        await first.Task.WaitAsync(Guard);
        Assert.AreEqual(1, Volatile.Read(ref served));
        Assert.IsFalse(wait.IsCompleted, "served while the inner wait still runs");
        blocked.Release();
        await wait.WaitAsync(Guard);

        // An instant wait is over before the loop can see an edit made during it: it is served once after.
        var instant = new InstantPacer(_ => q.Enqueue("soil.moisture", JsonValue.Create(21.0)));
        await new EditServicingPacer(instant, q.WhenPending, Serve).WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        Assert.AreEqual(2, Volatile.Read(ref served));
        Assert.AreEqual(0, q.TakeAll().Count);
    }

    // ---- edits while a request runs ----

    static ScriptTurn T(ChatResponse r) => (_, _) => r;

    static ScriptedChatClient Script(params ChatResponse[] responses) => new(responses.Select(T).ToArray());

    static object Step(string id, string text, params string[] devices) => new { id, text, devices };

    static ChatResponse SubmitPlan(params object[] steps) => Tool("submit_plan", new { steps });

    static ChatResponse SubmitPlan(double horizon, params object[] steps) => Tool("submit_plan", new { steps, horizon_hours = horizon });

    static ChatResponse LampPlan() => SubmitPlan(Step("s1", "Dim the floor lamp to 20%", "lamp"));

    static ChatResponse SetLamp20() => Tool("set_lamp", new { brightness = 20, step = "s1" });

    const string LampSummary = "I dimmed the floor lamp to 20%.";

    /// <summary>A week away with the watering rule (at most twice a day), the shape of ReplanWhatIfTests' WaterWeek.</summary>
    static ScriptedChatClient WaterWeek() => Script(
        SubmitPlan(168, Step("s1", "Water the plant when the soil is dry", "soil", "pump"), Step("s2", "Dim the floor lamp to 20%", "lamp")),
        Tools(("set_lamp", new { brightness = 20, step = "s2" }),
            ("add_rule", new
            {
                when = new { device = "soil", field = "moisture", op = "below", value = 30 },
                then = new { action = "run_pump", args = new { seconds = 6 } },
                max_per_day = 2,
                step = "s1",
            }),
            ("run_until", new { hours = 168 })),
        Text("DONE"),
        Text("I dimmed the floor lamp and watered the plant when the soil got dry."));

    async Task<string> Start(LiveSession s, IChatClient chat, IPacer? pacer = null)
    {
        Runtime.SetChat(chat);
        if (pacer is not null) _runPacer = pacer;
        var r = await M.StartRequestAsync(s, "Look after the place");
        Assert.IsNull(r.Error, r.Error);
        return r.Rid!;
    }

    static async Task<LiveEvent> WaitFor(LiveSession s, string kind, string rid)
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

    static async Task Approve(LiveSession s, string rid)
    {
        await WaitFor(s, "awaiting_approval", rid);
        Assert.IsNull(s.TryDecide(rid, Decision.Approve));
    }

    static async Task<EditOutcome> Applied(Task<EditOutcome>? edit)
    {
        Assert.IsNotNull(edit, "the edit was made");
        var o = await edit.WaitAsync(Guard);
        Assert.AreNotSame(EditOutcome.Queued, o, "applied, not left queued");
        Assert.IsNull(o.Error, o.Error);
        return o;
    }

    static bool IsEdit(LiveEvent e) => (string?)e.Data["src"] == "edit";

    static List<LiveEvent> States(LiveSession s, string path) =>
        All(s).Where(e => e.Kind == "state" && (string)e.Data["path"]! == path).ToList();

    /// <summary>The replay check: every state event applied in order to the room's starting values gives the room as it is now.</summary>
    static void AssertBrowserMatchesRoom(LiveSession s, JsonObject start)
    {
        var seen = (JsonObject)start.DeepClone();
        foreach (var e in All(s).Where(e => e.Kind == "state"))
        {
            var path = (string)e.Data["path"]!;
            var i = path.IndexOf('.');
            ((JsonObject)seen[path[..i]]!)[path[(i + 1)..]] = e.Data["value"]?.DeepClone();
        }
        foreach (var (dev, node) in s.Room.ToWire())
        {
            if (dev == "env") continue;
            foreach (var (key, v) in node!.AsObject())
                Assert.IsTrue(JsonNode.DeepEquals(v, seen[dev]?[key]),
                    $"{dev}.{key}: the browser has {seen[dev]?[key]?.ToJsonString()}, the room {v?.ToJsonString()}");
        }
    }

    /// <summary>The replay check from a <c>room</c> event: its state, then every later state event, gives the room as it is now.</summary>
    static void AssertBrowserMatchesRoom(LiveSession s, LiveEvent room)
    {
        var seen = (JsonObject)room.Data["state"]!.DeepClone();
        foreach (var e in All(s).Where(e => e.Kind == "state" && e.Seq > room.Seq))
        {
            var path = (string)e.Data["path"]!;
            var i = path.IndexOf('.');
            ((JsonObject)seen[path[..i]]!)[path[(i + 1)..]] = e.Data["value"]?.DeepClone();
        }
        foreach (var (dev, node) in s.Room.ToWire())
        {
            if (dev == "env") continue;
            foreach (var (key, v) in node!.AsObject())
                Assert.IsTrue(JsonNode.DeepEquals(v, seen[dev]?[key]),
                    $"{dev}.{key}: the browser has {seen[dev]?[key]?.ToJsonString()}, the room {v?.ToJsonString()}");
        }
    }

    static JsonObject Facts(ScriptedChatClient chat, int call)
    {
        var text = chat.Calls[call].Messages[^1].Text;
        var at = text.IndexOf(Prompts.FactsHeader, StringComparison.Ordinal);
        Assert.IsTrue(at >= 0, "the summary instruction carries the facts");
        return JsonNode.Parse(text[(at + Prompts.FactsHeader.Length)..])!.AsObject();
    }

    [TestMethod]
    public async Task Edit_DuringApprovalWait_AppliesAtOnce()
    {
        var s = NewSession();
        var rid = await Start(s, Script(LampPlan(), SetLamp20(), Text("DONE"), Text(LampSummary)));
        var ask = await WaitFor(s, "awaiting_approval", rid);

        // No fake time passes: the edit is applied while the card waits, not after the 2 s answer window.
        var o = await Applied(M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0)));
        Assert.IsTrue(o.Changes.Any(c => c.Path == "soil.moisture"));
        var edit = All(s).Single(e => IsState(e, "soil.moisture", 25));
        Assert.AreEqual(rid, edit.Rid);
        Assert.IsTrue(IsEdit(edit));
        Assert.IsTrue(edit.Seq > ask.Seq);
        Assert.IsTrue(s.Find(rid)!.Waiting, "still waiting for the answer");
        Assert.AreEqual(25, (double)s.Room.Get("soil.moisture")!);

        Assert.IsNull(s.TryDecide(rid, Decision.Approve));
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);
        var step = All(s).First(e => e.Kind == "step" && e.Rid == rid);
        Assert.IsTrue(edit.Seq < step.Seq, "the edit's event precedes the approved run");
        Assert.AreEqual(0.2, (double)s.Room.Get("lamp.brightness")!, "the plan ran as approved");
        Assert.AreEqual(25, (double)s.Room.Get("soil.moisture")!);
    }

    [TestMethod]
    public async Task Edit_DuringStalledModelCall_AppliesBeforeTheCallReturns()
    {
        var s = NewSession();
        var chat = Script(Stalled);
        var rid = await Start(s, chat);
        await chat.StallStarted.WaitAsync(Guard);

        await Applied(M.EditAsync(s, "lamp.brightness", JsonValue.Create(0.4)));
        var edit = All(s).Single(e => IsState(e, "lamp.brightness", 0.4));
        Assert.AreEqual(rid, edit.Rid);
        Assert.IsTrue(IsEdit(edit));
        Assert.IsTrue(All(s).Any(e => IsState(e, "lamp.on", true) && e.Rid == rid && IsEdit(e)));
        Assert.IsFalse(All(s).Any(e => e.Rid == rid && e.Kind is "done" or "limit" or "error"), "the planning call is still stalled");
        Assert.AreEqual(1, chat.Calls.Count);
        Assert.AreEqual(0.4, (double)s.Room.Get("lamp.brightness")!);

        Fake.Advance(Opt.MaxWork); // the stalled call runs out of work time
        Assert.AreEqual("limit", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);
    }

    [TestMethod]
    public async Task Edit_DuringFastForward_RuleFiresInThatTimeline()
    {
        var s = NewSession();
        var start = s.Room.ToWire();
        Task<EditOutcome>? edit = null;
        string? label = null;
        // At the first pacer wait inside the fast-forward (after its first clock event), the visitor drops the soil.
        var pacer = new InstantPacer(_ =>
        {
            if (edit is not null || All(s).LastOrDefault(e => e.Kind == "clock") is not { } clock) return;
            label = (string)clock.Data["label"]!;
            edit = M.EditAsync(s, "soil.moisture", JsonValue.Create(20.0));
        });
        var chat = WaterWeek();
        var rid = await Start(s, chat, pacer);
        await Approve(s, rid);
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);

        var ev = All(s);
        var row = ev.SingleOrDefault(e => e.Kind == "timeline" && IsEdit(e));
        Assert.IsNotNull(row, "the edit fired the rule");
        Assert.AreEqual("rule", (string)row.Data["origin"]!);
        Assert.AreEqual("pump", (string)row.Data["device"]!);
        Assert.AreEqual(rid, row.Rid);
        Assert.AreEqual(label, (string)row.Data["at"]!, "at the current simulated time");
        var clocks = ev.Where(e => e.Kind == "clock").ToList();
        var at = clocks.FindIndex(c => (string)c.Data["label"]! == label);
        Assert.IsTrue(row.Seq > clocks[at].Seq && row.Seq < clocks[at + 1].Seq, "in that tick of the week, before the clock moves on");
        await Applied(edit);

        // The fire is in this request's timeline: the summary's facts list it at that time.
        var list = Facts(chat, 3)["timeline"]!["list"]!.AsArray().Select(x => (string)x!).ToList();
        Assert.IsTrue(list.Any(x => x.StartsWith($"{label} ", StringComparison.Ordinal)), string.Join(" | ", list));
        AssertBrowserMatchesRoom(s, start);
    }

    [TestMethod]
    public async Task Edit_DuringFastForward_ALaterChangeBackStillStreams()
    {
        var s = NewSession();
        var start = s.Room.ToWire();
        Task<EditOutcome>? edit = null;
        // The lamp starts off; the visitor turns it on at the first fast-forward wait, and the 18:00 schedule turns it
        // off again, back to what the fast-forward last sent.
        var pacer = new InstantPacer(_ =>
        {
            if (edit is null && All(s).Any(e => e.Kind == "clock")) edit = M.EditAsync(s, "lamp.brightness", JsonValue.Create(0.6));
        });
        var rid = await Start(s, Script(
            SubmitPlan(4, Step("s1", "Turn the floor lamp off at 18:00", "lamp")),
            Tools(("schedule", new { at = "18:00", action = "set_lamp", args = new { brightness = 0 }, step = "s1" }), ("run_until", new { hours = 4 })),
            Text("DONE"),
            Text("I set the floor lamp to turn off at 18:00.")), pacer);
        await Approve(s, rid);
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);
        await Applied(edit);

        Assert.AreEqual(false, (bool)s.Room.Get("lamp.on")!);
        AssertBrowserMatchesRoom(s, start);
    }

    [TestMethod]
    public async Task Edit_AtRequestEnd_IsNotLost()
    {
        var s = NewSession();
        Task<EditOutcome>? edit = null;
        // The summary stream is the last model call: an edit made during it waits for the request's end.
        var chat = new ScriptedChatClient(T(LampPlan()), T(SetLamp20()), T(Text("DONE")), (_, _) =>
        {
            edit = M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
            return Text(LampSummary);
        });
        var rid = await Start(s, chat);
        await Approve(s, rid);
        var done = await WaitFor(s, "done", rid);
        Assert.AreEqual("ok", (string)done.Data["outcome"]!);

        await Applied(edit);
        var state = All(s).Single(e => IsState(e, "soil.moisture", 25));
        Assert.AreEqual(rid, state.Rid);
        Assert.IsTrue(IsEdit(state));
        Assert.IsTrue(state.Seq < done.Seq, "applied before done");
        Assert.AreEqual(25, (double)s.Room.Get("soil.moisture")!);
    }

    [TestMethod]
    public async Task Edit_WhenTheRunnerCrashes_IsNotLost()
    {
        var s = NewSession();
        _runnerLog = new ThrowingLogger<RequestRunner>(); // the summary's copy-guard hit logs, which throws past done
        Task<EditOutcome>? edit = null;
        var chat = new ScriptedChatClient(T(LampPlan()), T(SetLamp20()), T(Text("DONE")), (_, _) =>
        {
            edit = M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
            return Text("It's 100% done.");
        });
        var rid = await Start(s, chat);
        await Approve(s, rid);
        var done = await WaitFor(s, "done", rid);
        Assert.AreEqual("failed", (string)done.Data["outcome"]!);

        await Applied(edit);
        var state = All(s).Single(e => IsState(e, "soil.moisture", 25));
        Assert.IsTrue(IsEdit(state));
        Assert.IsTrue(state.Seq < done.Seq, "applied before done");
        Assert.AreEqual(25, (double)s.Room.Get("soil.moisture")!);
    }

    [TestMethod]
    public async Task DeviceCall_EmitsTheRoomsCurrentValue()
    {
        var s = NewSession();
        var start = s.Room.ToWire();
        var run = new ControlledPacer();
        var rid = await Start(s, Script(SubmitPlan(Step("s1", "Close the blinds", "blinds")),
            Tool("set_blinds", new { closed_percent = 100, step = "s1" }), Text("DONE"), Text("I closed the blinds.")), run);
        await Approve(s, rid);
        await WaitFor(s, "call", rid);

        // set_blinds waits 3000 ms for the blinds to move; the visitor sets them to 20% meanwhile.
        await Applied(M.EditAsync(s, "blinds.closed", JsonValue.Create(0.2)));
        CollectionAssert.AreEqual(new[] { TimeSpan.FromMilliseconds(3000) }, run.Waits.ToArray());
        run.Release(10);
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);

        var blinds = States(s, "blinds.closed");
        Assert.AreEqual(1.0, (double)blinds[0].Data["value"]!, "the AI's frame went out first");
        Assert.AreEqual(0.2, (double)blinds[^1].Data["value"]!, "the last value the browser has is the visitor's");
        Assert.AreEqual(0.2, (double)s.Room.Get("blinds.closed")!);
        AssertBrowserMatchesRoom(s, start);
    }

    [TestMethod]
    public async Task DeviceCall_EditMidAction_DropsTheAisLaterFrame()
    {
        var s = NewSession();
        var start = s.Room.ToWire();
        var run = new ControlledPacer();
        var rid = await Start(s, Script(SubmitPlan(Step("s1", "Water the plant", "pump")),
            Tool("run_pump", new { seconds = 6, step = "s1" }), Text("DONE"), Text("I ran the water pump.")), run);
        await Approve(s, rid);
        await WaitFor(s, "call", rid);

        // run_pump: running true now, the soil rises after 2000 ms, running false at 3000 ms. The visitor sets the soil first.
        await Applied(M.EditAsync(s, "soil.moisture", JsonValue.Create(20.0)));
        run.Release(10);
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);

        var soil = States(s, "soil.moisture");
        Assert.AreEqual(1, soil.Count, string.Join(", ", soil.Select(e => e.Data.ToJsonString())));
        Assert.IsTrue(IsEdit(soil[0]) && soil[0].Rid == rid);
        Assert.AreEqual(20, (double)s.Room.Get("soil.moisture")!);
        Assert.IsFalse(States(s, "pump.running")[^1].Data["value"]!.GetValue<bool>(), "the AI's other frames still play");
        AssertBrowserMatchesRoom(s, start);
    }

    [TestMethod]
    public async Task DeviceCall_DropsAnOlderEditsPendingFrames()
    {
        var steps = new StepPacer();
        M = Manager(steps);
        var s = NewSession();
        var start = s.Room.ToWire();
        // An idle hand pump-on: its soil rise (2000 ms) and pump-off (3000 ms) frames wait on steps.
        await Applied(M.EditAsync(s, "pump.running", JsonValue.Create(true)));

        // The AI then runs the pump too, and its frames for the same fields go out first.
        var rid = await Start(s, Script(SubmitPlan(Step("s1", "Water the plant", "pump")),
            Tool("run_pump", new { seconds = 6, step = "s1" }), Text("DONE"), Text("I ran the water pump.")));
        await Approve(s, rid);
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);
        var before = All(s).Count;

        await steps.Next(); // the edit's soil frame is decided
        await steps.Next(); // and its pump-off frame
        Assert.AreEqual(before, All(s).Count, "the older edit's frames are dropped");
        AssertBrowserMatchesRoom(s, start);
    }

    [TestMethod]
    public async Task FastForward_DropsAnOlderEditsPendingFrames()
    {
        var steps = new StepPacer();
        M = Manager(steps);
        var s = NewSession();
        var start = s.Room.ToWire();
        await Applied(M.EditAsync(s, "pump.running", JsonValue.Create(true)));

        // An hour of fast-forward: the soil dries a little, and its final frame goes out as the room's.
        var rid = await Start(s, Script(SubmitPlan(1, Step("s1", "Keep an eye on the plant", "soil")),
            Tool("run_until", new { hours = 1 }), Text("DONE"), Text("I kept an eye on the plant for an hour.")));
        await Approve(s, rid);
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);
        Assert.IsTrue(States(s, "soil.moisture").Any(e => e.Rid == rid && !IsEdit(e)), "the fast-forward sent the soil");
        var before = All(s).Count;

        await steps.Next();
        await steps.Next();
        Assert.IsFalse(All(s).Skip(before).Any(e => (string?)e.Data["path"] == "soil.moisture"), "the older soil frame is dropped");
        AssertBrowserMatchesRoom(s, start);
    }
}
