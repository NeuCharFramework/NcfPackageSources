/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RoomEditorTests.cs
    文件功能描述：测试房间手动编辑：可编辑路径、取值范围与设备不可用


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class RoomEditorTests
{
    static (SceneManifest m, RoomState r, RoomEditor ed) Setup()
    {
        var m = ManifestLoader.Load("home");
        return (m, RoomState.PostSetup(m), new RoomEditor(m, new Simulator(m, ManifestLoader.Behavior("home"))));
    }

    [TestMethod]
    public void Validate_RejectsUnknownHiddenWrongTypeAndOutOfRange()
    {
        var m = ManifestLoader.Load("home");
        foreach (var p in new[] { "soil.probeOut", "lamp.status", "nope.x", "lamp" })
            Assert.AreEqual("bad_value", RoomEditor.Validate(m, p, JsonValue.Create(true)), p);
        Assert.AreEqual("bad_value", RoomEditor.Validate(m, null, JsonValue.Create(1)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(m, "lamp.brightness", null));
        Assert.AreEqual("bad_value", RoomEditor.Validate(m, "lamp.brightness", JsonValue.Create(1.2)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(m, "thermostat.target", JsonValue.Create(9)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(m, "thermostat.current", JsonValue.Create(36)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(m, "pump.running", JsonValue.Create(1)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(m, "soil.moisture", JsonValue.Create(true)));
        Assert.AreEqual("bad_value", RoomEditor.Validate(m, "soil.moisture", JsonValue.Create("25")));
        Assert.IsNull(RoomEditor.Validate(m, "soil.moisture", JsonValue.Create(25)));
        Assert.IsNull(RoomEditor.Validate(m, "camera.motion", JsonValue.Create(true)));
        Assert.IsNull(RoomEditor.Validate(m, "thermostat.current", JsonValue.Create(18.5)));
        Assert.IsNull(RoomEditor.Validate(m, "thermostat.target", JsonValue.Create(10)));
        Assert.IsNull(RoomEditor.Validate(m, "thermostat.target", JsonValue.Create(28)));
    }

    [TestMethod]
    public void Apply_LampBrightnessSetsOn()
    {
        var (_, room, ed) = Setup();
        var o = ed.Apply(room, "lamp.brightness", JsonValue.Create(0.3));
        Assert.IsNull(o.Error);
        Assert.AreEqual(0.3, (double)room.Get("lamp.brightness")!);
        Assert.AreEqual(true, (bool)room.Get("lamp.on")!);
        CollectionAssert.AreEquivalent(new[] { "lamp.brightness", "lamp.on" }, o.Changes.Select(c => c.Path).ToArray());
        Assert.IsTrue(o.Changes.All(c => c.Ms == 0 && c.DelayMs == 0));
        Assert.AreEqual(0, o.Rows.Count);

        ed.Apply(room, "lamp.brightness", JsonValue.Create(0));
        Assert.AreEqual(false, (bool)room.Get("lamp.on")!);
    }

    [TestMethod]
    public void Apply_PumpOnRunsTheHandRun_OffStops()
    {
        var (_, room, ed) = Setup();
        var o = ed.Apply(room, "pump.running", JsonValue.Create(true));
        Assert.AreEqual(3, o.Changes.Count);
        Assert.AreEqual("pump.running", o.Changes[0].Path); Assert.AreEqual(true, (bool)o.Changes[0].Value!); Assert.AreEqual(0, o.Changes[0].DelayMs);
        Assert.AreEqual("soil.moisture", o.Changes[1].Path); Assert.AreEqual(34 + 7.2, (double)o.Changes[1].Value!, 1e-9); Assert.AreEqual(2000, o.Changes[1].DelayMs);
        Assert.AreEqual("pump.running", o.Changes[2].Path); Assert.AreEqual(false, (bool)o.Changes[2].Value!); Assert.AreEqual(3000, o.Changes[2].DelayMs);

        var off = ed.Apply(room, "pump.running", JsonValue.Create(false));
        Assert.AreEqual(1, off.Changes.Count);
        Assert.AreEqual("pump.running", off.Changes[0].Path); Assert.AreEqual(false, (bool)off.Changes[0].Value!);
    }

    [TestMethod]
    public void Apply_FaultedOrBusyDeviceIsUnavailable()
    {
        foreach (var status in new[] { "fault", "busy" })
        {
            var (_, room, ed) = Setup();
            room.Set("lamp.status", JsonValue.Create(status));
            var before = room.ToWire().ToJsonString();
            var o = ed.Apply(room, "lamp.brightness", JsonValue.Create(0.5));
            Assert.AreEqual("device_unavailable", o.Error);
            Assert.AreEqual(0, o.Changes.Count);
            Assert.AreEqual(before, room.ToWire().ToJsonString());
        }
    }

    static ActionCall Call(string action, string args, string step) => new(action, (JsonObject)JsonNode.Parse(args)!, step);

    static Rule Water(double below = 30, int seconds = 6, int max = 2) =>
        new("r1", "s2", "soil.moisture", RuleOp.Below, JsonValue.Create(below), Call("run_pump", $"{{\"seconds\":{seconds}}}", "s2"), null, max);

    static Rule SoilNotify(int max = 5) =>
        new("r3", "s2", "soil.moisture", RuleOp.Below, JsonValue.Create(30), null, "soil is dry", max);

    static Rule DoorNotify() =>
        new("r2", "s3", "camera.motion", RuleOp.Becomes, JsonValue.Create(true), null, "someone at the door", 2);

    static JsonValue N(double d) => JsonValue.Create(d);

    [TestMethod]
    public void Rule_FiresWhenAnEditCrossesIntoTheZone()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(Water());
        var o = ed.Apply(room, "soil.moisture", N(25));
        Assert.IsNull(o.Error);
        var row = o.Rows.Single();
        Assert.AreEqual("rule", row.Origin);
        Assert.AreEqual("Water pump → run 6 s", row.Text);
        Assert.AreEqual("pump", row.Device);
        Assert.AreEqual("soil.moisture", o.Changes[0].Path);
        Assert.AreEqual(25, (double)o.Changes[0].Value!);
        Assert.IsTrue(o.Changes.Any(c => c.Path == "pump.running" && (bool)c.Value! && c.DelayMs == 0));
        Assert.IsTrue(o.Changes.Any(c => c.Path == "soil.moisture" && c.DelayMs == 2000));
        Assert.AreEqual(1, room.Rules[0].FiredPerDay[0]);
        Assert.AreEqual(room.Hour, room.Rules[0].LastFiredHour);
    }

    [TestMethod]
    public void AboveRule_FiresWhenAnEditCrossesIntoTheZone()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(new("r7", "s5", "thermostat.current", RuleOp.Above, N(25), Call("set_blinds", "{\"closed_percent\":100}", "s5"), null, 5));
        Assert.AreEqual(0, ed.Apply(room, "thermostat.current", N(24.5)).Rows.Count, "still under the limit");

        var o = ed.Apply(room, "thermostat.current", N(26));
        var row = o.Rows.Single();
        Assert.AreEqual("rule", row.Origin);
        Assert.AreEqual("Blinds → closed", row.Text);
        Assert.AreEqual("blinds", row.Device);
        Assert.IsTrue(o.Changes.Any(c => c.Path == "blinds.closed" && (double)c.Value! == 1));
        Assert.AreEqual(1.0, (double)room.Get("blinds.closed")!);

        Assert.AreEqual(0, ed.Apply(room, "thermostat.current", N(27)).Rows.Count, "still in the zone");
        Assert.AreEqual(0, ed.Apply(room, "thermostat.current", N(25)).Rows.Count, "at the limit is not above it");
        Assert.AreEqual(1, ed.Apply(room, "thermostat.current", N(25.5)).Rows.Count, "crossed in again");
    }

    [TestMethod]
    public void Rule_RefiresOnlyAfterLeavingTheZone()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(SoilNotify());
        var hour = room.Hour;
        Assert.AreEqual(1, ed.Apply(room, "soil.moisture", N(25)).Rows.Count);
        Assert.AreEqual(0, ed.Apply(room, "soil.moisture", N(20)).Rows.Count, "still in the zone");
        Assert.AreEqual(0, ed.Apply(room, "soil.moisture", N(60)).Rows.Count);
        var again = ed.Apply(room, "soil.moisture", N(25));
        Assert.AreEqual(1, again.Rows.Count);
        Assert.AreEqual("rule", again.Rows[0].Origin);
        Assert.AreEqual(hour, room.Hour, "no 6 h gap, no clock move");
    }

    [TestMethod]
    public void Rule_HeldBackAfterMaxPerDay()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(Water(max: 2));
        Assert.AreEqual("rule", ed.Apply(room, "soil.moisture", N(25)).Rows.Single().Origin);
        Assert.AreEqual("rule", ed.Apply(room, "soil.moisture", N(20)).Rows.Single().Origin);
        ed.Apply(room, "soil.moisture", N(60));
        var third = ed.Apply(room, "soil.moisture", N(25));
        var row = third.Rows.Single();
        Assert.AreEqual("held", row.Origin);
        Assert.AreEqual("It already ran 2 times today", row.Text);
        Assert.AreEqual("notify", row.Kind); Assert.IsTrue(row.Ok); Assert.AreEqual("pump", row.Device); Assert.AreEqual("s2", row.Step);
        CollectionAssert.AreEqual(new[] { "soil.moisture" }, third.Changes.Select(c => c.Path).ToArray());
        Assert.AreEqual(25, (double)room.Get("soil.moisture")!, "pump untouched");
        Assert.AreEqual(2, room.Rules[0].FiredPerDay[0]);
    }

    [TestMethod]
    public void Rule_HeldBack_OncePerDayAndNotifyWording()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(SoilNotify(max: 1));
        ed.Apply(room, "soil.moisture", N(25));
        ed.Apply(room, "soil.moisture", N(60));
        var row = ed.Apply(room, "soil.moisture", N(25)).Rows.Single();
        Assert.AreEqual("It already ran once today", row.Text);
        Assert.IsNull(row.Device);
    }

    [TestMethod]
    public void Becomes_FiresOnMotion_NotForOldValues()
    {
        var (_, old, ed) = Setup();
        old.Set("camera.motion", JsonValue.Create(true));
        var rule = DoorNotify(); rule.LastSeen = JsonValue.Create(true);
        old.Rules.Add(rule);
        Assert.AreEqual(0, ed.Apply(old, "lamp.brightness", N(0.5)).Rows.Count);
        Assert.AreEqual(0, ed.Apply(old, "camera.motion", JsonValue.Create(true)).Rows.Count);

        var (_, room, ed2) = Setup();
        var r2 = DoorNotify(); r2.LastSeen = JsonValue.Create(false);
        room.Rules.Add(r2);
        Assert.AreEqual(0, ed2.Apply(room, "camera.motion", JsonValue.Create(false)).Rows.Count);
        var o = ed2.Apply(room, "camera.motion", JsonValue.Create(true));
        var row = o.Rows.Single();
        Assert.AreEqual("notify", row.Kind); Assert.AreEqual("someone at the door", row.Text); Assert.AreEqual("rule", row.Origin);
        Assert.AreEqual(0, o.Changes.Count(c => c.Path != "camera.motion"));
    }

    [TestMethod]
    public void TwoRulesOnSoil_BothFireOnOneEdit()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(Water());
        room.Rules.Add(new("r4", "s3", "soil.moisture", RuleOp.Below, N(30), Call("set_blinds", "{\"closed_percent\":100}", "s3"), null, 2));
        var o = ed.Apply(room, "soil.moisture", N(25));
        Assert.AreEqual(2, o.Rows.Count);
        Assert.IsTrue(o.Rows.All(r => r.Origin == "rule"));
        Assert.AreEqual("pump", o.Rows[0].Device); Assert.AreEqual("blinds", o.Rows[1].Device);
        Assert.IsTrue(o.Changes.Any(c => c.Path == "pump.running" && (bool)c.Value!));
        Assert.IsTrue(o.Changes.Any(c => c.Path == "blinds.closed" && (double)c.Value! == 1));
    }

    [TestMethod]
    public void FireDoesNotTriggerAnotherRule()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(Water());
        var becomes = new Rule("r5", "s4", "pump.running", RuleOp.Becomes, JsonValue.Create(true), null, "pump ran", 2) { LastSeen = JsonValue.Create(false) };
        room.Rules.Add(becomes);
        var o = ed.Apply(room, "soil.moisture", N(25));
        Assert.AreEqual(1, o.Rows.Count);
        Assert.AreEqual("pump", o.Rows[0].Device);
        Assert.AreEqual(false, (bool)becomes.LastSeen!);
    }

    [TestMethod]
    public void HandPumpOn_FiresBecomesRuleOnPumpRunning()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(new Rule("r6", "s4", "pump.running", RuleOp.Becomes, JsonValue.Create(true), null, "pump ran", 2) { LastSeen = JsonValue.Create(false) });
        var o = ed.Apply(room, "pump.running", JsonValue.Create(true));
        var row = o.Rows.Single();
        Assert.AreEqual("notify", row.Kind); Assert.AreEqual("rule", row.Origin); Assert.AreEqual("pump ran", row.Text);
        Assert.AreEqual(0, ed.Apply(room, "pump.running", JsonValue.Create(false)).Rows.Count);
    }

    [TestMethod]
    public void Edit_ToAnotherDevice_FiresNothing()
    {
        var (_, room, ed) = Setup();
        room.Rules.Add(Water());
        var o = ed.Apply(room, "lamp.brightness", N(0.5));
        Assert.AreEqual(0, o.Rows.Count);
        Assert.IsFalse(o.Changes.Any(c => c.Path.StartsWith("pump")));
    }

    [TestMethod]
    public void Rule_NotFiredForAValueAlreadyInTheZone_OrFailedSensor()
    {
        var (_, room, ed) = Setup();
        room.Set("soil.moisture", N(20));
        room.Rules.Add(Water());
        Assert.AreEqual(0, ed.Apply(room, "lamp.brightness", N(0.5)).Rows.Count);

        var (_, room2, ed2) = Setup();
        room2.Rules.Add(Water());
        room2.Failed.Add("soil");
        var o = ed2.Apply(room2, "soil.moisture", N(25));
        Assert.AreEqual(0, o.Rows.Count, "a hand edit never produces a fault");
    }

    [TestMethod]
    public void Apply_BlockedHandPump_IsUnavailableAndRunsNoRules()
    {
        var (_, room, ed) = Setup();
        room.Failed.Add("pump");
        room.Rules.Add(DoorNotify());
        var before = room.ToWire().ToJsonString();
        var o = ed.Apply(room, "pump.running", JsonValue.Create(true));
        Assert.AreEqual("device_unavailable", o.Error);
        Assert.AreEqual(0, o.Changes.Count); Assert.AreEqual(0, o.Rows.Count);
        Assert.AreEqual(before, room.ToWire().ToJsonString());
    }
}
