/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：GateTests.cs
    文件功能描述：NeuCharBox Live 测试：GateTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class GateTests
{
    // One manifest per test: MSTest makes a new instance for each test method.
    readonly SceneManifest M = ManifestLoader.Load("home");

    static JsonObject Obj(string json) => (JsonObject)JsonNode.Parse(json)!;

    static ApprovedPlan Plan(params (string Id, string[] Devices)[] steps)
    {
        var p = new ApprovedPlan();
        foreach (var (id, devices) in steps) p.Steps.Add(new PlanStep(id, $"step {id}", devices));
        return p;
    }

    static ApprovedPlan LampPlan() => Plan(("s1", new[] { "lamp" }));

    GateResult Gate(string action, string args, string? step, ApprovedPlan? plan = null) =>
        PlanGate.Check(new ActionCall(action, Obj(args), step), plan ?? LampPlan(), M);

    GateResult Tool(string tool, string args, ApprovedPlan? plan = null) =>
        PlanGate.CheckTool(tool, Obj(args), plan ?? LampPlan(), M);

    static string StepsJson(params (string Id, string Devices)[] steps) =>
        "[" + string.Join(",", steps.Select(s => $$"""{"id":"{{s.Id}}","text":"Do {{s.Id}}","devices":[{{s.Devices}}]}""")) + "]";

    // ---- PlanGate.Check: device actions ----

    [TestMethod]
    public void Gate_WriteOutsidePlan_Refused()
    {
        var g = Gate("set_blinds", "{\"closed_percent\":100}", "s1");
        Assert.IsFalse(g.Allowed); Assert.IsFalse(g.Safe);
        Assert.AreEqual("blinds isn't in step s1; call propose_replan to change the plan", g.Error);
    }

    [TestMethod]
    public void Gate_WriteInPlan_Allowed()
    {
        var g = Gate("set_lamp", "{\"brightness\":20}", "s1");
        Assert.IsTrue(g.Allowed); Assert.IsFalse(g.Safe); Assert.IsNull(g.Error);

        var two = Plan(("s1", new[] { "lamp" }), ("s2", new[] { "blinds", "lamp" }));
        Assert.IsTrue(Gate("set_blinds", "{\"closed_percent\":100}", "s2", two).Allowed);
        Assert.IsFalse(Gate("set_blinds", "{\"closed_percent\":100}", "s1", two).Allowed);
    }

    [TestMethod]
    public void Gate_SafeState_AlwaysAllowed()
    {
        var blindsOnly = Plan(("s1", new[] { "blinds" }));
        var off = Gate("set_lamp", "{\"brightness\":0}", null, blindsOnly);
        Assert.IsTrue(off.Allowed); Assert.IsTrue(off.Safe); Assert.IsNull(off.Error);
        Assert.IsTrue(Gate("set_lamp", "{\"brightness\":0.0}", null, new ApprovedPlan()).Safe);

        var stop = Gate("stop_pump", "{}", null, new ApprovedPlan());
        Assert.IsTrue(stop.Allowed); Assert.IsTrue(stop.Safe);

        // The same write away from its safe value needs an approved step.
        var on = Gate("set_lamp", "{\"brightness\":1}", null, blindsOnly);
        Assert.IsFalse(on.Allowed); Assert.IsFalse(on.Safe);
    }

    [TestMethod]
    public void Gate_EmptySafeWhen_IsNotSafe()
    {
        var schema = Obj("""{"type":"object","properties":{"v":{"type":"integer"}},"required":["v"],"additionalProperties":false}""");
        var m = new SceneManifest("t", "T", new DateTime(2026, 11, 13), 0, new[] { "x" }, new[]
        {
            new DeviceSpec("x", "X", "the x", new JsonObject(), new[] { new ActionSpec("set_x", "x", "Set x.", ActionKind.Write, schema, new JsonObject()) }),
        });
        var g = PlanGate.Check(new ActionCall("set_x", Obj("{\"v\":5}"), null), new ApprovedPlan(), m);
        Assert.IsFalse(g.Allowed); Assert.IsFalse(g.Safe);
    }

    [DataTestMethod]
    [DataRow("s9")]
    [DataRow((string?)null)]
    [DataRow("")]
    public void Gate_UnknownOrMissingStep_Refused(string? step)
    {
        var g = Gate("set_lamp", "{\"brightness\":20}", step);
        Assert.IsFalse(g.Allowed); Assert.IsFalse(g.Safe);
        Assert.AreEqual("every change needs the id of an approved step; lamp is in step s1", g.Error);

        var twice = Gate("set_lamp", "{\"brightness\":20}", step, Plan(("s1", new[] { "lamp" }), ("s2", new[] { "blinds", "lamp" })));
        Assert.AreEqual("every change needs the id of an approved step; lamp is in steps s1, s2", twice.Error);
    }

    [DataTestMethod]
    [DataRow("s9")]
    [DataRow((string?)null)]
    [DataRow("")]
    public void Gate_UnplannedDeviceWithoutStep_SaysProposeReplan(string? step)
    {
        var g = Gate("set_blinds", "{\"closed_percent\":100}", step);
        Assert.IsFalse(g.Allowed); Assert.IsFalse(g.Safe);
        Assert.AreEqual("blinds isn't in the approved plan; call propose_replan to change the plan", g.Error);
    }

    [TestMethod]
    public void Gate_BypassEdges_Refused()
    {
        var empty = new ApprovedPlan();
        var textZero = Gate("set_lamp", "{\"brightness\":\"0\"}", null, empty);
        Assert.IsFalse(textZero.Allowed); Assert.IsFalse(textZero.Safe);

        // Names, step ids and device ids match exactly.
        StringAssert.Contains(Gate("Set_Lamp", "{\"brightness\":20}", "s1").Error, "unknown action");
        Assert.IsFalse(Gate("set_lamp", "{\"brightness\":20}", "S1").Allowed);
        Assert.IsFalse(Gate("set_lamp", "{\"brightness\":20}", "s1", Plan(("s1", new[] { "Lamp" }))).Allowed);
        Assert.IsFalse(PlanGate.CheckPlan(JsonNode.Parse($$"""{"steps":{{StepsJson(("a", "\"Lamp\""))}}}""")!, M).Ok);
        Assert.IsFalse(PlanGate.CheckReplan(JsonNode.Parse($$"""{"steps":{{StepsJson(("a", "\"Lamp\""))}},"reason":"r"}""")!, LampPlan(), M).Ok);

        // schedule's inner args must be an object, as its schema says.
        var encoded = Tool("schedule", """{"at":"18:00","action":"set_lamp","args":"{\"brightness\":20}","step":"s1"}""");
        Assert.IsFalse(encoded.Allowed); StringAssert.Contains(encoded.Error, "args must be a JSON object");
        Assert.IsFalse(Tool("add_rule", """{"when":{"device":"camera","field":"motion","op":"becomes","value":true},"then":{"action":"set_lamp","args":"{\"brightness\":20}"},"step":"s1"}""").Allowed);

        // A safe value plus an extra key is refused on every path that leads to it.
        var extra = Tool("schedule", """{"at":"23:00","action":"set_lamp","args":{"brightness":0,"x":1},"step":"s1"}""", empty);
        Assert.IsFalse(extra.Allowed); StringAssert.Contains(extra.Error, "unexpected 'x'");
        var split = ArgValidator.SplitStep(Obj("{\"brightness\":0,\"x\":1}"));
        Assert.AreEqual("unexpected 'x'", ArgValidator.Validate(M.Action("set_lamp")!.Parameters, split.Args).Error);
        var room = RoomState.PostSetup(M);
        var o = new Simulator(M, ManifestLoader.Behavior("home")).Execute(new ActionCall("set_lamp", Obj("{\"brightness\":0,\"x\":1}"), null), room);
        Assert.AreEqual(Outcome.Invalid, o.Outcome);
    }

    [TestMethod]
    public void Gate_UnknownAction_Refused()
    {
        var g = Gate("fly", "{}", "s1");
        Assert.IsFalse(g.Allowed);
        StringAssert.Contains(g.Error, "unknown action");
    }

    [TestMethod]
    public void Gate_ReadsAlwaysAllowed()
    {
        var g = Gate("read_soil", "{}", null, new ApprovedPlan());
        Assert.IsTrue(g.Allowed); Assert.IsFalse(g.Safe); Assert.IsNull(g.Error);
    }

    // ---- PlanGate.CheckTool: generic tools ----

    [TestMethod]
    public void Gate_ScheduleAndRule_GateTheirInnerAction()
    {
        var blinds = Tool("schedule", """{"at":"18:00","action":"set_blinds","args":{"closed_percent":100},"step":"s1"}""");
        Assert.IsFalse(blinds.Allowed); StringAssert.Contains(blinds.Error, "propose_replan");

        Assert.IsTrue(Tool("schedule", """{"at":"18:00","action":"set_lamp","args":{"brightness":20},"step":"s1"}""").Allowed);

        var badInner = Tool("schedule", """{"at":"18:00","action":"set_lamp","args":{"brightness":"20%"},"step":"s1"}""");
        Assert.IsFalse(badInner.Allowed); StringAssert.Contains(badInner.Error, "brightness");

        var lightsOut = Tool("schedule", """{"at":"23:00","action":"set_lamp","args":{"brightness":0},"step":"s1"}""", new ApprovedPlan());
        Assert.IsTrue(lightsOut.Allowed); Assert.IsTrue(lightsOut.Safe);

        var noStep = Tool("schedule", """{"at":"18:00","action":"set_lamp","args":{"brightness":20},"step":"s9"}""");
        Assert.IsFalse(noStep.Allowed); Assert.AreEqual("every change needs the id of an approved step; lamp is in step s1", noStep.Error);

        var flySchedule = Tool("schedule", """{"at":"18:00","action":"fly","args":{},"step":"s1"}""");
        Assert.IsFalse(flySchedule.Allowed); StringAssert.Contains(flySchedule.Error, "unknown action");

        Assert.IsTrue(Tool("add_rule", """{"when":{"device":"camera","field":"motion","op":"becomes","value":true},"then":{"notify":"someone at the door"},"step":"s1"}""").Allowed);
        Assert.IsTrue(Tool("add_rule", """{"when":{"device":"camera","field":"motion","op":"becomes","value":false},"then":{"notify":"gone"},"step":"s1"}""").Allowed);
        Assert.IsTrue(Tool("add_rule", """{"when":{"device":"pump","field":"status","op":"becomes","value":"fault"},"then":{"notify":"pump down"},"step":"s1"}""").Allowed);
        Assert.IsTrue(Tool("add_rule", """{"when":{"device":"thermostat","field":"current","op":"becomes","value":18},"then":{"notify":"cold"},"step":"s1"}""").Allowed);

        const string water = """{"when":{"device":"soil","field":"moisture","op":"below","value":30},"then":{"action":"run_pump","args":{"seconds":6}},"max_per_day":2,"step":"s1"}""";
        var waterOffPlan = Tool("add_rule", water);
        Assert.IsFalse(waterOffPlan.Allowed); StringAssert.Contains(waterOffPlan.Error, "propose_replan");
        Assert.IsTrue(Tool("add_rule", water, Plan(("s1", new[] { "soil", "pump" }))).Allowed);

        var badRuleArgs = Tool("add_rule", """{"when":{"device":"soil","field":"moisture","op":"below","value":30},"then":{"action":"run_pump","args":{"seconds":60}},"step":"s1"}""", Plan(("s1", new[] { "pump" })));
        Assert.IsFalse(badRuleArgs.Allowed); StringAssert.Contains(badRuleArgs.Error, "seconds");

        Assert.IsTrue(Tool("notify_user", """{"text":"Back soon"}""").Allowed);
        Assert.IsTrue(Tool("run_until", """{"hours":24}""").Allowed);
        Assert.IsTrue(Tool("propose_replan", $$"""{"steps":{{StepsJson(("x", "\"blinds\""))}},"reason":"The blinds too"}""").Allowed);

        var fly = Tool("fly", "{}");
        Assert.IsFalse(fly.Allowed); StringAssert.Contains(fly.Error, "unknown action");
        Assert.IsFalse(Tool("submit_plan", $$"""{"steps":{{StepsJson(("a", "\"lamp\""))}}}""").Allowed);
        Assert.IsFalse(Tool("set_lamp", """{"brightness":20,"step":"s1"}""").Allowed);
    }

    [DataTestMethod]
    [DataRow("""{"device":"garage","field":"open","op":"becomes","value":true}""", """{"notify":"hi"}""", "garage")]
    [DataRow("""{"device":"soil","field":"moist","op":"below","value":30}""", """{"notify":"hi"}""", "moist")]
    [DataRow("""{"device":"soil","field":"moisture","op":"below","value":"30"}""", """{"notify":"hi"}""", "number")]
    [DataRow("""{"device":"camera","field":"motion","op":"becomes","value":true}""", """{"notify":"hi","action":"stop_pump","args":{}}""", "either")]
    [DataRow("""{"device":"camera","field":"motion","op":"becomes","value":true}""", "{}", "either")]
    [DataRow("""{"device":"camera","field":"motion","op":"becomes","value":"true"}""", """{"notify":"hi"}""", "a boolean, like camera.motion")]
    [DataRow("""{"device":"soil","field":"moisture","op":"becomes","value":"30"}""", """{"notify":"hi"}""", "a number, like soil.moisture")]
    [DataRow("""{"device":"pump","field":"status","op":"becomes","value":1}""", """{"notify":"hi"}""", "a string, like pump.status")]
    public void Gate_AddRule_RefusesRulesThatCouldNeverWork(string when, string then, string expected)
    {
        var g = Tool("add_rule", $$"""{"when":{{when}},"then":{{then}},"step":"s1"}""", Plan(("s1", new[] { "camera", "soil", "pump" })));
        Assert.IsFalse(g.Allowed);
        StringAssert.Contains(g.Error, expected);
    }

    // ---- ArgValidator ----

    [DataTestMethod]
    [DataRow("{\"brightness\":\"20%\"}")]
    [DataRow("{\"brightness\":150}")]
    [DataRow("{}")]
    [DataRow("{\"brightness\":20,\"x\":1}")]
    [DataRow("[1]")]
    [DataRow("{\"brightness\":20.5}")]
    [DataRow("{\"brightness\":null}")]
    [DataRow("{\"brightness\":true}")]
    [DataRow("{\"brightness\":-1}")]
    [DataRow("\"not json\"")]
    [DataRow("\"[1]\"")]
    [DataRow("\"null\"")]
    [DataRow("7")]
    [DataRow("null")]
    public void Validate_RejectsBadArgs(string args)
    {
        var v = ArgValidator.Validate(M.Action("set_lamp")!.Parameters, JsonNode.Parse(args));
        Assert.IsFalse(v.Ok); Assert.IsNull(v.Clean);
        Assert.IsFalse(string.IsNullOrWhiteSpace(v.Error));
    }

    [TestMethod]
    public void Validate_ErrorsSayWhatToFix()
    {
        var lamp = M.Action("set_lamp")!.Parameters;
        Assert.AreEqual("'brightness' must be an integer", ArgValidator.Validate(lamp, Obj("{\"brightness\":\"20%\"}")).Error);
        Assert.AreEqual("'brightness' must be at most 100", ArgValidator.Validate(lamp, Obj("{\"brightness\":150}")).Error);
        Assert.AreEqual("missing 'brightness'", ArgValidator.Validate(lamp, Obj("{}")).Error);
        Assert.AreEqual("unexpected 'x'", ArgValidator.Validate(lamp, Obj("{\"brightness\":20,\"x\":1}")).Error);
        Assert.AreEqual("args must be a JSON object", ArgValidator.Validate(lamp, JsonNode.Parse("[1]")).Error);
    }

    [TestMethod]
    public void Validate_AcceptsJsonStringArgs()
    {
        var lamp = M.Action("set_lamp")!.Parameters;
        var v = ArgValidator.Validate(lamp, JsonNode.Parse("\"{\\\"brightness\\\":20}\""));
        Assert.IsTrue(v.Ok); Assert.IsNull(v.Error);
        Assert.AreEqual(20, (int)v.Clean!["brightness"]!);

        var input = Obj("{\"brightness\":20}");
        var plain = ArgValidator.Validate(lamp, input);
        Assert.IsTrue(plain.Ok);
        plain.Clean!["brightness"] = 99;
        Assert.AreEqual(20, (int)input["brightness"]!, "Clean is a copy");

        Assert.IsTrue(ArgValidator.Validate(M.Action("read_soil")!.Parameters, null).Ok, "no args at all is {}");
        var repeated = ArgValidator.Validate(lamp, JsonValue.Create("{\"brightness\":20,\"brightness\":30}"));
        Assert.IsFalse(repeated.Ok, "a repeated key is refused, not thrown");
        Assert.AreEqual("args must be a JSON object", repeated.Error);
        Assert.IsNotNull(ArgValidator.SplitStep(JsonNode.Parse("{\"brightness\":20,\"step\":\"s1\",\"step\":\"s2\"}")).Error);
        Assert.IsFalse(PlanGate.CheckPlan(JsonNode.Parse("""{"steps":[{"id":"a","text":"t","devices":["lamp"],"text":"u"}]}""")!, M).Ok);
        Assert.IsTrue(ArgValidator.Validate(M.Action("set_thermostat")!.Parameters, Obj("{\"celsius\":21.5}")).Ok);
        Assert.IsTrue(ArgValidator.Validate(M.Action("set_door_camera")!.Parameters, Obj("{\"armed\":true}")).Ok);
    }

    [TestMethod]
    public void SplitStep_MovesStepOut()
    {
        var lamp = M.Action("set_lamp")!.Parameters;
        const string withStep = "{\"brightness\":20,\"step\":\"s1\"}";
        foreach (var args in new JsonNode[] { Obj(withStep), JsonValue.Create(withStep)! })
        {
            var s = ArgValidator.SplitStep(args);
            Assert.IsNull(s.Error);
            Assert.AreEqual("s1", s.Step);
            Assert.AreEqual("{\"brightness\":20}", s.Args!.ToJsonString());
            Assert.IsTrue(ArgValidator.Validate(lamp, s.Args).Ok);
        }

        var input = Obj(withStep);
        ArgValidator.SplitStep(input);
        Assert.AreEqual("s1", (string)input["step"]!, "the caller's args are left alone");

        var none = ArgValidator.SplitStep(Obj("{\"brightness\":0}"));
        Assert.IsNull(none.Error); Assert.IsNull(none.Step); Assert.AreEqual(0, (int)none.Args!["brightness"]!);

        var nullStep = ArgValidator.SplitStep(Obj("{\"brightness\":0,\"step\":null}"));
        Assert.IsNull(nullStep.Error); Assert.IsNull(nullStep.Step); Assert.IsFalse(nullStep.Args!.ContainsKey("step"));

        foreach (var bad in new[] { "{\"brightness\":20,\"step\":5}", "{\"brightness\":20,\"step\":[\"s1\"]}", "[1]", "\"nope\"", "3" })
        {
            var s = ArgValidator.SplitStep(JsonNode.Parse(bad));
            Assert.IsFalse(string.IsNullOrWhiteSpace(s.Error), bad);
            Assert.IsNull(s.Args, bad); Assert.IsNull(s.Step, bad);
        }

        Assert.AreEqual("{}", ArgValidator.SplitStep(null).Args!.ToJsonString());
    }

    [TestMethod]
    public void Validate_GenericToolSchemas()
    {
        var run = ToolCatalog.RunTools(M).ToDictionary(t => t.Name, ToolCatalog.SchemaOf);
        var plan = ToolCatalog.PlanTools(M).ToDictionary(t => t.Name, ToolCatalog.SchemaOf);

        void Good(JsonObject schema, string args) => Assert.IsTrue(ArgValidator.Validate(schema, JsonNode.Parse(args)).Ok, args + " → " + ArgValidator.Validate(schema, JsonNode.Parse(args)).Error);
        void Bad(JsonObject schema, string args) => Assert.IsFalse(ArgValidator.Validate(schema, JsonNode.Parse(args)).Ok, args);

        Good(run["schedule"], """{"at":"18:00","days":["Mon","Fri"],"action":"set_lamp","args":{"brightness":20},"step":"s1"}""");
        Good(run["schedule"], """{"at":"2026-11-14T07:30","action":"set_blinds","args":{"closed_percent":0},"step":"s2"}""");
        Bad(run["schedule"], """{"at":"6pm","action":"set_lamp","args":{"brightness":20},"step":"s1"}""");
        Bad(run["schedule"], """{"at":"24:00","action":"set_lamp","args":{"brightness":20},"step":"s1"}""");
        Bad(run["schedule"], """{"at":"18:00","days":["Funday"],"action":"set_lamp","args":{"brightness":20},"step":"s1"}""");
        Bad(run["schedule"], """{"at":"18:00","action":"fly","args":{},"step":"s1"}""");
        Bad(run["schedule"], """{"at":"18:00","action":"set_lamp","args":{"brightness":20}}""");
        Bad(run["schedule"], """{"at":"18:00\n","action":"set_lamp","args":{"brightness":20},"step":"s1"}""");
        Bad(run["schedule"], """{"at":"2026-11-14T07:30\n","action":"set_lamp","args":{"brightness":20},"step":"s1"}""");
        Bad(run["schedule"], """{"at":"18:00\n2026-11-14T07:30","action":"set_lamp","args":{"brightness":20},"step":"s1"}""");

        Good(run["add_rule"], """{"when":{"device":"soil","field":"moisture","op":"below","value":30},"then":{"action":"run_pump","args":{"seconds":6}},"max_per_day":2,"step":"s3"}""");
        Good(run["add_rule"], """{"when":{"device":"camera","field":"motion","op":"becomes","value":true},"then":{"notify":"Someone is at the door"},"step":"s4"}""");
        Bad(run["add_rule"], """{"when":{"device":"soil","field":"moisture","op":"equals","value":30},"then":{"notify":"x"},"step":"s3"}""");
        Bad(run["add_rule"], """{"when":{"device":"soil","field":"moisture","op":"below","value":30},"then":{"notify":"x"},"max_per_day":7,"step":"s3"}""");
        Bad(run["add_rule"], """{"when":{"device":"soil","field":"moisture","op":"below","value":30},"then":{"notify":"x","extra":1},"step":"s3"}""");

        Good(run["notify_user"], """{"text":"I closed the blinds."}""");
        Good(run["notify_user"], """{"text":"I closed the blinds.","step":"s2"}""");
        Bad(run["notify_user"], $$"""{"text":"{{new string('a', 201)}}"}""");
        Bad(run["notify_user"], """{"text":""}""");

        Good(run["run_until"], """{"hours":168}""");
        Bad(run["run_until"], """{"hours":169}""");
        Bad(run["run_until"], """{"hours":0}""");

        Good(run["propose_replan"], $$"""{"steps":{{StepsJson(("x", "\"blinds\""))}},"reason":"The blinds need closing too"}""");
        Bad(run["propose_replan"], $$"""{"steps":{{StepsJson(("x", "\"blinds\""))}}}""");

        Good(plan["cannot_do"], """{"reason":"I can't order pizza.","suggestions":["Dim the lamp for movie night"]}""");
        Bad(plan["cannot_do"], """{"reason":"No.","suggestions":[]}""");
        Bad(plan["cannot_do"], """{"reason":"No.","suggestions":["a","b","c","d"]}""");
    }

    [TestMethod]
    public void Validate_NullOnOptionalProperty_IsAbsent()
    {
        var run = ToolCatalog.RunTools(M).ToDictionary(t => t.Name, ToolCatalog.SchemaOf);

        var notify = ArgValidator.Validate(run["notify_user"], Obj("""{"text":"Back soon","step":null}"""));
        Assert.IsTrue(notify.Ok, notify.Error);
        Assert.AreEqual("""{"text":"Back soon"}""", notify.Clean!.ToJsonString());

        var schedule = ArgValidator.Validate(run["schedule"], Obj("""{"at":"18:00","days":null,"action":"set_lamp","args":{"brightness":20},"step":"s1"}"""));
        Assert.IsTrue(schedule.Ok, schedule.Error); Assert.IsFalse(schedule.Clean!.ContainsKey("days"));

        var rule = ArgValidator.Validate(run["add_rule"], Obj("""{"when":{"device":"camera","field":"motion","op":"becomes","value":true},"then":{"notify":"hi","action":null,"args":null},"max_per_day":null,"step":"s1"}"""));
        Assert.IsTrue(rule.Ok, rule.Error);
        Assert.AreEqual("""{"notify":"hi"}""", rule.Clean!["then"]!.ToJsonString());
        Assert.IsFalse(rule.Clean.ContainsKey("max_per_day"));
        Assert.IsTrue(PlanGate.CheckTool("add_rule", rule.Clean, LampPlan(), M).Allowed);

        var plan = PlanGate.CheckPlan(JsonNode.Parse($$"""{"steps":{{StepsJson(("a", "\"lamp\""))}},"horizon_hours":null}""")!, M);
        Assert.IsTrue(plan.Ok, plan.Error); Assert.IsNull(plan.Plan!.HorizonHours);

        // Null still fails where a value is required, and an unknown key is unexpected even when null.
        Assert.AreEqual("'brightness' must be an integer", ArgValidator.Validate(M.Action("set_lamp")!.Parameters, Obj("{\"brightness\":null}")).Error);
        Assert.AreEqual("unexpected 'x'", ArgValidator.Validate(M.Action("set_lamp")!.Parameters, Obj("{\"brightness\":20,\"x\":null}")).Error);
        Assert.AreEqual("missing 'step'", ArgValidator.Validate(run["schedule"], Obj("""{"at":"18:00","action":"set_lamp","args":{"brightness":20}}""")).Error);
    }

    // ---- CheckPlan / CheckReplan / Merge ----

    [TestMethod]
    public void CheckPlan_RenumbersAndValidates()
    {
        var ok = PlanGate.CheckPlan(JsonNode.Parse("""
            {"steps":[{"id":"a","text":"Dim the floor lamp to 20%","devices":["lamp"]},
                      {"id":"b","text":"Check the soil, water if dry","devices":["soil","pump"]}],
             "horizon_hours":24}
            """)!, M);
        Assert.IsTrue(ok.Ok, ok.Error); Assert.IsNull(ok.Error);
        CollectionAssert.AreEqual(new[] { "s1", "s2" }, ok.Plan!.Steps.Select(s => s.Id).ToArray());
        Assert.AreEqual("Dim the floor lamp to 20%", ok.Plan.Steps[0].Text);
        CollectionAssert.AreEqual(new[] { "soil", "pump" }, ok.Plan.Steps[1].Devices.ToArray());
        Assert.AreEqual(24, ok.Plan.HorizonHours);

        var sensorOnly = PlanGate.CheckPlan(JsonValue.Create($$"""{"steps":{{StepsJson(("q", "\"soil\""))}}}""")!, M);
        Assert.IsTrue(sensorOnly.Ok, sensorOnly.Error);
        Assert.IsNull(sensorOnly.Plan!.HorizonHours);
        Assert.AreEqual("s1", sensorOnly.Plan.Steps.Single().Id);

        var seven = StepsJson(Enumerable.Range(1, 7).Select(i => ($"s{i}", "\"lamp\"")).ToArray());
        var tooMany = PlanGate.CheckPlan(JsonNode.Parse($$"""{"steps":{{seven}}}""")!, M);
        Assert.IsFalse(tooMany.Ok); Assert.IsNull(tooMany.Plan); StringAssert.Contains(tooMany.Error, "steps");
        Assert.IsTrue(PlanGate.CheckPlan(JsonNode.Parse($$"""{"steps":{{seven}}}""")!, M, maxSteps: 7).Ok);

        var garage = PlanGate.CheckPlan(JsonNode.Parse($$"""{"steps":{{StepsJson(("a", "\"garage\""))}}}""")!, M);
        Assert.IsFalse(garage.Ok); StringAssert.Contains(garage.Error, "devices");

        Assert.IsFalse(PlanGate.CheckPlan(JsonNode.Parse("""{"steps":[]}""")!, M).Ok);
        Assert.IsFalse(PlanGate.CheckPlan(JsonNode.Parse($$"""{"steps":{{StepsJson(("a", "\"lamp\""))}},"horizon_hours":200}""")!, M).Ok);
        Assert.IsFalse(PlanGate.CheckPlan(JsonNode.Parse($$"""{"steps":[{"id":"a","text":"{{new string('a', 121)}}","devices":["lamp"]}]}""")!, M).Ok);
        Assert.IsFalse(PlanGate.CheckPlan(JsonNode.Parse("""{"steps":[{"id":"a","devices":["lamp"]}]}""")!, M).Ok);
        Assert.IsFalse(PlanGate.CheckPlan(JsonNode.Parse("[1]")!, M).Ok);
    }

    [TestMethod]
    public void CheckReplan_KeepsKnownIdsNumbersNew()
    {
        var current = Plan(("s1", new[] { "lamp" }), ("s2", new[] { "blinds" }), ("s3", new[] { "soil" }));
        var r = PlanGate.CheckReplan(JsonNode.Parse($$"""{"steps":{{StepsJson(("s2", "\"blinds\""), ("x", "\"pump\",\"soil\""))}},"reason":"Water it too"}""")!, current, M);
        Assert.IsTrue(r.Ok, r.Error); Assert.IsNull(r.Error);
        var steps = r.Steps!;
        CollectionAssert.AreEqual(new[] { "s2", "s4" }, steps.Select(s => s.Id).ToArray());
        Assert.AreEqual("Do x", steps[1].Text);
        CollectionAssert.AreEqual(new[] { "pump", "soil" }, steps[1].Devices.ToArray());
        Assert.AreEqual(3, current.Steps.Count, "CheckReplan doesn't change the plan");

        var two = PlanGate.CheckReplan(JsonNode.Parse($$"""{"steps":{{StepsJson(("new", "\"pump\""), ("new", "\"camera\""))}},"reason":"r"}""")!, current, M);
        CollectionAssert.AreEqual(new[] { "s4", "s5" }, two.Steps!.Select(s => s.Id).ToArray());

        var five = Plan(Enumerable.Range(1, 5).Select(i => ($"s{i}", new[] { "lamp" })).ToArray());
        var over = PlanGate.CheckReplan(JsonNode.Parse($$"""{"steps":{{StepsJson(("a", "\"pump\""), ("b", "\"soil\""))}},"reason":"r"}""")!, five, M);
        Assert.IsFalse(over.Ok); Assert.IsNull(over.Steps); StringAssert.Contains(over.Error, "6");
        var fits = PlanGate.CheckReplan(JsonNode.Parse($$"""{"steps":{{StepsJson(("s5", "\"pump\""), ("b", "\"soil\""))}},"reason":"r"}""")!, five, M);
        Assert.IsTrue(fits.Ok, fits.Error);
        CollectionAssert.AreEqual(new[] { "s5", "s6" }, fits.Steps!.Select(s => s.Id).ToArray());

        var garage = PlanGate.CheckReplan(JsonNode.Parse($$"""{"steps":{{StepsJson(("x", "\"garage\""))}},"reason":"r"}""")!, current, M);
        Assert.IsFalse(garage.Ok); StringAssert.Contains(garage.Error, "devices");

        var twice = PlanGate.CheckReplan(JsonNode.Parse($$"""{"steps":{{StepsJson(("s2", "\"blinds\""), ("s2", "\"lamp\""))}},"reason":"r"}""")!, current, M);
        Assert.IsFalse(twice.Ok); StringAssert.Contains(twice.Error, "s2");

        Assert.IsFalse(PlanGate.CheckReplan(JsonNode.Parse($$"""{"steps":{{StepsJson(("x", "\"pump\""))}}}""")!, current, M).Ok, "reason is required");
    }

    [TestMethod]
    public void Merge_ReplacesKnownAppendsNew()
    {
        var plan = Plan(("s1", new[] { "lamp" }), ("s2", new[] { "blinds" }), ("s3", new[] { "soil" }));
        plan.HorizonHours = 168;
        var copy = plan.Clone();
        var s2 = new PlanStep("s2", "Close the blinds halfway", new[] { "blinds" });
        var s4 = new PlanStep("s4", "Water the plant", new[] { "pump", "soil" });
        plan.Merge(new[] { s2, s4 });

        CollectionAssert.AreEqual(new[] { "s1", "s2", "s3", "s4" }, plan.Steps.Select(s => s.Id).ToArray());
        Assert.AreSame(s2, plan.Steps[1]); Assert.AreSame(s4, plan.Steps[3]);
        Assert.AreSame(s2, plan.Step("s2")); Assert.IsNull(plan.Step("s9"));
        Assert.AreEqual(168, plan.HorizonHours);

        CollectionAssert.AreEqual(new[] { "s1", "s2", "s3" }, copy.Steps.Select(s => s.Id).ToArray(), "Clone is independent");
        Assert.AreEqual("step s2", copy.Steps[1].Text);
        Assert.AreEqual(168, copy.HorizonHours);
    }

    // ---- ToolCatalog ----

    static string[] Names(IEnumerable<AITool> tools) => tools.Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

    static string[] Strings(JsonNode? array) => array?.AsArray().Select(x => (string)x!).ToArray() ?? Array.Empty<string>();

    [TestMethod]
    public void Tools_Names()
    {
        string[] Sorted(params string[] n) => n.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var actions = M.Devices.SelectMany(d => d.Actions).Select(a => a.Name).ToArray();

        CollectionAssert.AreEqual(Sorted("read_soil", "submit_plan", "cannot_do"), Names(ToolCatalog.PlanTools(M)));
        var run = ToolCatalog.RunTools(M);
        CollectionAssert.AreEqual(Sorted(actions.Concat(new[] { "schedule", "add_rule", "notify_user", "run_until", "propose_replan" }).ToArray()), Names(run));
        CollectionAssert.AreEqual(Sorted("set_lamp", "stop_pump", "notify_user", "propose_replan"), Names(ToolCatalog.FaultTurnTools(M)));

        var schemas = run.ToDictionary(t => t.Name, ToolCatalog.SchemaOf);
        CollectionAssert.Contains(Strings(schemas["set_lamp"]["required"]), "step");
        CollectionAssert.Contains(Strings(schemas["set_lamp"]["required"]), "brightness");
        foreach (var optional in new[] { "stop_pump", "read_soil" })
        {
            Assert.IsNotNull(schemas[optional]["properties"]!["step"], optional);
            CollectionAssert.DoesNotContain(Strings(schemas[optional]["required"]), "step", optional);
        }
        CollectionAssert.DoesNotContain(Strings(schemas["notify_user"]["required"]), "step");
        CollectionAssert.Contains(Strings(schemas["schedule"]["required"]), "step");
        CollectionAssert.Contains(Strings(schemas["add_rule"]["required"]), "step");

        var plan = ToolCatalog.PlanTools(M).ToDictionary(t => t.Name, ToolCatalog.SchemaOf);
        Assert.IsNull(plan["read_soil"]["properties"]!["step"], "planning reads carry no step");

        var fault = ToolCatalog.FaultTurnTools(M).ToDictionary(t => t.Name, ToolCatalog.SchemaOf);
        Assert.AreEqual("[0]", fault["set_lamp"]["properties"]!["brightness"]!["enum"]!.ToJsonString(), "the fault turn can only turn the lamp off");
        CollectionAssert.DoesNotContain(Strings(fault["set_lamp"]["required"]), "step");
        CollectionAssert.DoesNotContain(Strings(fault["stop_pump"]["required"]), "step");

        foreach (var t in run.Concat(ToolCatalog.PlanTools(M)).Concat(ToolCatalog.FaultTurnTools(M)))
        {
            Assert.IsInstanceOfType<AIFunctionDeclaration>(t);
            Assert.IsFalse(string.IsNullOrWhiteSpace(t.Description), t.Name);
            var s = ToolCatalog.SchemaOf(t);
            Assert.AreEqual("object", (string)s["type"]!, t.Name);
            Assert.AreEqual(false, (bool)s["additionalProperties"]!, t.Name);
        }

        // Building tools never touches the manifest's own schemas.
        Assert.IsNull(M.Action("set_lamp")!.Parameters["properties"]!["step"]);
        Assert.IsNull(M.Action("set_lamp")!.Parameters["properties"]!["brightness"]!["enum"]);
        CollectionAssert.DoesNotContain(Strings(M.Action("set_lamp")!.Parameters["required"]), "step");
    }

    // ---- Simulator keeps its contract on top of ArgValidator ----

    [TestMethod]
    public void Simulator_InvalidArgsBeatFailedDevice()
    {
        var room = RoomState.PostSetup(M);
        room.Failed.Add("lamp");
        var sim = new Simulator(M, ManifestLoader.Behavior("home"));
        var before = room.ToWire().ToJsonString();

        var bad = sim.Execute(new ActionCall("set_lamp", Obj("{\"brightness\":\"20%\"}"), "s1"), room);
        Assert.AreEqual(Outcome.Invalid, bad.Outcome); Assert.IsFalse(bad.Ok);
        Assert.AreEqual("'brightness' must be an integer", bad.Text);
        Assert.AreEqual(false, (bool)bad.ModelResult["ok"]!);

        Assert.AreEqual(Outcome.Failed, sim.Execute(new ActionCall("set_lamp", Obj("{\"brightness\":20}"), "s1"), room).Outcome);
        Assert.AreEqual(before, room.ToWire().ToJsonString());
    }
}
