/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdvanceTests.cs
    文件功能描述：测试推进模拟时钟（advance）：时长校验、忙碌冲突、事件与限流


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
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

/// <summary>The visitor's clock advance between requests (spec 3.5), through SessionManager.AdvanceAsync.</summary>
[TestClass]
public class AdvanceTests
{
    static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);

    readonly FakeTimeProvider Fake = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    readonly LiveOptions Opt = new();
    readonly InMemoryLiveStore Store;
    readonly AccessService Access;
    readonly TokenMeter Meter;
    readonly BlockingPacer Pacer = new();
    SessionManager M;
    InviteCode Code = null!;
    /// <summary>The runtime each request reads its snapshot from; set its chat client before a request.</summary>
    readonly StaticLiveRuntime Runtime = new(Snapshots.On(new ScriptedChatClient()));

    public AdvanceTests()
    {
        Store = new InMemoryLiveStore(Fake);
        Meter = new TokenMeter(Store, Runtime, Fake);
        Access = new AccessService(Store, Store, Meter, Runtime, UsageDay.Utc, Fake);
        M = new SessionManager(Opt, Fake, Access,
            () => new RequestRunner(Opt, new InstantPacer(), Fake, Store, Meter, UsageDay.Utc, new ListLogger<RequestRunner>()),
            Runtime, UsageDay.Utc, pacer: Pacer);
    }

    /// <summary>Instant until Hold is called; then each wait blocks until Free.</summary>
    sealed class BlockingPacer : IPacer
    {
        readonly SemaphoreSlim _gate = new(0);
        int _waits;
        volatile bool _hold;
        public int Waits => Volatile.Read(ref _waits);
        public void Hold() => _hold = true;

        public void Free()
        {
            _hold = false;
            _gate.Release(100000);
        }

        public async Task WaitAsync(TimeSpan d, CancellationToken ct)
        {
            Interlocked.Increment(ref _waits);
            if (_hold) await _gate.WaitAsync(ct);
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

    static IReadOnlyList<LiveEvent> All(LiveSession s) => s.Events.WaitAfterAsync(0, TimeSpan.Zero, CancellationToken.None).GetAwaiter().GetResult();

    static async Task Until(Func<bool> done)
    {
        using var guard = new CancellationTokenSource(Guard);
        while (!done()) await Task.Delay(1, guard.Token);
    }

    static async Task<LiveEvent> WaitFor(LiveSession s, string kind, string rid)
    {
        using var guard = new CancellationTokenSource(Guard);
        long after = 0;
        while (true)
        {
            var batch = await s.Events.WaitAfterAsync(after, TimeSpan.FromDays(1), guard.Token);
            if (batch.FirstOrDefault(e => e.Kind == kind && e.Rid == rid) is { } hit) return hit;
            if (batch.Count > 0) after = batch[^1].Seq;
        }
    }

    static ScriptedChatClient Script(params ChatResponse[] responses) => new(responses.Select(r => (ScriptTurn)((_, _) => r)).ToArray());

    static ChatResponse LampPlan() =>
        Tool("submit_plan", new { steps = new[] { new { id = "s1", text = "Dim the floor lamp to 20%", devices = new[] { "lamp" } } } });

    const string LampSummary = "I dimmed the floor lamp to 20%.";

    async Task<string> Start(LiveSession s, IChatClient chat)
    {
        Runtime.SetChat(chat);
        var r = await M.StartRequestAsync(s, "Dim the lamp");
        Assert.IsNull(r.Error, r.Error);
        return r.Rid!;
    }

    [TestMethod]
    public async Task Advance_FiresScheduleAndRuleInOrder()
    {
        var s = NewSession();
        s.Room.Set("soil.moisture", JsonValue.Create(30.2)); // 6 h of drying (2 per day) takes it under the rule's 30
        s.Room.Schedules.Add(new Schedule("sc1", "s1", new ActionCall("set_lamp", (JsonObject)JsonNode.Parse("{\"brightness\":20}")!, "s1"), "19:00", null, null));
        s.Room.Rules.Add(new Rule("r1", "s2", "soil.moisture", RuleOp.Below, JsonValue.Create(30.0),
            new ActionCall("run_pump", (JsonObject)JsonNode.Parse("{\"seconds\":6}")!, "s2"), null, 2));

        var result = await M.AdvanceAsync(s, 6);

        Assert.IsNull(result.Error);
        Assert.AreEqual(22.5, result.Clock!.Hour, 1e-6);
        Assert.AreEqual("Fri 22:30", result.Clock.Label);
        Assert.AreEqual(22.5, s.Room.Hour, 1e-6);

        var events = All(s);
        var clocks = events.Where(e => e.Kind == "clock").ToList();
        Assert.IsTrue(clocks.Count > 0);
        Assert.IsTrue(clocks.All(e => e.Rid is null && (string)e.Data["src"]! == "edit"));
        Assert.AreEqual("Fri 22:30", (string)clocks[^1].Data["label"]!);

        var rows = events.Where(e => e.Kind == "timeline").ToList();
        Assert.IsTrue(rows.All(e => e.Rid is null && (string)e.Data["src"]! == "edit"));
        var sched = rows.Single(e => (string?)e.Data["origin"] == "schedule");
        Assert.AreEqual("Fri 19:00", (string)sched.Data["at"]!);
        var rule = rows.Single(e => (string?)e.Data["origin"] == "rule");
        Assert.AreEqual("Fri 18:00", (string)rule.Data["at"]!, "the dry-soil rule fired as the soil dried under 30");
        Assert.IsTrue(rule.Seq < sched.Seq, "in time order: the 18:00 rule fire, then the 19:00 schedule");
        Assert.IsTrue(events.Where(e => e.Kind == "state").All(e => e.Rid is null && (string)e.Data["src"]! == "edit"));
        Assert.IsFalse(s.Busy);
    }

    [TestMethod]
    public async Task Advance_TwoRulesOnOneValue_BothFireInTheSameTick()
    {
        var s = NewSession();
        s.Room.Set("soil.moisture", JsonValue.Create(30.2)); // under 30 at Fri 18:00
        s.Room.Rules.Add(new Rule("r1", "s2", "soil.moisture", RuleOp.Below, JsonValue.Create(30.0),
            new ActionCall("run_pump", (JsonObject)JsonNode.Parse("{\"seconds\":6}")!, "s2"), null, 2));
        s.Room.Rules.Add(new Rule("r4", "s3", "soil.moisture", RuleOp.Below, JsonValue.Create(30.0),
            new ActionCall("set_blinds", (JsonObject)JsonNode.Parse("{\"closed_percent\":100}")!, "s3"), null, 2));

        var result = await M.AdvanceAsync(s, 24);

        Assert.IsNull(result.Error);
        var rows = All(s).Where(e => e.Kind == "timeline").ToList();
        var pump = rows.Single(e => (string?)e.Data["device"] == "pump");
        var blinds = rows.Single(e => (string?)e.Data["device"] == "blinds");
        Assert.AreEqual("Fri 18:00", (string)pump.Data["at"]!);
        Assert.AreEqual("Fri 18:00", (string)blinds.Data["at"]!, "the blinds rule read the tick's soil, not the pump's");
        Assert.IsTrue(pump.Seq < blinds.Seq);
        Assert.AreEqual(1.0, (double)s.Room.Get("blinds.closed")!);
    }

    [TestMethod]
    public async Task Advance_RefusedMidRequest()
    {
        var s = NewSession();
        var rid = await Start(s, Script(LampPlan()));
        await WaitFor(s, "awaiting_approval", rid);
        var before = All(s).Count;

        var result = await M.AdvanceAsync(s, 1);

        Assert.AreEqual("busy", result.Error);
        Assert.IsNull(result.Clock);
        Assert.AreEqual(before, All(s).Count);
        Assert.IsFalse(s.Advancing);
    }

    [TestMethod]
    public async Task Request_WhatIf_And_Reset_RefusedWhileAdvancing()
    {
        var s = NewSession();
        var rid = await Start(s, Script(LampPlan(), Tool("set_lamp", new { brightness = 20, step = "s1" }), Text("DONE"), Text(LampSummary)));
        await WaitFor(s, "awaiting_approval", rid);
        Assert.IsNull(s.TryDecide(rid, Decision.Approve));
        await WaitFor(s, "done", rid);
        await Until(() => !s.Busy);

        Pacer.Hold();
        var advance = M.AdvanceAsync(s, 6);
        await Until(() => s.Advancing);

        Assert.AreEqual("busy", (await M.StartRequestAsync(s, "Dim the lamp")).Error);
        Assert.AreEqual("busy", (await M.StartWhatIfAsync(s, rid, "lamp")).Error);
        Assert.IsFalse(M.Reset(s));
        Assert.AreEqual("busy", (await M.AdvanceAsync(s, 1)).Error);
        Assert.IsFalse(advance.IsCompleted);

        Pacer.Free();
        var done = await advance.WaitAsync(Guard);
        Assert.IsNull(done.Error);
        Assert.IsFalse(s.Busy);

        Runtime.SetChat(Script(Tool("set_lamp", new { brightness = 20, step = "s1" }), Text("DONE"), Text(LampSummary)));
        var whatIf = await M.StartWhatIfAsync(s, rid, "lamp");
        Assert.IsNull(whatIf.Error, whatIf.Error);
        await WaitFor(s, "done", whatIf.Rid!);
        await Until(() => !s.Busy);

        Assert.IsTrue(M.Reset(s));
        Runtime.SetChat(Script(LampPlan()));
        Assert.IsNull((await M.StartRequestAsync(s, "Dim the lamp")).Error);
    }

    [TestMethod]
    public async Task Advance_EditDuringAdvance_Applies()
    {
        var s = NewSession();
        Pacer.Hold();
        var advance = M.AdvanceAsync(s, 3);
        await Until(() => Pacer.Waits > 0 && s.Advancing);

        var edit = await M.EditAsync(s, "lamp.brightness", JsonValue.Create(0.5));
        Assert.IsNull(edit.Error);
        Assert.AreNotSame(EditOutcome.Queued, edit, "applied during the wait, not left queued");

        Pacer.Free();
        var result = await advance.WaitAsync(Guard);
        Assert.IsNull(result.Error);

        var events = All(s);
        var lamp = events.Single(e => e.Kind == "state" && (string)e.Data["path"]! == "lamp.brightness");
        Assert.IsNull(lamp.Rid);
        Assert.AreEqual("edit", (string)lamp.Data["src"]!);
        var lastClock = events.Last(e => e.Kind == "clock");
        Assert.IsTrue(lamp.Seq < lastClock.Seq, "the edit lands before the advance's last clock");
        Assert.AreEqual(0.5, (double)s.Room.Get("lamp.brightness")!, 1e-9);
    }
}
