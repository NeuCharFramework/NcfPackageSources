/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CatalogTests.cs
    文件功能描述：测试设备目录的格式与自定义房间的设备列表校验


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>The device catalog: the shipped file is clean, and each catalog rule catches its own breakage.</summary>
[TestClass]
public class CatalogTests
{
    const string Valid = """
    {
      "version": 1, "maxDevices": 20,
      "categories": ["Lights","Climate","Sensors"],
      "suggestions": [
        { "needs": ["lamp","probe"], "text": "Turn on {lamp} when {probe} reads low" },
        { "needs": [], "text": "Check each device and tell me what it's doing" }
      ],
      "types": [
        { "type": "lamp", "name": "Lamp", "ref": "the lamp", "category": "Lights", "mount": "floor", "new": false,
          "initial": { "brightness": 0, "on": false }, "faultText": "off the network",
          "edit": [ { "field": "brightness", "kind": "number", "min": 0, "max": 1 }, { "field": "on", "kind": "bool" } ],
          "drift": [ { "field": "brightness", "perHour": 0.1, "toward": 0.5, "while": { "on": true }, "max": 1 } ],
          "actions": [
            { "verb": "set", "kind": "Write", "outcome": "confirmed",
              "description": "Set {ref} brightness from 0 (off) to 100.",
              "parameters": { "type": "object", "properties": { "brightness": { "type": "integer", "minimum": 0, "maximum": 100 } },
                              "required": ["brightness"], "additionalProperties": false },
              "safeWhen": { "brightness": 0 },
              "effects": [ { "set": "brightness", "arg": "brightness", "scale": 0.01, "ms": 1200 } ],
              "label": [ { "when": { "brightness": 0 }, "text": "{name} → off" }, { "text": "{name} → {brightness} units" } ] },
            { "verb": "read", "kind": "Read", "outcome": "confirmed",
              "description": "Read {ref} brightness.",
              "parameters": { "type": "object", "properties": {}, "required": [], "additionalProperties": false },
              "reads": [ { "field": "brightness", "round": 1, "unit": "" } ],
              "label": [ { "text": "{name} → read" } ] },
            { "verb": "off", "kind": "Safe", "outcome": "confirmed",
              "description": "Switch {ref} off.",
              "parameters": { "type": "object", "properties": {}, "required": [], "additionalProperties": false },
              "effects": [ { "set": "brightness", "value": 0 } ],
              "label": [ { "text": "{name} → off" } ] }
          ] },
        { "type": "probe", "name": "Probe", "ref": "the probe", "category": "Sensors", "mount": "wall", "new": false,
          "initial": { "level": 5 }, "faultText": "not responding", "edit": [], "drift": [],
          "actions": [
            { "verb": "read", "kind": "Read", "outcome": "confirmed", "description": "Read {ref}.",
              "parameters": { "type": "object", "properties": {}, "required": [], "additionalProperties": false },
              "reads": [ { "field": "level", "round": 0, "unit": " pts" } ],
              "label": [ { "text": "{name} → read" } ] }
          ] }
      ]
    }
    """;

    static IReadOnlyList<string> ProblemsOf(Action<JsonObject> edit)
    {
        var root = JsonNode.Parse(Valid)!.AsObject();
        edit(root);
        return Catalog.Problems(Catalog.Parse(root.ToJsonString()));
    }

    static JsonObject Lamp(JsonObject root) => root["types"]![0]!.AsObject();
    static JsonObject Action(JsonObject root, int i) => Lamp(root)["actions"]![i]!.AsObject();

    [TestMethod]
    public void ValidFixture_HasNoProblems() => CollectionAssert.AreEqual(Array.Empty<string>(), ProblemsOf(_ => { }).ToArray());

    [TestMethod]
    public void Shipped_HasNoProblems()
    {
        var c = Catalog.Load();
        Assert.AreEqual("", string.Join(" ; ", Catalog.Problems(c)));
        CollectionAssert.AreEqual(
            new[] { "floor_lamp", "blinds", "soil_sensor", "water_pump", "door_camera", "thermostat",
                    "bed_sensor", "bedroom_door", "hallway_motion", "kettle_plug", "night_light", "hall_light",
                    "key_light", "main_camera", "mic", "capture_card", "overlay", "on_air_sign", "led_strip",
                    "laser", "mirror", "linear_stage", "power_meter", "conveyor", "scanner_arch", "sorter_gate", "cart", "loading_dock",
                    "smart_plug", "ceiling_light", "colour_bulb", "tv", "smart_speaker", "robot_vacuum", "ceiling_fan",
                    "air_conditioner", "space_heater", "humidifier", "air_purifier",
                    "smart_lock", "video_doorbell", "door_window_sensor", "smoke_co_alarm", "siren", "garage_door",
                    "temp_humidity_sensor", "water_leak_sensor", "presence_sensor", "energy_meter", "ev_charger",
                    "sprinkler_valve", "printer_3d", "robot_arm", "label_printer" },
            c.Types.Select(t => t.Type).ToArray());
        Assert.AreEqual(20, c.MaxDevices);
        Assert.AreEqual(24, c.Suggestions.Count);
        Assert.AreEqual(54, c.Types.Count);
        Assert.AreEqual(28, c.Types.Count(t => !t.New));
        Assert.AreEqual(26, c.Types.Count(t => t.New));
        Assert.AreEqual(0, c.Suggestions[^1].Needs.Count);
        Assert.AreEqual("commanded", c.Find("water_pump")!.Actions[0].Outcome);
        Assert.AreEqual(ActionKind.Safe, c.Find("water_pump")!.Actions[1].Kind);
        Assert.IsNull(c.Find("nope"));
    }

    [TestMethod]
    public void Load_IsSharedAndFrozen() => Assert.AreSame(Catalog.Load(), Catalog.Load());

    [TestMethod]
    public void Problems_CatchEachRule()
    {
        var cases = new (string Name, Action<JsonObject> Break, string? Names)[]
        {
            ("unknown effect field", r => Action(r, 0)["effects"]![0]!["set"] = "nope", "lamp"),
            ("status in initial", r => Lamp(r)["initial"]!["status"] = "online", "lamp"),
            ("duplicate verb", r => Action(r, 2)["verb"] = "set", "lamp"),
            ("read with effects", r => Action(r, 1)["effects"] = new JsonArray(new JsonObject { ["set"] = "brightness", ["value"] = 0 }), "lamp"),
            ("safe with required arg", r => Action(r, 2)["parameters"] = JsonNode.Parse(
                """{ "type": "object", "properties": { "x": { "type": "integer" } }, "required": ["x"], "additionalProperties": false }"""), "lamp"),
            ("label placeholder", r => Action(r, 0)["label"]![1]!["text"] = "{name} → {nope}", "lamp"),
            ("label without fallback", r => Action(r, 0)["label"]!.AsArray().RemoveAt(1), "lamp"),
            ("edit number without max", r => Lamp(r)["edit"]![0]!.AsObject().Remove("max"), "lamp"),
            ("edit on status", r => Lamp(r)["edit"]![1]!["field"] = "status", "lamp"),
            ("description with 100%", r => Action(r, 0)["description"] = "Set {ref} to 100% bright.", "lamp"),
            ("suggestion needs unknown type", r => r["suggestions"]![0]!["needs"] = new JsonArray("lamp", "ghost"), "ghost"),
            ("type Floor-Lamp", r => Lamp(r)["type"] = "Floor-Lamp", "Floor-Lamp"),
            ("mount shelf", r => Lamp(r)["mount"] = "shelf", "lamp"),
            ("safeWhen key not a parameter", r => Action(r, 0)["safeWhen"] = JsonNode.Parse("""{ "zzz": 1 }"""), "lamp"),
            ("required not in properties", r => Action(r, 0)["parameters"]!["required"] = new JsonArray("brightness", "zzz"), "lamp"),
            ("additionalProperties true", r => Action(r, 0)["parameters"]!["additionalProperties"] = true, "lamp"),
            ("verb with capitals", r => Action(r, 2)["verb"] = "Off", "lamp"),
            ("write without effects", r => Action(r, 0)["effects"] = new JsonArray(), "lamp"),
            ("read with unknown field", r => Action(r, 1)["reads"]![0]!["field"] = "nope", "lamp"),
            ("edit field missing from initial", r => Lamp(r)["edit"]![1]!["field"] = "ghost", "lamp"),
            ("edit initial out of range", r => Lamp(r)["initial"]!["brightness"] = 5, "lamp"),
            ("drift field missing", r => Lamp(r)["drift"]![0]!["field"] = "ghost", "lamp"),
            ("drift toward unknown field", r => Lamp(r)["drift"]![0]!["toward"] = "ghost", "lamp"),
            ("ref without the", r => Lamp(r)["ref"] = "a lamp", "lamp"),
            ("unknown category", r => Lamp(r)["category"] = "Gadgets", "lamp"),
            ("guarded fault text", r => Lamp(r)["faultText"] = "guaranteed gone", "lamp"),
            ("guarded label", r => Action(r, 0)["label"]![0]!["text"] = "{name} → 100%", "lamp"),
            ("maxDevices", r => r["maxDevices"] = 30, null),
            ("duplicate type", r => r["types"]!.AsArray().Add(r["types"]![1]!.DeepClone()), "probe"),
            ("last suggestion not catch-all", r => r["suggestions"]![1]!["needs"] = new JsonArray("lamp"), null),
            ("suggestion placeholder not in needs", r => r["suggestions"]![0]!["text"] = "Turn on {lamp} near {ghost}", "ghost"),
        };
        foreach (var (name, brk, names) in cases)
        {
            var broken = ProblemsOf(brk);
            Assert.IsTrue(broken.Count > 0, $"no problem reported for: {name}");
            if (names is not null)
                Assert.IsTrue(broken.Any(p => p.Contains(names)),
                    $"problems for '{name}' do not name '{names}': {string.Join(" | ", broken)}");
        }
    }

    [TestMethod]
    public void Problems_DoesNotThrowOnOddButWellFormedJson()
    {
        var p = ProblemsOf(r =>
        {
            Action(r, 0)["parameters"] = new JsonObject();
            Action(r, 1)["reads"] = new JsonArray();
            Lamp(r)["drift"] = new JsonArray(new JsonObject { ["field"] = "brightness", ["perHour"] = 1, ["toward"] = true, ["while"] = new JsonObject { ["x"] = 1 } });
        });
        Assert.IsTrue(p.Count > 0);
    }
}
