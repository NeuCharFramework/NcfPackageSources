/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CatalogBehaviorTests.cs
    文件功能描述：测试自定义房间的设备目录行为：动作效果、读数、标签与漂移


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class CatalogBehaviorTests
{
    static RoomDevice D(string type, string id) => new(type, id);
    static JsonObject J(string json) => JsonNode.Parse(json)!.AsObject();

    /// <summary>A room of the shipped catalog: every device is its type's first, so the id is the type.</summary>
    static (SceneManifest M, Simulator Sim, RoomState Room) Home(params string[] types)
    {
        var m = CustomRoom.Build(types.Select(t => D(t, t)).ToList());
        return (m, new Simulator(m, ManifestLoader.Behavior(m)), RoomState.PostSetup(m));
    }

    static ActionOutcome Run((SceneManifest M, Simulator Sim, RoomState Room) r, string action, string args) =>
        r.Sim.Execute(new ActionCall(action, J(args), null), r.Room);

    /// <summary>A hand-built room from a test-only catalog, one device per type with the type as its id.</summary>
    static (SceneManifest M, CatalogBehavior B, Simulator Sim, RoomState Room) Test(string catalogJson, params string[] types)
    {
        var cat = Catalog.Parse(catalogJson);
        var devices = types.Select(t =>
        {
            var ct = cat.Find(t)!;
            var initial = (JsonObject)ct.Initial.DeepClone();
            initial["status"] = "online";
            var actions = ct.Actions.Select(a => new ActionSpec($"{a.Verb}_{t}", t, a.Description, a.Kind,
                (JsonObject)a.Parameters.DeepClone(), a.SafeWhen is null ? null : (JsonObject)a.SafeWhen.DeepClone()) { Verb = a.Verb }).ToList();
            return new DeviceSpec(t, ct.Name, ct.Ref, initial, actions) { Type = t };
        }).ToList();
        var m = new SceneManifest(CustomRoom.SceneId, "test", new DateTime(2026, 11, 13), 16.5, new List<string>(), devices);
        var b = new CatalogBehavior(m, cat);
        return (m, b, new Simulator(m, b), RoomState.PostSetup(m));
    }

    const string TestCatalog = """
    {
      "version": 1, "maxDevices": 20, "categories": ["Lights"], "suggestions": [],
      "types": [
        { "type": "kettle", "name": "Kettle", "ref": "the kettle", "category": "Lights", "mount": "floor", "new": false,
          "initial": { "on": false, "watts": 0 }, "faultText": "x", "edit": [], "drift": [],
          "actions": [ { "verb": "set", "kind": "Write", "outcome": "confirmed", "description": "d",
            "parameters": { "type": "object", "properties": { "on": { "type": "boolean" } }, "required": ["on"], "additionalProperties": false },
            "safeWhen": { "on": false },
            "effects": [ { "set": "on", "arg": "on", "ms": 500 },
                         { "set": "watts", "value": 2200, "if": { "on": true }, "ms": 700 },
                         { "set": "watts", "value": 0, "if": { "on": false }, "ms": 700 } ],
            "label": [ { "text": "{name} {on}" } ] } ] },
        { "type": "printer", "name": "Printer", "ref": "the printer", "category": "Lights", "mount": "floor", "new": false,
          "initial": { "printed": 0 }, "faultText": "x", "edit": [], "drift": [],
          "actions": [ { "verb": "print", "kind": "Write", "outcome": "confirmed", "description": "d",
            "parameters": { "type": "object", "properties": { "copies": { "type": "integer", "minimum": 1, "maximum": 20 } }, "required": ["copies"], "additionalProperties": false },
            "effects": [ { "add": "printed", "arg": "copies", "ms": 1500 } ],
            "label": [ { "text": "{name} x{copies}" } ] } ] },
        { "type": "dock", "name": "Dock", "ref": "the dock", "category": "Lights", "mount": "wall", "new": false,
          "initial": { "open": false, "parcels": 0 }, "faultText": "x",
          "edit": [],
          "drift": [ { "field": "parcels", "perHour": 30, "while": { "open": true }, "max": 500 } ],
          "actions": [ { "verb": "set", "kind": "Write", "outcome": "confirmed", "description": "d",
            "parameters": { "type": "object", "properties": { "open": { "type": "boolean" } }, "required": ["open"], "additionalProperties": false },
            "effects": [ { "set": "open", "arg": "open", "ms": 100 } ],
            "label": [ { "text": "{name}" } ] } ] }
      ]
    }
    """;

    [TestMethod]
    public void Set_ScalesArgAndTweens()
    {
        var r = Home("floor_lamp");
        var o = Run(r, "set_floor_lamp", """{"brightness":30}""");
        Assert.AreEqual(Outcome.Confirmed, o.Outcome);
        Assert.AreEqual("confirmed", o.Text);
        Assert.AreEqual(1, o.Changes.Count);
        Assert.AreEqual("floor_lamp.brightness", o.Changes[0].Path);
        Assert.AreEqual(0.3, (double)o.Changes[0].Value!, 1e-9);
        Assert.AreEqual(1200, o.Changes[0].Ms);
        Assert.AreEqual(1200, o.DurationMs);
        Assert.AreEqual(0.3, (double)r.Room.Get("floor_lamp.brightness")!, 1e-9);
        var spec = r.M.Action("set_floor_lamp")!;
        Assert.AreEqual("Floor lamp → 30%", r.Sim.Behavior.Label(spec, J("""{"brightness":30}""")));
        Assert.AreEqual(true, (bool)o.ModelResult["confirmed"]!);
    }

    [TestMethod]
    public void Set_ScaledValuesCarryNoFloatNoise()
    {
        var r = Home("floor_lamp");
        var o = Run(r, "set_floor_lamp", """{"brightness":70}""");
        Assert.AreEqual(0.7, (double)o.Changes[0].Value!, 0.0); // 70 * 0.01 is 0.7000000000000001 unrounded
    }

    [TestMethod]
    public void Set_SafeWhenIsMadeSafe()
    {
        var r = Home("floor_lamp");
        var spec = r.M.Action("set_floor_lamp")!;
        var o = Run(r, "set_floor_lamp", """{"brightness":0}""");
        Assert.AreEqual(Outcome.MadeSafe, o.Outcome);
        Assert.AreEqual("made safe", o.Text);
        Assert.AreEqual("Floor lamp → off", r.Sim.Behavior.Label(spec, J("""{"brightness":0}""")));
        Assert.AreEqual("Floor lamp → full", r.Sim.Behavior.Label(spec, J("""{"brightness":100}""")));
        Assert.AreEqual(Outcome.Confirmed, Run(r, "set_floor_lamp", """{"brightness":100}""").Outcome);
    }

    [TestMethod]
    public void TimedRun_StopsAfterArg()
    {
        var r = Home("water_pump");
        var o = Run(r, "run_water_pump", """{"seconds":6}""");
        Assert.AreEqual(Outcome.Commanded, o.Outcome);
        Assert.AreEqual("commanded", o.Text);
        Assert.AreEqual(false, (bool)o.ModelResult["confirmed"]!);
        Assert.AreEqual(2, o.Changes.Count);
        Assert.AreEqual(true, (bool)o.Changes[0].Value!);
        Assert.AreEqual(0, o.Changes[0].DelayMs);
        Assert.AreEqual(false, (bool)o.Changes[1].Value!);
        Assert.AreEqual(3000, o.Changes[1].DelayMs);
        Assert.AreEqual(3000, o.DurationMs);
        Assert.AreEqual(2500, Run(r, "run_water_pump", """{"seconds":1}""").Changes[1].DelayMs);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "stop_water_pump", "{}").Outcome);
    }

    [TestMethod]
    public void Read_ReturnsReadingAndText()
    {
        var r = Home("soil_sensor");
        var o = Run(r, "read_soil_sensor", "{}");
        Assert.AreEqual(Outcome.Reading, o.Outcome);
        Assert.AreEqual("moisture 34%", o.Text);
        Assert.AreEqual(34, (double)o.ModelResult["reading"]!["moisture"]!, 1e-9);
        Assert.AreEqual(0, o.Changes.Count);
        Assert.AreEqual(600, o.DurationMs);
        r.Room.Set("soil_sensor.moisture", JsonValue.Create(100));
        var full = Run(r, "read_soil_sensor", "{}");
        Assert.AreEqual("moisture 100 percent", full.Text);
        Assert.AreEqual(0, CopyGuard.Find(full.Text).Count);
    }

    [TestMethod]
    public void If_AppliesOnlyWhenArgsMatch()
    {
        var r = Test(TestCatalog, "kettle");
        var on = r.Sim.Execute(new ActionCall("set_kettle", J("""{"on":true}"""), null), r.Room);
        Assert.AreEqual(Outcome.Confirmed, on.Outcome);
        Assert.AreEqual(2, on.Changes.Count);
        Assert.AreEqual(2200, (double)r.Room.Get("kettle.watts")!, 1e-9);
        Assert.AreEqual(700, on.DurationMs);
        var off = r.Sim.Execute(new ActionCall("set_kettle", J("""{"on":false}"""), null), r.Room);
        Assert.AreEqual(Outcome.MadeSafe, off.Outcome);
        Assert.AreEqual(2, off.Changes.Count);
        Assert.AreEqual(0, (double)r.Room.Get("kettle.watts")!, 1e-9);
        Assert.AreEqual("Kettle true", r.B.Label(r.M.Action("set_kettle")!, J("""{"on":true}""")));
    }

    [TestMethod]
    public void Add_AddsToCurrent()
    {
        var r = Test(TestCatalog, "printer");
        r.Sim.Execute(new ActionCall("print_printer", J("""{"copies":3}"""), null), r.Room);
        var o = r.Sim.Execute(new ActionCall("print_printer", J("""{"copies":3}"""), null), r.Room);
        Assert.AreEqual(6, (double)r.Room.Get("printer.printed")!, 1e-9);
        Assert.AreEqual(1500, o.DurationMs);
        Assert.AreEqual("Printer x3", r.B.Label(r.M.Action("print_printer")!, J("""{"copies":3}""")));
    }

    static void ApplyAll(RoomState room, IReadOnlyList<SimHappening> hs)
    {
        foreach (var h in hs) foreach (var c in h.Changes) room.Set(c.Path, c.Value);
    }

    [TestMethod]
    public void Drift_TowardWhileAndClamp()
    {
        var r = Home("thermostat", "soil_sensor");
        var b = r.Sim.Behavior;
        r.Room.Set("thermostat.target", JsonValue.Create(18));
        r.Room.Set("thermostat.current", JsonValue.Create(21));
        ApplyAll(r.Room, b.Advance(r.Room, 0, 2));
        Assert.AreEqual(20, (double)r.Room.Get("thermostat.current")!, 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 2, 12));
        Assert.AreEqual(18, (double)r.Room.Get("thermostat.current")!, 1e-9);

        r.Room.Set("soil_sensor.moisture", JsonValue.Create(1));
        ApplyAll(r.Room, b.Advance(r.Room, 0, 24));
        Assert.AreEqual(0, (double)r.Room.Get("soil_sensor.moisture")!, 1e-9);
        Assert.IsNull(b.NextHappening(r.Room));

        // soil drops 2 per day while moisture is fed from 34
        r.Room.Set("soil_sensor.moisture", JsonValue.Create(34));
        ApplyAll(r.Room, b.Advance(r.Room, 0, 24));
        Assert.AreEqual(32, (double)r.Room.Get("soil_sensor.moisture")!, 0.01);

        var w = Test(TestCatalog, "dock");
        ApplyAll(w.Room, w.B.Advance(w.Room, 0, 2));
        Assert.AreEqual(0, (double)w.Room.Get("dock.parcels")!, 1e-9);
        w.Room.Set("dock.open", JsonValue.Create(true));
        ApplyAll(w.Room, w.B.Advance(w.Room, 2, 4));
        Assert.AreEqual(60, (double)w.Room.Get("dock.parcels")!, 1e-9);
        ApplyAll(w.Room, w.B.Advance(w.Room, 4, 40));
        Assert.AreEqual(500, (double)w.Room.Get("dock.parcels")!, 1e-9);
    }

    [TestMethod]
    public void Advance_ReturnsOneHappeningWithoutRow()
    {
        var r = Home("thermostat");
        var hs = r.Sim.Behavior.Advance(r.Room, 0, 1);
        Assert.AreEqual(1, hs.Count);
        Assert.IsNull(hs[0].Row);
    }

    [TestMethod]
    public void Failed_DeviceDoesNothing()
    {
        var r = Home("floor_lamp");
        r.Room.Failed.Add("floor_lamp");
        var o = Run(r, "set_floor_lamp", """{"brightness":50}""");
        Assert.AreEqual(Outcome.Failed, o.Outcome);
        Assert.AreEqual("not responding", o.Text);
        Assert.AreEqual(0, (double)r.Room.Get("floor_lamp.brightness")!, 1e-9);
    }

    [TestMethod]
    public void Behavior_FactoryPicksByManifest()
    {
        Assert.IsInstanceOfType<CatalogBehavior>(ManifestLoader.Behavior(CustomRoom.Build(new[] { D("floor_lamp", "floor_lamp") })));
        Assert.IsInstanceOfType<HomeBehavior>(ManifestLoader.Behavior(ManifestLoader.Load("home")));
    }

    static JsonNode Value(JsonObject s, double v) =>
        (string)s["type"]! == "integer" ? JsonValue.Create((long)Math.Round(v)) : JsonValue.Create(v);

    /// <summary>
    /// A value for each argument at its edges: every min and max, every enum value, both bools. Optional arguments
    /// are tried absent and present.
    /// </summary>
    static IEnumerable<JsonObject> BoundaryArgs(ActionSpec spec)
    {
        var props = spec.Parameters["properties"]!.AsObject();
        var required = (spec.Parameters["required"] as JsonArray)?.Select(x => (string)x!).ToHashSet() ?? new HashSet<string>();
        if (props.Count == 0) { yield return new JsonObject(); yield break; }

        static IEnumerable<JsonNode> Values(JsonObject s)
        {
            if (s["enum"] is JsonArray e) { foreach (var x in e) yield return x!.DeepClone(); yield break; }
            if ((string)s["type"]! == "boolean") { yield return JsonValue.Create(true); yield return JsonValue.Create(false); yield break; }
            var min = (double)s["minimum"]!; var max = (double)s["maximum"]!;
            foreach (var v in new[] { min, max, (min + max) / 2 }) yield return Value(s, v);
        }

        JsonObject Base(string? skip)
        {
            var args = new JsonObject();
            foreach (var (n, s) in props)
                if (n != skip && required.Contains(n)) args[n] = Values(s!.AsObject()).First();
            return args;
        }

        yield return Base(null);
        var all = Base(null);
        foreach (var (n, s) in props) if (!all.ContainsKey(n)) all[n] = Values(s!.AsObject()).First();
        yield return all;
        foreach (var (name, schema) in props)
            foreach (var v in Values(schema!.AsObject()))
            {
                var args = (JsonObject)all.DeepClone();
                args[name] = v.DeepClone();
                yield return args;
            }
    }

    [TestMethod]
    public void ElderAndCreator_KettleOptionalArgsAndStrings()
    {
        var r = Home("kettle_plug", "led_strip", "key_light", "capture_card");
        var on = Run(r, "set_kettle_plug", """{"on":true}""");
        Assert.AreEqual(Outcome.Confirmed, on.Outcome);
        Assert.AreEqual(2200, (double)r.Room.Get("kettle_plug.watts")!, 1e-9);
        var off = Run(r, "set_kettle_plug", """{"on":false}""");
        Assert.AreEqual(Outcome.MadeSafe, off.Outcome);
        Assert.AreEqual(0, (double)r.Room.Get("kettle_plug.watts")!, 1e-9);

        // An absent optional argument leaves its field alone; a present one sets it.
        Run(r, "set_led_strip", """{"brightness":40}""");
        Assert.AreEqual("blue", (string)r.Room.Get("led_strip.color")!);
        Assert.AreEqual(0.4, (double)r.Room.Get("led_strip.brightness")!, 1e-9);
        Run(r, "set_led_strip", """{"brightness":40,"color":"red"}""");
        Assert.AreEqual("red", (string)r.Room.Get("led_strip.color")!);
        Run(r, "set_key_light", """{"brightness":80}""");
        Assert.AreEqual(5600, (double)r.Room.Get("key_light.kelvin")!, 1e-9);
        Run(r, "set_key_light", """{"brightness":80,"kelvin":3200}""");
        Assert.AreEqual(3200, (double)r.Room.Get("key_light.kelvin")!, 1e-9);

        Run(r, "set_capture_card", """{"input":"USB"}""");
        Assert.AreEqual("USB", (string)r.Room.Get("capture_card.input")!);
        var read = Run(r, "read_capture_card", "{}");
        Assert.AreEqual(Outcome.Reading, read.Outcome);
    }

    [TestMethod]
    public void LabAndWarehouse_CartConveyorScannerDock()
    {
        // Cart: it starts driving at once, arrives (and stops) 4 s later; stop is a Safe.
        var r = Home("cart", "conveyor", "scanner_arch", "loading_dock", "laser");
        var send = Run(r, "send_cart", """{"to":"charger"}""");
        Assert.AreEqual(Outcome.Confirmed, send.Outcome);
        Assert.AreEqual(4000, send.DurationMs);
        Assert.AreEqual(3, send.Changes.Count);
        Assert.AreEqual(true, (bool)send.Changes[0].Value!);
        Assert.AreEqual(0, send.Changes[0].DelayMs);
        Assert.AreEqual("cart.at", send.Changes[1].Path);
        Assert.AreEqual("charger", (string)send.Changes[1].Value!);
        Assert.AreEqual(4000, send.Changes[1].DelayMs);
        Assert.AreEqual(false, (bool)send.Changes[2].Value!);
        Assert.AreEqual(4000, send.Changes[2].DelayMs);
        Assert.AreEqual("Cart → charger", r.Sim.Behavior.Label(r.M.Action("send_cart")!, J("""{"to":"charger"}""")));
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "stop_cart", "{}").Outcome);
        Assert.AreEqual(false, (bool)r.Room.Get("cart.driving")!);

        // Conveyor: 40 percent is 0.2 m/s; 0 is made safe; stop is a Safe; full has its own label.
        var spec = r.M.Action("set_conveyor")!;
        var go = Run(r, "set_conveyor", """{"speed_percent":40}""");
        Assert.AreEqual(Outcome.Confirmed, go.Outcome);
        Assert.AreEqual(0.2, (double)r.Room.Get("conveyor.speed")!, 1e-9);
        Assert.AreEqual(1500, go.DurationMs);
        Assert.AreEqual("Conveyor → 40% speed", r.Sim.Behavior.Label(spec, J("""{"speed_percent":40}""")));
        Assert.AreEqual("Conveyor → full speed", r.Sim.Behavior.Label(spec, J("""{"speed_percent":100}""")));
        Assert.AreEqual(0.5, (double)Run(r, "set_conveyor", """{"speed_percent":100}""").Changes[0].Value!, 1e-9);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_conveyor", """{"speed_percent":0}""").Outcome);
        Run(r, "set_conveyor", """{"speed_percent":60}""");
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "stop_conveyor", "{}").Outcome);
        Assert.AreEqual(0, (double)r.Room.Get("conveyor.speed")!, 1e-9);

        // Scanner arch: the count only grows while armed; the reading reports it.
        var b = r.Sim.Behavior;
        ApplyAll(r.Room, b.Advance(r.Room, 0, 2));
        Assert.AreEqual(0, (double)r.Room.Get("scanner_arch.scanned")!, 1e-9);
        Run(r, "set_scanner_arch", """{"armed":true}""");
        ApplyAll(r.Room, b.Advance(r.Room, 2, 4));
        Assert.AreEqual(240, (double)r.Room.Get("scanner_arch.scanned")!, 1e-9);
        var read = Run(r, "read_scanner_arch", "{}");
        Assert.AreEqual("scanned 240", read.Text);
        Assert.AreEqual(240, (double)read.ModelResult["reading"]!["scanned"]!, 1e-9);

        // Loading dock: parcels arrive only while the door is open, up to 500.
        ApplyAll(r.Room, b.Advance(r.Room, 4, 6));
        Assert.AreEqual(0, (double)r.Room.Get("loading_dock.parcels")!, 1e-9);
        Assert.AreEqual(3000, Run(r, "set_loading_dock", """{"open":true}""").DurationMs);
        ApplyAll(r.Room, b.Advance(r.Room, 6, 8));
        Assert.AreEqual(60, (double)r.Room.Get("loading_dock.parcels")!, 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 8, 100));
        Assert.AreEqual(500, (double)r.Room.Get("loading_dock.parcels")!, 1e-9);

        // Laser: closing the shutter is made safe whatever the power; an absent mW leaves it alone.
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_laser", """{"open":false}""").Outcome);
        Assert.AreEqual(5, (double)r.Room.Get("laser.mW")!, 1e-9);
        Assert.AreEqual(Outcome.Confirmed, Run(r, "set_laser", """{"open":true,"mW":12.5}""").Outcome);
        Assert.AreEqual(12.5, (double)r.Room.Get("laser.mW")!, 1e-9);
        Assert.AreEqual("Laser & shutter → shutter open", r.Sim.Behavior.Label(r.M.Action("set_laser")!, J("""{"open":true,"mW":12.5}""")));
    }

    [TestMethod]
    public void HomeAndLiving_DriftsOptionalsAndPlug()
    {
        var r = Home("robot_vacuum", "air_conditioner", "space_heater", "humidifier", "air_purifier", "smart_plug", "tv", "colour_bulb", "ceiling_fan");
        var b = r.Sim.Behavior;
        double N(string path) => (double)r.Room.Get(path)!;

        // Robot vacuum: it charges while docked (capped at 100) and drains while cleaning (floored at 0).
        r.Room.Set("robot_vacuum.battery", 40);
        ApplyAll(r.Room, b.Advance(r.Room, 0, 1));
        Assert.AreEqual(90, N("robot_vacuum.battery"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 1, 5));
        Assert.AreEqual(100, N("robot_vacuum.battery"), 1e-9);
        Assert.AreEqual(Outcome.Confirmed, Run(r, "set_robot_vacuum", """{"cleaning":true}""").Outcome);
        ApplyAll(r.Room, b.Advance(r.Room, 5, 6));
        Assert.AreEqual(70, N("robot_vacuum.battery"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 6, 20));
        Assert.AreEqual(0, N("robot_vacuum.battery"), 1e-9);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_robot_vacuum", """{"cleaning":false}""").Outcome);

        // Air conditioner: the room drifts toward 27 while off, toward its target while on; an absent celsius leaves the target.
        r.Room.Set("air_conditioner.room", 25);
        ApplyAll(r.Room, b.Advance(r.Room, 0, 1));
        Assert.AreEqual(25.3, N("air_conditioner.room"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 1, 10));
        Assert.AreEqual(27, N("air_conditioner.room"), 1e-9);
        Assert.AreEqual(Outcome.Confirmed, Run(r, "set_air_conditioner", """{"on":true}""").Outcome);
        Assert.AreEqual(24, N("air_conditioner.target"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 10, 12));
        Assert.AreEqual(25, N("air_conditioner.room"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 12, 40));
        Assert.AreEqual(24, N("air_conditioner.room"), 1e-9);
        Run(r, "set_air_conditioner", """{"on":true,"celsius":20.5}""");
        Assert.AreEqual(20.5, N("air_conditioner.target"), 1e-9);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_air_conditioner", """{"on":false}""").Outcome);
        Assert.AreEqual("Air conditioner → off", b.Label(r.M.Action("set_air_conditioner")!, J("""{"on":false,"celsius":20}""")));
        Assert.AreEqual("Air conditioner → on", b.Label(r.M.Action("set_air_conditioner")!, J("""{"on":true}""")));

        // Heater, humidifier and purifier: each moves toward a different number depending on the switch.
        Run(r, "set_space_heater", """{"on":true}""");
        ApplyAll(r.Room, b.Advance(r.Room, 0, 3));
        Assert.AreEqual(20, N("space_heater.room"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 3, 30));
        Assert.AreEqual(24, N("space_heater.room"), 1e-9);
        Run(r, "set_space_heater", """{"on":false}""");
        ApplyAll(r.Room, b.Advance(r.Room, 30, 40));
        Assert.AreEqual(19, N("space_heater.room"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 40, 80));
        Assert.AreEqual(17, N("space_heater.room"), 1e-9);

        Run(r, "set_humidifier", """{"on":true}""");
        ApplyAll(r.Room, b.Advance(r.Room, 0, 2));
        Assert.AreEqual(43, N("humidifier.humidity"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 2, 50));
        Assert.AreEqual(55, N("humidifier.humidity"), 1e-9);
        Run(r, "set_humidifier", """{"on":false}""");
        ApplyAll(r.Room, b.Advance(r.Room, 50, 52));
        Assert.AreEqual(51, N("humidifier.humidity"), 1e-9);

        ApplyAll(r.Room, b.Advance(r.Room, 0, 3));
        // Off, it only moves toward 18, where it already is.
        Assert.AreEqual(18, N("air_purifier.pm25"), 1e-9);
        Run(r, "set_air_purifier", """{"on":true}""");
        ApplyAll(r.Room, b.Advance(r.Room, 3, 4));
        Assert.AreEqual(8, N("air_purifier.pm25"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 4, 9));
        Assert.AreEqual(3, N("air_purifier.pm25"), 1e-9);

        // Smart plug: 60 W on, 0 off (made safe).
        var plugOn = Run(r, "set_smart_plug", """{"on":true}""");
        Assert.AreEqual(Outcome.Confirmed, plugOn.Outcome);
        Assert.AreEqual(60, N("smart_plug.watts"), 1e-9);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_smart_plug", """{"on":false}""").Outcome);
        Assert.AreEqual(0, N("smart_plug.watts"), 1e-9);

        // TV volume and the bulb's colour are optional: absent leaves the field alone.
        Run(r, "set_tv", """{"on":true}""");
        Assert.AreEqual(20, N("tv.volume"), 1e-9);
        Run(r, "set_tv", """{"on":true,"volume":55}""");
        Assert.AreEqual(55, N("tv.volume"), 1e-9);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_tv", """{"on":false}""").Outcome);
        Assert.AreEqual("warm", (string)r.Room.Get("colour_bulb.color")!);
        Run(r, "set_colour_bulb", """{"brightness":50}""");
        Assert.AreEqual("warm", (string)r.Room.Get("colour_bulb.color")!);
        Assert.AreEqual(0.5, N("colour_bulb.brightness"), 1e-9);
        Run(r, "set_colour_bulb", """{"brightness":50,"color":"green"}""");
        Assert.AreEqual("green", (string)r.Room.Get("colour_bulb.color")!);
        Assert.AreEqual(1500, Run(r, "set_ceiling_fan", """{"speed":2}""").DurationMs);
        Assert.AreEqual("Ceiling fan → speed 2", b.Label(r.M.Action("set_ceiling_fan")!, J("""{"speed":2}""")));
    }

    [TestMethod]
    public void SecurityAndAccess_LockSirenGarageDoorbell()
    {
        var r = Home("smart_lock", "siren", "garage_door", "video_doorbell", "door_window_sensor", "smoke_co_alarm");
        var b = r.Sim.Behavior;

        // Lock: locked:true is the safe state; unlocking is a plain confirmed write that takes 1500 ms.
        var unlock = Run(r, "set_smart_lock", """{"locked":false}""");
        Assert.AreEqual(Outcome.Confirmed, unlock.Outcome);
        Assert.AreEqual(1500, unlock.DurationMs);
        Assert.AreEqual(false, (bool)r.Room.Get("smart_lock.locked")!);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_smart_lock", """{"locked":true}""").Outcome);
        Assert.AreEqual(true, (bool)r.Room.Get("smart_lock.locked")!);
        Assert.AreEqual("Smart lock → unlocked", b.Label(r.M.Action("set_smart_lock")!, J("""{"locked":false}""")));

        // Siren: stopping it is the safe outcome.
        Assert.AreEqual(Outcome.Confirmed, Run(r, "set_siren", """{"sounding":true}""").Outcome);
        Assert.AreEqual(true, (bool)r.Room.Get("siren.sounding")!);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_siren", """{"sounding":false}""").Outcome);
        Assert.AreEqual(false, (bool)r.Room.Get("siren.sounding")!);

        // Garage door: slow, 8 s.
        var open = Run(r, "set_garage_door", """{"open":true}""");
        Assert.AreEqual(8000, open.DurationMs);
        Assert.AreEqual(Outcome.Confirmed, open.Outcome);
        Assert.AreEqual(true, (bool)r.Room.Get("garage_door.open")!);

        // Doorbell: one read returns both fields.
        r.Room.Set("video_doorbell.ringing", true);
        var read = Run(r, "read_video_doorbell", "{}");
        Assert.AreEqual(Outcome.Reading, read.Outcome);
        Assert.AreEqual("ringing true, motion false", read.Text);
        Assert.IsTrue((bool)read.ModelResult["reading"]!["ringing"]!);
        Assert.IsFalse((bool)read.ModelResult["reading"]!["motion"]!);
    }

    [TestMethod]
    public void SensorsEnergyOutdoorWork_SprinklerPrinterLabelMeterChargerSensor()
    {
        var r = Home("sprinkler_valve", "printer_3d", "label_printer", "energy_meter", "ev_charger", "temp_humidity_sensor", "robot_arm");
        var b = r.Sim.Behavior;
        double N(string path) => (double)r.Room.Get(path)!;

        // Sprinkler: run is commanded, and the valve closes after minutes x 500 ms (at least 2500); close is made safe.
        var run = Run(r, "run_sprinkler_valve", """{"minutes":10}""");
        Assert.AreEqual(Outcome.Commanded, run.Outcome);
        Assert.AreEqual(2, run.Changes.Count);
        Assert.AreEqual(true, (bool)run.Changes[0].Value!);
        Assert.AreEqual(false, (bool)run.Changes[1].Value!);
        Assert.AreEqual(5000, run.Changes[1].DelayMs);
        Assert.AreEqual(5000, run.DurationMs);
        Assert.AreEqual(2500, Run(r, "run_sprinkler_valve", """{"minutes":2}""").Changes[1].DelayMs);
        Assert.AreEqual(30000, Run(r, "run_sprinkler_valve", """{"minutes":60}""").Changes[1].DelayMs);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "close_sprinkler_valve", "{}").Outcome);
        Assert.AreEqual(false, (bool)r.Room.Get("sprinkler_valve.open")!);
        Assert.AreEqual("Sprinkler valve → run 10 min", b.Label(r.M.Action("run_sprinkler_valve")!, J("""{"minutes":10}""")));

        // 3D printer: start takes no arguments and resets progress; progress grows only while printing, up to 100; cancel is safe.
        ApplyAll(r.Room, b.Advance(r.Room, 0, 2));
        Assert.AreEqual(0, N("printer_3d.progress"), 1e-9);
        var start = Run(r, "start_printer_3d", "{}");
        Assert.AreEqual(Outcome.Confirmed, start.Outcome);
        Assert.AreEqual(true, (bool)r.Room.Get("printer_3d.printing")!);
        ApplyAll(r.Room, b.Advance(r.Room, 2, 6));
        Assert.AreEqual(100, N("printer_3d.progress"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 6, 10));
        Assert.AreEqual(100, N("printer_3d.progress"), 1e-9);
        Run(r, "start_printer_3d", "{}");
        Assert.AreEqual(0, N("printer_3d.progress"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 10, 12));
        Assert.AreEqual(50, N("printer_3d.progress"), 1e-9);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "cancel_printer_3d", "{}").Outcome);
        Assert.AreEqual(false, (bool)r.Room.Get("printer_3d.printing")!);
        ApplyAll(r.Room, b.Advance(r.Room, 12, 20));
        Assert.AreEqual(50, N("printer_3d.progress"), 1e-9);
        Assert.AreEqual("3D printer → start", b.Label(r.M.Action("start_printer_3d")!, J("{}")));

        // Label printer: each print adds its copies to the total.
        var p1 = Run(r, "print_label_printer", """{"copies":3}""");
        Assert.AreEqual(1500, p1.DurationMs);
        Run(r, "print_label_printer", """{"copies":4}""");
        Assert.AreEqual(7, N("label_printer.printed"), 1e-9);
        Assert.AreEqual("Label printer → print 4", b.Label(r.M.Action("print_label_printer")!, J("""{"copies":4}""")));

        // Energy meter: one read gives watts and kWh with their units; the energy used today grows 0.42 an hour.
        var e = Home("energy_meter");
        var read = Run(e, "read_energy_meter", "{}");
        Assert.AreEqual("watts 420 W, kwh today 3.2 kWh", read.Text);
        Assert.AreEqual(420, (double)read.ModelResult["reading"]!["watts"]!, 1e-9);
        ApplyAll(e.Room, e.Sim.Behavior.Advance(e.Room, 0, 2));
        Assert.AreEqual(4.04, (double)e.Room.Get("energy_meter.kwh_today")!, 1e-9);
        Assert.AreEqual("watts 420 W, kwh today 4 kWh", Run(e, "read_energy_meter", "{}").Text);

        // EV charger: the battery rises only while charging, up to 100; stopping is made safe.
        ApplyAll(r.Room, b.Advance(r.Room, 22, 24));
        Assert.AreEqual(40, N("ev_charger.battery"), 1e-9);
        Assert.AreEqual(Outcome.Confirmed, Run(r, "set_ev_charger", """{"charging":true}""").Outcome);
        ApplyAll(r.Room, b.Advance(r.Room, 24, 27));
        Assert.AreEqual(64, N("ev_charger.battery"), 1e-9);
        ApplyAll(r.Room, b.Advance(r.Room, 27, 60));
        Assert.AreEqual(100, N("ev_charger.battery"), 1e-9);
        Assert.AreEqual(Outcome.MadeSafe, Run(r, "set_ev_charger", """{"charging":false}""").Outcome);
        Assert.AreEqual("EV charger → stopped", b.Label(r.M.Action("set_ev_charger")!, J("""{"charging":false}""")));

        // Temperature and humidity: a small negative reading never shows as "-0".
        Assert.AreEqual("temperature 21.5°C, humidity 45%", Run(r, "read_temp_humidity_sensor", "{}").Text);
        r.Room.Set("temp_humidity_sensor.temperature", JsonValue.Create(-0.04));
        var cold = Run(r, "read_temp_humidity_sensor", "{}");
        Assert.AreEqual("temperature 0°C, humidity 45%", cold.Text);
        Assert.IsFalse(cold.ModelResult["reading"]!.ToJsonString().Contains("-0"));
        r.Room.Set("temp_humidity_sensor.temperature", JsonValue.Create(-3.26));
        Assert.AreEqual("temperature -3.3°C, humidity 45%", Run(r, "read_temp_humidity_sensor", "{}").Text);

        // Robot arm: a pose move takes 2500 ms; the gripper is a plain confirmed write.
        Assert.AreEqual(2500, Run(r, "move_robot_arm", """{"pose":"pick"}""").DurationMs);
        Assert.AreEqual("pick", (string)r.Room.Get("robot_arm.pose")!);
        Assert.AreEqual(Outcome.Confirmed, Run(r, "grip_robot_arm", """{"hold":true}""").Outcome);
        Assert.AreEqual("Robot arm → release", b.Label(r.M.Action("grip_robot_arm")!, J("""{"hold":false}""")));
    }

    [TestMethod]
    public void Texts_PassGuard()
    {
        var cat = Catalog.Load();
        var cases = 0;
        foreach (var t in cat.Types)
        {
            var m = CustomRoom.Build(new[] { D(t.Type, t.Type) });
            var b = ManifestLoader.Behavior(m);
            foreach (var spec in m.Devices.SelectMany(d => d.Actions))
                foreach (var args in BoundaryArgs(spec))
                {
                    var label = b.Label(spec, args);
                    Assert.AreEqual(0, CopyGuard.Find(label).Count, $"{t.Type}: label '{label}' ({spec.Name} {args.ToJsonString()})");
                    Assert.IsFalse(label.Contains('{') || label.Contains('}'), $"{t.Type}: label '{label}' ({spec.Name} {args.ToJsonString()})");
                    Assert.IsFalse(label.Contains("  ") || label.TrimEnd().EndsWith('→'), $"{t.Type}: label '{label}' has a gap ({spec.Name} {args.ToJsonString()})");
                    var text = b.Apply(spec, args, RoomState.PostSetup(m)).Text;
                    Assert.AreEqual(0, CopyGuard.Find(text).Count, $"{t.Type}: text '{text}' ({spec.Name} {args.ToJsonString()})");
                    cases++;
                }

            // Reads, with every edit field at its edges.
            foreach (var spec in m.Devices.SelectMany(d => d.Actions).Where(a => a.Kind == ActionKind.Read))
                foreach (var e in t.Edit)
                    foreach (var v in e.Kind == "bool" ? new JsonNode[] { true, false } : new JsonNode[] { e.Min!.Value, e.Max!.Value })
                    {
                        var room = RoomState.PostSetup(m);
                        room.Set($"{t.Type}.{e.Field}", v);
                        var text = b.Apply(spec, new JsonObject(), room).Text;
                        Assert.AreEqual(0, CopyGuard.Find(text).Count, $"{t.Type}: read '{text}' ({e.Field}={v.ToJsonString()})");
                        cases++;
                    }

            Assert.AreEqual(0, CopyGuard.Find($"{m.Devices[0].Name} not responding").Count, t.Type);
        }
        Assert.IsTrue(cases >= cat.Types.Sum(t => t.Actions.Count), $"only {cases} cases");
    }
}
