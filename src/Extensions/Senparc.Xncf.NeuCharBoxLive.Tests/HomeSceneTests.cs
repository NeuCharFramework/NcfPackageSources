/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：HomeSceneTests.cs
    文件功能描述：测试 Home 场景的清单、起始状态与行为


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class HomeSceneTests
{
    static JsonObject Obj(string json) => (JsonObject)JsonNode.Parse(json)!;

    static (SceneManifest m, RoomState r, Simulator sim) Setup()
    {
        var m = ManifestLoader.Load("home");
        return (m, RoomState.PostSetup(m), new Simulator(m, ManifestLoader.Behavior("home")));
    }

    [TestMethod]
    public void PostSetup_MatchesScriptedScene()
    {
        var m = ManifestLoader.Load("home"); var r = RoomState.PostSetup(m);
        Assert.AreEqual(34, (double)r.Get("soil.moisture")!);
        Assert.AreEqual(21, (double)r.Get("thermostat.target")!); Assert.AreEqual(21.4, (double)r.Get("thermostat.current")!);
        Assert.AreEqual(false, (bool)r.Get("lamp.on")!); Assert.AreEqual(0, (double)r.Get("blinds.closed")!);
        Assert.AreEqual(false, (bool)r.Get("camera.armed")!); Assert.AreEqual(false, (bool)r.Get("pump.running")!);
        foreach (var d in m.Devices) Assert.AreEqual("online", (string)r.Get($"{d.Id}.status")!);
        Assert.AreEqual(16.5, r.Hour); Assert.AreEqual(16.5, (double)r.ToWire()["env"]!["hour"]!);
        Assert.AreEqual(false, (bool)r.Get("blinds.jammed")!);
        CollectionAssert.AreEqual(new[] { "lamp", "blinds", "soil", "pump", "camera", "thermostat" }, m.Devices.Select(d => d.Id).ToArray());
        Assert.IsTrue(m.Suggestions.Count is >= 2 and <= 4);
        Assert.IsInstanceOfType<HomeBehavior>(ManifestLoader.Behavior("home"));
        Assert.ThrowsException<KeyNotFoundException>(() => ManifestLoader.Load("nope"));
        Assert.ThrowsException<KeyNotFoundException>(() => ManifestLoader.Behavior("nope"));
        CollectionAssert.AreEqual(new[] { "home" }, ManifestLoader.SceneIds.ToArray());
    }

    [TestMethod]
    public void Manifest_ActionsAndDevicesResolve()
    {
        var m = ManifestLoader.Load("home");
        Assert.AreEqual("pump", m.Action("run_pump")!.Device);
        Assert.AreEqual(ActionKind.Safe, m.Action("stop_pump")!.Kind);
        Assert.AreEqual(ActionKind.Read, m.Action("read_soil")!.Kind);
        Assert.AreEqual(0, (int)m.Action("set_lamp")!.SafeWhen!["brightness"]!);
        Assert.IsNull(m.Action("nope")); Assert.IsNull(m.Device("nope"));
        Assert.AreEqual("the water pump", m.Device("pump")!.Ref);
        StringAssert.Contains(m.Action("run_pump")!.Description, "Confirm watering by reading the soil sensor");
        Assert.AreEqual(7, m.Devices.Sum(d => d.Actions.Count));
    }

    [TestMethod]
    public void Clone_IsDeep()
    {
        var (_, r, _) = Setup();
        r.Schedules.Add(new Schedule("s1", "step1", new ActionCall("set_lamp", Obj("{\"brightness\":20}"), "step1"), "18:00", new HashSet<DayOfWeek> { DayOfWeek.Friday }, null));
        r.Rules.Add(new Rule("r1", "step2", "soil.moisture", RuleOp.Below, JsonValue.Create(30), new ActionCall("run_pump", Obj("{\"seconds\":5}"), "step2"), null, 1)
        { LastFiredHour = 10, LastSeen = JsonValue.Create(40) });
        r.Rules[0].FiredPerDay[1] = 1;
        var c = r.Clone();

        c.Set("lamp.brightness", JsonValue.Create(0.9));
        c.Rules[0].LastFiredHour = 99; c.Rules[0].FiredPerDay[1] = 5; c.Rules[0].FiredPerDay[2] = 1; c.Rules[0].LastSeen = JsonValue.Create(1);
        c.Rules[0].Then!.Args["seconds"] = 30;
        c.Schedules[0].Call.Args["brightness"] = 99;
        c.Failed.Add("pump"); c.Faulted.Add("lamp"); c.Hour = 40; c.VisitorDone = true;

        Assert.AreEqual(0, (double)r.Get("lamp.brightness")!);
        Assert.AreEqual(10, r.Rules[0].LastFiredHour);
        Assert.AreEqual(1, r.Rules[0].FiredPerDay[1]); Assert.AreEqual(1, r.Rules[0].FiredPerDay.Count);
        Assert.AreEqual(40, (int)r.Rules[0].LastSeen!);
        Assert.AreEqual(5, (int)r.Rules[0].Then!.Args["seconds"]!);
        Assert.AreEqual(20, (int)r.Schedules[0].Call.Args["brightness"]!);
        Assert.AreEqual(0, r.Failed.Count); Assert.AreEqual(0, r.Faulted.Count);
        Assert.AreEqual(16.5, r.Hour); Assert.IsFalse(r.VisitorDone);
    }

    [TestMethod]
    public void Set_StoresACopy()
    {
        var (_, r, _) = Setup();
        var node = Obj("{\"a\":1}");
        r.Set("lamp.x", node); r.Set("pump.x", node);
        node["a"] = 2;
        Assert.AreEqual(1, (int)r.Get("lamp.x")!["a"]!);
        Assert.AreEqual(1, (int)r.Get("pump.x")!["a"]!);
        Assert.IsNull(r.Get("lamp.nothing")); Assert.IsNull(r.Get("ghost.on"));
    }

    [TestMethod]
    public void Clock_StartIsFriday1630()
    {
        var c = SimClock.At(new DateTime(2026, 11, 13), 16.5);
        Assert.AreEqual("2026-11-13T16:30:00", c.Iso); Assert.AreEqual("Fri 16:30", c.Label);
        Assert.AreEqual("Sat 07:10", SimClock.At(new DateTime(2026, 11, 13), 31 + 10 / 60.0).Label);
    }

    [DataTestMethod]
    [DataRow("set_lamp", "{\"brightness\":20}", "lamp.brightness", 0.2, 1200, "Floor lamp → 20%")]
    [DataRow("set_lamp", "{\"brightness\":0}", "lamp.brightness", 0.0, 1200, "Floor lamp → off")]
    [DataRow("set_blinds", "{\"closed_percent\":100}", "blinds.closed", 1.0, 3000, "Blinds → closed")]
    [DataRow("set_blinds", "{\"closed_percent\":0}", "blinds.closed", 0.0, 3000, "Blinds → open")]
    [DataRow("set_blinds", "{\"closed_percent\":40}", "blinds.closed", 0.4, 3000, "Blinds → 40% closed")]
    [DataRow("set_thermostat", "{\"celsius\":18}", "thermostat.target", 18.0, 600, "Thermostat → 18 °C")]
    public void Write_ChangesPathAndLabels(string action, string args, string path, double value, int ms, string label)
    {
        var (m, r, sim) = Setup();
        var a = Obj(args);
        var o = sim.Execute(new ActionCall(action, a, "s"), r);
        Assert.IsTrue(o.Ok);
        var ch = o.Changes.Single(c => c.Path == path);
        Assert.AreEqual(value, (double)ch.Value!); Assert.AreEqual(ms, ch.Ms);
        Assert.AreEqual(value, (double)r.Get(path)!);
        Assert.AreEqual(label, ManifestLoader.Behavior("home").Label(m.Action(action)!, a));
        Assert.AreEqual(action == "set_lamp" && value == 0 ? Outcome.MadeSafe : Outcome.Confirmed, o.Outcome);
        Assert.AreEqual(ms, o.DurationMs);
    }

    [TestMethod]
    public void Lamp_OnFlagFollowsBrightness()
    {
        var (_, r, sim) = Setup();
        var on = sim.Execute(new ActionCall("set_lamp", Obj("{\"brightness\":20}"), null), r);
        Assert.AreEqual("lamp.on", on.Changes[0].Path); Assert.AreEqual(true, (bool)on.Changes[0].Value!); Assert.AreEqual(0, on.Changes[0].DelayMs);
        Assert.AreEqual(true, (bool)r.Get("lamp.on")!);
        var off = sim.Execute(new ActionCall("set_lamp", Obj("{\"brightness\":0}"), null), r);
        var flag = off.Changes.Single(c => c.Path == "lamp.on");
        Assert.AreEqual(false, (bool)flag.Value!); Assert.AreEqual(1200, flag.DelayMs);
        Assert.AreEqual(false, (bool)r.Get("lamp.on")!);
    }

    [TestMethod]
    public void Pump_IsCommandedNotConfirmed_WaterArrivesAfter2s()
    {
        var (m, r, sim) = Setup();
        var a = Obj("{\"seconds\":6}");
        var o = sim.Execute(new ActionCall("run_pump", a, null), r);
        Assert.AreEqual(Outcome.Commanded, o.Outcome); Assert.AreEqual("commanded", o.Text);
        var start = o.Changes.First(c => c.Path == "pump.running");
        Assert.AreEqual(true, (bool)start.Value!); Assert.AreEqual(0, start.DelayMs);
        var soil = o.Changes.Single(c => c.Path == "soil.moisture");
        Assert.AreEqual(34 + 1.2 * 6, (double)soil.Value!, 1e-9); Assert.AreEqual(2000, soil.DelayMs); Assert.AreEqual(1500, soil.Ms);
        var stop = o.Changes.Last(c => c.Path == "pump.running");
        Assert.AreEqual(false, (bool)stop.Value!); Assert.AreEqual(3000, stop.DelayMs); Assert.AreEqual(3000, o.DurationMs);
        Assert.AreEqual(false, o.ModelResult["confirmed"]!.GetValue<bool>());
        Assert.AreEqual("Water pump → run 6 s", ManifestLoader.Behavior("home").Label(m.Action("run_pump")!, a));
        Assert.AreEqual(2500, sim.Execute(new ActionCall("run_pump", Obj("{\"seconds\":2}"), null), r).DurationMs);
    }

    [TestMethod]
    public void Pump_SoilCappedAt60()
    {
        var (_, r, sim) = Setup();
        r.Set("soil.moisture", JsonValue.Create(55.0));
        sim.Execute(new ActionCall("run_pump", Obj("{\"seconds\":30}"), null), r);
        Assert.AreEqual(60, (double)r.Get("soil.moisture")!);
    }

    [TestMethod]
    public void ReadSoil_ReturnsReading()
    {
        var (_, r, sim) = Setup();
        var o = sim.Execute(new ActionCall("read_soil", new JsonObject(), null), r);
        Assert.AreEqual(Outcome.Reading, o.Outcome); Assert.AreEqual("34%", o.Text);
        Assert.AreEqual("{\"ok\":true,\"commanded\":false,\"reading\":{\"moisture\":34}}", o.ModelResult.ToJsonString());
        Assert.AreEqual(0, o.Changes.Count); Assert.AreEqual(600, o.DurationMs);
    }

    [TestMethod]
    public void FailedDevice_NotResponding()
    {
        var (_, r, sim) = Setup();
        r.Failed.Add("pump");
        var o = sim.Execute(new ActionCall("run_pump", Obj("{\"seconds\":6}"), null), r);
        Assert.IsFalse(o.Ok); Assert.AreEqual(Outcome.Failed, o.Outcome); Assert.AreEqual("not responding", o.Text);
        Assert.AreEqual(0, o.Changes.Count);
        Assert.AreEqual(34, (double)r.Get("soil.moisture")!);
        Assert.AreEqual(false, (bool)r.Get("pump.running")!);
    }

    [DataTestMethod]
    [DataRow("set_lamp", "{\"brightness\":\"20%\"}")]
    [DataRow("set_lamp", "{\"brightness\":150}")]
    [DataRow("set_lamp", "{}")]
    [DataRow("run_pump", "{\"seconds\":0}")]
    [DataRow("set_door_camera", "{\"armed\":\"yes\"}")]
    [DataRow("set_thermostat", "{\"celsius\":40}")]
    [DataRow("set_lamp", "{\"brightness\":5,\"extra\":1}")]
    [DataRow("nope", "{}")]
    public void Execute_InvalidArgs_AreRejectedWithoutChanges(string action, string args)
    {
        var (_, r, sim) = Setup();
        var before = r.ToWire().ToJsonString();
        var o = sim.Execute(new ActionCall(action, Obj(args), null), r);
        Assert.IsFalse(o.Ok); Assert.AreEqual(Outcome.Invalid, o.Outcome); Assert.AreEqual(0, o.Changes.Count);
        Assert.AreEqual(before, r.ToWire().ToJsonString());
    }

    [TestMethod]
    public void EveryLabelPassesCopyGuardWords()
    {
        var m = ManifestLoader.Load("home"); var b = ManifestLoader.Behavior("home");
        var boundary = new Dictionary<string, JsonObject[]>
        {
            ["set_lamp"] = new[] { Obj("{\"brightness\":0}"), Obj("{\"brightness\":1}"), Obj("{\"brightness\":99}"), Obj("{\"brightness\":100}") },
            ["set_blinds"] = new[] { Obj("{\"closed_percent\":0}"), Obj("{\"closed_percent\":99}"), Obj("{\"closed_percent\":100}") },
            ["read_soil"] = new[] { new JsonObject() },
            ["run_pump"] = new[] { Obj("{\"seconds\":1}"), Obj("{\"seconds\":30}") },
            ["stop_pump"] = new[] { new JsonObject() },
            ["set_door_camera"] = new[] { Obj("{\"armed\":true}"), Obj("{\"armed\":false}") },
            ["set_thermostat"] = new[] { Obj("{\"celsius\":10}"), Obj("{\"celsius\":28}"), Obj("{\"celsius\":21.5}") },
        };
        foreach (var d in m.Devices) foreach (var a in d.Actions)
        {
            Assert.IsTrue(boundary.ContainsKey(a.Name), a.Name);
            foreach (var args in boundary[a.Name])
            {
                var label = b.Label(a, args);
                Assert.IsFalse(Regex.IsMatch(label, @"100\s?%"), label);
            }
        }
    }
}
