/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：EventTests.cs
    文件功能描述：测试事件构造器与事件日志（序号、续传、数据行编码）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Microsoft.Extensions.Time.Testing;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class EventTests
{
    static readonly Clock Clk = new("2026-11-13T16:30:00", "Fri 16:30", 16.5);
    static readonly IReadOnlyList<PlanStep> Steps = new[] { new PlanStep("s1", "Dim the floor lamp to 20%", new[] { "lamp" }) };

    [TestMethod]
    public void Wire_FieldNamesMatchContract()
    {
        var (k, d) = Ev.Call("s1", "c1", "lamp", "set_lamp", new JsonObject { ["brightness"] = 20 }, "Floor lamp → 20%");
        var e = new LiveEvent(5, k, "r1", d);
        Assert.AreEqual("call", e.Kind);
        Assert.AreEqual("{\"step\":\"s1\",\"callId\":\"c1\",\"device\":\"lamp\",\"action\":\"set_lamp\",\"args\":{\"brightness\":20},\"label\":\"Floor lamp → 20%\",\"rid\":\"r1\"}", e.DataJson());
    }

    static (string kind, JsonObject data) Sample(string kind) => kind switch
    {
        "room" => Ev.Room(new JsonObject { ["env"] = new JsonObject { ["hour"] = 16.5 } }, Clk),
        "automations" => Ev.Automations(new JsonArray()),
        "planning" => Ev.Planning(),
        "reading" => Ev.Reading("soil"),
        "plan" => Ev.Plan(Steps, 2.5),
        "awaiting_approval" => Ev.AwaitingApproval("plan"),
        "cannot_do" => Ev.CannotDo("No such device", new[] { "Dim the lamp" }),
        "declined" => Ev.Declined(),
        "expired" => Ev.Expired(),
        "step" => Ev.Step("s1", "running"),
        "call" => Ev.Call("s1", "c1", "lamp", "set_lamp", new JsonObject(), "Floor lamp → 20%"),
        "state" => Ev.State(new StateChange("lamp.brightness", JsonValue.Create(0.2), 1200)),
        "result" => Ev.Result("c1", "s1", "lamp", true, "confirmed", "confirmed"),
        "fault" => Ev.Fault("lamp", "not responding"),
        "clock" => Ev.ClockEv(Clk),
        "timeline" => Ev.Timeline(new TimelineRow("Sat 18:00", "Floor lamp → 35%", true, "action", "lamp", "s1")),
        "replan" => Ev.Replan(Steps, "lamp is offline"),
        "say" => Ev.Say("Hello"),
        "summary" => Ev.Summary("Done."),
        "done" => Ev.Done("ok", new Usage(1, 2, 3), 7),
        "limit" => Ev.Limit("calls"),
        "error" => Ev.Error("model", "oops"),
        _ => throw new ArgumentException(kind),
    };

    [DataTestMethod]
    [DataRow("room", "state,clock,automations")] [DataRow("automations", "items")] [DataRow("planning", "")] [DataRow("reading", "device")]
    [DataRow("plan", "steps,horizonHours")] [DataRow("awaiting_approval", "kind")] [DataRow("cannot_do", "reason,suggestions")]
    [DataRow("declined", "")] [DataRow("expired", "")] [DataRow("step", "id,status")]
    [DataRow("call", "step,callId,device,action,args,label")] [DataRow("state", "path,value,ms")]
    [DataRow("result", "callId,step,device,ok,outcome,text")] [DataRow("fault", "device,text")] [DataRow("clock", "iso,label,hour")]
    [DataRow("timeline", "at,text,ok,kind")] [DataRow("replan", "steps,reason")] [DataRow("say", "delta")] [DataRow("summary", "text")]
    [DataRow("done", "outcome,usage,remainingToday")] [DataRow("limit", "kind")] [DataRow("error", "kind,message")]
    public void Wire_KindFields(string kind, string fields)
    {
        var (k, d) = Sample(kind);
        Assert.AreEqual(kind, k);
        var e = new LiveEvent(1, k, "r1", d);
        var keys = ((JsonObject)JsonNode.Parse(e.DataJson())!).Select(p => p.Key).ToArray();
        var expected = fields.Length == 0 ? new string[0] : fields.Split(',');
        CollectionAssert.AreEquivalent(expected.Append("rid").ToArray(), keys);
    }

    [TestMethod]
    public void Timeline_SendsOriginAndDeviceOnlyWhenSet()
    {
        var plain = Ev.Timeline(new TimelineRow("Sat 18:00", "x", true, "action", "lamp", "s1")).data;
        Assert.IsFalse(plain.ContainsKey("origin")); Assert.IsFalse(plain.ContainsKey("device"));
        var o = Ev.Timeline(new TimelineRow("Sat 18:00", "x", true, "notify", null, "s1", "held")).data;
        Assert.AreEqual("held", (string)o["origin"]!);
        Assert.IsTrue(o.ContainsKey("device")); Assert.IsNull(o["device"]);
        var d = Ev.Timeline(new TimelineRow("Sat 18:00", "x", true, "action", "pump", "s1", "rule")).data;
        Assert.AreEqual("pump", (string)d["device"]!);
    }

    [TestMethod]
    public void FromEdit_MarksTheEventAsEdit()
    {
        var (k, d) = Ev.FromEdit(Ev.State(new StateChange("lamp.brightness", JsonValue.Create(0.2), 0)));
        Assert.AreEqual("state", k);
        Assert.AreEqual("edit", (string)d["src"]!);
    }

    [TestMethod]
    public void Wire_RidOmittedWhenNull()
    {
        var (k, d) = Ev.Planning();
        Assert.AreEqual("{}", new LiveEvent(1, k, null, d).DataJson());
    }

    [TestMethod]
    public void Wire_NullStep()
    {
        var (_, c) = Ev.Call(null, "c1", "soil", "read_soil", new JsonObject(), "Soil moisture");
        var (_, r) = Ev.Result("c1", null, "soil", true, "reading", "34%");
        StringAssert.Contains(new LiveEvent(1, "call", null, c).DataJson(), "\"step\":null");
        StringAssert.Contains(new LiveEvent(2, "result", null, r).DataJson(), "\"step\":null");
    }

    [TestMethod]
    public void Wire_NullHorizon()
    {
        var (_, d) = Ev.Plan(Steps, null);
        StringAssert.Contains(new LiveEvent(1, "plan", null, d).DataJson(), "\"horizonHours\":null");
    }

    [TestMethod]
    public void Wire_PlanStepsAreCamelCase()
    {
        var (_, d) = Ev.Plan(Steps, null);
        StringAssert.Contains(new LiveEvent(1, "plan", null, d).DataJson(), "\"steps\":[{\"id\":\"s1\",\"text\":\"Dim the floor lamp to 20%\",\"devices\":[\"lamp\"]}]");
    }

    [TestMethod]
    public void Wire_OutcomeNames()
    {
        Assert.AreEqual("confirmed", Ev.Wire(Outcome.Confirmed));
        Assert.AreEqual("commanded", Ev.Wire(Outcome.Commanded));
        Assert.AreEqual("failed", Ev.Wire(Outcome.Failed));
        Assert.AreEqual("reading", Ev.Wire(Outcome.Reading));
        Assert.AreEqual("made_safe", Ev.Wire(Outcome.MadeSafe));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Ev.Wire(Outcome.Invalid));
    }

    [TestMethod]
    public void Ev_CopiesNodes()
    {
        var args = new JsonObject { ["brightness"] = 20 };
        var (_, d1) = Ev.Call("s1", "c1", "lamp", "set_lamp", args, "a");
        var (_, d2) = Ev.Call("s1", "c2", "lamp", "set_lamp", args, "b");
        var room = new RoomState();
        room.Set("lamp.args", args);
        var (_, st) = Ev.State(new StateChange("lamp.cfg", new JsonObject { ["x"] = 1 }, 0));
        room.Set("lamp.cfg", st["value"]);
        args["brightness"] = 99;
        Assert.AreEqual(20, (int)d1["args"]!["brightness"]!);
        Assert.AreEqual(20, (int)d2["args"]!["brightness"]!);
        Assert.AreEqual(20, (int)room.Get("lamp.args")!["brightness"]!);
    }

    [TestMethod]
    public void Ev_StateCopiesValue()
    {
        var v = new JsonObject { ["x"] = 1 };
        var (_, d) = Ev.State(new StateChange("lamp.cfg", v, 0));
        v["x"] = 2;
        Assert.AreEqual(1, (int)d["value"]!["x"]!);
    }

    [TestMethod]
    public void Ev_RoomCopiesState()
    {
        var state = new JsonObject { ["lamp"] = new JsonObject { ["on"] = true } };
        var (_, d) = Ev.Room(state, Clk);
        state["lamp"]!["on"] = false;
        Assert.IsTrue((bool)d["state"]!["lamp"]!["on"]!);
    }

    [TestMethod]
    public void Done_UsageShape()
    {
        var (_, d) = Ev.Done("ok", new Usage(1, 2, 3), 5);
        Assert.AreEqual("{\"promptTokens\":1,\"completionTokens\":2,\"calls\":3}", d["usage"]!.ToJsonString());
    }

    [TestMethod]
    public async Task Log_ResumeAfter()
    {
        var fake = new FakeTimeProvider();
        var log = new EventLog(fake);
        for (var i = 1; i <= 5; i++) log.Append("say", "r1", new JsonObject { ["delta"] = i.ToString() });
        Assert.AreEqual(5, log.LastSeq);
        var got = await log.WaitAfterAsync(3, TimeSpan.FromSeconds(1), CancellationToken.None);
        CollectionAssert.AreEqual(new long[] { 4, 5 }, got.Select(e => e.Seq).ToArray());

        var t = log.WaitAfterAsync(5, TimeSpan.FromMilliseconds(50), CancellationToken.None);
        Assert.IsFalse(t.IsCompleted);
        fake.Advance(TimeSpan.FromMilliseconds(50));
        Assert.AreEqual(0, (await t).Count);
    }

    [TestMethod]
    public async Task Log_WakesWaiter()
    {
        var fake = new FakeTimeProvider();
        var log = new EventLog(fake);
        for (var i = 0; i < 5; i++) log.Append("say", null, new JsonObject());
        var t = log.WaitAfterAsync(5, TimeSpan.FromMinutes(1), CancellationToken.None);
        Assert.IsFalse(t.IsCompleted);
        log.Append("say", null, new JsonObject());
        var got = await t.WaitAsync(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(6, got.Single().Seq);
    }

    [TestMethod]
    public async Task Log_CancelledWaitThrows()
    {
        var log = new EventLog(new FakeTimeProvider());
        using var cts = new CancellationTokenSource();
        var t = log.WaitAfterAsync(0, TimeSpan.FromMinutes(1), cts.Token);
        cts.Cancel();
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => t); // a subclass of OperationCanceledException
    }

    [TestMethod]
    public void Log_ConcurrentAppends_UniqueOrderedSeq()
    {
        var log = new EventLog(new FakeTimeProvider());
        Parallel.For(0, 8, new ParallelOptions { MaxDegreeOfParallelism = 8 }, _ =>
        {
            for (var i = 0; i < 500; i++) log.Append("say", null, new JsonObject());
        });
        var all = log.WaitAfterAsync(0, TimeSpan.Zero, CancellationToken.None).Result;
        Assert.AreEqual(4000, all.Count);
        for (var i = 0; i < all.Count; i++) Assert.AreEqual(i + 1, all[i].Seq);
        Assert.AreEqual(4000, log.LastSeq);
    }
}
