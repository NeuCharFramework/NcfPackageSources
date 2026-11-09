/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FastForwardTests.cs
    文件功能描述：NeuCharBox Live 测试：FastForwardTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Time.Testing;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class FastForwardTests
{
    const double Start = 16.5, Week = 168;

    static JsonObject Obj(string json) => (JsonObject)JsonNode.Parse(json)!;
    static ActionCall Call(string action, string args, string step) => new(action, Obj(args), step);
    static double Num(RoomState r, string path) => (double)r.Get(path)!;

    static Rule Water(double below = 30, int seconds = 6, int max = 2) =>
        new("r1", "s2", "soil.moisture", RuleOp.Below, JsonValue.Create(below), Call("run_pump", $"{{\"seconds\":{seconds}}}", "s2"), null, max);

    static Rule DoorNotify() =>
        new("r2", "s3", "camera.motion", RuleOp.Becomes, JsonValue.Create(true), null, "someone at the door", 2);

    static Schedule Daily(string id, string step, ActionCall call, string at, params DayOfWeek[] days) =>
        new(id, step, call, at, days.Length == 0 ? null : days.ToHashSet(), null);

    sealed record FirstFault(string Device, TimelineRow Row);

    /// <summary>Records every call in order. OnFirstFault stands in for the runner's fault turn.</summary>
    sealed class Sink : IFastForwardSink
    {
        public readonly List<object> Events = new();
        public Func<string, TimelineRow, bool> OnFirstFault = (_, _) => true;
        public List<Clock> Clocks => Events.OfType<Clock>().ToList();
        public List<StateChange> States => Events.OfType<StateChange>().ToList();
        public List<TimelineRow> Rows => Events.OfType<TimelineRow>().ToList();
        public List<FirstFault> FirstFaults => Events.OfType<FirstFault>().ToList();

        public Task ClockAsync(Clock c) { Events.Add(c); return Task.CompletedTask; }
        public Task StateAsync(StateChange s) { Events.Add(s); return Task.CompletedTask; }
        public Task RowAsync(TimelineRow r) { Events.Add(r); return Task.CompletedTask; }
        public Task<bool> FirstFaultAsync(string device, TimelineRow r)
        {
            Events.Add(new FirstFault(device, r));
            return Task.FromResult(OnFirstFault(device, r));
        }

        /// <summary>The hour of the clock sent before this row.</summary>
        public double HourOf(TimelineRow row) => Events.Take(Events.IndexOf(row)).OfType<Clock>().Last().Hour;
    }

    /// <summary>Home behaviour that notes the hour and the soil moisture each time the pump really runs.</summary>
    sealed class PumpSpy : ISceneBehavior
    {
        readonly HomeBehavior _home = new();
        public readonly List<(double Hour, double Soil)> Runs = new();

        public ActionOutcome Apply(ActionSpec spec, JsonObject args, RoomState room)
        {
            if (spec.Name == "run_pump") Runs.Add((room.Hour, (double)room.Get("soil.moisture")!));
            return _home.Apply(spec, args, room);
        }
        public string Label(ActionSpec spec, JsonObject args) => _home.Label(spec, args);
        public IReadOnlyList<SimHappening> Advance(RoomState room, double fromHour, double toHour) => _home.Advance(room, fromHour, toHour);
        public double? NextHappening(RoomState room) => _home.NextHappening(room);
    }

    sealed record Rig(SceneManifest M, RoomState Room, FastForward Ff, InstantPacer Pacer, Sink Sink);

    static Rig Setup(ISceneBehavior? b = null, FakeTimeProvider? fake = null)
    {
        var m = ManifestLoader.Load("home");
        b ??= ManifestLoader.Behavior("home");
        var pacer = new InstantPacer(fake is null ? null : fake.Advance);
        return new(m, RoomState.PostSetup(m), new FastForward(new Simulator(m, b), b, pacer, new FastForwardPacing()), pacer, new Sink());
    }

    [TestMethod]
    public async Task Soil_Drops2PerDay_Plus1OnSunnyAfternoons()
    {
        var (_, shaded, ff, _, sink) = Setup();
        shaded.Set("blinds.closed", JsonValue.Create(1.0));
        await ff.RunAsync(shaded, Week, sink, default);
        Assert.AreEqual(34 - 14, Num(shaded, "soil.moisture"), 0.05);

        // Blinds open: Fri 16:30–17:00, Sat–Thu 13:00–17:00 and Fri 13:00–16:30 are 28 sunny hours.
        var (_, open, ff2, _, sink2) = Setup();
        await ff2.RunAsync(open, Week, sink2, default);
        Assert.AreEqual(34 - 14 - 7, Num(open, "soil.moisture"), 0.05);

        var (_, dry, ff3, _, sink3) = Setup();
        dry.Set("soil.moisture", JsonValue.Create(1.0));
        await ff3.RunAsync(dry, Week, sink3, default);
        Assert.AreEqual(0, Num(dry, "soil.moisture"));
    }

    [TestMethod]
    public async Task Thermostat_DriftsHalfDegreePerHour()
    {
        var (_, room, ff, _, sink) = Setup();
        room.Set("thermostat.target", JsonValue.Create(18.0));
        await ff.RunAsync(room, 4, sink, default);
        Assert.AreEqual(19.4, Num(room, "thermostat.current"), 1e-9);
        await ff.RunAsync(room, 3, sink, default);
        Assert.AreEqual(18.0, Num(room, "thermostat.current")); // 21.4 − 3.5 would be 17.9: it stops at the target

        var (_, warm, ff2, _, sink2) = Setup();
        warm.Set("thermostat.target", JsonValue.Create(22.0));
        await ff2.RunAsync(warm, 4, sink2, default);
        Assert.AreEqual(22.0, Num(warm, "thermostat.current"));
    }

    [TestMethod]
    public async Task Schedule_DailyFiresEachDay_WithLabelRow()
    {
        var (_, room, ff, _, sink) = Setup();
        room.Schedules.Add(Daily("sc1", "s1", Call("set_lamp", "{\"brightness\":35}", "s1"), "18:00"));
        var report = await ff.RunAsync(room, 72, sink, default);

        CollectionAssert.AreEqual(new[] { "Fri 18:00", "Sat 18:00", "Sun 18:00" }, report.Rows.Select(r => r.At).ToArray());
        foreach (var r in report.Rows)
        {
            Assert.AreEqual("Floor lamp → 35%", r.Text); Assert.IsTrue(r.Ok);
            Assert.AreEqual("action", r.Kind); Assert.AreEqual("s1", r.Step); Assert.AreEqual("lamp", r.Device);
        }
        CollectionAssert.AreEqual(report.Rows.ToArray(), sink.Rows.ToArray());
        Assert.IsFalse(report.Paused);
        Assert.AreEqual(0.35, Num(room, "lamp.brightness"), 1e-9);
        // A device action's numbers go out at once; only drift waits for a 0.5 step.
        Assert.AreEqual(0.35, (double)sink.States.Single(s => s.Path == "lamp.brightness").Value!, 1e-9);
        Assert.AreEqual(true, (bool)sink.States.Single(s => s.Path == "lamp.on").Value!);
    }

    [TestMethod]
    public async Task Schedule_DaysFilterAndOnce()
    {
        var (m, room, ff, _, sink) = Setup();
        var once = (DateTime.ParseExact("2026-11-14T07:00", "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture) - m.StartDay).TotalHours;
        room.Schedules.Add(Daily("we", "s1", Call("set_lamp", "{\"brightness\":50}", "s1"), "09:00", DayOfWeek.Saturday, DayOfWeek.Sunday));
        room.Schedules.Add(new Schedule("once", "s2", Call("set_blinds", "{\"closed_percent\":100}", "s2"), null, null, once));

        var week1 = await ff.RunAsync(room, Week, sink, default);
        CollectionAssert.AreEqual(new[] { "Sat 09:00", "Sun 09:00" }, week1.Rows.Where(r => r.Step == "s1").Select(r => r.At).ToArray());
        var o = week1.Rows.Single(r => r.Step == "s2");
        Assert.AreEqual("Sat 07:00", o.At); Assert.AreEqual("Blinds → closed", o.Text);

        var week2 = await ff.RunAsync(room, Week, sink, default);
        Assert.AreEqual(2, week2.Rows.Count(r => r.Step == "s1"));
        Assert.AreEqual(0, week2.Rows.Count(r => r.Step == "s2"));
    }

    [TestMethod]
    public async Task Rule_BelowWithMaxPerDay_WatersAndCaps()
    {
        var spy = new PumpSpy();
        var (_, room, ff, _, sink) = Setup(spy);
        room.Rules.Add(Water());
        var report = await ff.RunAsync(room, Week, sink, default);

        var waters = report.Rows.Where(r => r.Text == "Water pump → run 6 s").ToList();
        Assert.IsTrue(waters.Count >= 1);
        Assert.AreEqual(waters.Count, spy.Runs.Count);
        Assert.IsTrue(waters.All(r => r.Ok && r.Kind == "action" && r.Step == "s2" && r.Device == "pump"));
        foreach (var (hour, soil) in spy.Runs) Assert.IsTrue(soil < 30, $"watered at {soil} (hour {hour})");
        for (var i = 1; i < spy.Runs.Count; i++) Assert.IsTrue(spy.Runs[i].Hour - spy.Runs[i - 1].Hour >= 6 - 1e-9);
        Assert.IsTrue(spy.Runs.GroupBy(r => Math.Floor(r.Hour / 24)).All(g => g.Count() <= 2));
        Assert.IsTrue(Num(room, "soil.moisture") > 20);

        // Below 60 always holds, so only the 6 h gap and the cap of 2 a day hold it back.
        var spy2 = new PumpSpy();
        var (_, room2, ff2, _, sink2) = Setup(spy2);
        room2.Rules.Add(Water(below: 60, seconds: 1));
        await ff2.RunAsync(room2, Week, sink2, default);
        var perDay = spy2.Runs.GroupBy(r => (int)Math.Floor(r.Hour / 24)).ToDictionary(g => g.Key, g => g.Count());
        for (var day = 1; day <= 6; day++) Assert.AreEqual(2, perDay[day], $"day {day}");
        for (var i = 1; i < spy2.Runs.Count; i++) Assert.IsTrue(spy2.Runs[i].Hour - spy2.Runs[i - 1].Hour >= 6 - 1e-9);
        Assert.IsTrue(room2.Rules[0].FiredPerDay.Values.All(n => n <= 2));
    }

    static Rule CloseBlinds() =>
        new("r4", "s3", "soil.moisture", RuleOp.Below, JsonValue.Create(30.0), Call("set_blinds", "{\"closed_percent\":100}", "s3"), null, 2);

    [TestMethod]
    public async Task Rules_InOneTick_SeeTheTicksValues_NotAnEarlierFire()
    {
        // Soil 30.2 dries under 30 at Fri 18:00. The pump fires first and waters it to ~37; the blinds rule still reads
        // the tick's value (under 30), as a hand edit does, so both fire at the same time.
        var (_, room, ff, _, sink) = Setup();
        room.Set("soil.moisture", JsonValue.Create(30.2));
        room.Rules.Add(Water());
        room.Rules.Add(CloseBlinds());
        var report = await ff.RunAsync(room, 24, sink, default);

        var pump = report.Rows.Single(r => r.Device == "pump");
        var blinds = report.Rows.Single(r => r.Device == "blinds");
        Assert.AreEqual("Water pump → run 6 s", pump.Text);
        Assert.AreEqual("Blinds → closed", blinds.Text);
        Assert.IsTrue(pump.Origin == "rule" && blinds.Origin == "rule");
        Assert.AreEqual(pump.At, blinds.At, "both rules fired in the same tick");
        Assert.IsTrue(sink.Rows.IndexOf(pump) < sink.Rows.IndexOf(blinds), "in rule order");
        Assert.AreEqual(1.0, Num(room, "blinds.closed"));
        Assert.IsTrue(Num(room, "soil.moisture") > 30, "the pump watered the soil");
    }

    [TestMethod]
    public async Task FastForward_RowsCarryOrigin()
    {
        var (_, room, ff, _, sink) = Setup();
        room.Schedules.Add(Daily("sc1", "s1", Call("set_lamp", "{\"brightness\":35}", "s1"), "18:00"));
        room.Rules.Add(Water(below: 60, seconds: 1));
        var report = await ff.RunAsync(room, Week, sink, default);
        var lamp = report.Rows.Where(r => r.Text.StartsWith("Floor lamp")).ToList();
        Assert.IsTrue(lamp.Count > 0 && lamp.All(r => r.Origin == "schedule"));
        var water = report.Rows.Where(r => r.Text.StartsWith("Water pump")).ToList();
        Assert.IsTrue(water.Count > 0 && water.All(r => r.Origin == "rule"));
    }

    [TestMethod]
    public async Task Rule_BecomesNotify_VisitorTuesday()
    {
        var (_, room, ff, _, sink) = Setup();
        room.Set("camera.armed", JsonValue.Create(true));
        room.Rules.Add(DoorNotify());
        var report = await ff.RunAsync(room, Week, sink, default);

        var n = report.Rows.Single(r => r.Kind == "notify");
        Assert.AreEqual("Tue 14:20", n.At); Assert.AreEqual("someone at the door", n.Text);
        Assert.IsTrue(n.Ok); Assert.AreEqual("s3", n.Step);
        Assert.AreEqual(false, (bool)room.Get("camera.motion")!);
        CollectionAssert.AreEqual(new[] { true, false }, sink.States.Where(s => s.Path == "camera.motion").Select(s => (bool)s.Value!).ToArray());
        Assert.IsTrue(room.VisitorDone);
    }

    [TestMethod]
    public async Task Visitor_OnlyWhenArmed_AndOnlyOnce()
    {
        var home = new HomeBehavior();
        var (_, disarmed, ff, _, sink) = Setup();
        Assert.AreEqual(110 + 20 / 60.0, home.NextHappening(disarmed)!.Value, 1e-9);
        disarmed.Rules.Add(DoorNotify());
        var none = await ff.RunAsync(disarmed, Week, sink, default);
        Assert.AreEqual(0, none.Rows.Count);
        Assert.IsFalse(sink.States.Any(s => s.Path == "camera.motion"));

        var (_, armed, ff2, _, sink2) = Setup();
        armed.Set("camera.armed", JsonValue.Create(true));
        armed.Rules.Add(DoorNotify());
        await ff2.RunAsync(armed, 110 + 20 / 60.0 - Start, sink2, default); // stop on the visitor
        Assert.AreEqual(true, (bool)armed.Get("camera.motion")!);
        Assert.AreEqual(110 + 21 / 60.0, home.NextHappening(armed)!.Value, 1e-9);
        var rest = await ff2.RunAsync(armed, Start + Week - armed.Hour, sink2, default);
        Assert.AreEqual(false, (bool)armed.Get("camera.motion")!);
        Assert.IsNull(home.NextHappening(armed));
        var week2 = await ff2.RunAsync(armed, Week, sink2, default);
        Assert.AreEqual(1, sink2.Rows.Count(r => r.Kind == "notify"));
        Assert.AreEqual(0, rest.Rows.Count + week2.Rows.Count);
    }

    [TestMethod]
    public async Task FailedDevice_FirstHitPauses_LaterHitsOneRowPerDay()
    {
        var (_, room, ff, _, sink) = Setup();
        room.Failed.Add("pump");
        room.Rules.Add(Water());
        sink.OnFirstFault = (device, _) => { room.Set($"{device}.status", JsonValue.Create("fault")); return true; };
        var report = await ff.RunAsync(room, Week, sink, default);

        var faults = report.Rows.Where(r => r.Kind == "fault").ToList();
        Assert.IsTrue(faults.Count >= 2);
        Assert.AreEqual(faults.Count, report.Rows.Count);
        Assert.IsTrue(faults.All(r => !r.Ok && r.Device == "pump" && r.Text == "Water pump not responding" && r.Step == "s2"));
        var first = sink.FirstFaults.Single();
        Assert.AreEqual("pump", first.Device); Assert.AreEqual(faults[0], first.Row);
        Assert.AreEqual(first, sink.Events[sink.Events.IndexOf(faults[0]) + 1]);
        Assert.IsTrue(faults.GroupBy(r => Math.Floor(sink.HourOf(r) / 24)).All(g => g.Count() == 1));
        // A failed fire counts as a fire: twice on some day, still one row that day.
        Assert.AreEqual(2, room.Rules[0].FiredPerDay.Values.Max());
        Assert.IsFalse(sink.States.Any(s => s.Path == "pump.status"));
        Assert.AreEqual("fault", (string)room.Get("pump.status")!);
        Assert.IsTrue(room.Faulted.Contains("pump"));
        Assert.AreEqual(13, Num(room, "soil.moisture"), 0.05); // the pump never watered
        Assert.IsFalse(report.Paused);
        Assert.AreEqual("not responding", (string)report.EndReadings["pump"]!["error"]!);
    }

    [TestMethod]
    public async Task FirstFault_SinkReturnsFalse_StopsPaused()
    {
        var (m, room, ff, _, sink) = Setup();
        room.Failed.Add("pump");
        room.Rules.Add(Water());
        sink.OnFirstFault = (_, _) => false;
        var report = await ff.RunAsync(room, Week, sink, default);

        Assert.IsTrue(report.Paused);
        var row = report.Rows.Single();
        Assert.AreEqual("fault", row.Kind);
        Assert.AreEqual(SimClock.At(m.StartDay, room.Hour).Label, row.At);
        Assert.IsTrue(room.Hour < Start + Week);
        Assert.IsTrue(sink.Rows.All(r => sink.HourOf(r) <= room.Hour)); // no row later than the fault's hour
        Assert.AreEqual(1, sink.Rows.Count);

        // A row from the same instant, after the fault in the step, still goes out: its state already did.
        var (_, same, ff2, _, sink2) = Setup();
        same.Failed.Add("pump");
        same.Schedules.Add(Daily("p", "s2", Call("run_pump", "{\"seconds\":6}", "s2"), "18:00"));
        same.Schedules.Add(Daily("l", "s1", Call("set_lamp", "{\"brightness\":35}", "s1"), "18:00"));
        sink2.OnFirstFault = (_, _) => false;
        var paused = await ff2.RunAsync(same, Week, sink2, default);

        Assert.IsTrue(paused.Paused);
        Assert.AreEqual(18, same.Hour);
        CollectionAssert.AreEqual(new[] { "Water pump not responding", "Floor lamp → 35%" }, paused.Rows.Select(r => r.Text).ToArray());
        Assert.IsTrue(paused.Rows.All(r => r.At == "Fri 18:00"));
        CollectionAssert.AreEqual(paused.Rows.ToArray(), sink2.Rows.ToArray());
        Assert.IsTrue(sink2.Rows.All(r => sink2.HourOf(r) <= same.Hour));
        var handOff = sink2.Events.FindIndex(e => e is FirstFault);
        Assert.IsTrue(sink2.Events.Skip(handOff + 1).All(e => e is TimelineRow r && r.At == "Fri 18:00"));
    }

    [TestMethod]
    public async Task Visitor_FailedCamera_MissesVisitor()
    {
        var (_, room, ff, _, sink) = Setup();
        room.Set("camera.armed", JsonValue.Create(true));
        room.Failed.Add("camera");
        room.Rules.Add(DoorNotify());
        var report = await ff.RunAsync(room, Week, sink, default);

        Assert.IsTrue(room.VisitorDone);
        Assert.AreEqual(false, (bool)room.Get("camera.motion")!);
        Assert.IsFalse(sink.States.Any(s => s.Path == "camera.motion"));
        Assert.AreEqual(0, report.Rows.Count(r => r.Kind == "notify"));
        Assert.IsTrue(report.Rows.Any(r => r.Kind == "fault" && r.Device == "camera" && r.Text == "Door camera not responding"));
        Assert.IsNull(new HomeBehavior().NextHappening(room));
    }

    [TestMethod]
    public async Task Pacing_WeekUnderCap()
    {
        var fake = new FakeTimeProvider();
        var t0 = fake.GetUtcNow();
        var (_, room, ff, pacer, sink) = Setup(fake: fake);
        room.Schedules.Add(Daily("a", "s1", Call("set_lamp", "{\"brightness\":35}", "s1"), "18:00"));
        room.Schedules.Add(Daily("b", "s1", Call("set_lamp", "{\"brightness\":0}", "s1"), "23:00"));
        room.Schedules.Add(Daily("c", "s4", Call("set_blinds", "{\"closed_percent\":0}", "s4"), "08:00"));
        room.Schedules.Add(Daily("d", "s4", Call("set_blinds", "{\"closed_percent\":100}", "s4"), "20:00"));
        room.Schedules.Add(Daily("e", "s5", Call("set_thermostat", "{\"celsius\":19}", "s5"), "07:00"));
        room.Set("camera.armed", JsonValue.Create(true));
        room.Rules.Add(Water());
        room.Rules.Add(DoorNotify());
        var report = await ff.RunAsync(room, Week, sink, default);

        Assert.IsTrue(0.1 * Week + 0.6 * report.Rows.Count > 30, "the busy week must reach the cap");
        Assert.AreEqual(30, pacer.Total.TotalSeconds, 1e-3); // 25 s × 168/168 + 5 s, a hard cap
        Assert.IsTrue(pacer.Total.TotalSeconds <= 30 + 1e-6);
        Assert.AreEqual(pacer.Total, fake.GetUtcNow() - t0); // the fake clock sees pacer time

        var (_, idle, ff2, pacer2, sink2) = Setup();
        await ff2.RunAsync(idle, 24, sink2, default);
        Assert.AreEqual(2.4, pacer2.Total.TotalSeconds, 1e-3);
    }

    [TestMethod]
    public async Task Clock_EmittedHourlyAndAtEvents_StateOnlyWhenMoved()
    {
        var (_, room, ff, _, sink) = Setup();
        await ff.RunAsync(room, Week, sink, default);

        var clocks = sink.Clocks;
        Assert.AreEqual(169, clocks.Count);
        Assert.AreEqual("Fri 17:00", clocks[0].Label);
        CollectionAssert.AreEqual(Enumerable.Range(17, 168).Select(h => (double)h).ToArray(), clocks.Take(168).Select(c => c.Hour).ToArray());
        Assert.AreEqual("Fri 16:30", clocks[^1].Label); Assert.AreEqual(184.5, clocks[^1].Hour);

        // Numbers go out when they moved 0.5 since the last one sent; after the last clock, a final exact one per drifting path.
        var sent = new Dictionary<string, double> { ["soil.moisture"] = 34, ["thermostat.current"] = 21.4 };
        var lastClock = sink.Events.FindLastIndex(e => e is Clock);
        var finals = new HashSet<string>();
        for (var i = 0; i < sink.Events.Count; i++)
        {
            if (sink.Events[i] is not StateChange s) continue;
            Assert.IsTrue(sent.ContainsKey(s.Path), s.Path);
            var v = (double)s.Value!;
            var moved = Math.Abs(v - sent[s.Path]) >= 0.5 - 1e-9;
            if (!moved)
            {
                Assert.IsTrue(i > lastClock, $"{s.Path} {v} sent mid-run without moving 0.5");
                Assert.IsTrue(finals.Add(s.Path), $"second final state for {s.Path}");
            }
            sent[s.Path] = v;
            Assert.AreEqual(0, s.Ms);
        }
        Assert.AreEqual(Num(room, "soil.moisture"), sent["soil.moisture"]);
        Assert.AreEqual(21.0, sent["thermostat.current"]);
        Assert.AreEqual(1, sink.States.Count(s => s.Path == "thermostat.current")); // 21.4 → 21 never moved 0.5
        // Soil falls 21: at most 21 / 0.5 sends plus the final; each send covers under 0.5 + 1/3 (the sunniest hour).
        Assert.IsTrue(sink.States.Count(s => s.Path == "soil.moisture") is >= 26 and <= 43);

        // With rows: a clock goes out with them, even off the hour, and each row's At is that clock's label.
        var (_, busy, ff2, _, sink2) = Setup();
        busy.Set("camera.armed", JsonValue.Create(true));
        busy.Rules.Add(DoorNotify());
        busy.Schedules.Add(Daily("sc1", "s1", Call("set_lamp", "{\"brightness\":35}", "s1"), "18:00"));
        await ff2.RunAsync(busy, Week, sink2, default);
        Clock? last = null;
        foreach (var e in sink2.Events)
        {
            if (e is Clock c) last = c;
            if (e is TimelineRow r) Assert.AreEqual(last!.Label, r.At);
        }
        Assert.AreEqual(8, sink2.Rows.Count);
        Assert.AreEqual(170, sink2.Clocks.Count); // the hourly 169, plus Tue 14:20
        Assert.AreEqual(110 + 20 / 60.0, sink2.Clocks.Single(c => c.Label == "Tue 14:20").Hour, 1e-9);
    }

    [TestMethod]
    public async Task Hours_Over168_Rejected()
    {
        var (_, room, ff, _, sink) = Setup();
        foreach (var h in new[] { 169, 168.01, 0, -1, double.NaN })
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => ff.RunAsync(room, h, sink, default), h.ToString(CultureInfo.InvariantCulture));
        Assert.AreEqual(Start, room.Hour);
        Assert.AreEqual(0, sink.Events.Count);
    }
}
