/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AutomationsTests.cs
    文件功能描述：测试自动化（规则与定时）：文字生成、开关、触发、每日次数与事件


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 新增测试：移除单条自动化与清空全部

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 测试夹具改用试点时期的上限（Pilot）

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

/// <summary>
/// The live automations (spec 2026-11-08 §2, §3): the visitor's on/off switch on each rule and schedule, the cards' text,
/// the automations event and the toggle route. Scripted model, FakeTimeProvider; every wait is bounded by a real-time guard.
/// </summary>
[TestClass]
public class AutomationsTests
{
    static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);
    static readonly SceneManifest Home = ManifestLoader.Load("home");
    static readonly ISceneBehavior HomeB = ManifestLoader.Behavior("home");

    readonly FakeTimeProvider Fake = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    readonly LiveOptions Opt = Fakes.Pilot.Options(); // the pilot's bounds: the defaults have none
    readonly InMemoryLiveStore Store;
    readonly AccessService Access;
    readonly TokenMeter Meter;
    readonly SessionManager M;
    InviteCode Code = null!;
    /// <summary>The runtime each request reads its snapshot from; set its chat client before a request.</summary>
    readonly StaticLiveRuntime Runtime = new(Snapshots.On(new ScriptedChatClient()));
    IPacer _runPacer = new InstantPacer();

    public AutomationsTests()
    {
        Store = new InMemoryLiveStore(Fake);
        Meter = new TokenMeter(Store, Runtime, Fake);
        Access = new AccessService(Store, Store, Meter, Runtime, UsageDay.Utc, Fake);
        M = new SessionManager(Opt, Fake, Access, () =>
        {
            var pacer = _runPacer; // each runner keeps what it was built with
            return new RequestRunner(Opt, pacer, Fake, Store, Meter, UsageDay.Utc, new ListLogger<RequestRunner>());
        }, Runtime, UsageDay.Utc, pacer: new InstantPacer());
    }

    [TestInitialize]
    public async Task Init() => Code = await Store.CreateAsync("visitor@example.com", 20);

    // ---- helpers ----

    static JsonObject Obj(string json) => (JsonObject)JsonNode.Parse(json)!;

    static ActionCall Call(string action, string args, string step = "s1") => new(action, Obj(args), step);

    static Rule Water(string id = "rule1", double below = 30, int seconds = 10, int max = 2) =>
        new(id, "s1", "soil.moisture", RuleOp.Below, JsonValue.Create(below), Call("run_pump", $"{{\"seconds\":{seconds}}}"), null, max);

    static Rule DoorNotify(string id = "rule2") =>
        new(id, "s2", "camera.motion", RuleOp.Becomes, JsonValue.Create(true), null, "Someone is at the door", 2) { LastSeen = JsonValue.Create(false) };

    static Schedule Daily(string id, string at, ActionCall call, params DayOfWeek[] days) =>
        new(id, "s1", call, at, days.Length == 0 ? null : days.ToHashSet(), null);

    static RoomState HomeRoom() => RoomState.PostSetup(Home);

    static FastForward Ff() => new(new Simulator(Home, HomeB), HomeB, new InstantPacer(), new FastForwardPacing());

    static RoomEditor Editor() => new(Home, new Simulator(Home, HomeB));

    sealed class RowSink : IFastForwardSink
    {
        public List<TimelineRow> Rows { get; } = new();
        public int Steps { get; private set; }
        public Task ClockAsync(Clock c) => Task.CompletedTask;
        public Task StateAsync(StateChange s) => Task.CompletedTask;
        public Task RowAsync(TimelineRow r) { Rows.Add(r); return Task.CompletedTask; }
        public Task<bool> FirstFaultAsync(string device, TimelineRow r) => Task.FromResult(true);
        public Task StepDoneAsync() { Steps++; return Task.CompletedTask; }
    }

    static IReadOnlyList<LiveEvent> All(LiveSession s) => s.Events.WaitAfterAsync(0, TimeSpan.Zero, CancellationToken.None).GetAwaiter().GetResult();

    static List<LiveEvent> AutomationEvents(LiveSession s) => All(s).Where(e => e.Kind == "automations").ToList();

    static JsonArray Items(LiveEvent e) => e.Data["items"]!.AsArray();

    static JsonObject Item(LiveEvent e, string id) => Items(e).OfType<JsonObject>().Single(i => (string)i["id"]! == id);

    static bool IsEdit(LiveEvent e) => (string?)e.Data["src"] == "edit";

    LiveSession NewSession()
    {
        var r = M.Create(Code, "home");
        Assert.IsNull(r.Error, r.Error);
        return r.Session!;
    }

    /// <summary>Puts automations straight into an idle session's room and streams the list, as the AI's add would.</summary>
    static void Seed(LiveSession s, params object[] automations)
    {
        foreach (var a in automations)
            switch (a)
            {
                case Rule r: s.Room.Rules.Add(r with { Order = s.Room.NextOrder() }); break;
                case Schedule sc: s.Room.Schedules.Add(sc with { Order = s.Room.NextOrder() }); break;
            }
        EditApplier.EmitAutomations(s, s.Room, null, edit: true);
    }

    static ScriptTurn T(ChatResponse r) => (_, _) => r;

    static ScriptedChatClient Script(params ChatResponse[] responses) => new(responses.Select(T).ToArray());

    static object Step(string id, string text, params string[] devices) => new { id, text, devices };

    static ChatResponse SubmitPlan(params object[] steps) => Tool("submit_plan", new { steps });

    static ChatResponse SubmitPlan(double horizon, params object[] steps) => Tool("submit_plan", new { steps, horizon_hours = horizon });

    static (string, object) WaterRule(string step = "s1", int max = 2) => ("add_rule", new
    {
        when = new { device = "soil", field = "moisture", op = "below", value = 30 },
        then = new { action = "run_pump", args = new { seconds = 6 } },
        max_per_day = max,
        step,
    });

    static ScriptedChatClient LampOk() => Script(SubmitPlan(Step("s1", "Dim the floor lamp to 20%", "lamp")),
        Tool("set_lamp", new { brightness = 20, step = "s1" }), Text("DONE"), Text("I dimmed the floor lamp to 20%."));

    /// <summary>A day with the watering rule: the soil starts at 25, so it fires at the first tick, and once more on Saturday afternoon.</summary>
    static ScriptedChatClient WaterDay() => Script(
        SubmitPlan(24, Step("s1", "Water the plant when the soil is dry", "soil", "pump")),
        Tools(WaterRule(), ("run_until", new { hours = 24 })),
        Text("DONE"),
        Text("I watered the plant when the soil got dry."));

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

    static async Task Idle(LiveSession s)
    {
        using var guard = new CancellationTokenSource(Guard);
        while (s.Busy) await Task.Delay(1, guard.Token);
    }

    async Task<string> RunOk(LiveSession s, IChatClient chat, IPacer? pacer = null)
    {
        var rid = await Start(s, chat, pacer);
        await Approve(s, rid);
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);
        await Idle(s);
        return rid;
    }

    // ---- the cards' text ----

    [TestMethod]
    public void Text_Rules()
    {
        Assert.AreEqual("When Soil sensor is below 30% → Water pump: run 10 s", Automations.Text(Home, HomeB, Water()),
            "a sensor's one number needs no field name");
        Assert.AreEqual("When Door camera detects motion → notify: Someone is at the door", Automations.Text(Home, HomeB, DoorNotify()));
        var warm = new Rule("rule3", "s3", "thermostat.current", RuleOp.Above, JsonValue.Create(24), Call("set_blinds", "{\"closed_percent\":100}"), null, 2);
        Assert.AreEqual("When Thermostat temperature is above 24 °C → Blinds: closed", Automations.Text(Home, HomeB, warm));
        var bright = new Rule("rule4", "s1", "lamp.brightness", RuleOp.Above, JsonValue.Create(0.5), Call("set_blinds", "{\"closed_percent\":0}"), null, 2);
        Assert.AreEqual("When Floor lamp brightness is above 50% → Blinds: open", Automations.Text(Home, HomeB, bright), "a 0–1 card field reads as a percent");
        var shut = new Rule("rule5", "s1", "blinds.closed", RuleOp.Above, JsonValue.Create(0.5), Call("set_lamp", "{\"brightness\":40}"), null, 2);
        Assert.AreEqual("When Blinds closed is above 50% → Floor lamp: 40%", Automations.Text(Home, HomeB, shut));
        var dimmed = new Rule("rule6", "s1", "lamp.brightness", RuleOp.Becomes, JsonValue.Create(0.3), null, "Dimmed", 2);
        Assert.AreEqual("When Floor lamp brightness reaches 30% → notify: Dimmed", Automations.Text(Home, HomeB, dimmed));

        // A switch reads as what it does.
        var lampOff = new Rule("rule7", "s1", "lamp.on", RuleOp.Becomes, JsonValue.Create(false), null, "The lamp went off", 2);
        Assert.AreEqual("When Floor lamp turns off → notify: The lamp went off", Automations.Text(Home, HomeB, lampOff));
        var pumpStop = new Rule("rule8", "s1", "pump.running", RuleOp.Becomes, JsonValue.Create(false), null, "The pump stopped", 2);
        Assert.AreEqual("When Water pump stops running → notify: The pump stopped", Automations.Text(Home, HomeB, pumpStop));
        var camOff = new Rule("rule9", "s1", "camera.off", RuleOp.Becomes, JsonValue.Create(true), null, "The camera is off", 2);
        Assert.AreEqual("When Door camera turns off → notify: The camera is off", Automations.Text(Home, HomeB, camOff));
        var stuck = new Rule("rule10", "s1", "blinds.jammed", RuleOp.Becomes, JsonValue.Create(true), null, "Stuck", 2);
        Assert.AreEqual("When Blinds jammed turns on → notify: Stuck", Automations.Text(Home, HomeB, stuck), "any other switch");

        var full = new Rule("rule11", "s1", "soil.moisture", RuleOp.Below, JsonValue.Create(100), null, "Not soaked yet", 2);
        var text = Automations.Text(Home, HomeB, full);
        Assert.AreEqual("When Soil sensor is below 100 percent → notify: Not soaked yet", text);
        Assert.AreEqual(0, CopyGuard.Find(text).Count, "the copy guard holds for server-made card text");
    }

    [TestMethod]
    public void Text_Schedules()
    {
        Assert.AreEqual("Every day at 19:00 → Floor lamp → 35%", Automations.Text(Home, HomeB, Daily("sched1", "19:00", Call("set_lamp", "{\"brightness\":35}"))));
        Assert.AreEqual("Mon, Wed, Fri at 07:00 → Blinds → open", Automations.Text(Home, HomeB,
            Daily("sched2", "07:00", Call("set_blinds", "{\"closed_percent\":0}"), DayOfWeek.Friday, DayOfWeek.Monday, DayOfWeek.Wednesday)));
        Assert.AreEqual("Every day at 07:00 → Blinds → open", Automations.Text(Home, HomeB,
            Daily("sched3", "07:00", Call("set_blinds", "{\"closed_percent\":0}"), Enum.GetValues<DayOfWeek>())), "all seven days is every day");
        // Home's start day is Fri 2026-11-13, so env.hour 32 is Saturday 08:00.
        var once = new Schedule("sched4", "s1", Call("set_thermostat", "{\"celsius\":19}"), null, null, 32);
        Assert.AreEqual("Once, Sat 08:00 → Thermostat → 19 °C", Automations.Text(Home, HomeB, once));
    }

    [TestMethod]
    public void Text_CustomRoom_UsesTheCatalogNames()
    {
        var m = CustomRoom.Build(new[]
        {
            new RoomDevice("soil_sensor", "soil_sensor"), new RoomDevice("water_pump", "water_pump"),
            new RoomDevice("temp_humidity_sensor", "temp_humidity_sensor"), new RoomDevice("floor_lamp", "floor_lamp"), new RoomDevice("floor_lamp", "floor_lamp_2"),
            new RoomDevice("cart", "cart"), new RoomDevice("power_meter", "power_meter"), new RoomDevice("energy_meter", "energy_meter"),
        });
        var b = ManifestLoader.Behavior(m);
        var water = new Rule("rule1", "s1", "soil_sensor.moisture", RuleOp.Below, JsonValue.Create(30), Call("run_water_pump", "{\"seconds\":10}"), null, 2);
        Assert.AreEqual("When Soil sensor is below 30% → Water pump: run 10 s", Automations.Text(m, b, water));
        var damp = new Rule("rule2", "s1", "temp_humidity_sensor.humidity", RuleOp.Above, JsonValue.Create(60), Call("set_floor_lamp_2", "{\"brightness\":0}"), null, 2);
        Assert.AreEqual("When Temperature & humidity sensor humidity is above 60% → Floor lamp 2: off", Automations.Text(m, b, damp));
        var cold = new Rule("rule3", "s1", "temp_humidity_sensor.temperature", RuleOp.Below, JsonValue.Create(18.5), null, "It's getting cold", 2);
        Assert.AreEqual("When Temperature & humidity sensor temperature is below 18.5 °C → notify: It's getting cold", Automations.Text(m, b, cold));
        var docked = new Rule("rule4", "s1", "cart.at", RuleOp.Becomes, JsonValue.Create("dock"), null, "The cart is back", 2);
        Assert.AreEqual("When Cart reaches dock → notify: The cart is back", Automations.Text(m, b, docked));
        var beam = new Rule("rule5", "s1", "power_meter.mW", RuleOp.Above, JsonValue.Create(10), null, "Beam is up", 2);
        Assert.AreEqual("When Power meter is above 10 mW → notify: Beam is up", Automations.Text(m, b, beam));
        var load = new Rule("rule6", "s1", "energy_meter.watts", RuleOp.Above, JsonValue.Create(500), null, "High load", 2);
        Assert.AreEqual("When Energy meter power is above 500 W → notify: High load", Automations.Text(m, b, load), "a sensor with two numbers names the field");
        var evening = new Schedule("sched1", "s1", Call("set_floor_lamp", "{\"brightness\":30}"), "19:00", null, null);
        Assert.AreEqual("Every day at 19:00 → Floor lamp → 30%", Automations.Text(m, b, evening));

        var room = RoomState.PostSetup(m);
        room.Rules.Add(damp with { Order = room.NextOrder() });
        var item = Automations.List(m, b, room).Single()!.AsObject();
        CollectionAssert.AreEqual(new[] { "temp_humidity_sensor", "floor_lamp_2" }, item["devices"]!.AsArray().Select(d => (string)d!).ToList());
    }

    [TestMethod]
    public void List_ShapeOrderDevicesAndFiredToday()
    {
        var room = HomeRoom();
        room.Hour = 24 + 10; // Saturday 10:00: day 1
        var sched = Daily("sched1", "19:00", Call("set_lamp", "{\"brightness\":35}")) with { Order = room.NextOrder() };
        sched.FiredPerDay[1] = 1;
        room.Schedules.Add(sched);
        var rule = Water() with { Order = room.NextOrder() };
        rule.FiredPerDay[0] = 2; // yesterday's runs don't count today
        rule.FiredPerDay[1] = 1;
        rule.Enabled = false;
        room.Rules.Add(rule);
        room.Rules.Add(DoorNotify() with { Order = room.NextOrder() });

        var list = Automations.List(Home, HomeB, room);
        CollectionAssert.AreEqual(new[] { "sched1", "rule1", "rule2" }, list.Select(i => (string)i!["id"]!).ToList(), "creation order across kinds");
        var first = list[0]!.AsObject();
        CollectionAssert.AreEqual(new[] { "id", "kind", "text", "devices", "enabled", "firedToday", "done" }, first.Select(kv => kv.Key).ToList());
        Assert.IsTrue(list.All(i => !(bool)i!["done"]!), "only a one-off whose time passed is done");
        Assert.AreEqual("schedule", (string)first["kind"]!);
        Assert.AreEqual(1, (int)first["firedToday"]!);
        CollectionAssert.AreEqual(new[] { "lamp" }, first["devices"]!.AsArray().Select(d => (string)d!).ToList());
        var water = list[1]!.AsObject();
        Assert.AreEqual("rule", (string)water["kind"]!);
        Assert.IsFalse((bool)water["enabled"]!);
        Assert.AreEqual(1, (int)water["firedToday"]!);
        CollectionAssert.AreEqual(new[] { "soil", "pump" }, water["devices"]!.AsArray().Select(d => (string)d!).ToList(), "watched device, then the acted-on one");
        CollectionAssert.AreEqual(new[] { "camera" }, list[2]!["devices"]!.AsArray().Select(d => (string)d!).ToList(), "a notify rule has only its watched device");
        Assert.AreEqual(0, (int)list[2]!["firedToday"]!);
    }

    [TestMethod]
    public void DeepClone_CarriesTheSwitchAndCounters()
    {
        var room = HomeRoom();
        var rule = Water() with { Order = room.NextOrder() };
        rule.Enabled = false;
        room.Rules.Add(rule);
        var sched = Daily("sched1", "19:00", Call("set_lamp", "{\"brightness\":35}")) with { Order = room.NextOrder() };
        sched.Enabled = false;
        sched.FiredPerDay[0] = 1;
        room.Schedules.Add(sched);

        var c = room.Clone();
        Assert.IsFalse(c.Rules[0].Enabled);
        Assert.AreEqual(1, c.Rules[0].Order);
        Assert.IsFalse(c.Schedules[0].Enabled);
        Assert.AreEqual(2, c.Schedules[0].Order);
        Assert.AreEqual(1, c.Schedules[0].FiredPerDay[0]);
        Assert.AreEqual(3, c.NextOrder(), "the order counter is carried");

        c.Schedules[0].FiredPerDay[0] = 5;
        c.Rules[0].Enabled = true;
        Assert.AreEqual(1, room.Schedules[0].FiredPerDay[0], "the clone's counters are its own");
        Assert.IsFalse(room.Rules[0].Enabled);
    }

    // ---- off never fires; switching back on ----

    [TestMethod]
    public void Off_HandCrossingDoesNotFire()
    {
        var room = HomeRoom();
        room.Rules.Add(Water());
        Assert.IsTrue(Automations.SetEnabled(room, "rule1", false));
        var o = Editor().Apply(room, "soil.moisture", JsonValue.Create(25.0));
        Assert.IsNull(o.Error);
        Assert.AreEqual(0, o.Rows.Count, "an off rule doesn't fire on a hand crossing");
        Assert.AreEqual(0, room.Rules[0].FiredPerDay.Count);
        Assert.IsFalse((bool)room.Get("pump.running")!);
    }

    [TestMethod]
    public async Task Off_FastForward_RuleAndScheduleNeverFire()
    {
        var room = HomeRoom();
        room.Set("soil.moisture", JsonValue.Create(25.0));
        room.Rules.Add(Water());
        room.Schedules.Add(Daily("sched1", "18:00", Call("set_lamp", "{\"brightness\":35}")));
        Automations.SetEnabled(room, "rule1", false);
        Automations.SetEnabled(room, "sched1", false);

        var sink = new RowSink();
        await Ff().RunAsync(room, 48, sink, CancellationToken.None);
        Assert.AreEqual(0, sink.Rows.Count, string.Join(" | ", sink.Rows.Select(r => r.Text)));
        Assert.AreEqual(0, (double)room.Get("lamp.brightness")!);
        Assert.AreEqual(0, room.Rules[0].FiredPerDay.Count + room.Schedules[0].FiredPerDay.Count);
        Assert.IsTrue(sink.Steps > 0, "the sink hears about every step");
    }

    [TestMethod]
    public async Task Off_RuleOnAFailedSensor_ReportsNoFault()
    {
        var on = HomeRoom();
        on.Rules.Add(Water());
        on.Failed.Add("soil");
        var onSink = new RowSink();
        await Ff().RunAsync(on, 2, onSink, CancellationToken.None);
        Assert.AreEqual("fault", onSink.Rows.Single().Kind, "control: a rule that is on reports its failed sensor");
        Assert.AreEqual("rule1", onSink.Rows.Single().Automation);

        var off = HomeRoom();
        off.Rules.Add(Water());
        off.Failed.Add("soil");
        Automations.SetEnabled(off, "rule1", false);
        var offSink = new RowSink();
        await Ff().RunAsync(off, 2, offSink, CancellationToken.None);
        Assert.AreEqual(0, offSink.Rows.Count);
    }

    [TestMethod]
    public async Task ReEnable_InZone_FiresAtTheNextTick_LikeANewRule()
    {
        var room = HomeRoom(); // Fri 16:30
        room.Set("soil.moisture", JsonValue.Create(25.0));
        room.Rules.Add(Water());
        Automations.SetEnabled(room, "rule1", false);
        var sink = new RowSink();
        await Ff().RunAsync(room, 2, sink, CancellationToken.None); // to 18:30, off: nothing
        Assert.AreEqual(0, sink.Rows.Count);

        Assert.IsTrue(Automations.SetEnabled(room, "rule1", true));
        Assert.AreEqual(0, sink.Rows.Count, "switching on fires nothing by itself");
        await Ff().RunAsync(room, 1, sink, CancellationToken.None); // the 19:00 tick finds the soil in the zone
        var row = sink.Rows.Single();
        Assert.AreEqual(("Fri 19:00", "rule", "rule1"), (row.At, row.Origin, row.Automation));
    }

    [TestMethod]
    public async Task ReEnable_KeepsTheGapAndTheDailyCap()
    {
        var room = HomeRoom();
        room.Set("soil.moisture", JsonValue.Create(25.0));
        var rule = Water(seconds: 1); // a short run leaves the soil in the zone
        room.Rules.Add(rule);
        var sink = new RowSink();
        await Ff().RunAsync(room, 1, sink, CancellationToken.None);
        Assert.AreEqual("Fri 17:00", sink.Rows.Single().At);

        Automations.SetEnabled(room, "rule1", false);
        Automations.SetEnabled(room, "rule1", true);
        await Ff().RunAsync(room, 5, sink, CancellationToken.None); // to 22:30: inside the 6 h gap
        Assert.AreEqual(1, sink.Rows.Count, "the 6 h gap holds across off and on");
        await Ff().RunAsync(room, 1, sink, CancellationToken.None);
        Assert.AreEqual("Fri 23:00", sink.Rows[^1].At);
        Assert.AreEqual(2, rule.FiredPerDay[0]);

        var capped = HomeRoom();
        capped.Set("soil.moisture", JsonValue.Create(25.0));
        capped.Rules.Add(Water(max: 2));
        capped.Rules[0].FiredPerDay[0] = 2;
        Automations.SetEnabled(capped, "rule1", false);
        Automations.SetEnabled(capped, "rule1", true);
        var cappedSink = new RowSink();
        await Ff().RunAsync(capped, 4, cappedSink, CancellationToken.None);
        Assert.AreEqual(0, cappedSink.Rows.Count, "today's count is kept, and so is the cap");
    }

    [TestMethod]
    public void ReEnable_HandEdit_FiresOnlyOnAMoveIntoTheZone()
    {
        var room = HomeRoom();
        room.Set("soil.moisture", JsonValue.Create(25.0));
        room.Rules.Add(Water());
        room.Rules[0].FiredPerDay[0] = 1;
        Automations.SetEnabled(room, "rule1", false);
        Automations.SetEnabled(room, "rule1", true);
        Assert.AreEqual(1, room.Rules[0].FiredPerDay[0], "the per-day count stays across off and on");
        Assert.AreEqual(0, Editor().Apply(room, "soil.moisture", JsonValue.Create(24.0)).Rows.Count, "an edit inside the zone is no crossing");
        Assert.AreEqual(0, Editor().Apply(room, "soil.moisture", JsonValue.Create(40.0)).Rows.Count);
        var fired = Editor().Apply(room, "soil.moisture", JsonValue.Create(25.0)).Rows.Single();
        Assert.AreEqual(("rule", "rule1"), (fired.Origin, fired.Automation));
        Assert.AreEqual(2, room.Rules[0].FiredPerDay[0]);
    }

    [TestMethod]
    public void ReEnable_InZone_TickFires()
    {
        var room = HomeRoom();
        var engine = new RuleEngine(new Simulator(Home, HomeB));
        var rule = Water();
        room.Rules.Add(rule);
        room.Set("soil.moisture", JsonValue.Create(25.0));
        Automations.SetEnabled(room, "rule1", false);
        var faults = new HashSet<(string, int)>();
        Assert.IsNull(engine.Tick(rule, JsonValue.Create(25.0), "Fri 17:00", 0, room, faults), "off");
        Automations.SetEnabled(room, "rule1", true);
        Assert.IsNotNull(engine.Tick(rule, JsonValue.Create(25.0), "Fri 18:00", 0, room, faults), "back on, in the zone");
    }

    [TestMethod]
    public async Task ReEnable_Becomes_AlreadyEqual_DoesNotFire()
    {
        var room = HomeRoom();
        room.Rules.Add(DoorNotify("rule1"));
        Automations.SetEnabled(room, "rule1", false);
        // While off: no fire, and LastSeen follows the value.
        Assert.AreEqual(0, Editor().Apply(room, "camera.motion", JsonValue.Create(true)).Rows.Count);
        Assert.IsTrue((bool)room.Rules[0].LastSeen!);

        Automations.SetEnabled(room, "rule1", true);
        var sink = new RowSink();
        await Ff().RunAsync(room, 2, sink, CancellationToken.None); // the camera isn't armed: motion stays true
        Assert.AreEqual(0, sink.Rows.Count, "switched on with motion already on: no fire");
        Assert.AreEqual(0, Editor().Apply(room, "camera.motion", JsonValue.Create(false)).Rows.Count);
        var row = Editor().Apply(room, "camera.motion", JsonValue.Create(true)).Rows.Single();
        Assert.AreEqual("Someone is at the door", row.Text);
        Assert.AreEqual("rule1", row.Automation);
    }

    [TestMethod]
    public void ReEnable_Becomes_LastSeenTakenAtSwitchOn()
    {
        // A value an AI action changed while the rule was off (no tick or edit saw it) is taken as seen when it goes on.
        var room = HomeRoom();
        room.Rules.Add(DoorNotify("rule1"));
        Automations.SetEnabled(room, "rule1", false);
        room.Set("camera.motion", JsonValue.Create(true));
        Automations.SetEnabled(room, "rule1", true);
        Assert.IsTrue((bool)room.Rules[0].LastSeen!);
    }

    [TestMethod]
    public async Task Schedule_SwitchedBackOn_RunsAtItsNextTime()
    {
        var room = HomeRoom(); // Fri 16:30
        room.Schedules.Add(Daily("sched1", "18:00", Call("set_lamp", "{\"brightness\":35}")));
        room.Schedules.Add(new Schedule("sched2", "s1", Call("set_blinds", "{\"closed_percent\":100}"), null, null, 20)); // once, Fri 20:00
        Automations.SetEnabled(room, "sched1", false);
        Automations.SetEnabled(room, "sched2", false);
        var sink = new RowSink();
        await Ff().RunAsync(room, 6, sink, CancellationToken.None); // to 22:30: both times pass while off
        Assert.AreEqual(0, sink.Rows.Count);

        Automations.SetEnabled(room, "sched1", true);
        Automations.SetEnabled(room, "sched2", true);
        await Ff().RunAsync(room, 24, sink, CancellationToken.None);
        var row = sink.Rows.Single();
        Assert.AreEqual(("Sat 18:00", "schedule", "sched1"), (row.At, row.Origin, row.Automation), "a one-off that passed while off doesn't catch up");
        Assert.AreEqual(1, room.Schedules[0].FiredPerDay[1]);
    }

    [TestMethod]
    public void Toggle_UnknownId_IsFalse()
    {
        var room = HomeRoom();
        room.Rules.Add(Water());
        Assert.IsFalse(Automations.SetEnabled(room, "rule9", false));
        Assert.IsTrue(room.Rules[0].Enabled);
    }

    // ---- the model's view ----

    [TestMethod]
    public void Prompts_ListAutomationsWithTheirSwitch()
    {
        var room = HomeRoom();
        room.Rules.Add(Water() with { Order = room.NextOrder() });
        room.Schedules.Add(Daily("sched1", "19:00", Call("set_lamp", "{\"brightness\":35}")) with { Order = room.NextOrder() });
        Automations.SetEnabled(room, "sched1", false);
        var clock = SimClock.At(Home.StartDay, room.Hour);
        foreach (var prompt in new[] { Prompts.PlanSystem(Home, room, clock), Prompts.RunSystem(Home, room, clock, new ApprovedPlan()) })
        {
            StringAssert.Contains(prompt, "- on: When Soil sensor is below 30% → Water pump: run 10 s");
            StringAssert.Contains(prompt, "- off: Every day at 19:00 → Floor lamp → 35%");
            StringAssert.Contains(prompt, "An automation that is off doesn't run");
            StringAssert.Contains(prompt, "You may set up a changed version");
            StringAssert.Contains(prompt, "never by an id like rule1 or sched1");
            Assert.IsFalse(prompt.Contains("- rule1") || prompt.Contains("- sched1"), "the list carries no ids");
        }
        Assert.IsFalse(Prompts.PlanSystem(Home, HomeRoom(), clock).Contains("Automations already set up"), "nothing when there are none");
        StringAssert.Contains(Prompts.SummaryInstruction(new JsonObject()), "Never name a rule or schedule by an id like rule1 or sched1");
    }

    [TestMethod]
    public void Prompts_ListAtMost20_TheNewest()
    {
        var room = HomeRoom();
        for (var i = 1; i <= 22; i++) room.Rules.Add(Water($"rule{i}", below: i) with { Order = room.NextOrder() });
        var prompt = Prompts.PlanSystem(Home, room, SimClock.At(Home.StartDay, room.Hour));
        StringAssert.Contains(prompt, "- (2 older ones not shown)");
        StringAssert.Contains(prompt, "is below 22% →");
        StringAssert.Contains(prompt, "is below 3% →");
        Assert.IsFalse(prompt.Contains("is below 2% →") || prompt.Contains("is below 1% →"), "the two oldest are left out");
        Assert.AreEqual(20, prompt.Split('\n').Count(l => l.StartsWith("- on: ", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task OneOff_IsDoneOnceItsTimePassed()
    {
        var room = HomeRoom(); // Fri 16:30
        room.Schedules.Add(new Schedule("sched1", "s1", Call("set_blinds", "{\"closed_percent\":100}"), null, null, 20) { Order = room.NextOrder() });
        room.Schedules.Add(Daily("sched2", "19:00", Call("set_lamp", "{\"brightness\":35}")) with { Order = room.NextOrder() });
        Assert.IsFalse((bool)Automations.List(Home, HomeB, room)[0]!["done"]!);
        await Ff().RunAsync(room, 4, new RowSink(), CancellationToken.None); // to 20:30: the one-off ran at 20:00
        var list = Automations.List(Home, HomeB, room);
        Assert.IsTrue((bool)list[0]!["done"]!);
        Assert.IsTrue((bool)list[0]!["enabled"]!, "done is not off");
        Assert.AreEqual(1, (int)list[0]!["firedToday"]!);
        Assert.IsFalse((bool)list[1]!["done"]!, "a daily schedule is never done");
        StringAssert.Contains(Prompts.PlanSystem(Home, room, SimClock.At(Home.StartDay, room.Hour)), "- done: Once, Fri 20:00 → Blinds → closed");

        // A one-off whose time went by while it was off is done too: it never runs again.
        var missed = HomeRoom();
        missed.Schedules.Add(new Schedule("sched1", "s1", Call("set_blinds", "{\"closed_percent\":100}"), null, null, 20));
        Automations.SetEnabled(missed, "sched1", false);
        await Ff().RunAsync(missed, 4, new RowSink(), CancellationToken.None);
        Assert.IsTrue((bool)Automations.List(Home, HomeB, missed)[0]!["done"]!);
    }

    [TestMethod]
    public void List_ATextThatThrowsIsLeftOut()
    {
        var room = HomeRoom();
        room.Rules.Add(Water() with { Order = room.NextOrder() });
        // set_lamp with no brightness: Home's label can't read it (validated args never look like this).
        room.Schedules.Add(Daily("sched1", "19:00", Call("set_lamp", "{}")) with { Order = room.NextOrder() });
        var skipped = new List<string>();
        var list = Automations.List(Home, HomeB, room, (id, _) => skipped.Add(id));
        CollectionAssert.AreEqual(new[] { "rule1" }, list.Select(i => (string)i!["id"]!).ToList());
        CollectionAssert.AreEqual(new[] { "sched1" }, skipped);
    }

    [TestMethod]
    public async Task CardTextThatThrows_FailsNeitherAnEditNorAnAdvance()
    {
        var log = new ListLogger<SessionManager>();
        var m = new SessionManager(Opt, Fake, Access, () => new RequestRunner(Opt, new InstantPacer(), Fake, Store, Meter, UsageDay.Utc,
            new ListLogger<RequestRunner>()), Runtime, UsageDay.Utc, log, new InstantPacer());
        var s = m.Create(Code, "home").Session!;
        s.Room.Rules.Add(Water() with { Order = s.Room.NextOrder() });
        s.Room.Schedules.Add(Daily("sched1", "19:00", Call("set_lamp", "{}")) with { Order = s.Room.NextOrder() });

        var o = await m.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        Assert.IsNull(o.Error);
        Assert.IsTrue(All(s).Any(e => e.Kind == "state" && (string)e.Data["path"]! == "soil.moisture"), "the edit streamed");
        Assert.IsTrue(All(s).Any(e => e.Kind == "timeline"), "the rule fired");
        CollectionAssert.AreEqual(new[] { "rule1" }, Items(AutomationEvents(s).Single()).Select(i => (string)i!["id"]!).ToList());
        Assert.IsNull((await m.AdvanceAsync(s, 3)).Error, "an advance over the bad card still plays");
        Assert.AreEqual(1, log.Entries.Count(e => e.Id == EditApplier.AutomationTextFailed), "logged once, not at every rebuild");
    }

    [TestMethod]
    public async Task Request_RoomFull_RefusesAnotherRuleOrSchedule()
    {
        var s = NewSession();
        Seed(s, Enumerable.Range(1, Automations.MaxPerRoom - 1).Select(i => (object)Water($"rule{i}"))
            .Append(Daily("sched1", "19:00", Call("set_lamp", "{\"brightness\":35}"))).ToArray());
        var chat = Script(
            SubmitPlan(Step("s1", "Water the plant when the soil is dry", "soil", "pump"), Step("s2", "Lamp on at 20:00", "lamp")),
            Tools(WaterRule(), ("schedule", new { at = "20:00", action = "set_lamp", args = new { brightness = 35 }, step = "s2" })),
            Text("DONE"), Text("The room already has as many automations as it can hold."));
        var rid = await Start(s, chat);
        await Approve(s, rid);
        await WaitFor(s, "done", rid);

        var results = chat.Calls[2].Messages.SelectMany(x => x.Contents).OfType<FunctionResultContent>().Select(r => r.Result?.ToString() ?? "").ToList();
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(r => r.Contains("\"ok\":false") && r.Contains($"already has {Automations.MaxPerRoom} automations")), string.Join(" | ", results));
        Assert.AreEqual(Automations.MaxPerRoom, s.Room.Rules.Count + s.Room.Schedules.Count);
        Assert.IsFalse(All(s).Any(e => e.Kind == "automations" && e.Rid == rid), "nothing was added");
    }

    [TestMethod]
    public async Task Toggle_ItselfNeverFires()
    {
        var s = NewSession();
        s.Room.Set("soil.moisture", JsonValue.Create(25.0));
        Seed(s, Water());
        Assert.IsNull(await M.ToggleAsync(s, "rule1", false));
        Assert.IsNull(await M.ToggleAsync(s, "rule1", true));
        Assert.IsTrue(All(s).All(e => e.Kind == "automations"), string.Join(", ", All(s).Select(e => e.Kind)));
        Assert.AreEqual(0, s.Room.Rules[0].FiredPerDay.Count);
        Assert.IsFalse((bool)s.Room.Get("pump.running")!);
    }

    // ---- the automations event ----

    [TestMethod]
    public async Task Request_AddRuleAndSchedule_StreamTheListWithTheRid()
    {
        var s = NewSession();
        s.Room.Set("soil.moisture", JsonValue.Create(25.0));
        var rid = await RunOk(s, Script(
            SubmitPlan(24, Step("s1", "Water the plant when the soil is dry", "soil", "pump"), Step("s2", "Lamp on at 19:00", "lamp")),
            Tools(WaterRule(), ("schedule", new { at = "19:00", action = "set_lamp", args = new { brightness = 35 }, step = "s2" }), ("run_until", new { hours = 24 })),
            Text("DONE"),
            Text("I watered the plant and put the floor lamp on at 19:00.")));

        var ev = All(s).Where(e => e.Rid == rid).ToList();
        var lists = ev.Where(e => e.Kind == "automations").ToList();
        Assert.IsTrue(lists.All(e => !IsEdit(e)), "the AI's own changes carry the rid, not src edit");
        var firstClock = ev.First(e => e.Kind == "clock").Seq;
        CollectionAssert.AreEqual(new[] { "rule1" }, Items(lists[0]).Select(i => (string)i!["id"]!).ToList(), "the rule's card comes as it is made");
        CollectionAssert.AreEqual(new[] { "rule1", "sched1" }, Items(lists[1]).Select(i => (string)i!["id"]!).ToList());
        Assert.IsTrue(lists[1].Seq < firstClock, "both before the clock moves");
        Assert.AreEqual("When Soil sensor is below 30% → Water pump: run 6 s", (string)Item(lists[1], "rule1")["text"]!);
        Assert.AreEqual("Every day at 19:00 → Floor lamp → 35%", (string)Item(lists[1], "sched1")["text"]!);
        Assert.IsTrue(Items(lists[1]).All(i => (bool)i!["enabled"]! && (int)i["firedToday"]! == 0));

        // Fires change firedToday, and each change streams once: no list repeats the one before it.
        var rows = ev.Where(e => e.Kind == "timeline" && e.Data["origin"] is not null).ToList();
        Assert.IsTrue(rows.Any(e => (string)e.Data["origin"]! == "rule" && (string)e.Data["automation"]! == "rule1"));
        Assert.IsTrue(rows.Any(e => (string)e.Data["origin"]! == "schedule" && (string)e.Data["automation"]! == "sched1"));
        var firstFire = rows.First(e => (string)e.Data["origin"]! == "rule");
        var after = lists.First(e => e.Seq > firstFire.Seq);
        Assert.AreEqual(1, (int)Item(after, "rule1")["firedToday"]!, "the fire's count follows its row");
        for (var i = 1; i < lists.Count; i++)
            Assert.AreNotEqual(lists[i - 1].Data["items"]!.ToJsonString(), lists[i].Data["items"]!.ToJsonString(), $"list {i} repeats");
        Assert.AreEqual(Automations.List(s.Scene, s.Behavior, s.Room).ToJsonString(), lists[^1].Data["items"]!.ToJsonString(),
            "the last list streamed is the room's");
    }

    [TestMethod]
    public async Task Replan_DropStreamsTheListWithoutTheDroppedOnes()
    {
        var s = NewSession();
        var rid = await Start(s, Script(
            SubmitPlan(168, Step("s1", "Put the floor lamp on in the evenings", "lamp"), Step("s2", "Water the plant when it's dry", "soil", "pump"),
                Step("s3", "Tell you if someone comes to the door", "camera")),
            Tools(("schedule", new { at = "18:00", action = "set_lamp", args = new { brightness = 35 }, step = "s1" }),
                ("schedule", new { at = "23:00", action = "set_lamp", args = new { brightness = 0 }, step = "s1" }),
                WaterRule("s2"),
                ("add_rule", new { when = new { device = "camera", field = "motion", op = "becomes", value = true }, then = new { notify = "someone at the door" }, step = "s3" })),
            Tool("propose_replan", new { steps = new[] { Step("s1", "Keep the room at 20 °C", "thermostat"), Step("s2", "Tell you if the soil gets dry", "soil") }, reason = "Keep the room warm instead" }),
            Text("DONE"), Text("I changed the plan.")));
        await Approve(s, rid);
        var replan = await WaitFor(s, "replan", rid);
        await WaitFor(s, "awaiting_approval", rid);
        // the second card: wait until it's the re-plan's
        using (var guard = new CancellationTokenSource(Guard))
            while (All(s).Count(e => e.Kind == "awaiting_approval" && e.Rid == rid) < 2) await Task.Delay(1, guard.Token);
        Assert.IsNull(s.TryDecide(rid, Decision.Approve));
        await WaitFor(s, "done", rid);

        var lists = AutomationEvents(s);
        CollectionAssert.AreEqual(new[] { "sched1", "sched2", "rule1", "rule2" }, Items(lists.Last(e => e.Seq < replan.Seq)).Select(i => (string)i!["id"]!).ToList());
        var dropped = lists.Single(e => e.Seq > replan.Seq);
        Assert.AreEqual(rid, dropped.Rid);
        CollectionAssert.AreEqual(new[] { "sched2", "rule2" }, Items(dropped).Select(i => (string)i!["id"]!).ToList());
        var removedRows = All(s).Where(e => e.Kind == "timeline" && e.Seq > replan.Seq).ToList();
        Assert.IsTrue(dropped.Seq > removedRows.Max(e => e.Seq), "the list follows the removed rows");
    }

    [TestMethod]
    public async Task Toggle_Idle_StreamsSrcEditAndAHandCrossingDoesNotFire()
    {
        var s = NewSession();
        Seed(s, Water());
        var seeded = AutomationEvents(s).Single();
        Assert.IsTrue((bool)Item(seeded, "rule1")["enabled"]!);

        Assert.IsNull(await M.ToggleAsync(s, "rule1", false));
        var off = AutomationEvents(s)[^1];
        Assert.AreNotEqual(seeded.Seq, off.Seq);
        Assert.IsTrue(IsEdit(off));
        Assert.IsNull(off.Rid, "outside a request there is no rid");
        Assert.IsFalse((bool)Item(off, "rule1")["enabled"]!);
        Assert.IsFalse(s.Room.Rules[0].Enabled);

        var o = await M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        Assert.IsNull(o.Error);
        Assert.IsFalse(All(s).Any(e => e.Kind == "timeline"), "an off rule doesn't fire on a hand crossing");

        // A switch that changes nothing still answers with the list, so the card ends on the room's value.
        Assert.IsNull(await M.ToggleAsync(s, "rule1", false));
        Assert.AreEqual(3, AutomationEvents(s).Count);
    }

    [TestMethod]
    public async Task Toggle_HandFire_StreamsTheNewCount()
    {
        var s = NewSession();
        Seed(s, Water());
        await M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        var row = All(s).Single(e => e.Kind == "timeline");
        Assert.AreEqual("rule1", (string)row.Data["automation"]!);
        var list = AutomationEvents(s)[^1];
        Assert.IsTrue(list.Seq > row.Seq && IsEdit(list) && list.Rid is null);
        Assert.AreEqual(1, (int)Item(list, "rule1")["firedToday"]!);
    }

    [TestMethod]
    public async Task Toggle_UnknownId_NoAutomation_IdleAndBusy()
    {
        var s = NewSession();
        Seed(s, Water());
        var before = All(s).Count;
        Assert.AreEqual("no_automation", await M.ToggleAsync(s, "rule9", false));
        Assert.AreEqual(before, All(s).Count, "nothing streams for an unknown id");

        s.Advancing = true; // something owns the room: the id is checked against the list the visitor has
        Assert.AreEqual("no_automation", await M.ToggleAsync(s, "rule9", false));
        Assert.AreEqual(0, s.Edits.TakeAll().Count, "nothing was queued");
    }

    [TestMethod]
    public async Task Toggle_WhileOwned_QueuesForTheOwner()
    {
        var s = NewSession();
        Seed(s, Water());
        s.Advancing = true;
        var pending = M.ToggleAsync(s, "rule1", false);
        Assert.IsFalse(pending.IsCompleted);
        Assert.IsTrue(s.Room.Rules[0].Enabled, "nothing is applied while the room is owned");
        Fake.Advance(TimeSpan.FromSeconds(2));
        Assert.IsNull(await pending, "answered 202: queued");

        EditApplier.ApplyAll(s, new RoomEditor(s.Scene, new Simulator(s.Scene, s.Behavior)), s.Room, "r1", new InstantPacer());
        Assert.IsFalse(s.Room.Rules[0].Enabled);
        var list = AutomationEvents(s)[^1];
        Assert.AreEqual("r1", list.Rid, "the owner's rid");
        Assert.IsTrue(IsEdit(list));
    }

    [TestMethod]
    public async Task Advance_OffScheduleDoesNotRun_OnRunsAndStreamsItsCount()
    {
        var s = NewSession(); // Fri 16:30
        Seed(s, Daily("sched1", "19:00", Call("set_lamp", "{\"brightness\":35}")));
        Assert.IsNull(await M.ToggleAsync(s, "sched1", false));
        var r = await M.AdvanceAsync(s, 6);
        Assert.IsNull(r.Error);
        Assert.IsFalse(All(s).Any(e => e.Kind == "timeline"), "an off schedule doesn't run in a clock advance");
        Assert.AreEqual(0, (double)s.Room.Get("lamp.brightness")!);

        Assert.IsNull(await M.ToggleAsync(s, "sched1", true));
        var mark = All(s)[^1].Seq;
        Assert.IsNull((await M.AdvanceAsync(s, 24)).Error); // to Sat 22:30
        var row = All(s).Single(e => e.Kind == "timeline");
        Assert.AreEqual(("Sat 19:00", "schedule", "sched1"), ((string)row.Data["at"]!, (string)row.Data["origin"]!, (string)row.Data["automation"]!));
        var list = AutomationEvents(s).Single(e => e.Seq > mark);
        Assert.IsTrue(list.Seq > row.Seq && IsEdit(list) && list.Rid is null);
        Assert.AreEqual(1, (int)Item(list, "sched1")["firedToday"]!);
        Assert.AreEqual(0.35, (double)s.Room.Get("lamp.brightness")!, 1e-9);
    }

    [TestMethod]
    public async Task Advance_ANewDayStreamsTheCountBackToZero()
    {
        var s = NewSession();
        Seed(s, Daily("sched1", "19:00", Call("set_lamp", "{\"brightness\":35}")));
        Assert.IsNull((await M.AdvanceAsync(s, 3)).Error); // to 19:30: ran once
        Assert.AreEqual(1, (int)Item(AutomationEvents(s)[^1], "sched1")["firedToday"]!);
        Assert.IsNull((await M.AdvanceAsync(s, 6)).Error); // past midnight
        Assert.AreEqual(0, (int)Item(AutomationEvents(s)[^1], "sched1")["firedToday"]!);
    }

    [TestMethod]
    public async Task Reset_StreamsTheEmptyList()
    {
        var s = NewSession();
        Seed(s, Water(), DoorNotify());
        Assert.IsTrue(M.Reset(s));
        var list = AutomationEvents(s)[^1];
        Assert.AreEqual(0, Items(list).Count);
        Assert.IsTrue(IsEdit(list) && list.Rid is null);
        Assert.AreEqual(0, s.Room.Rules.Count);

        var count = All(s).Count;
        Assert.IsTrue(M.Reset(s));
        Assert.AreEqual(count, All(s).Count, "a reset with nothing to clear streams nothing");
    }

    [TestMethod]
    public async Task WhatIf_ReplaysTheRoomsAutomationsWithTheirSwitch()
    {
        var s = NewSession();
        // A first request adds the watering rule; the visitor switches it off; a second request dims the lamp.
        await RunOk(s, Script(SubmitPlan(Step("s1", "Water the plant when the soil is dry", "soil", "pump")),
            Tools(WaterRule()), Text("DONE"), Text("I set up watering for when the soil gets dry.")));
        Assert.IsNull(await M.ToggleAsync(s, "rule1", false));
        var lampChat = LampOk();
        var rid = await RunOk(s, lampChat);
        StringAssert.Contains(lampChat.Calls[0].Messages[0].Text, "- off: When Soil sensor is below 30% → Water pump: run 6 s",
            "the AI sees the rule as off");

        Runtime.SetChat(Script(Tool("set_lamp", new { brightness = 20, step = "s1" }), Text("DONE"), Text("The floor lamp didn't respond, so I couldn't dim it.")));
        var whatIf = await M.StartWhatIfAsync(s, rid, "lamp");
        Assert.IsNull(whatIf.Error, whatIf.Error);
        await WaitFor(s, "done", whatIf.Rid!);

        var ev = All(s).Where(e => e.Rid == whatIf.Rid).ToList();
        Assert.AreEqual("room", ev[0].Kind);
        var roomItems = ev[0].Data["automations"]!.AsArray();
        Assert.AreEqual(1, roomItems.Count);
        Assert.IsFalse((bool)roomItems[0]!["enabled"]!, "the replayed room's rule is off, as it was");
        Assert.AreEqual("automations", ev[1].Kind);
        Assert.AreEqual(roomItems.ToJsonString(), ev[1].Data["items"]!.ToJsonString());
        Assert.IsFalse(IsEdit(ev[1]));
        Assert.IsFalse(s.Room.Rules.Single().Enabled);
    }

    // ---- a switch during a request ----

    [TestMethod]
    public async Task Toggle_DuringApprovalWait_AppliesAtOnceWithTheRid()
    {
        var s = NewSession();
        Seed(s, Water());
        var rid = await Start(s, LampOk());
        await WaitFor(s, "awaiting_approval", rid);

        Assert.IsNull(await M.ToggleAsync(s, "rule1", false).WaitAsync(Guard));
        Assert.IsFalse(s.Room.Rules[0].Enabled, "applied while the card waits, not after the request");
        var list = AutomationEvents(s)[^1];
        Assert.AreEqual(rid, list.Rid);
        Assert.IsTrue(IsEdit(list));
        Assert.AreEqual("no_automation", await M.ToggleAsync(s, "rule9", true), "mid-request, an unknown id is still refused");

        Assert.IsNull(s.TryDecide(rid, Decision.Approve));
        Assert.AreEqual("ok", (string)(await WaitFor(s, "done", rid)).Data["outcome"]!);
    }

    [TestMethod]
    public async Task Toggle_DuringFastForward_LandsBetweenTicksAndStopsLaterFires()
    {
        // Control: the same day with the rule on all along fires twice (Fri 17:00, then Saturday afternoon).
        var control = NewSession();
        control.Room.Set("soil.moisture", JsonValue.Create(25.0));
        var controlRid = await RunOk(control, WaterDay());
        Assert.AreEqual(2, All(control).Count(e => e.Rid == controlRid && e.Kind == "timeline" && (string?)e.Data["origin"] == "rule"));

        var s = NewSession();
        s.Room.Set("soil.moisture", JsonValue.Create(25.0));
        Task<string?>? toggle = null;
        // At the first fast-forward wait after the rule's first fire, the visitor switches it off.
        var pacer = new InstantPacer(_ =>
        {
            if (toggle is null && All(s).Any(e => e.Kind == "timeline" && (string?)e.Data["origin"] == "rule"))
                toggle = M.ToggleAsync(s, "rule1", false);
        });
        var rid = await RunOk(s, WaterDay(), pacer);
        Assert.IsNotNull(toggle, "the switch was made");
        Assert.IsNull(await toggle.WaitAsync(Guard), "applied at the fast-forward's wait, not left queued");

        var ev = All(s).Where(e => e.Rid == rid).ToList();
        var fires = ev.Where(e => e.Kind == "timeline" && (string?)e.Data["origin"] == "rule").ToList();
        Assert.AreEqual(1, fires.Count, "no fire after the switch");
        var off = ev.Single(e => e.Kind == "automations" && IsEdit(e));
        Assert.IsFalse((bool)Item(off, "rule1")["enabled"]!);
        Assert.IsTrue(off.Seq > fires[0].Seq);
        // Between ticks: after the tick's own events, and the run's next event is the next tick's clock.
        var next = ev.First(e => e.Seq > off.Seq && !IsEdit(e));
        Assert.AreEqual("clock", next.Kind, $"next was {next.Kind} {next.Data.ToJsonString()}");
        var clocks = ev.Where(e => e.Kind == "clock").ToList();
        var tickStart = clocks.Last(c => c.Seq < off.Seq).Seq;
        Assert.IsTrue(ev.Where(e => e.Seq > tickStart && e.Seq < off.Seq).All(e => e.Kind is "state" or "timeline" or "automations"),
            "the switch landed after its tick's events, not inside them");
        Assert.IsFalse(s.Room.Rules.Single().Enabled);
        Assert.AreEqual(1, s.Room.Rules.Single().FiredPerDay.Values.Sum());
    }

    // ---- the route ----

    static void AssertError(LiveHost.Reply r, int status, string kind)
    {
        Assert.AreEqual(status, r.Code, $"{kind}: {r.Body?.ToJsonString()}");
        Assert.AreEqual(kind, r.Error, r.Body?.ToJsonString());
        Assert.IsFalse(string.IsNullOrWhiteSpace((string?)r.Body!["message"]), "every error carries a message");
    }

    [TestMethod]
    public async Task Route_202_400_401_404()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        var s = h.Get<SessionManager>().Get(sid, c)!;
        s.Room.Rules.Add(Water()); // idle: the route checks the room itself
        await using var sse = await h.OpenSseAsync(c, sid);
        var path = $"sessions/{sid}/automations/rule1";

        var r = await h.PostAsync(path, new { enabled = false }, c.Code);
        Assert.AreEqual(202, r.Code, r.Body?.ToJsonString());
        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), r.Body));
        var e = await sse.NextAsync();
        Assert.AreEqual("automations", e.Kind);
        Assert.AreEqual("edit", (string?)e.Data["src"]);
        Assert.IsNull(e.Data["rid"], "outside a request there is no rid");
        var item = e.Data["items"]!.AsArray().Single()!.AsObject();
        Assert.AreEqual("rule1", (string)item["id"]!);
        Assert.IsFalse((bool)item["enabled"]!);
        Assert.IsFalse(s.Room.Rules[0].Enabled);

        AssertError(await h.PostAsync(path, new { }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(path, new { enabled = "no" }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(path, new { enabled = 1 }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(path, new { enabled = (bool?)null }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(path, "{not json", c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(path, new JsonArray(), c.Code), 400, "bad_request");
        AssertError(await h.PostAsync($"sessions/{sid}/automations/rule9", new { enabled = true }, c.Code), 404, "no_automation");
        AssertError(await h.PostAsync(path, new { enabled = true }), 401, "bad_code");
        AssertError(await h.PostAsync(path, new { enabled = true }, "NCB-2222-2222"), 401, "bad_code");
        AssertError(await h.PostAsync("sessions/nope/automations/rule1", new { enabled = true }, c.Code), 404, "no_session");
        Assert.IsFalse(s.Room.Rules[0].Enabled, "no refused call changed it");

        Assert.AreEqual(202, (await h.PostAsync(path, new { enabled = true }, c.Code)).Code);
        Assert.IsTrue(s.Room.Rules[0].Enabled);
    }

    [TestMethod]
    public async Task Route_SharesTheEditRateLimit_CheckedBeforeTheBody()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        h.Get<SessionManager>().Get(sid, c)!.Room.Rules.Add(Water());
        var path = $"sessions/{sid}/automations/rule1";

        for (var i = 0; i < 9; i++)
            Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/set", new { path = "soil.moisture", value = 40 + i }, c.Code)).Code);
        Assert.AreEqual(202, (await h.PostAsync(path, new { enabled = false }, c.Code)).Code, "the 10th in the window");
        AssertError(await h.PostAsync(path, new { enabled = true }, c.Code), 429, "rate");
        AssertError(await h.PostAsync(path, "{not json", c.Code), 429, "rate");
        AssertError(await h.PostAsync($"sessions/{sid}/set", new { path = "soil.moisture", value = 40 }, c.Code), 429, "rate");
        AssertError(await h.PostAsync($"sessions/{sid}/advance", new { hours = 1 }, c.Code), 429, "rate");
        h.Fake.Advance(TimeSpan.FromSeconds(5));
        Assert.AreEqual(202, (await h.PostAsync(path, new { enabled = true }, c.Code)).Code, "a new window");
    }

    [TestMethod]
    public async Task Route_AllowedMidRequest_NeverBusy()
    {
        await using var h = await LiveHost.StartAsync();
        h.Chat.Inner = LampOk();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        h.Get<SessionManager>().Get(sid, c)!.Room.Rules.Add(Water());
        await using var sse = await h.OpenSseAsync(c, sid);
        var path = $"sessions/{sid}/automations/rule1";
        Assert.AreEqual(202, (await h.PostAsync(path, new { enabled = false }, c.Code)).Code);
        await sse.UntilAsync("automations");

        var rid = await h.RequestAsync(c, sid);
        await sse.UntilAsync("awaiting_approval");
        var r = await h.PostAsync(path, new { enabled = true }, c.Code);
        Assert.AreEqual(202, r.Code, r.Body?.ToJsonString());
        var list = (await sse.UntilAsync("automations"))[^1];
        Assert.AreEqual(rid, list.Rid, "applied by the running request, with its rid");
        Assert.AreEqual("edit", (string?)list.Data["src"]);
        Assert.IsTrue((bool)list.Data["items"]![0]!["enabled"]!);
        AssertError(await h.PostAsync($"sessions/{sid}/automations/rule9", new { enabled = true }, c.Code), 404, "no_automation");

        await h.PostAsync($"sessions/{sid}/requests/{rid}/approve", new { }, c.Code);
        Assert.AreEqual("ok", (string?)(await sse.UntilAsync("done"))[^1].Data["outcome"]);
    }
    // ---- removing automations (the card's remove button, "Clear all") ----

    [TestMethod]
    public void Remove_OneById_OrAllWithNull()
    {
        var room = new RoomState();
        room.Rules.Add(Water());
        room.Rules.Add(DoorNotify());
        Assert.IsFalse(Automations.Remove(room, "rule9"), "an unknown id removes nothing");
        Assert.AreEqual(2, room.Rules.Count);
        Assert.IsTrue(Automations.Remove(room, "rule1"));
        Assert.AreEqual("rule2", room.Rules.Single().Id);
        Assert.IsTrue(Automations.Remove(room, null));
        Assert.AreEqual(0, room.Rules.Count + room.Schedules.Count);
        Assert.IsTrue(Automations.Remove(room, null), "clearing an empty room is fine");
    }

    [TestMethod]
    public async Task Remove_Idle_StreamsTheListWithoutIt_AndTheRuleNoLongerFires()
    {
        var s = NewSession();
        Seed(s, Water());
        s.Room.Rules.Add(DoorNotify());

        Assert.IsNull(await M.RemoveAutomationAsync(s, "rule1"));
        var list = AutomationEvents(s)[^1];
        Assert.IsTrue(IsEdit(list));
        Assert.IsNull(list.Rid, "outside a request there is no rid");
        var ids = list.Data["items"]!.AsArray().Select(i => (string)i!["id"]!).ToList();
        CollectionAssert.AreEqual(new[] { "rule2" }, ids);
        Assert.AreEqual("rule2", s.Room.Rules.Single().Id);

        var o = await M.EditAsync(s, "soil.moisture", JsonValue.Create(25.0));
        Assert.IsNull(o.Error);
        Assert.IsFalse(All(s).Any(e => e.Kind == "timeline"), "a removed rule doesn't fire");

        Assert.AreEqual("no_automation", await M.RemoveAutomationAsync(s, "rule1"), "it is gone");
    }

    [TestMethod]
    public async Task ClearAll_Idle_StreamsAnEmptyList_EvenFromAnEmptyRoom()
    {
        var s = NewSession();
        Seed(s, Water());
        s.Room.Rules.Add(DoorNotify());

        Assert.IsNull(await M.RemoveAutomationAsync(s, null));
        Assert.AreEqual(0, s.Room.Rules.Count + s.Room.Schedules.Count);
        var list = AutomationEvents(s)[^1];
        Assert.IsTrue(IsEdit(list));
        Assert.AreEqual(0, list.Data["items"]!.AsArray().Count);

        var n = AutomationEvents(s).Count;
        Assert.IsNull(await M.RemoveAutomationAsync(s, null), "nothing to clear is not an error");
        Assert.AreEqual(n + 1, AutomationEvents(s).Count, "and the empty list is sent again, so the section ends on the room's");
    }

    [TestMethod]
    public async Task Remove_WhileOwned_QueuesForTheOwner_AndAnUnknownIdIsRefusedAtOnce()
    {
        var s = NewSession();
        Seed(s, Water());
        s.Advancing = true;
        Assert.AreEqual("no_automation", await M.RemoveAutomationAsync(s, "rule9"));
        Assert.AreEqual(0, s.Edits.TakeAll().Count, "nothing was queued");

        var pending = M.RemoveAutomationAsync(s, "rule1");
        Assert.IsFalse(pending.IsCompleted);
        Assert.AreEqual(1, s.Room.Rules.Count, "nothing is applied while the room is owned");
        Fake.Advance(TimeSpan.FromSeconds(2));
        Assert.IsNull(await pending, "answered 202: queued");

        EditApplier.ApplyAll(s, new RoomEditor(s.Scene, new Simulator(s.Scene, s.Behavior)), s.Room, "r1", new InstantPacer());
        Assert.AreEqual(0, s.Room.Rules.Count);
        var list = AutomationEvents(s)[^1];
        Assert.AreEqual("r1", list.Rid, "the owner's rid");
        Assert.AreEqual(0, list.Data["items"]!.AsArray().Count);
    }

    [TestMethod]
    public async Task RemoveAndClearRoutes_202_401_404()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        var s = h.Get<SessionManager>().Get(sid, c)!;
        s.Room.Rules.Add(Water());
        s.Room.Rules.Add(DoorNotify());
        await using var sse = await h.OpenSseAsync(c, sid);

        AssertError(await h.PostAsync($"sessions/{sid}/automations/rule9/remove", new { }, c.Code), 404, "no_automation");
        AssertError(await h.PostAsync($"sessions/{sid}/automations/rule1/remove", new { }), 401, "bad_code");
        AssertError(await h.PostAsync("sessions/nope/automations/rule1/remove", new { }, c.Code), 404, "no_session");
        AssertError(await h.PostAsync($"sessions/{sid}/automations/clear", new { }), 401, "bad_code");
        AssertError(await h.PostAsync("sessions/nope/automations/clear", new { }, c.Code), 404, "no_session");
        Assert.AreEqual(2, s.Room.Rules.Count, "no refused call changed it");

        var r = await h.PostAsync($"sessions/{sid}/automations/rule1/remove", new { }, c.Code);
        Assert.AreEqual(202, r.Code, r.Body?.ToJsonString());
        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), r.Body));
        var e = await sse.NextAsync();
        Assert.AreEqual("automations", e.Kind);
        Assert.AreEqual("edit", (string?)e.Data["src"]);
        Assert.AreEqual("rule2", (string)e.Data["items"]!.AsArray().Single()!["id"]!);

        // "clear" is the Clear all route, not a switch on an automation called "clear": no body is needed.
        r = await h.PostAsync($"sessions/{sid}/automations/clear", new { }, c.Code);
        Assert.AreEqual(202, r.Code, r.Body?.ToJsonString());
        e = await sse.NextAsync();
        Assert.AreEqual("automations", e.Kind);
        Assert.AreEqual(0, e.Data["items"]!.AsArray().Count);
        Assert.AreEqual(0, s.Room.Rules.Count);
        Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/automations/clear", new { }, c.Code)).Code, "an empty room clears fine");
    }
}
