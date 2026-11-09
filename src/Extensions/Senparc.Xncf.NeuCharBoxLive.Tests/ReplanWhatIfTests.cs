/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ReplanWhatIfTests.cs
    文件功能描述：NeuCharBox Live 测试：ReplanWhatIfTests


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

/// <summary>
/// Task 8: re-plans, run_until with its fault turn, and live what-ifs, against a scripted model on a FakeTimeProvider.
/// Pacer waits advance the fake clock; every wait on the runner is bounded by a real-time guard.
/// </summary>
[TestClass]
public class ReplanWhatIfTests
{
    static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);
    const string MovieText = "Movie night, but keep an eye on the plant";
    const string WeekText = "I'm away for a week, look after the place";

    readonly FakeTimeProvider Fake = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    readonly InMemoryLiveStore Store = new();
    readonly ListLogger<RequestRunner> Log = new();
    readonly LiveOptions Opt = new();
    readonly LiveSession S;
    readonly InstantPacer Pacer;
    int _rids;

    public ReplanWhatIfTests()
    {
        var code = new InviteCode(1, "NCB-TEST-0001", "test", Fake.GetUtcNow(), 20, false, 0, null);
        S = new LiveSession("sess1", code, ManifestLoader.Load("home"), new EventLog(Fake), Fake.GetUtcNow());
        Pacer = new InstantPacer(Fake.Advance);
    }

    // ---- helpers ----

    static ScriptedChatClient Chat(params ChatResponse[] responses) => new(responses.Select(T).ToArray());

    static ScriptTurn T(ChatResponse r) => (_, _) => r;

    static object Step(string id, string text, params string[] devices) => new { id, text, devices };

    static ChatResponse SubmitPlan(params object[] steps) => Tool("submit_plan", new { steps });

    static ChatResponse SubmitPlan(double horizon, params object[] steps) => Tool("submit_plan", new { steps, horizon_hours = horizon });

    static ChatResponse MoviePlan() => SubmitPlan(Step("s1", "Dim the floor lamp to 20%", "lamp"), Step("s2", "Close the blinds", "blinds"),
        Step("s3", "Check the soil, and water only if it's dry", "soil", "pump"));

    static ChatResponse MovieRun() =>
        Tools(("set_lamp", new { brightness = 20, step = "s1" }), ("set_blinds", new { closed_percent = 100, step = "s2" }), ("read_soil", new { step = "s3" }));

    static ChatResponse Replan(string reason, params object[] steps) => Tool("propose_replan", new { steps, reason });

    static (string, object) WaterRule(string step = "s1") => ("add_rule", new
    {
        when = new { device = "soil", field = "moisture", op = "below", value = 30 },
        then = new { action = "run_pump", args = new { seconds = 6 } },
        step,
    });

    static (string, object) RunUntil(double hours) => ("run_until", new { hours });

    RequestRunner Runner(IChatClient chat) => new(_ => chat, Opt, Pacer, Fake, Store, new StubMeter(), Log);

    RequestRecord Request(string text = MovieText) => new($"r{++_rids}", text, S.Room.Clone(), Fake);

    Task Start(IChatClient chat, RequestRecord r) => Runner(chat).RunAsync(S, r, CancellationToken.None);

    async Task<IReadOnlyList<LiveEvent>> Of(RequestRecord r) =>
        (await S.Events.WaitAfterAsync(0, TimeSpan.Zero, CancellationToken.None)).Where(e => e.Rid == r.Rid).ToList();

    /// <summary>The nth event of that kind in the request (1-based), waiting for it.</summary>
    async Task<LiveEvent> WaitFor(string kind, RequestRecord r, int nth = 1)
    {
        using var guard = new CancellationTokenSource(Guard);
        long after = 0;
        var seen = 0;
        try
        {
            while (true)
            {
                var batch = await S.Events.WaitAfterAsync(after, TimeSpan.FromDays(1), guard.Token);
                foreach (var e in batch)
                    if (e.Kind == kind && e.Rid == r.Rid && ++seen == nth) return e;
                if (batch.Count > 0) after = batch[^1].Seq;
            }
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"no '{kind}' event #{nth} for {r.Rid}");
            throw;
        }
    }

    async Task Answer(RequestRecord r, int nth, Decision d)
    {
        await WaitFor("awaiting_approval", r, nth);
        Assert.IsNull(S.TryDecide(r.Rid, d));
    }

    /// <summary>Runs a request to its end, approving the plan card.</summary>
    async Task<IReadOnlyList<LiveEvent>> RunApproved(IChatClient chat, RequestRecord r)
    {
        var run = Start(chat, r);
        await Answer(r, 1, Decision.Approve);
        await run.WaitAsync(Guard);
        return await Of(r);
    }

    static string Str(JsonNode? n) => n?.GetValue<string>() ?? "-";

    static string Describe(LiveEvent e) => e.Kind switch
    {
        "reading" => $"reading {Str(e.Data["device"])}",
        "awaiting_approval" => $"awaiting_approval {Str(e.Data["kind"])}",
        "step" => $"step {Str(e.Data["id"])} {Str(e.Data["status"])}",
        "call" => $"call {Str(e.Data["step"])} {Str(e.Data["action"])}",
        "state" => $"state {Str(e.Data["path"])}",
        "result" => $"result {Str(e.Data["outcome"])}",
        "fault" => $"fault {Str(e.Data["device"])}",
        "timeline" => $"timeline {Str(e.Data["kind"])}",
        "limit" => $"limit {Str(e.Data["kind"])}",
        "error" => $"error {Str(e.Data["kind"])}",
        "done" => $"done {Str(e.Data["outcome"])}",
        _ => e.Kind,
    };

    /// <summary>Event descriptions, with each run of "say" collapsed into one.</summary>
    static List<string> Kinds(IEnumerable<LiveEvent> events)
    {
        var list = new List<string>();
        foreach (var d in events.Select(Describe))
            if (!(d == "say" && list.Count > 0 && list[^1] == "say")) list.Add(d);
        return list;
    }

    static List<string> ToolResults(IReadOnlyList<ChatMessage> messages) =>
        messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Select(r => r.Result?.ToString() ?? "").ToList();

    static string Outcome(IReadOnlyList<LiveEvent> ev) => Str(ev[^1].Data["outcome"]);

    /// <summary>The scripted demo's movie night, run to ok: the request a what-if replays.</summary>
    async Task<RequestRecord> MovieNight()
    {
        var r = Request();
        var ev = await RunApproved(Chat(Tool("read_soil", new { }), MoviePlan(), MovieRun(), Text("DONE"),
            Text("I dimmed the floor lamp to 20% and closed the blinds. The soil reads 34%, so I didn't water.")), r);
        Assert.AreEqual("ok", Outcome(ev));
        return r;
    }

    static (string, object) LampTo20 => ("set_lamp", new { brightness = 20, step = "s2" });

    /// <summary>A week away with the watering rule, run to ok: the request the fault-turn what-ifs replay.</summary>
    async Task<RequestRecord> WaterWeek()
    {
        var r = Request(WeekText);
        var ev = await RunApproved(Chat(
            SubmitPlan(168, Step("s1", "Water the plant when the soil is dry", "soil", "pump"), Step("s2", "Dim the floor lamp to 20%", "lamp")),
            Tools(LampTo20, WaterRule(), RunUntil(168)), Text("DONE"),
            Text("I dimmed the floor lamp and watered the plant when the soil got dry.")), r);
        Assert.AreEqual("ok", Outcome(ev));
        return r;
    }

    RequestRecord WhatIfRecord(RequestRecord prior) => new($"r{++_rids}", prior.Text, S.Room.Clone(), Fake);

    Task StartWhatIf(IChatClient chat, RequestRecord prior, string device, RequestRecord r) =>
        Runner(chat).RunWhatIfAsync(S, prior, device, r, CancellationToken.None);

    async Task<(RequestRecord R, IReadOnlyList<LiveEvent> Ev)> WhatIf(RequestRecord prior, string device, IChatClient chat)
    {
        var r = WhatIfRecord(prior);
        await StartWhatIf(chat, prior, device, r).WaitAsync(Guard);
        return (r, await Of(r));
    }

    static JsonObject Facts(ScriptedChatClient chat, int call)
    {
        var text = chat.Calls[call].Messages[^1].Text;
        var at = text.IndexOf(Prompts.FactsHeader, StringComparison.Ordinal);
        Assert.IsTrue(at >= 0, "the summary instruction carries the facts");
        return JsonNode.Parse(text[(at + Prompts.FactsHeader.Length)..])!.AsObject();
    }

    // ---- re-plans ----

    [TestMethod]
    public async Task Replan_ApproveMergesAndRuns()
    {
        var chat = Chat(MoviePlan(),
            Replan("You asked for it warm too", Step("t", "Warm the room to 22 °C", "thermostat")),
            Tool("set_thermostat", new { celsius = 22, step = "s4" }),
            Text("DONE"), Text("I set the thermostat to 22 °C."));
        var r = Request();
        var run = Start(chat, r);
        await Answer(r, 1, Decision.Approve);
        var replan = await WaitFor("replan", r);
        var ask = await WaitFor("awaiting_approval", r, 2);
        Assert.AreEqual("replan", Str(ask.Data["kind"]));
        Assert.IsTrue(ask.Seq > replan.Seq, "the card is asked after the replan event");
        Assert.IsNull(S.TryDecide(r.Rid, Decision.Approve));
        await run.WaitAsync(Guard);

        var steps = replan.Data["steps"]!.AsArray();
        Assert.AreEqual(1, steps.Count);
        Assert.AreEqual("s4", Str(steps[0]!["id"]), "a new step gets the next free id");
        Assert.AreEqual("Warm the room to 22 °C", Str(steps[0]!["text"]));
        Assert.AreEqual("[\"thermostat\"]", steps[0]!["devices"]!.ToJsonString());
        Assert.AreEqual("You asked for it warm too", Str(replan.Data["reason"]));

        var answer = ToolResults(chat.Calls[2].Messages)[^1];
        StringAssert.Contains(answer, "\"answer\":\"approved\"");
        StringAssert.Contains(answer, "\"id\":\"s4\"", "the model learns the final ids");

        var ev = await Of(r);
        var kinds = Kinds(ev);
        CollectionAssert.AreEqual(new[]
        {
            "replan", "awaiting_approval replan", "step s4 running", "call s4 set_thermostat", "state thermostat.target", "result confirmed",
        }, kinds.SkipWhile(k => k != "replan").Take(6).ToList(), string.Join(", ", kinds));
        Assert.AreEqual(22.0, (double)S.Room.Get("thermostat.target")!);
        Assert.AreEqual("ok", Outcome(ev));
        CollectionAssert.AreEqual(new[] { "s1", "s2", "s3", "s4" }, r.Plan!.Steps.Select(s => s.Id).ToList());
        Assert.AreEqual("done", Str(Facts(chat, 4)["steps"]![3]!["status"]));
        StringAssert.Contains(S.History.Single().Assistant, "4. Warm the room to 22 °C.");
    }

    [TestMethod]
    public async Task Replan_KnownIdReplaces_Over6Refused()
    {
        var chat = Chat(MoviePlan(),
            Replan("Warmth matters more than the blinds",
                Step("s2", "Warm the room to 22 °C", "thermostat"), Step("x", "Turn the floor lamp off later", "lamp")),
            Replan("More to do", Step("a", "Arm the door camera", "camera"), Step("b", "Close the blinds", "blinds"), Step("c", "Read the soil", "soil")),
            Tool("set_thermostat", new { celsius = 22, step = "s2" }),
            Text("DONE"), Text("I set the thermostat to 22 °C."));
        var r = Request();
        var run = Start(chat, r);
        await Answer(r, 1, Decision.Approve);
        var replan = await WaitFor("replan", r);
        await Answer(r, 2, Decision.Approve);
        await run.WaitAsync(Guard);

        CollectionAssert.AreEqual(new[] { "s2", "s4" }, replan.Data["steps"]!.AsArray().Select(s => Str(s!["id"])).ToList(),
            "a known id replaces its step; a new one gets s{n+1}");
        CollectionAssert.AreEqual(new[] { "thermostat" }, r.Plan!.Step("s2")!.Devices.ToList());
        Assert.AreEqual("Warm the room to 22 °C", r.Plan.Step("s2")!.Text);
        CollectionAssert.AreEqual(new[] { "s1", "s2", "s3", "s4" }, r.Plan.Steps.Select(s => s.Id).ToList(), "s2 replaced in place");

        var refused = ToolResults(chat.Calls[3].Messages)[^1];
        StringAssert.Contains(refused, "\"ok\":false");
        StringAssert.Contains(refused, "at most 6");
        var ev = await Of(r);
        Assert.AreEqual(1, ev.Count(e => e.Kind == "replan"), "a refused re-plan emits nothing");
        Assert.AreEqual(2, ev.Count(e => e.Kind == "awaiting_approval"));
        Assert.AreEqual(22.0, (double)S.Room.Get("thermostat.target")!);
        Assert.AreEqual("ok", Outcome(ev));
    }

    [TestMethod]
    public async Task Replan_DeclineContinues()
    {
        var chat = Chat(MoviePlan(),
            Replan("You asked for it warm too", Step("t", "Warm the room to 22 °C", "thermostat")),
            Tool("set_thermostat", new { celsius = 22, step = "s4" }),
            Text("DONE"), Text("I left the thermostat as it was."));
        var r = Request();
        var run = Start(chat, r);
        await Answer(r, 1, Decision.Approve);
        await Answer(r, 2, Decision.Decline);
        await run.WaitAsync(Guard);

        StringAssert.Contains(ToolResults(chat.Calls[2].Messages)[^1], "declined; carry on without these changes");
        var refused = ToolResults(chat.Calls[3].Messages)[^1];
        StringAssert.Contains(refused, "\"ok\":false");
        StringAssert.Contains(refused, "propose_replan");
        Assert.AreEqual(21.0, (double)S.Room.Get("thermostat.target")!);
        var ev = await Of(r);
        Assert.IsFalse(ev.Any(e => e.Kind is "declined" or "call"), "declining a re-plan emits no event, and nothing ran");
        Assert.AreEqual("ok", Outcome(ev));
        Assert.AreEqual(3, r.Plan!.Steps.Count);
        Assert.AreEqual("not_waiting", S.TryDecide(r.Rid, Decision.Approve));
    }

    [TestMethod]
    public async Task Replan_DroppingDevice_RemovesItsSchedulesAndRules()
    {
        var chat = Chat(
            SubmitPlan(168, Step("s1", "Put the floor lamp on in the evenings", "lamp"), Step("s2", "Water the plant when it's dry", "soil", "pump"),
                Step("s3", "Tell you if someone comes to the door", "camera")),
            Tools(("schedule", new { at = "18:00", action = "set_lamp", args = new { brightness = 35 }, step = "s1" }),
                ("schedule", new { at = "23:00", action = "set_lamp", args = new { brightness = 0 }, step = "s1" }),
                WaterRule("s2"),
                ("add_rule", new { when = new { device = "camera", field = "motion", op = "becomes", value = true }, then = new { notify = "someone at the door" }, step = "s3" })),
            Replan("Keep the room warm instead",
                Step("s1", "Keep the room at 20 °C", "thermostat"), Step("s2", "Tell you if the soil gets dry", "soil")),
            Text("DONE"), Text("I changed the plan."));
        var r = Request(WeekText);
        var run = Start(chat, r);
        await Answer(r, 1, Decision.Approve);
        await Answer(r, 2, Decision.Approve);
        await run.WaitAsync(Guard);

        // The 18:00 lamp schedule and the watering rule lost their device; the safe 23:00 lamp-off and the notify rule stay.
        Assert.AreEqual("23:00", S.Room.Schedules.Single().DailyAt);
        Assert.AreEqual("camera.motion", S.Room.Rules.Single().Path);
        var ev = await Of(r);
        var rows = ev.Where(e => e.Kind == "timeline").Select(e => e.Data).ToList();
        CollectionAssert.AreEqual(new[] { "Floor lamp schedule removed (not in the new plan)", "Water pump rule removed (not in the new plan)" },
            rows.Select(d => Str(d["text"])).ToList());
        Assert.IsTrue(rows.All(d => !(bool)d["ok"]! && Str(d["kind"]) == "action" && Str(d["at"]) == "Fri 16:30"));
        var replanAt = ev.ToList().FindIndex(e => e.Kind == "replan");
        Assert.IsTrue(ev.ToList().FindIndex(e => e.Kind == "timeline") > replanAt);
        var answer = ToolResults(chat.Calls[3].Messages)[^1];
        StringAssert.Contains(answer, "Floor lamp schedule removed");
        foreach (var d in S.Scene.Devices)
            foreach (var what in new[] { "schedule", "rule" })
                Assert.AreEqual(0, CopyGuard.Find($"{d.Name} {what} removed (not in the new plan)").Count);
        Assert.AreEqual("ok", Outcome(ev));
    }

    // ---- what-ifs ----

    [TestMethod]
    public async Task WhatIf_EventOrder_RoomPlanThenRun()
    {
        var prior = await MovieNight();
        var priorPlan = (await Of(prior)).Single(e => e.Kind == "plan");
        var chat = Chat(MovieRun(), Text("DONE"),
            Text("I dimmed the floor lamp to 20% and closed the blinds. The soil sensor didn't respond, so I can't say how the plant is."));
        var (r, ev) = await WhatIf(prior, "soil", chat);

        var kinds = Kinds(ev);
        CollectionAssert.AreEqual(new[] { "room", "automations", "plan", "step s1 running" }, kinds.Take(4).ToList(), string.Join(", ", kinds));
        Assert.IsFalse(ev.Any(e => e.Kind is "awaiting_approval" or "planning"), "the approved plan replays without asking again");
        Assert.IsTrue(JsonNode.DeepEquals(prior.Before.ToWire(), ev[0].Data["state"]), "the room goes back to just before the request");
        Assert.AreEqual("Fri 16:30", Str(ev[0].Data["clock"]!["label"]));
        Assert.IsTrue(JsonNode.DeepEquals(priorPlan.Data, ev[2].Data), "the same approved plan");
        CollectionAssert.AreEqual(new[]
        {
            "step s3 running", "reading soil", "call s3 read_soil", "fault soil", "state soil.status", "result failed", "step s3 failed",
        }, kinds.SkipWhile(k => k != "step s3 running").TakeWhile(k => k != "say").ToList(), string.Join(", ", kinds));
        var failed = ev.Single(e => e.Kind == "result" && Str(e.Data["device"]) == "soil").Data;
        Assert.IsFalse((bool)failed["ok"]!);
        Assert.AreEqual("not responding", Str(failed["text"]));
        Assert.AreEqual("not responding", Str(ev.Single(e => e.Kind == "fault").Data["text"]), "the demo adds the device name");
        Assert.AreEqual("failed", Outcome(ev));

        // The summary is never handed a soil reading: the facts mark the sensor failed.
        var facts = Facts(chat, 2);
        CollectionAssert.Contains(facts["failedDevices"]!.AsArray().Select(Str).ToList(), "soil");
        Assert.AreEqual("not responding", Str(facts["endReadings"]!["soil"]!["error"]));
        Assert.IsFalse(facts["calls"]!.AsArray().Any(c => Str(c!["outcome"]) == "reading"));
        Assert.IsNull(facts["untouchedFailedDevice"]);
        Assert.IsFalse(chat.Calls[0].Messages.Any(m => m.Text.Contains("not responding")), "the AI isn't told in advance");
        Assert.AreEqual(4, chat.Calls[0].Messages.Count, "the replay starts from the request and its plan, not the replayed outcome");

        // A what-if adds no turn, never becomes LastFinished, and is logged as a what-if that doesn't count.
        Assert.AreEqual(1, S.History.Count);
        Assert.AreSame(prior, S.LastFinished);
        Assert.IsNull(S.Active);
        Assert.AreEqual("soil", r.WhatIfDevice);
        var row = (await Store.ListAsync(1)).Single();
        Assert.IsTrue(row.Start.WhatIf);
        Assert.AreEqual("soil", row.Start.WhatIfDevice);
        Assert.AreEqual(MovieText, row.Start.Text);
        Assert.AreEqual("failed", row.End!.Outcome);
        Assert.AreEqual(1, await Store.CountTodayAsync(UsageDay.For(Fake.GetUtcNow()), null));

        // A second what-if on the same request replays it again from the same room.
        var (_, again) = await WhatIf(prior, "soil", Chat(MovieRun(), Text("DONE"), Text("The soil sensor didn't respond.")));
        Assert.IsTrue(JsonNode.DeepEquals(ev[0].Data, again[0].Data));
    }

    [TestMethod]
    public async Task WhatIf_UnusedDevice_FlaggedAtSummary()
    {
        var prior = await MovieNight();
        var chat = Chat(MovieRun(), Text("DONE"),
            Text("I dimmed the floor lamp, closed the blinds and read the soil at 34%. The plan didn't need the door camera; I've flagged it."));
        var (_, ev) = await WhatIf(prior, "camera", chat);

        Assert.IsFalse(ev.Any(e => e.Kind == "call" && Str(e.Data["device"]) == "camera"));
        var kinds = Kinds(ev);
        var fault = kinds.IndexOf("fault camera");
        Assert.IsTrue(fault > kinds.IndexOf("step s3 done"), string.Join(", ", kinds));
        CollectionAssert.AreEqual(new[] { "fault camera", "state camera.status", "say" }, kinds.Skip(fault).Take(3).ToList(), "the card turns red at summary time");
        Assert.AreEqual("fault", Str(ev.Single(e => e.Kind == "state" && Str(e.Data["path"]) == "camera.status").Data["value"]));
        Assert.AreEqual("fault", Str(S.Room.Get("camera.status")));
        StringAssert.Contains(chat.Calls[2].Messages[^1].Text, "Say the plan didn't need the door camera and that you've flagged it.");
        var facts = Facts(chat, 2);
        Assert.AreEqual("the door camera", Str(facts["untouchedFailedDevice"]));
        CollectionAssert.Contains(facts["failedDevices"]!.AsArray().Select(Str).ToList(), "camera");
        Assert.AreEqual("ok", Outcome(ev));
    }

    [TestMethod]
    public async Task WhatIf_CallsCappedAt14()
    {
        var prior = await MovieNight();
        var chat = new ScriptedChatClient(Enumerable.Range(0, 20).Select(_ => (ScriptTurn)((_, _) => Tool("read_soil", new { }))).ToArray());
        var (_, ev) = await WhatIf(prior, "lamp", chat);

        Assert.AreEqual(14, chat.Calls.Count);
        CollectionAssert.AreEqual(new[] { "limit calls", "fault lamp", "state lamp.status", "done limit" }, Kinds(ev).TakeLast(4).ToList(),
            "a what-if that ends early still flags its device");
        Assert.AreEqual(14, (int)ev[^1].Data["usage"]!["calls"]!);
    }

    [TestMethod]
    public async Task WhatIf_NextRequestClearsFault()
    {
        var prior = await MovieNight();
        await WhatIf(prior, "soil", Chat(MovieRun(), Text("DONE"), Text("The soil sensor didn't respond.")));
        Assert.IsTrue(S.Room.Faulted.Contains("soil"));
        Assert.AreEqual("fault", Str(S.Room.Get("soil.status")));

        var chat = Chat(SubmitPlan(Step("s1", "Check the soil", "soil")), Tool("read_soil", new { step = "s1" }), Text("DONE"), Text("The soil reads 34%."));
        var r = Request("Check the plant");
        Assert.AreEqual("fault", Str(r.Before.Get("soil.status")));
        var ev = await RunApproved(chat, r);

        var kinds = Kinds(ev);
        CollectionAssert.AreEqual(new[] { "state soil.status", "planning", "plan" }, kinds.Take(3).ToList(), string.Join(", ", kinds));
        Assert.AreEqual("online", Str(ev[0].Data["value"]));
        Assert.AreEqual(0, S.Room.Failed.Count + S.Room.Faulted.Count);
        Assert.AreEqual("online", Str(S.Room.Get("soil.status")));
        Assert.AreEqual("online", Str(r.Before.Get("soil.status")), "a later what-if of this request starts online too");
        Assert.AreEqual(0, r.Before.Failed.Count + r.Before.Faulted.Count);
        Assert.AreEqual("reading", Str(ev.Single(e => e.Kind == "result").Data["outcome"]));
        Assert.AreEqual("ok", Outcome(ev));

        // Nothing to clear: the next request starts with planning.
        var r2 = Request("Check the plant again");
        var ev2 = await RunApproved(Chat(SubmitPlan(Step("s1", "Check the soil", "soil")), Text("DONE"), Text("I left everything as it was.")), r2);
        Assert.AreEqual("planning", ev2[0].Kind);
    }

    [TestMethod]
    public async Task WhatIf_ReplanApproved_LeavesReplayedPlanAlone()
    {
        var prior = await MovieNight();
        var chat = Chat(Replan("You asked for it warm too", Step("t", "Warm the room to 22 °C", "thermostat")),
            Tool("set_thermostat", new { celsius = 22, step = "s4" }), Text("DONE"), Text("I set the thermostat to 22 °C."));
        var r = WhatIfRecord(prior);
        var run = StartWhatIf(chat, prior, "lamp", r);
        var ask = await WaitFor("awaiting_approval", r);
        Assert.AreEqual("replan", Str(ask.Data["kind"]), "a re-plan in a replay is asked as usual");
        Assert.IsNull(S.TryDecide(r.Rid, Decision.Approve));
        await run.WaitAsync(Guard);

        Assert.AreEqual(4, r.Plan!.Steps.Count);
        Assert.AreEqual(3, prior.Plan!.Steps.Count, "the replayed request keeps its own plan");
        Assert.AreEqual("ok", Outcome(await Of(r)));
        var (_, again) = await WhatIf(prior, "lamp", Chat(Text("DONE"), Text("I left everything as it was.")));
        Assert.AreEqual(3, again.Single(e => e.Kind == "plan").Data["steps"]!.AsArray().Count);
    }

    // ---- run_until and the fault turn ----

    [TestMethod]
    public async Task AwayWeek_FastForwardTimelineAndSummary()
    {
        ScriptedChatClient? chat = null;
        TimeSpan paceAtRunUntil = default, paceAfter = default;
        int callsAtRunUntil = 0, callsAfter = 0;
        chat = new ScriptedChatClient(
            T(SubmitPlan(168, Step("s1", "Put the floor lamp on in the evenings", "lamp"), Step("s2", "Water the plant when the soil is dry", "soil", "pump"),
                Step("s3", "Watch the door and tell you if someone comes", "camera"))),
            T(Tools(("set_door_camera", new { armed = true, step = "s3" }),
                ("schedule", new { at = "18:00", action = "set_lamp", args = new { brightness = 35 }, step = "s1" }),
                ("schedule", new { at = "23:00", action = "set_lamp", args = new { brightness = 0 }, step = "s1" }),
                WaterRule("s2"),
                ("add_rule", new { when = new { device = "camera", field = "motion", op = "becomes", value = true }, then = new { notify = "someone at the door" }, step = "s3" }))),
            (_, _) =>
            {
                paceAtRunUntil = Pacer.Total;
                callsAtRunUntil = chat!.Calls.Count;
                return Tool("run_until", new { hours = 168 });
            },
            (_, _) =>
            {
                paceAfter = Pacer.Total;
                callsAfter = chat!.Calls.Count;
                return Text("DONE");
            },
            T(Text("I kept the floor lamp on in the evenings and watered the plant when it got dry. I told you about the visitor on Tuesday.")));
        var ev = await RunApproved(chat, Request(WeekText));

        var clock = ev.Last(e => e.Kind == "clock").Data;
        Assert.AreEqual("Fri 16:30", Str(clock["label"]));
        Assert.AreEqual(16.5 + 168, (double)clock["hour"]!, 1e-9);
        Assert.AreEqual("2026-11-20T16:30:00", Str(clock["iso"]));
        var rows = ev.Where(e => e.Kind == "timeline").Select(e => e.Data).ToList();
        var visitor = rows.Single(d => Str(d["kind"]) == "notify");
        Assert.AreEqual("Tue 14:20", Str(visitor["at"]));
        Assert.AreEqual("someone at the door", Str(visitor["text"]));
        Assert.IsTrue(rows.Count(d => Str(d["text"]) == "Water pump → run 6 s" && (bool)d["ok"]!) >= 1);
        Assert.AreEqual(7, rows.Count(d => Str(d["text"]) == "Floor lamp → 35%"));
        Assert.AreEqual("ok", Outcome(ev));

        var fastForward = paceAfter - paceAtRunUntil;
        Assert.IsTrue(fastForward > TimeSpan.Zero && fastForward <= TimeSpan.FromSeconds(30), $"fast-forward paced {fastForward}");
        Assert.AreEqual(1, callsAfter - callsAtRunUntil, "no model call during fast-forward");

        // The model gets a compact report: repeats collapsed, and the end readings.
        var report = ToolResults(chat.Calls[3].Messages)[^1];
        var json = JsonNode.Parse(report)!;
        Assert.IsTrue((bool)json["ok"]!);
        var lines = json["rows"]!.AsArray().Select(Str).ToList();
        Assert.IsTrue(lines.Count <= 40);
        CollectionAssert.Contains(lines, "Fri 18:00 Floor lamp → 35% ×7, last Thu 18:00");
        CollectionAssert.Contains(lines, "Tue 14:20 sent you: someone at the door");
        Assert.AreEqual(Math.Round((double)S.Room.Get("soil.moisture")!), (double)json["endReadings"]!["soil"]!["moisture"]!,
            "a rule watched the soil, so the box polled it");
        var facts = Facts(chat, 4);
        Assert.AreEqual(1, (int)facts["timeline"]!["notify"]!);
        CollectionAssert.Contains(facts["timeline"]!["list"]!.AsArray().Select(Str).ToList(), "Tue 14:20 sent you: someone at the door");
    }

    [TestMethod]
    public async Task RunUntil_UnwatchedSensor_NotRead()
    {
        var chat = Chat(SubmitPlan(24, Step("s1", "Put the floor lamp on this evening", "lamp")),
            Tools(("schedule", new { at = "18:00", action = "set_lamp", args = new { brightness = 35 }, step = "s1" }), RunUntil(24)),
            Text("DONE"), Text("I put the floor lamp on at 18:00."));
        await RunApproved(chat, Request("Put the lamp on this evening"));

        var result = JsonNode.Parse(ToolResults(chat.Calls[2].Messages)[^1])!;
        Assert.AreEqual("not read", Str(result["endReadings"]!["soil"]), "no rule watched the soil, so nothing read it");
        Assert.AreEqual(0.35, (double)result["endReadings"]!["lamp"]!["brightness"]!, 1e-9);
        Assert.IsNull(S.Room.Observed["soil"]);
        Assert.AreEqual("not read", Str(Facts(chat, 3)["endReadings"]!["soil"]));

        // A rule on the soil means the box polls it: that reading is reported and becomes the last observed one.
        var chat2 = Chat(SubmitPlan(24, Step("s1", "Water the plant if it gets very dry", "soil", "pump")),
            Tools(("add_rule", new
            {
                when = new { device = "soil", field = "moisture", op = "below", value = 20 },
                then = new { action = "run_pump", args = new { seconds = 6 } },
                step = "s1",
            }), RunUntil(24)),
            Text("DONE"), Text("The soil stayed above 20%, so I didn't water."));
        await RunApproved(chat2, Request("Water the plant if it gets very dry"));

        var moisture = Math.Round((double)S.Room.Get("soil.moisture")!);
        var result2 = JsonNode.Parse(ToolResults(chat2.Calls[2].Messages)[^1])!;
        Assert.AreEqual(moisture, (double)result2["endReadings"]!["soil"]!["moisture"]!);
        var seen = S.Room.Observed["soil"]!;
        Assert.AreEqual(moisture, (double)seen["reading"]!["moisture"]!);
        Assert.AreEqual("Sun 16:30", Str(seen["at"]));
        Assert.AreEqual(moisture, (double)Facts(chat2, 3)["endReadings"]!["soil"]!["moisture"]!);
    }

    [TestMethod]
    public async Task FaultDuringFastForward_PausesOneTurn()
    {
        var prior = await WaterWeek();
        var chat = Chat(Tools(LampTo20, WaterRule(), RunUntil(168)),
            Tools(("notify_user", new { text = "The water pump isn't responding, so the plant may need you." }), ("set_lamp", new { brightness = 0 })),
            Text("DONE"), Text("I couldn't water the plant: the water pump didn't respond, so I let you know and turned the floor lamp off."));
        var (_, ev) = await WhatIf(prior, "pump", chat);

        var list = ev.ToList();
        var first = list.FindIndex(e => e.Kind == "timeline" && Str(e.Data["kind"]) == "fault");
        Assert.IsTrue(first > 0, string.Join(", ", Kinds(ev)));
        Assert.AreEqual("Water pump not responding", Str(list[first].Data["text"]));
        Assert.IsFalse((bool)list[first].Data["ok"]!);
        CollectionAssert.AreEqual(new[]
        {
            "timeline fault", "fault pump", "state pump.status", "step s1 failed", "timeline notify",
            "call - set_lamp", "state lamp.brightness", "state lamp.on", "result made_safe",
        }, Kinds(list.Skip(first)).Take(9).ToList(), string.Join(", ", Kinds(list.Skip(first))));
        Assert.AreEqual("not responding", Str(list[first + 1].Data["text"]));
        Assert.AreEqual("fault", Str(list[first + 2].Data["value"]));
        Assert.AreEqual(Str(list[first].Data["at"]), Str(list[first + 4].Data["at"]), "the clock is paused for the turn");
        Assert.AreEqual("The water pump isn't responding, so the plant may need you.", Str(list[first + 4].Data["text"]));

        // Exactly one extra model call: the fault turn, with its tools and the fault row. It counts in done.usage.
        Assert.AreEqual(4, chat.Calls.Count);
        Assert.AreEqual(4, (int)ev[^1].Data["usage"]!["calls"]!);
        var turn = chat.Calls[1];
        CollectionAssert.AreEquivalent(ToolCatalog.FaultTurnTools(S.Scene).Select(t => t.Name).ToList(), turn.Options!.Tools!.Select(t => t.Name).ToList());
        Assert.AreEqual(Opt.MaxCompletionTokens, turn.Options.MaxOutputTokens);
        StringAssert.Contains(turn.Messages[^1].Text, "Water pump not responding");
        StringAssert.Contains(turn.Messages[^1].Text,
            "The clock is paused. You may make devices safe or notify the user without approval, or propose a re-plan. Reply DONE otherwise.");

        // The clock resumes; later hits on the pump only log failed rows; no second fault turn.
        var after = list.Skip(first + 9).ToList();
        Assert.IsTrue(after.Any(e => e.Kind == "clock"));
        var later = after.Where(e => e.Kind == "timeline" && Str(e.Data["text"]) == "Water pump not responding").ToList();
        Assert.IsTrue(later.Count >= 1);
        Assert.IsTrue(later.All(e => !(bool)e.Data["ok"]!));
        Assert.AreEqual(1, ev.Count(e => e.Kind == "fault"));
        Assert.AreEqual(1, ev.Count(e => e.Kind == "step" && Str(e.Data["id"]) == "s1" && Str(e.Data["status"]) == "failed"), "failed once, and it stays failed");
        Assert.IsFalse(after.Any(e => e.Kind == "step" && Str(e.Data["id"]) == "s1"));
        Assert.IsFalse(after.Any(e => e.Kind == "state" && Str(e.Data["path"]).StartsWith("lamp.")), "the turn's changes aren't sent twice");
        Assert.AreEqual("Fri 16:30", Str(list.Last(e => e.Kind == "clock").Data["label"]));
        Assert.AreEqual(0.0, (double)S.Room.Get("lamp.brightness")!);

        // The run goes on with a report of what the paused turn did.
        var report = ToolResults(chat.Calls[2].Messages)[^1];
        StringAssert.Contains(report, "Water pump not responding");
        StringAssert.Contains(report, "notify_user");
        Assert.AreEqual("failed", Outcome(ev), "a device that didn't respond fails the run");
    }

    [TestMethod]
    public async Task FaultTurn_ReplanExpired_EndsExpired()
    {
        var prior = await WaterWeek();
        var chat = Chat(Tools(LampTo20, WaterRule(), RunUntil(168)),
            Replan("The water pump isn't responding", Step("t", "Tell you when the soil gets dry", "soil")));
        var r = WhatIfRecord(prior);
        var run = StartWhatIf(chat, prior, "pump", r);
        var ask = await WaitFor("awaiting_approval", r);
        Assert.AreEqual("replan", Str(ask.Data["kind"]));
        Fake.Advance(Opt.ApprovalWait);
        await run.WaitAsync(Guard);

        var list = (await Of(r)).ToList();
        var replanAt = list.FindIndex(e => e.Kind == "replan");
        Assert.IsTrue(replanAt > list.FindIndex(e => e.Kind == "fault"));
        Assert.IsFalse(list.Skip(replanAt).Any(e => e.Kind == "clock"), "fast-forward stopped: no later clock");
        CollectionAssert.AreEqual(new[] { "expired", "done expired" }, Kinds(list).TakeLast(2).ToList());
        Assert.AreEqual(2, chat.Calls.Count, "nothing after the expired card");
        Assert.IsTrue(S.Room.Hour < 16.5 + 168);
        Assert.AreEqual(S.Room.Hour, (double)list.Last(e => e.Kind == "clock").Data["hour"]!, 1e-9, "the clock stopped at the fault");
        Assert.AreEqual("expired", r.Outcome);
        Assert.AreEqual(2, r.Plan!.Steps.Count);
        Assert.AreEqual("not_waiting", S.TryDecide(r.Rid, Decision.Approve));
        Assert.IsNull(S.Active);
    }

    [TestMethod]
    public async Task FaultTurn_NonSafeWrite_Refused()
    {
        var prior = await WaterWeek();
        var chat = Chat(Tools(LampTo20, WaterRule(), RunUntil(168)),
            Tools(("set_lamp", new { brightness = 60, step = "s2" }), ("set_blinds", new { closed_percent = 100 }), ("read_soil", new { })),
            Text("DONE"), Text("The water pump didn't respond."));
        var (_, ev) = await WhatIf(prior, "pump", chat);

        var list = ev.ToList();
        var fault = list.FindIndex(e => e.Kind == "fault");
        Assert.IsTrue(fault > 0);
        Assert.IsFalse(list.Skip(fault).Any(e => e.Kind is "call" or "result" or "reading"), "the paused turn ran nothing");
        Assert.AreEqual(0.2, (double)S.Room.Get("lamp.brightness")!, 1e-9, "set_lamp 60 is in the plan but isn't safe");
        Assert.AreEqual(0.0, (double)S.Room.Get("blinds.closed")!);
        var results = JsonNode.Parse(ToolResults(chat.Calls[2].Messages)[^1])!["pauses"]![0]!["results"]!.AsArray().Select(Str).ToList();
        Assert.AreEqual(3, results.Count);
        StringAssert.StartsWith(results[0], "set_lamp: ");
        StringAssert.Contains(results[0], "\"ok\":false");
        StringAssert.Contains(results[0], "only safe-state calls");
        StringAssert.Contains(results[0], "set_lamp with brightness 0");
        StringAssert.Contains(results[0], "stop_pump");
        StringAssert.Contains(results[1], "set_blinds isn't available while the clock is paused");
        StringAssert.Contains(results[2], "read_soil isn't available while the clock is paused");
        Assert.AreEqual(4, chat.Calls.Count);
    }

    // ---- fix round 1 ----

    [TestMethod]
    public async Task WhatIf_DeclinedOrExpiredPrior_RunsNothing()
    {
        var declined = Request();
        var run = Start(Chat(MoviePlan()), declined);
        await Answer(declined, 1, Decision.Decline);
        await run.WaitAsync(Guard);
        var expired = Request();
        run = Start(Chat(MoviePlan()), expired);
        await WaitFor("awaiting_approval", expired);
        Fake.Advance(Opt.ApprovalWait);
        await run.WaitAsync(Guard);
        Assert.IsNull(declined.Plan, "a plan the visitor never approved isn't kept as approved");
        Assert.IsNull(expired.Plan);

        foreach (var prior in new[] { declined, expired })
        {
            var room = S.Room.ToWire();
            var chat = Chat(MovieRun(), Text("DONE"), Text("I dimmed the floor lamp."));
            var (r, ev) = await WhatIf(prior, "soil", chat);
            CollectionAssert.AreEqual(new[] { "error internal", "done failed" }, Kinds(ev), "refused: nothing replays");
            Assert.AreEqual(0, chat.Calls.Count);
            Assert.IsTrue(JsonNode.DeepEquals(room, S.Room.ToWire()), "no device changed");
            Assert.AreEqual(0, S.Room.Failed.Count);
            Assert.IsNull(r.Plan);
        }
        Assert.AreSame(expired, S.LastFinished);
        Assert.IsNull(S.Active);
    }

    [TestMethod]
    public async Task RunUntil_UntouchedFailedDevice_StaysHidden()
    {
        var prior = await WaterWeek();
        var chat = Chat(Tools(LampTo20, WaterRule(), RunUntil(168)), Text("DONE"),
            Text("I dimmed the floor lamp and watered the plant. The plan didn't need the blinds controller; I've flagged it."));
        var (_, ev) = await WhatIf(prior, "blinds", chat);

        var report = ToolResults(chat.Calls[1].Messages)[^1];
        Assert.IsFalse(report.Contains("not responding"), report);
        Assert.AreEqual("not read", Str(JsonNode.Parse(report)!["endReadings"]!["blinds"]), "the AI isn't told in advance");
        var list = ev.ToList();
        var fault = list.FindIndex(e => e.Kind == "fault");
        Assert.IsTrue(fault > list.FindLastIndex(e => e.Kind == "clock"), "no fault event while time passed");
        Assert.IsTrue(fault < list.FindIndex(e => e.Kind == "say"), "the card turns red at summary time");
        Assert.AreEqual("blinds", Str(list[fault].Data["device"]));
        Assert.AreEqual("the blinds controller", Str(Facts(chat, 2)["untouchedFailedDevice"]));
    }

    [TestMethod]
    public async Task Replan_KeepsSchedulesAndRulesFromEarlierRequests()
    {
        await RunApproved(Chat(SubmitPlan(Step("s1", "Water the plant when it's dry", "soil", "pump")), Tools(WaterRule()), Text("DONE"),
            Text("I'll water the plant when the soil gets dry.")), Request("Water the plant when it's dry"));

        var chat = Chat(SubmitPlan(Step("s1", "Put the floor lamp on in the evenings", "lamp")),
            Tools(("schedule", new { at = "18:00", action = "set_lamp", args = new { brightness = 35 }, step = "s1" })),
            Replan("Keep the room warm instead", Step("s1", "Keep the room at 20 °C", "thermostat")),
            Text("DONE"), Text("I changed the plan."));
        var b = Request("Put the lamp on in the evenings");
        var run = Start(chat, b);
        await Answer(b, 1, Decision.Approve);
        await Answer(b, 2, Decision.Approve);
        await run.WaitAsync(Guard);

        Assert.AreEqual("soil.moisture", S.Room.Rules.Single().Path, "the earlier request's rule isn't this re-plan's to remove");
        Assert.AreEqual(0, S.Room.Schedules.Count, "this request's own lamp schedule lost its device");
        CollectionAssert.AreEqual(new[] { "Floor lamp schedule removed (not in the new plan)" },
            (await Of(b)).Where(e => e.Kind == "timeline").Select(e => Str(e.Data["text"])).ToList());
    }

    [TestMethod]
    public async Task RunUntil_HeldToTheApprovedHorizon()
    {
        var chat = Chat(SubmitPlan(24, Step("s1", "Put the floor lamp on this evening", "lamp")),
            Tool("run_until", new { hours = 48 }), Tool("run_until", new { hours = 10 }), Text("DONE"), Text("I looked after the place for a day."));
        var ev = await RunApproved(chat, Request("Look after the place today"));

        var first = JsonNode.Parse(ToolResults(chat.Calls[2].Messages)[^1])!;
        Assert.IsTrue((bool)first["ok"]!);
        Assert.AreEqual(24.0, (double)first["hours"]!, "clamped to the approved 24 hours");
        StringAssert.Contains(Str(first["note"]), "24 hours");
        var second = ToolResults(chat.Calls[3].Messages)[^1];
        StringAssert.Contains(second, "\"ok\":false");
        StringAssert.Contains(second, "24 hours");
        Assert.AreEqual(16.5 + 24, S.Room.Hour, 1e-9);
        Assert.AreEqual("Sat 16:30", Str(ev.Last(e => e.Kind == "clock").Data["label"]));
        Assert.AreEqual("ok", Outcome(ev));

        // A plan that covers no period of time can't fast-forward.
        var none = Chat(SubmitPlan(Step("s1", "Dim the floor lamp to 20%", "lamp")), Tool("run_until", new { hours = 24 }), Text("DONE"),
            Text("I left the floor lamp as it was."));
        var ev2 = await RunApproved(none, Request());
        var refused = ToolResults(none.Calls[2].Messages)[^1];
        StringAssert.Contains(refused, "\"ok\":false");
        StringAssert.Contains(refused, "no period of time");
        Assert.IsFalse(ev2.Any(e => e.Kind is "clock" or "timeline"));
        Assert.AreEqual(16.5 + 24, S.Room.Hour, 1e-9);
    }

    [TestMethod]
    public async Task RunUntil_PacingCountsAgainstWorkTime()
    {
        Opt.MaxWork = TimeSpan.FromSeconds(10); // a quiet week paces 16.8 s
        var chat = Chat(SubmitPlan(168, Step("s1", "Keep an eye on the place", "lamp")), Tool("run_until", new { hours = 168 }),
            Text("DONE"), Text("I kept an eye on the place."));
        var ev = await RunApproved(chat, Request(WeekText));

        CollectionAssert.AreEqual(new[] { "limit time", "done limit" }, Kinds(ev).TakeLast(2).ToList(), string.Join(", ", Kinds(ev)));
        Assert.IsTrue(S.Room.Hour < 16.5 + 168);
        Assert.IsTrue(Pacer.Total < TimeSpan.FromSeconds(11), $"paced {Pacer.Total}");
        Assert.AreEqual(2, chat.Calls.Count);
    }

    [TestMethod]
    public async Task FaultTurn_ApprovalWaitIsNotWorkTime()
    {
        var prior = await WaterWeek();
        var chat = Chat(Tools(LampTo20, WaterRule(), RunUntil(168)),
            Replan("The water pump isn't responding", Step("t", "Tell you when the soil gets dry", "soil")),
            Text("DONE"), Text("The water pump didn't respond, so I changed the plan."));
        var r = WhatIfRecord(prior);
        var run = StartWhatIf(chat, prior, "pump", r);
        await WaitFor("awaiting_approval", r);
        Fake.Advance(TimeSpan.FromMinutes(9)); // longer than the 3-minute work time, inside the 10-minute wait
        Assert.IsNull(S.TryDecide(r.Rid, Decision.Approve));
        await run.WaitAsync(Guard);

        var ev = await Of(r);
        Assert.IsFalse(ev.Any(e => e.Kind == "limit"), string.Join(", ", Kinds(ev)));
        Assert.AreEqual("Fri 16:30", Str(ev.Last(e => e.Kind == "clock").Data["label"]), "the week ran to its end");
        Assert.AreEqual(3, r.Plan!.Steps.Count);
        Assert.AreEqual(4, chat.Calls.Count);
    }

    [TestMethod]
    public async Task WhatIf_EndsEarly_StillFlagsTheDevice()
    {
        var prior = await MovieNight();

        // No answer to a re-plan: expired.
        var r = WhatIfRecord(prior);
        var run = StartWhatIf(Chat(Replan("You asked for it warm too", Step("t", "Warm the room to 22 °C", "thermostat"))), prior, "camera", r);
        await WaitFor("awaiting_approval", r);
        Fake.Advance(Opt.ApprovalWait);
        await run.WaitAsync(Guard);
        CollectionAssert.AreEqual(new[] { "expired", "fault camera", "state camera.status", "done expired" }, Kinds(await Of(r)).TakeLast(4).ToList());

        // The model service fails: error.
        var (_, ev) = await WhatIf(prior, "camera", new ScriptedChatClient((_, _) => throw new HttpRequestException("503 Service Unavailable")));
        CollectionAssert.AreEqual(new[] { "error model", "fault camera", "state camera.status", "done failed" }, Kinds(ev).TakeLast(4).ToList());
        Assert.AreEqual("fault", Str(S.Room.Get("camera.status")));
        Assert.AreEqual("not responding", Str(ev.Single(e => e.Kind == "fault").Data["text"]));
    }

    [TestMethod]
    public async Task FaultTurn_CountsTowardTheCallCap()
    {
        var prior = await WaterWeek();
        Opt.MaxCallsWhatIf = 10; // the script is sized for this cap
        var turns = Enumerable.Range(0, 8).Select(_ => T(Tool("read_soil", new { }))).ToList(); // calls 1–8
        turns.Add(T(Tools(WaterRule(), RunUntil(168))));                                         // call 9
        turns.Add(T(Tool("notify_user", new { text = "The water pump isn't responding." })));     // call 10: the fault turn
        turns.Add(T(Text("DONE")));                                                              // would be call 11
        var chat = new ScriptedChatClient(turns.ToArray());
        var (_, ev) = await WhatIf(prior, "pump", chat);

        Assert.AreEqual(10, chat.Calls.Count);
        CollectionAssert.Contains(chat.Calls[9].Options!.Tools!.Select(t => t.Name).ToList(), "notify_user");
        CollectionAssert.DoesNotContain(chat.Calls[9].Options!.Tools!.Select(t => t.Name).ToList(), "run_until", "call 10 is the fault turn");
        Assert.AreEqual(10, (int)ev[^1].Data["usage"]!["calls"]!);
        CollectionAssert.AreEqual(new[] { "limit calls", "done limit" }, Kinds(ev).TakeLast(2).ToList());
    }
}
