/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RunnerTests.cs
    文件功能描述：测试请求执行器：规划、批准、执行、总结与各类结束结果


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增测试：模型调用失败与请求失败的日志只有类型、状态码与清洗后的首行，不带异常对象

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
/// The AI loop against a scripted model, on a FakeTimeProvider. Pacer waits advance the fake clock. Every wait on the
/// runner is bounded by a real-time guard, so a hang fails the test instead of freezing the run.
/// </summary>
[TestClass]
public class RunnerTests
{
    static readonly TimeSpan Guard = TimeSpan.FromSeconds(20);
    const string MovieText = "Movie night, but keep an eye on the plant";

    readonly FakeTimeProvider Fake = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    readonly InMemoryLiveStore Store = new();
    readonly StubMeter Meter = new();
    readonly ListLogger<RequestRunner> Log = new();
    readonly LiveOptions Opt = new();

    /// <summary>The limits the next request's snapshot carries.</summary>
    LiveLimits Limits = LiveLimits.Defaults;
    LiveSession S;
    int _rids;

    public RunnerTests()
    {
        var code = new InviteCode(1, "NCB-TEST-0001", "test", Fake.GetUtcNow(), 20, false, 0, null);
        S = new LiveSession("sess1", code, ManifestLoader.Load("home"), new EventLog(Fake), Fake.GetUtcNow());
    }

    // ---- helpers ----

    static ScriptedChatClient Chat(params ChatResponse[] responses) => new(responses.Select(T).ToArray());

    static ScriptTurn T(ChatResponse r) => (_, _) => r;

    static object Step(string id, string text, params string[] devices) => new { id, text, devices };

    static ChatResponse SubmitPlan(params object[] steps) => Tool("submit_plan", new { steps });

    static ChatResponse LampPlan() => SubmitPlan(Step("s1", "Dim the floor lamp to 20%", "lamp"));

    static ChatResponse SetLamp20() => Tool("set_lamp", new { brightness = 20, step = "s1" });

    RequestRunner Runner(IPacer? pacer = null) =>
        new(Opt, pacer ?? new InstantPacer(Fake.Advance), Fake, Store, Meter, UsageDay.Utc, Log);

    /// <summary>The request's snapshot: plan and run on <paramref name="chat"/>, with <see cref="Limits"/>.</summary>
    LiveSnapshot Snap(IChatClient chat) => Snapshots.On(chat, Limits);

    static string Says(IEnumerable<LiveEvent> ev) => string.Concat(ev.Where(e => e.Kind == "say").Select(e => Str(e.Data["delta"])));

    RequestRecord Request(string text = MovieText) => new($"r{++_rids}", text, S.Room.Clone(), Fake);

    Task Start(IChatClient chat, RequestRecord r) => Runner().RunAsync(S, r, Snap(chat), CancellationToken.None);

    async Task<IReadOnlyList<LiveEvent>> Of(RequestRecord r) =>
        (await S.Events.WaitAfterAsync(0, TimeSpan.Zero, CancellationToken.None)).Where(e => e.Rid == r.Rid).ToList();

    async Task<LiveEvent> WaitFor(string kind, RequestRecord r)
    {
        using var guard = new CancellationTokenSource(Guard);
        long after = 0;
        try
        {
            while (true)
            {
                var batch = await S.Events.WaitAfterAsync(after, TimeSpan.FromDays(1), guard.Token);
                if (batch.FirstOrDefault(e => e.Kind == kind && e.Rid == r.Rid) is { } hit) return hit;
                if (batch.Count > 0) after = batch[^1].Seq;
            }
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"no '{kind}' event for {r.Rid}");
            throw;
        }
    }

    async Task Approve(RequestRecord r)
    {
        await WaitFor("awaiting_approval", r);
        Assert.IsNull(S.TryDecide(r.Rid, Decision.Approve));
    }

    async Task<IReadOnlyList<LiveEvent>> RunApproved(IChatClient chat, RequestRecord? r = null)
    {
        r ??= Request();
        var run = Start(chat, r);
        await Approve(r);
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
        "result" => Str(e.Data["outcome"]) == "reading" ? $"result reading {Str(e.Data["text"])}" : $"result {Str(e.Data["outcome"])}",
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

    static JsonObject Facts(ScriptedChatClient chat, int call)
    {
        var text = chat.Calls[call].Messages[^1].Text;
        var at = text.IndexOf(Prompts.FactsHeader, StringComparison.Ordinal);
        Assert.IsTrue(at >= 0, "the summary instruction carries the facts");
        return JsonNode.Parse(text[(at + Prompts.FactsHeader.Length)..])!.AsObject();
    }

    int GuardHits() => Log.Entries.Count(e => e.Id.Name == "CopyGuardHit");

    sealed class QuietSink : IFastForwardSink
    {
        public Task ClockAsync(Clock c) => Task.CompletedTask;
        public Task StateAsync(StateChange s) => Task.CompletedTask;
        public Task RowAsync(TimelineRow r) => Task.CompletedTask;
        public Task<bool> FirstFaultAsync(string device, TimelineRow r) => Task.FromResult(true);
    }

    // ---- the brief's tests ----

    [TestMethod]
    public async Task MovieNight_PlanApproveRunSummary()
    {
        const string summary = "I dimmed the floor lamp to 20% and closed the blinds. The soil reads 34%, so I didn't water.";
        var chat = Chat(
            Tool("read_soil", new { }),
            SubmitPlan(Step("s1", "Dim the floor lamp to 20%", "lamp"), Step("s2", "Close the blinds", "blinds"),
                Step("s3", "Check the soil, and water only if it's dry", "soil", "pump")),
            Tools(("set_lamp", new { brightness = 20, step = "s1" }), ("set_blinds", new { closed_percent = 100, step = "s2" }),
                ("read_soil", new { step = "s3" })),
            Text("DONE"),
            Text(summary));
        var r = Request();
        var ev = await RunApproved(chat, r);

        CollectionAssert.AreEqual(new[]
        {
            "planning", "reading soil", "plan", "awaiting_approval plan",
            "step s1 running", "call s1 set_lamp", "state lamp.on", "state lamp.brightness", "result confirmed",
            "step s1 done", "step s2 running", "call s2 set_blinds", "state blinds.closed", "result confirmed",
            "step s2 done", "step s3 running", "reading soil", "call s3 read_soil", "result reading 34%", "step s3 done",
            "say", "summary", "done ok",
        }, Kinds(ev), string.Join(", ", Kinds(ev)));

        var lampCall = ev.First(e => e.Kind == "call");
        Assert.AreEqual("{\"brightness\":20}", lampCall.Data["args"]!.ToJsonString(), "step is moved out of the args");
        Assert.AreEqual("Floor lamp → 20%", Str(lampCall.Data["label"]));
        Assert.AreEqual(summary, string.Concat(ev.Where(e => e.Kind == "say").Select(e => Str(e.Data["delta"]))));
        Assert.AreEqual(summary, Str(ev.Single(e => e.Kind == "summary").Data["text"]));

        var done = ev[^1].Data;
        Assert.AreEqual(5, (int)done["usage"]!["calls"]!);
        Assert.AreEqual(500, (int)done["usage"]!["promptTokens"]!);
        Assert.AreEqual(100, (int)done["usage"]!["completionTokens"]!);
        Assert.AreEqual(19, (int)done["remainingToday"]!);

        Assert.AreEqual(0.2, (double)S.Room.Get("lamp.brightness")!);
        Assert.AreEqual(1.0, (double)S.Room.Get("blinds.closed")!);
        Assert.AreEqual(1, S.History.Count);
        Assert.AreEqual(MovieText, S.History[0].User);
        StringAssert.StartsWith(S.History[0].Assistant, "Plan: 1. Dim the floor lamp to 20%");
        StringAssert.EndsWith(S.History[0].Assistant, $"Result: {summary}");
        Assert.IsNull(S.Active);
        Assert.AreSame(r, S.LastFinished);
        Assert.AreEqual("ok", r.Outcome);

        var row = (await Store.ListAsync(10)).Single();
        Assert.AreEqual(MovieText, row.Start.Text);
        Assert.AreEqual("ok", row.End!.Outcome);
        Assert.AreEqual(5, row.End.Calls);
        Assert.AreEqual(summary, row.End.Summary);
        StringAssert.Contains(row.End.PlanJson!, "Close the blinds");
        Assert.AreEqual(0, GuardHits());
    }

    [TestMethod]
    public async Task Planning_NeverChangesDevices()
    {
        var chat = Chat(Tool("set_lamp", new { brightness = 50 }), Tool("schedule", new { at = "18:00", action = "set_lamp", args = new { brightness = 50 }, step = "s1" }), LampPlan());
        var r = Request();
        var run = Start(chat, r);
        await WaitFor("awaiting_approval", r);
        Assert.IsNull(S.TryDecide(r.Rid, Decision.Decline));
        await run.WaitAsync(Guard);

        StringAssert.Contains(ToolResults(chat.Calls[1].Messages).Single(), "planning can't change devices");
        Assert.AreEqual(2, ToolResults(chat.Calls[2].Messages).Count);
        StringAssert.Contains(ToolResults(chat.Calls[2].Messages)[1], "planning can't change devices");
        var ev = await Of(r);
        Assert.IsFalse(ev.Any(e => e.Kind is "call" or "state" or "result"));
        Assert.IsTrue(JsonNode.DeepEquals(r.Before.ToWire(), S.Room.ToWire()));
        Assert.AreEqual(0, S.Room.Schedules.Count);
    }

    [TestMethod]
    public async Task Decline_NothingRuns()
    {
        var chat = Chat(LampPlan());
        var r = Request();
        var run = Start(chat, r);
        await WaitFor("awaiting_approval", r);
        Assert.IsNull(S.TryDecide(r.Rid, Decision.Decline));
        await run.WaitAsync(Guard);

        var ev = await Of(r);
        CollectionAssert.AreEqual(new[] { "planning", "plan", "awaiting_approval plan", "declined", "done declined" }, Kinds(ev));
        Assert.IsTrue(JsonNode.DeepEquals(r.Before.ToWire(), S.Room.ToWire()));
        Assert.AreEqual(1, chat.Calls.Count);
        Assert.AreEqual("not_waiting", S.TryDecide(r.Rid, Decision.Approve));
        StringAssert.Contains(S.History.Single().Assistant, "Plan: 1. Dim the floor lamp to 20%");
        StringAssert.Contains(S.History.Single().Assistant, "Not this; nothing ran");
    }

    [TestMethod]
    public async Task CannotDo_PizzaHasSuggestions()
    {
        var chat = Chat(Tool("cannot_do", new
        {
            reason = "I can't order food; I only run the devices in this room.",
            suggestions = new[] { "Dim the lamp for dinner", "Close the blinds" },
        }));
        var r = Request("Order a pizza");
        await Start(chat, r).WaitAsync(Guard);

        var ev = await Of(r);
        CollectionAssert.AreEqual(new[] { "planning", "cannot_do", "done cannot_do" }, Kinds(ev));
        var c = ev[1].Data;
        Assert.AreEqual("I can't order food; I only run the devices in this room.", Str(c["reason"]));
        Assert.AreEqual(2, c["suggestions"]!.AsArray().Count);
        Assert.AreEqual("Order a pizza", S.History.Single().User);
        Assert.IsNull(r.Plan);
    }

    [TestMethod]
    public async Task Expired_After10Minutes()
    {
        var r = Request();
        var run = Start(Chat(LampPlan()), r);
        await WaitFor("awaiting_approval", r);
        Fake.Advance(Opt.ApprovalWait - TimeSpan.FromSeconds(1));
        Assert.IsTrue(r.Waiting);
        Fake.Advance(TimeSpan.FromSeconds(1));
        await run.WaitAsync(Guard);

        var ev = await Of(r);
        CollectionAssert.AreEqual(new[] { "planning", "plan", "awaiting_approval plan", "expired", "done expired" }, Kinds(ev));
        Assert.IsFalse(r.Waiting);
        Assert.AreEqual("not_waiting", S.TryDecide(r.Rid, Decision.Approve));
    }

    // ---- the custom scene ----

    [TestMethod]
    public async Task Custom_PlanAndRun_LampConfirmed()
    {
        var manifest = CustomRoom.Build(new[] { new RoomDevice("floor_lamp", "floor_lamp") });
        var code = new InviteCode(1, "NCB-TEST-0001", "test", Fake.GetUtcNow(), 20, false, 0, null);
        S = new LiveSession("sess-custom", code, manifest, new EventLog(Fake), Fake.GetUtcNow());
        var chat = Chat(SubmitPlan(Step("s1", "Dim the floor lamp to 40%", "floor_lamp")),
            Tool("set_floor_lamp", new { brightness = 40, step = "s1" }), Text("DONE"), Text("I set the floor lamp to 40%."));
        var ev = await RunApproved(chat);

        CollectionAssert.AreEqual(new[] { "step s1 running", "call s1 set_floor_lamp", "state floor_lamp.brightness", "result confirmed", "step s1 done" },
            Kinds(ev).SkipWhile(k => !k.StartsWith("step")).TakeWhile(k => k != "say").ToList());
        Assert.AreEqual("ok", Outcome(ev));
        Assert.AreEqual(0.4, (double)S.Room.Get("floor_lamp.brightness")!, 1e-9);
        var runSystem = chat.Calls[1].Messages.First(m => m.Role == ChatRole.System).Text;
        Assert.IsFalse(runSystem.Contains("soil sensor"), runSystem);
    }

    [TestMethod]
    public async Task Home_RunSystem_StillMentionsSoil()
    {
        var chat = Chat(LampPlan(), SetLamp20(), Text("DONE"), Text("I set the floor lamp to 20%."));
        await RunApproved(chat);
        var runSystem = chat.Calls[1].Messages.First(m => m.Role == ChatRole.System).Text;
        StringAssert.Contains(runSystem, "confirm watering by reading the soil sensor");
    }

    [TestMethod]
    public async Task Run_WriteOutsidePlan_RefusedWithReplanHint()
    {
        var chat = Chat(LampPlan(), Tool("set_thermostat", new { celsius = 25, step = "s1" }), Text("DONE"), Text("I left the thermostat as it was."));
        var ev = await RunApproved(chat);

        StringAssert.Contains(ToolResults(chat.Calls[2].Messages).Single(), "propose_replan");
        Assert.AreEqual(21.0, (double)S.Room.Get("thermostat.target")!);
        Assert.IsFalse(ev.Any(e => e.Kind is "call" or "state" or "result"));
        Assert.AreEqual("ok", Outcome(ev));
    }

    [TestMethod]
    public async Task Run_MalformedArgs_ToolErrorThenRecovers()
    {
        var chat = Chat(LampPlan(), Tool("set_lamp", new { brightness = "20%", step = "s1" }), SetLamp20(), Text("DONE"), Text("I set the floor lamp to 20%."));
        var ev = await RunApproved(chat);

        var errors = ToolResults(chat.Calls[2].Messages);
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "\"ok\":false");
        StringAssert.Contains(errors[0], "brightness");
        CollectionAssert.AreEqual(new[] { "step s1 running", "call s1 set_lamp", "state lamp.on", "state lamp.brightness", "result confirmed", "step s1 done" },
            Kinds(ev).SkipWhile(k => !k.StartsWith("step")).TakeWhile(k => k != "say").ToList());
        Assert.AreEqual("ok", Outcome(ev));
        Assert.AreEqual(5, (int)ev[^1].Data["usage"]!["calls"]!);
        Assert.AreEqual(0.2, (double)S.Room.Get("lamp.brightness")!);
    }

    [TestMethod]
    public async Task Run_MalformedVariants_AllToolErrorsNoEvents()
    {
        var chat = Chat(LampPlan(),
            Tools(("set_lamp", new { brightness = 150, step = "s1" }), ("set_lamp", new { brightness = 20 }), ("open_garage", new { }),
                ("set_lamp", new { brightness = 20, step = "s1", extra = 1 }), ("set_lamp", new { brightness = 20, step = 1 }),
                ("schedule", new { at = "25:00", action = "set_lamp", args = new { brightness = 20 }, step = "s1" }),
                ("add_rule", new { when = new { device = "soil", field = "moisture", op = "below", value = 30 }, then = new { action = "run_pump", args = new { seconds = 5 } }, step = "s1" })),
            Text("DONE"), Text("I left the room as it was."));
        var ev = await RunApproved(chat);

        var errors = ToolResults(chat.Calls[2].Messages);
        Assert.AreEqual(7, errors.Count);
        Assert.IsTrue(errors.All(e => e.Contains("\"ok\":false")), string.Join("\n", errors));
        StringAssert.Contains(errors[1], "step s1");
        StringAssert.Contains(errors[6], "propose_replan");
        Assert.IsFalse(ev.Any(e => e.Kind is "call" or "state" or "result"));
        Assert.IsTrue(ev.Where(e => e.Kind == "step").All(e => Str(e.Data["status"]) == "skipped"));
        Assert.IsTrue(JsonNode.DeepEquals(RoomState.PostSetup(S.Scene).ToWire(), S.Room.ToWire()));
        Assert.AreEqual(0, S.Room.Schedules.Count + S.Room.Rules.Count);
        Assert.AreEqual("ok", Outcome(ev));
        Assert.AreEqual(4, (int)ev[^1].Data["usage"]!["calls"]!);
    }

    [TestMethod]
    public async Task Budget_CallsCappedAt12()
    {
        Opt.MaxCalls = 12; // the cap under test (the default is 60)
        var turns = new List<ScriptTurn> { T(LampPlan()) };
        turns.AddRange(Enumerable.Range(0, 20).Select(_ => (ScriptTurn)((_, _) => Tool("read_soil", new { }))));
        var chat = new ScriptedChatClient(turns.ToArray());
        var ev = await RunApproved(chat);

        Assert.AreEqual(12, chat.Calls.Count);
        CollectionAssert.AreEqual(new[] { "limit calls", "done limit" }, Kinds(ev).TakeLast(2).ToList());
        Assert.AreEqual(12, (int)ev[^1].Data["usage"]!["calls"]!);
        Assert.IsFalse(ev.Any(e => e.Kind is "say" or "summary"));
    }

    [TestMethod]
    public async Task Budget_TimeExcludesApprovalWait()
    {
        var pumps = Enumerable.Range(0, 10).Select(_ => ("run_pump", (object)new { seconds = 30, step = "s1" })).ToArray();
        var chat = Chat(SubmitPlan(Step("s1", "Water the plant", "pump", "soil")), Tools(pumps), Text("DONE"), Text("I ran the pump ten times."));
        var r = Request();
        var run = Start(chat, r);
        await WaitFor("awaiting_approval", r);
        var t0 = Fake.GetUtcNow();
        Fake.Advance(TimeSpan.FromMinutes(9));
        Assert.IsNull(S.TryDecide(r.Rid, Decision.Approve));
        await run.WaitAsync(Guard);

        Assert.IsTrue(Fake.GetUtcNow() - t0 >= TimeSpan.FromMinutes(11.5), $"only {Fake.GetUtcNow() - t0} passed");
        var ev = await Of(r);
        Assert.IsFalse(ev.Any(e => e.Kind is "limit" or "error"));
        Assert.AreEqual("ok", Outcome(ev));
        Assert.AreEqual(4, chat.Calls.Count);
    }

    [TestMethod]
    public async Task Budget_TimeRunsOutBetweenToolCalls()
    {
        // 13 pump runs of 15 s each in one response: 3 minutes of work time run out after the 12th.
        Opt.MaxWork = TimeSpan.FromMinutes(3); // the default is 30 minutes
        var pumps = Enumerable.Range(0, 13).Select(_ => ("run_pump", (object)new { seconds = 30, step = "s1" })).ToArray();
        var chat = Chat(SubmitPlan(Step("s1", "Water the plant", "pump", "soil")), Tools(pumps), Text("DONE"), Text("I ran the pump."));
        var ev = await RunApproved(chat);

        Assert.AreEqual(12, ev.Count(e => e.Kind == "call"));
        Assert.AreEqual(12, ev.Count(e => e.Kind == "result"));
        CollectionAssert.AreEqual(new[] { "step s1 done", "limit time", "done limit" }, Kinds(ev).TakeLast(3).ToList());
        Assert.AreEqual(2, chat.Calls.Count);
    }

    [TestMethod]
    public async Task Run_FailedDevice_FaultAtFirstFailingCallOnly()
    {
        // A device fails only in a what-if (Task 8): the same approved plan replays with the soil sensor dead.
        var reads = Tools(("read_soil", new { step = "s1" }), ("read_soil", new { step = "s1" }));
        var prior = Request();
        await RunApproved(Chat(SubmitPlan(Step("s1", "Check the soil", "soil")), reads, Text("DONE"), Text("The soil reads 34%.")), prior);
        var chat = Chat(reads, Text("DONE"), Text("The soil sensor didn't respond."));
        var r = new RequestRecord($"r{++_rids}", prior.Text, S.Room.Clone(), Fake);
        await Runner().RunWhatIfAsync(S, prior, "soil", r, Snap(chat), CancellationToken.None).WaitAsync(Guard);
        var ev = await Of(r);

        CollectionAssert.AreEqual(new[]
        {
            "step s1 running", "reading soil", "call s1 read_soil", "fault", "state soil.status", "result failed",
            "reading soil", "call s1 read_soil", "result failed", "step s1 failed",
        }, Kinds(ev).SkipWhile(k => !k.StartsWith("step")).TakeWhile(k => k != "say").ToList());
        Assert.AreEqual("not responding", Str(ev.Single(e => e.Kind == "fault").Data["text"]), "the demo adds the device name");
        Assert.AreEqual("fault", Str(ev.Single(e => e.Kind == "state").Data["value"]));
        Assert.IsTrue(S.Room.Faulted.Contains("soil"));
        Assert.AreEqual("fault", Str(S.Room.Get("soil.status")));
        Assert.AreEqual("failed", Outcome(ev), "a failed step fails the request");
        var facts = Facts(chat, 2);
        CollectionAssert.AreEqual(new[] { "soil" }, facts["failedDevices"]!.AsArray().Select(Str).ToList());
        Assert.AreEqual("not responding", Str(facts["endReadings"]!["soil"]!["error"]));
        Assert.IsFalse(chat.Calls[0].Messages[0].Text.Contains("not responding"), "the AI isn't told in advance");
    }

    [TestMethod]
    public async Task Budget_TokenKillSwitch_LimitBudget()
    {
        Meter.IsSpent = true;
        var chat = Chat(LampPlan());
        var r = Request();
        await Start(chat, r).WaitAsync(Guard);

        CollectionAssert.AreEqual(new[] { "planning", "limit budget", "done limit" }, Kinds(await Of(r)));
        Assert.AreEqual(0, chat.Calls.Count);
    }

    [TestMethod]
    public async Task Run_ModelThrowsMidRun_ErrorThenDoneFailed()
    {
        var chat = new ScriptedChatClient(T(LampPlan()), T(SetLamp20()), (_, _) => throw new HttpRequestException("503 Service Unavailable"));
        var r = Request();
        var ev = await RunApproved(chat, r);

        CollectionAssert.AreEqual(new[] { "error model", "done failed" }, Kinds(ev).TakeLast(2).ToList());
        Assert.AreEqual("The AI service didn't answer.", Str(ev[^2].Data["message"]));
        Assert.AreEqual(0.2, (double)S.Room.Get("lamp.brightness")!);
        var end = (await Store.ListAsync(1)).Single().End!;
        Assert.AreEqual("failed", end.Outcome);
        Assert.AreEqual(3, end.Calls);
        // The two answered calls reported 100 + 100; the failed one reached the service, so its prompt is estimated too.
        Assert.IsTrue(end.PromptTokens > 200, $"the failed call's prompt is counted ({end.PromptTokens})");
        Assert.AreEqual(41, end.CompletionTokens, "20 + 20 reported, 1 estimated for the failed call");
        Assert.AreEqual(end.PromptTokens + end.CompletionTokens, Meter.Added, "the day's meter got the same tokens");
        Assert.IsNull(S.Active);
        Assert.AreEqual(1, Log.Entries.Count(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning));
    }

    /// <summary>
    /// Final review M7: an SDK exception's message carries the provider's response body after its first line (it can echo
    /// part of the key or the request). The warning has the type, the status and the first line only, and no exception object.
    /// </summary>
    [TestMethod]
    public async Task ModelFailure_LogsTypeStatusAndFirstLine_NeverTheBodyOrTheException()
    {
        const string Body = "{\"error\":{\"message\":\"Incorrect API key provided: sk-proj-abcDEF123456ghiJKL789012mnoPQR\"}}";
        var chat = new ScriptedChatClient(T(LampPlan()), T(SetLamp20()), (_, _) => throw new HttpRequestException(
            "Service request failed.\nStatus: 401 (Unauthorized)\n\nContent:\n" + Body, null, System.Net.HttpStatusCode.Unauthorized));
        await RunApproved(chat);

        var entry = Log.Entries.Single(e => e.Id.Name == "ModelCallFailed");
        Assert.IsNull(entry.Exception, "no exception object: a logger would print its whole message and inner exceptions");
        StringAssert.Contains(entry.Message, "System.Net.Http.HttpRequestException");
        StringAssert.Contains(entry.Message, "401");
        StringAssert.Contains(entry.Message, "Service request failed.");
        foreach (var leak in new[] { "sk-proj", "Incorrect API key", "Content:", "Unauthorized)" })
            Assert.IsFalse(entry.Message.Contains(leak), leak);
    }

    /// <summary>The same for an unexpected failure (here the run store): type, status, first line and the stack trace only.</summary>
    [TestMethod]
    public async Task RunFailure_LogsTypeAndFirstLine_NotTheRestOrTheException()
    {
        var runs = new StartFails(Store, new InvalidOperationException("Duplicate key in Run\nINSERT INTO Run (Text) VALUES ('" + MovieText + "')"));
        var r = Request();
        await new RequestRunner(Opt, new InstantPacer(Fake.Advance), Fake, runs, Meter, UsageDay.Utc, Log)
            .RunAsync(S, r, Snap(Chat(LampPlan())), CancellationToken.None).WaitAsync(Guard);

        CollectionAssert.AreEqual(new[] { "error internal", "done failed" }, Kinds(await Of(r)).TakeLast(2).ToList());
        var entry = Log.Entries.Single(e => e.Id.Name == "RunFailed");
        Assert.IsNull(entry.Exception);
        StringAssert.Contains(entry.Message, "System.InvalidOperationException");
        StringAssert.Contains(entry.Message, "Duplicate key in Run");
        StringAssert.Contains(entry.Message, nameof(StartFails), "where it was thrown");
        Assert.IsFalse(entry.Message.Contains("INSERT") || entry.Message.Contains(MovieText), "never the rest of the message, nor visitor text");
    }

    sealed class StartFails(IRunStore inner, Exception e) : IRunStore
    {
        public Task<long> StartAsync(RunStart s) => throw e;
        public Task FinishAsync(long runId, RunEnd end) => inner.FinishAsync(runId, end);
        public Task<int> CountTodayAsync(DateOnly day, string? code) => inner.CountTodayAsync(day, code);
        public Task<long> TokensTodayAsync(DateOnly day) => inner.TokensTodayAsync(day);
        public Task BlankTextOlderThanAsync(DateTimeOffset cutoff) => inner.BlankTextOlderThanAsync(cutoff);
        public Task<IReadOnlyList<RunRow>> ListAsync(int take) => inner.ListAsync(take);
    }

    sealed class StatusException(int status, string message) : Exception(message)
    {
        public int Status { get; } = status;
    }

    [TestMethod]
    public void SafeError_FirstLine_KeyLikeRunsMasked_Capped_StatusFound()
    {
        var (type, status, message) = SafeError.Of(new StatusException(429, "Too many requests for key sk-abcdef0123456789ABCDEF0123\nbody"));
        Assert.AreEqual(typeof(StatusException).FullName, type);
        Assert.AreEqual("429", status, "an int Status, as the OpenAI and Azure SDK exceptions have");
        Assert.AreEqual("Too many requests for key ***", message);

        Assert.AreEqual("none", SafeError.Of(new InvalidOperationException("x")).Status);
        Assert.AreEqual("SQLite Error 1: 'no such table: Senparc_NeuCharBoxLive_InviteCode'",
            SafeError.Of(new InvalidOperationException("SQLite Error 1: 'no such table: Senparc_NeuCharBoxLive_InviteCode'\r\nmore")).Message,
            "an identifier without digits is kept");
        var cut = SafeError.Of(new InvalidOperationException(new string('a', 500))).Message;
        Assert.AreEqual(SafeError.MaxMessage + 1, cut.Length);
        StringAssert.EndsWith(cut, "…");
        Assert.AreEqual("503", SafeError.Of(new HttpRequestException("x", null, System.Net.HttpStatusCode.ServiceUnavailable)).Status);
    }

    /// <summary>
    /// A call that reached the service and then stalled or failed still costs tokens: its prompt is estimated into the
    /// request (RunEnd) and the day's meter. Covers a stalled run call, and a summary stream that fails before any text.
    /// </summary>
    [TestMethod]
    public async Task FailedOrStalledCall_CountsInMeterAndRunEnd()
    {
        var chat = Chat(LampPlan(), SetLamp20(), Stalled);
        var pacer = new InstantPacer(Fake.Advance);
        var r = Request();
        var run = Runner(pacer).RunAsync(S, r, Snap(chat), CancellationToken.None);
        await Approve(r);
        await chat.StallStarted.WaitAsync(Guard);
        Fake.Advance(Opt.MaxWork - pacer.Total);
        await run.WaitAsync(Guard);

        var end = (await Store.ListAsync(1)).Single().End!;
        Assert.AreEqual("limit", end.Outcome, "the work budget cut the stalled call off: limit {time}");
        Assert.IsTrue(end.PromptTokens > 200, $"the stalled call's prompt is counted ({end.PromptTokens})");
        Assert.AreEqual(41, end.CompletionTokens);
        Assert.AreEqual(end.PromptTokens + end.CompletionTokens, Meter.Added);
        var done = (await Of(r)).Single(e => e.Kind == "done").Data["usage"]!;
        Assert.AreEqual(end.PromptTokens, (int)done["promptTokens"]!, "done.usage matches the run log");

        // The summary stream fails before it sends a word.
        var before = Meter.Added;
        var chat2 = new ScriptedChatClient(T(LampPlan()), T(Text("DONE")), (_, _) => throw new HttpRequestException("502 Bad Gateway"));
        var r2 = Request();
        await RunApproved(chat2, r2);
        var end2 = (await Store.ListAsync(10)).Single(x => x.Id == r2.RunId).End!;
        Assert.AreEqual("failed", end2.Outcome);
        Assert.IsTrue(end2.PromptTokens > 200, $"the failed stream's prompt is counted ({end2.PromptTokens})");
        Assert.AreEqual(41, end2.CompletionTokens);
        Assert.AreEqual(end2.PromptTokens + end2.CompletionTokens, Meter.Added - before);
    }

    /// <summary>A call that never connected (DNS, TCP, TLS: the retry handler's own cases) never reached the service: no tokens.</summary>
    [TestMethod]
    public async Task CallThatNeverConnected_IsNotMetered()
    {
        var chat = new ScriptedChatClient(T(LampPlan()), T(SetLamp20()),
            (_, _) => throw new HttpRequestException(HttpRequestError.ConnectionError, "connection refused"));
        var r = Request();
        var ev = await RunApproved(chat, r);

        CollectionAssert.AreEqual(new[] { "error model", "done failed" }, Kinds(ev).TakeLast(2).ToList());
        var end = (await Store.ListAsync(1)).Single().End!;
        Assert.AreEqual(3, end.Calls, "it still counts as a model call against the request's bound");
        Assert.AreEqual(200, end.PromptTokens);
        Assert.AreEqual(40, end.CompletionTokens);
        Assert.AreEqual(240, Meter.Added);
    }

    [TestMethod]
    public async Task Run_StalledStream_EndsAtTheTimeLimit()
    {
        // A stalled run call. The work time left is MaxWork less the pacer time so far (planning and approval took none).
        // A call the work budget cuts off is the request's time bound, not a model failure: limit {time}.
        var chat = Chat(LampPlan(), SetLamp20(), Stalled);
        var pacer = new InstantPacer(Fake.Advance);
        var r = Request();
        var run = Runner(pacer).RunAsync(S, r, Snap(chat), CancellationToken.None);
        await Approve(r);
        await chat.StallStarted.WaitAsync(Guard);
        Assert.AreEqual(TimeSpan.FromMilliseconds(1200), pacer.Total);
        var remaining = Opt.MaxWork - pacer.Total;
        Fake.Advance(remaining - TimeSpan.FromSeconds(1));
        Assert.IsFalse(run.IsCompleted, "still stalled one second before the budget runs out");
        Assert.IsFalse((await Of(r)).Any(e => e.Kind is "error" or "done"));
        Fake.Advance(TimeSpan.FromSeconds(1));
        await run.WaitAsync(Guard);

        var ev = await Of(r);
        CollectionAssert.AreEqual(new[] { "limit time", "done limit" }, Kinds(ev).TakeLast(2).ToList());
        Assert.IsFalse(ev.Any(e => e.Kind == "error"));

        // A summary stream that stalls after one word.
        var chat2 = Chat(LampPlan(), Text("DONE"), Stalled);
        var pacer2 = new InstantPacer(Fake.Advance);
        var r2 = Request();
        var run2 = Runner(pacer2).RunAsync(S, r2, Snap(chat2), CancellationToken.None);
        await Approve(r2);
        await chat2.StallStarted.WaitAsync(Guard);
        Fake.Advance(Opt.MaxWork - pacer2.Total - TimeSpan.FromSeconds(1));
        Assert.IsFalse(run2.IsCompleted, "the summary stream is still stalled");
        Fake.Advance(TimeSpan.FromSeconds(1));
        await run2.WaitAsync(Guard);

        var ev2 = await Of(r2);
        CollectionAssert.AreEqual(new[] { "say", "limit time", "done limit" }, Kinds(ev2).TakeLast(3).ToList());
        Assert.AreEqual("I", Str(ev2.Single(e => e.Kind == "say").Data["delta"]), "the word is sent, its trailing space held");
        Assert.IsFalse(ev2.Any(e => e.Kind == "summary"));
        Assert.IsNull(S.Active);
    }

    [TestMethod]
    public async Task Run_StepOptionalOnSafeAndReads()
    {
        var chat = Chat(
            SubmitPlan(Step("s1", "Dim the floor lamp to 20%", "lamp"), Step("s2", "Close the blinds", "blinds")),
            Tools(("set_lamp", new { brightness = 0 }), ("read_soil", new { }), ("read_soil", new { step = "s9" }),
                ("set_lamp", new { brightness = 0, step = "s2" }), ("read_soil", new { step = (string?)null })),
            SetLamp20(),
            Text("DONE"), Text("I turned the lamp off, read the soil, then set the lamp to 20%."));
        var ev = await RunApproved(chat);

        Assert.IsFalse(ToolResults(chat.Calls[2].Messages).Any(t => t.Contains("\"ok\":false")));
        var calls = ev.Where(e => e.Kind == "call").ToList();
        var results = ev.Where(e => e.Kind == "result").ToList();
        Assert.AreEqual(6, calls.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.IsTrue(calls[i].Data.ContainsKey("step") && calls[i].Data["step"] is null, $"call {i}");
            Assert.IsTrue(results[i].Data.ContainsKey("step") && results[i].Data["step"] is null, $"result {i}");
        }
        CollectionAssert.AreEqual(new[] { "made_safe", "reading", "reading", "made_safe", "reading", "confirmed" },
            results.Select(e => Str(e.Data["outcome"])).ToList());
        var firstStep = ev.ToList().FindIndex(e => e.Kind == "step");
        Assert.IsTrue(firstStep > ev.ToList().IndexOf(results[4]), "the stepless calls touch no step");
        Assert.AreEqual("s1", Str(calls[5].Data["step"]));
        Assert.AreEqual("{\"brightness\":20}", calls[5].Data["args"]!.ToJsonString());
    }

    [TestMethod]
    public async Task Run_NotifyStripped()
    {
        var chat = Chat(LampPlan(), Tool("notify_user", new { text = "It's 100% done" }), Text("DONE"), Text("I sent you a note."));
        var ev = await RunApproved(chat);

        var row = ev.Single(e => e.Kind == "timeline").Data;
        Assert.AreEqual("It's done", Str(row["text"]));
        Assert.AreEqual("notify", Str(row["kind"]));
        Assert.AreEqual("Fri 16:30", Str(row["at"]));
        Assert.AreEqual(1, GuardHits());
        Assert.IsFalse(Log.Entries.Any(e => e.Message.Contains(MovieText) || e.Message.Contains("It's")), "logs never carry the texts");
        Assert.AreEqual(4, chat.Calls.Count, "no rephrase call");
        StringAssert.Contains(ToolResults(chat.Calls[2].Messages).Single(), "\"ok\":true");
    }

    [TestMethod]
    public async Task PlanAndRun_UseTheirModels()
    {
        var plan = Chat(Tool("read_soil", new { }), LampPlan());
        var run = Chat(SetLamp20(), Text("DONE"), Text("I set the floor lamp to 20%."));
        var snap = new LiveSnapshot(LiveLimits.Defaults, new LiveModel("p", plan), new LiveModel("r", run), LiveHost.Origins, null);
        var r = Request();
        var t = Runner().RunAsync(S, r, snap, CancellationToken.None);
        await Approve(r);
        await t.WaitAsync(Guard);

        Assert.AreEqual("ok", Outcome(await Of(r)));
        Assert.AreEqual(2, plan.Calls.Count);
        Assert.AreEqual(3, run.Calls.Count);
        foreach (var (_, o) in plan.Calls)
        {
            Assert.AreEqual("p", o!.ModelId);
            Assert.AreEqual(1500, o.MaxOutputTokens);
            Assert.AreEqual(ChatToolMode.RequireAny, o.ToolMode);
            Assert.IsTrue(o.Tools!.Any(x => x.Name == "submit_plan"));
        }
        foreach (var (_, o) in run.Calls.Take(2))
        {
            Assert.AreEqual("r", o!.ModelId);
            Assert.AreEqual(1500, o.MaxOutputTokens);
            Assert.AreEqual(ChatToolMode.Auto, o.ToolMode);
            Assert.IsTrue(o.Tools!.Any(x => x.Name == "propose_replan"));
        }
        var summary = run.Calls[2].Options!;
        Assert.AreEqual("r", summary.ModelId);
        Assert.AreEqual(200, summary.MaxOutputTokens);
        Assert.IsTrue(summary.Tools is null || summary.Tools.Count == 0);
    }

    [TestMethod]
    public void Run_OffSnapshot_IsRefused()
    {
        // SessionManager answers live_off first; a runner handed an off snapshot anyway refuses before anything begins.
        var r = Request();
        Assert.ThrowsException<ArgumentException>(() => Runner().RunAsync(S, r, LiveSnapshot.Off(LiveOff.NoModel, LiveHost.Origins), CancellationToken.None));
        Assert.ThrowsException<ArgumentException>(() =>
            Runner().RunWhatIfAsync(S, r, "lamp", Request(), Snap(Chat()) with { OffReason = LiveOff.Disabled }, CancellationToken.None));
        Assert.IsNull(S.Active, "nothing began");
    }

    [TestMethod]
    public async Task Done_RemainingTodayUsesCodeLimit()
    {
        S.Code = S.Code with { DailyLimit = 2 };
        var day = UsageDay.Utc.For(Fake.GetUtcNow());
        RunStart Admit(string text) => new(S.Code.Code, "home", text, false, null, Snapshots.Model, day, Fake.GetUtcNow());
        var pizza = Tool("cannot_do", new { reason = "I can't order food here.", suggestions = new[] { "Dim the lamp" } });

        await Store.StartAsync(Admit("earlier"));
        var r = Request("Order a pizza");
        r.RunId = await Store.StartAsync(Admit(r.Text));
        await Start(Chat(pizza), r).WaitAsync(Guard);
        Assert.AreEqual(0, (int)(await Of(r))[^1].Data["remainingToday"]!);
        var rows = await Store.ListAsync(10);
        Assert.AreEqual(2, rows.Count, "an admitted run isn't logged twice");
        Assert.AreEqual("cannot_do", rows[0].End!.Outcome);

        // The global cap applies too: min(10 − 3, 4 − 3) = 1.
        S.Code = S.Code with { DailyLimit = 10 };
        Limits = Limits with { GlobalDaily = 4 }; // the request's snapshot's GlobalDaily
        var r2 = Request("Order a pizza");
        r2.RunId = await Store.StartAsync(Admit(r2.Text));
        await Start(Chat(Tool("cannot_do", new { reason = "I can't order food here.", suggestions = new[] { "Dim the lamp" } })), r2).WaitAsync(Guard);
        Assert.AreEqual(1, (int)(await Of(r2))[^1].Data["remainingToday"]!);
    }

    [TestMethod]
    public async Task Summary_GuardedAndTwoSentences()
    {
        var chat = Chat(LampPlan(), Text("DONE"), Text("It's 100% done. Two. Three."), Text("Still 100% done. Two. Three."));
        var ev = await RunApproved(chat);

        Assert.AreEqual("Still done. Two.", Str(ev.Single(e => e.Kind == "summary").Data["text"]));
        var says = ev.Where(e => e.Kind == "say").ToList();
        Assert.IsTrue(says.Count > 0);
        Assert.IsFalse(says.Any(e => Str(e.Data["delta"]).Contains('%')), "no say carries the banned term");
        Assert.AreEqual(0, CopyGuard.Find(Says(ev)).Count);
        Assert.AreEqual("It's", Says(ev), "the stream stops at the hit");
        Assert.IsTrue(ev.ToList().IndexOf(says[^1]) < ev.ToList().FindIndex(e => e.Kind == "summary"), "the guarded summary follows");
        Assert.AreEqual(4, chat.Calls.Count);
        var rephrase = chat.Calls[3];
        Assert.AreEqual(200, rephrase.Options!.MaxOutputTokens);
        Assert.IsTrue(rephrase.Options.Tools is null || rephrase.Options.Tools.Count == 0);
        StringAssert.Contains(rephrase.Messages[^1].Text, "It's 100% done. Two. Three.");
        Assert.AreEqual(2, GuardHits());
        Assert.AreEqual("ok", Outcome(ev));
        StringAssert.EndsWith(S.History.Single().Assistant, "Result: Still done. Two.");
    }

    [TestMethod]
    public async Task Say_NeverShowsMoreThanTheTwoSentenceSummary()
    {
        var chat = Chat(LampPlan(), Text("DONE"), Text("I left the lamp as it was. The blinds are open. I also checked nothing else."));
        var ev = await RunApproved(chat);

        Assert.AreEqual("I left the lamp as it was. The blinds are open.", Says(ev));
        Assert.AreEqual(Says(ev), Str(ev.Single(e => e.Kind == "summary").Data["text"]));
        Assert.AreEqual(0, GuardHits());
    }

    [TestMethod]
    public async Task Summary_RephraseRefusedAtCallCap_StripsAndEndsOk()
    {
        // 7 calls before the summary; the summary is call 8, so its rephrase would be a 9th.
        Opt.MaxCalls = 8; // the script is sized for this cap
        var turns = new List<ChatResponse> { LampPlan() };
        turns.AddRange(Enumerable.Range(0, 5).Select(_ => Tool("read_soil", new { })));
        turns.Add(Text("DONE"));
        turns.Add(Text("It's 100% done."));
        var chat = Chat(turns.ToArray());
        var ev = await RunApproved(chat);

        Assert.AreEqual(8, chat.Calls.Count);
        Assert.AreEqual("It's done.", Str(ev.Single(e => e.Kind == "summary").Data["text"]));
        CollectionAssert.AreEqual(new[] { "summary", "done ok" }, Kinds(ev).TakeLast(2).ToList());
        Assert.IsTrue((int)ev[^1].Data["usage"]!["calls"]! <= 8);
        Assert.IsFalse(ev.Any(e => e.Kind is "limit" or "error"));
        StringAssert.EndsWith(S.History.Single().Assistant, "Result: It's done.");
        Assert.AreEqual(1, GuardHits(), "the hit is logged once");
        Assert.AreEqual(1, Log.Entries.Count(e => e.Id.Name == "RephraseSkipped"));
    }

    [TestMethod]
    public async Task PlanGuard_RephraseRefused_StripsAndShowsPlan()
    {
        Opt.MaxCalls = 1;
        var chat = Chat(SubmitPlan(Step("s1", "Dim the floor lamp, 100% sure", "lamp")));
        var r = Request();
        var run = Start(chat, r);
        var plan = await WaitFor("plan", r);
        Assert.AreEqual("Dim the floor lamp, sure", Str(plan.Data["steps"]![0]!["text"]));
        await WaitFor("awaiting_approval", r);
        Assert.IsNull(S.TryDecide(r.Rid, Decision.Decline));
        await run.WaitAsync(Guard);

        CollectionAssert.AreEqual(new[] { "declined", "done declined" }, Kinds(await Of(r)).TakeLast(2).ToList());
        Assert.AreEqual(1, chat.Calls.Count);
        StringAssert.EndsWith(S.History.Single().Assistant, "Result: Not this; nothing ran.");
    }

    [TestMethod]
    public async Task CannotDoGuard_RephraseRefused_StripsAndEnds()
    {
        Opt.MaxCalls = 1;
        var chat = Chat(Tool("cannot_do", new { reason = "I can't do that over Zigbee.", suggestions = new[] { "Dim the lamp" } }));
        var r = Request("Pair my bulbs");
        await Start(chat, r).WaitAsync(Guard);

        var ev = await Of(r);
        CollectionAssert.AreEqual(new[] { "planning", "cannot_do", "done cannot_do" }, Kinds(ev));
        Assert.AreEqual("I can't do that over.", Str(ev[1].Data["reason"]));
    }

    [TestMethod]
    public async Task Approve_TwiceConcurrently_OneWinsOneNotWaiting()
    {
        var chat = Chat(LampPlan(), SetLamp20(), Text("DONE"), Text("I set the floor lamp to 20%."));
        var r = Request();
        var run = Start(chat, r);
        await WaitFor("awaiting_approval", r);
        using var barrier = new Barrier(2);
        string? Decide() { barrier.SignalAndWait(Guard); return S.TryDecide(r.Rid, Decision.Approve); }
        var answers = await Task.WhenAll(Task.Run(Decide), Task.Run(Decide));
        Assert.AreEqual(1, answers.Count(a => a is null));
        Assert.AreEqual(1, answers.Count(a => a == "not_waiting"));
        Assert.AreEqual("no_request", S.TryDecide("r-unknown", Decision.Approve));
        await run.WaitAsync(Guard);

        var ev = await Of(r);
        Assert.AreEqual(1, ev.Count(e => e.Kind == "call"));
        Assert.AreEqual(1, ev.Count(e => e.Kind == "done"));
        Assert.AreEqual(4, chat.Calls.Count);
        Assert.AreEqual("not_waiting", S.TryDecide(r.Rid, Decision.Decline));
    }

    [TestMethod]
    public async Task Decide_RaceManyTimes_TheWinnersDecisionIsUsed()
    {
        for (var i = 0; i < 50; i++)
        {
            var rec = new RequestRecord($"race{i}", "x", S.Room, Fake);
            S.Track(rec);
            Assert.AreEqual("not_waiting", S.TryDecide(rec.Rid, Decision.Approve), "not waiting before the wait starts");
            var wait = rec.WaitDecisionAsync(Opt.ApprovalWait, CancellationToken.None);
            Assert.IsTrue(rec.Waiting);
            using var barrier = new Barrier(2);
            var answers = await Task.WhenAll(
                Task.Run(() => { barrier.SignalAndWait(Guard); return S.TryDecide(rec.Rid, Decision.Approve); }),
                Task.Run(() => { barrier.SignalAndWait(Guard); return S.TryDecide(rec.Rid, Decision.Decline); }));
            Assert.AreEqual(1, answers.Count(a => a is null), $"round {i}");
            Assert.AreEqual(answers[0] is null ? Decision.Approve : Decision.Decline, await wait.WaitAsync(Guard));
            Assert.IsFalse(rec.Waiting);
        }
    }

    [TestMethod]
    public async Task Pump_ConfirmedOnlyThroughSoil()
    {
        var plan = SubmitPlan(Step("s1", "Water the plant", "soil", "pump"));
        var pumpOnly = Chat(plan, Tool("run_pump", new { seconds = 5, step = "s1" }), Text("DONE"), Text("I ran the pump for 5 s."));
        var ev = await RunApproved(pumpOnly);
        Assert.AreEqual("commanded", Str(ev.Single(e => e.Kind == "result").Data["outcome"]));
        var pump = Facts(pumpOnly, 3)["calls"]!.AsArray().Single()!;
        Assert.AreEqual("run_pump", Str(pump["action"]));
        Assert.AreEqual("commanded", Str(pump["outcome"]));
        Assert.IsFalse((bool)pump["confirmed"]!);
        // The soil now holds 40, but nothing read it: neither the facts nor the next prompt may say so.
        Assert.AreEqual(40.0, (double)S.Room.Get("soil.moisture")!);
        var facts = Facts(pumpOnly, 3);
        Assert.AreEqual("not read", Str(facts["endReadings"]!["soil"]));
        Assert.IsFalse(Prompts.ToJson(facts).Contains("\"moisture\":40"), Prompts.ToJson(facts));

        var thenRead = Chat(SubmitPlan(Step("s1", "Water the plant", "soil", "pump")),
            Tools(("run_pump", new { seconds = 5, step = "s1" }), ("read_soil", new { step = "s1" })), Text("DONE"), Text("I watered the plant."));
        await RunApproved(thenRead);
        var prompt = thenRead.Calls[0].Messages[0].Text;
        StringAssert.Contains(prompt, "soil (Soil sensor): not read yet");
        Assert.IsFalse(prompt.Contains("\"moisture\":40") || prompt.Contains("\"moisture\":34"), "no unread sensor value in the prompt");
        Assert.IsFalse(prompt.Contains("probeOut"));
        var thenFacts = Facts(thenRead, 3);
        var calls = thenFacts["calls"]!.AsArray();
        Assert.IsTrue((bool)calls[0]!["confirmed"]!, "the soil rose after the pump");
        Assert.AreEqual("reading", Str(calls[1]!["outcome"]));
        Assert.AreEqual("{\"moisture\":46}", Prompts.ToJson(thenFacts["endReadings"]!["soil"]!), "the reading it took");

        // A later prompt shows the last reading the AI saw, and when.
        var third = Chat(Tool("cannot_do", new { reason = "I can't do that here.", suggestions = new[] { "Water the plant" } }));
        await Start(third, Request("What now?")).WaitAsync(Guard);
        StringAssert.Contains(third.Calls[0].Messages[0].Text, "soil (Soil sensor): last read at Fri 16:30: {\"moisture\":46}");
    }

    [TestMethod]
    public async Task FollowUp_UsesRoomAndHistory()
    {
        var first = Chat(LampPlan(), SetLamp20(), Text("DONE"), Text("I set the floor lamp to 20%."));
        await RunApproved(first);

        var second = Chat(Tool("cannot_do", new { reason = "I can't do that here.", suggestions = new[] { "Turn the lamp off" } }));
        var r2 = Request("Now make it warmer in here");
        await Start(second, r2).WaitAsync(Guard);

        var m = second.Calls[0].Messages;
        Assert.AreEqual(4, m.Count);
        Assert.AreEqual(ChatRole.System, m[0].Role);
        StringAssert.Contains(m[0].Text, "\"brightness\":0.2");
        Assert.AreEqual(ChatRole.User, m[1].Role);
        Assert.AreEqual(MovieText, m[1].Text);
        Assert.AreEqual(ChatRole.Assistant, m[2].Role);
        Assert.AreEqual("Plan: 1. Dim the floor lamp to 20%. Result: I set the floor lamp to 20%.", m[2].Text);
        Assert.AreEqual("Now make it warmer in here", m[3].Text);
        Assert.AreEqual(2, S.History.Count);
    }

    // ---- carried decisions ----

    [TestMethod]
    public async Task AddRule_Becomes_AlreadyTrue_DoesNotFireAtOnce()
    {
        var chat = Chat(SubmitPlan(Step("s1", "Tell you if the door camera gets disarmed", "camera")),
            Tool("add_rule", new
            {
                when = new { device = "camera", field = "armed", op = "becomes", value = false },
                then = new { notify = "The door camera was disarmed" },
                step = "s1",
            }),
            Text("DONE"), Text("I'll tell you if the door camera is disarmed."));
        var ev = await RunApproved(chat);

        StringAssert.Contains(ToolResults(chat.Calls[2].Messages).Single(), "\"id\":\"rule1\"");
        var rule = S.Room.Rules.Single();
        Assert.AreEqual("camera.armed", rule.Path);
        Assert.AreEqual(RuleOp.Becomes, rule.Op);
        Assert.AreEqual(2, rule.MaxPerDay, "max_per_day defaults to 2");
        Assert.AreEqual("The door camera was disarmed", rule.Notify);
        Assert.IsFalse((bool)rule.LastSeen!);
        Assert.IsNull(rule.LastSeen!.Parent, "LastSeen is a copy, not the room's node");
        CollectionAssert.AreEqual(new[] { "step s1 running", "step s1 done" }, Kinds(ev).Where(k => k.StartsWith("step")).ToList());

        var b = ManifestLoader.Behavior("home");
        var report = await new FastForward(new Simulator(S.Scene, b), b, new InstantPacer(), Opt.Pacing)
            .RunAsync(S.Room, 2, new QuietSink(), CancellationToken.None);
        Assert.AreEqual(0, report.Rows.Count, "the condition already held, so the rule waits for a change");
    }

    [TestMethod]
    public async Task Schedule_ParsesAtExactly()
    {
        var chat = Chat(SubmitPlan(Step("s1", "Put the floor lamp on in the evenings", "lamp")),
            Tools(
                ("schedule", new { at = "2026-02-30T10:00", action = "set_lamp", args = new { brightness = 30 }, step = "s1" }),
                ("schedule", new { at = "18:00", days = new[] { "Sat", "Sun" }, action = "set_lamp", args = new { brightness = 30 }, step = "s1" }),
                ("schedule", new { at = "2026-11-14T07:10", action = "set_lamp", args = new { brightness = 0 }, step = "s1" }),
                ("schedule", new { at = "2026-11-13T09:00", action = "set_lamp", args = new { brightness = 30 }, step = "s1" }),
                ("schedule", new { at = "2026-11-14T07:10", days = new[] { "Sat" }, action = "set_lamp", args = new { brightness = 30 }, step = "s1" })),
            Text("DONE"), Text("I scheduled the floor lamp."));
        await RunApproved(chat);

        var results = ToolResults(chat.Calls[2].Messages);
        StringAssert.Contains(results[0], "\"ok\":false");
        StringAssert.Contains(results[1], "\"id\":\"sched1\"");
        StringAssert.Contains(results[2], "\"id\":\"sched2\"");
        StringAssert.Contains(results[3], "\"ok\":false");
        StringAssert.Contains(results[4], "\"ok\":false");
        Assert.AreEqual(2, S.Room.Schedules.Count);
        var daily = S.Room.Schedules[0];
        Assert.AreEqual("18:00", daily.DailyAt);
        Assert.IsTrue(daily.Days!.SetEquals(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }));
        Assert.AreEqual("{\"brightness\":30}", daily.Call.Args.ToJsonString());
        Assert.AreEqual("s1", daily.Step);
        var once = S.Room.Schedules[1];
        Assert.IsNull(once.DailyAt);
        Assert.AreEqual(24 + 7 + 10 / 60.0, once.OnceHour!.Value, 1e-9);
    }

    [TestMethod]
    public async Task Schedule_SafeActionOnAnotherStep_TouchesNoStep()
    {
        var chat = Chat(SubmitPlan(Step("s1", "Dim the floor lamp to 20%", "lamp")),
            Tool("schedule", new { at = "18:00", action = "stop_pump", args = new { }, step = "s1" }),
            Text("DONE"), Text("I scheduled the pump to stop at 18:00."));
        var ev = await RunApproved(chat);

        StringAssert.Contains(ToolResults(chat.Calls[2].Messages).Single(), "\"ok\":true");
        Assert.AreEqual(1, S.Room.Schedules.Count);
        CollectionAssert.AreEqual(new[] { "step s1 skipped" }, Kinds(ev).Where(k => k.StartsWith("step")).ToList());
    }

    [TestMethod]
    public void FixedTexts_PassGuard()
    {
        var m = ManifestLoader.Load("home");
        var texts = Prompts.FixedTexts.Concat(m.Suggestions)
            .Concat(m.Devices.Select(d => Prompts.StepFallback(new PlanStep("s1", "x", new[] { d.Id }), m, 1)))
            .Append(Prompts.StepFallback(new PlanStep("s1", "x", m.Devices.Select(d => d.Id).ToList()), m, 1));
        foreach (var t in texts)
            Assert.AreEqual(0, CopyGuard.Find(t).Count, t);
        Assert.AreEqual("Use the floor lamp and the blinds controller",
            Prompts.StepFallback(new PlanStep("s1", "x", new[] { "lamp", "blinds" }), m, 1));
    }

    [TestMethod]
    public async Task StepTracker_RetryCountsAsDone_OtherDeviceFailureDoesNot()
    {
        var log = new EventLog(Fake);
        var plan = new ApprovedPlan();
        plan.Steps.AddRange(new[]
        {
            new PlanStep("s1", "a", new[] { "lamp" }), new PlanStep("s2", "b", new[] { "lamp", "blinds" }),
            new PlanStep("s3", "c", new[] { "blinds" }), new PlanStep("s4", "d", new[] { "soil" }),
        });
        var t = new StepTracker(log, "r1", plan);
        t.OnCall("s1"); t.OnResult("s1", false, "lamp"); t.OnCall("s1"); t.OnResult("s1", true, "lamp");
        t.OnCall(null); t.OnResult(null, false, "soil");
        t.OnCall("s2"); t.OnResult("s2", false, "lamp"); t.OnCall("s2"); t.OnResult("s2", true, "blinds");
        t.OnCall("s3"); t.OnResult("s3", true, "blinds");
        t.Finish();
        t.Finish();

        var seen = (await log.WaitAfterAsync(0, TimeSpan.Zero, CancellationToken.None)).Select(Describe).ToList();
        CollectionAssert.AreEqual(new[]
        {
            "step s1 running", "step s1 done", "step s2 running", "step s2 failed", "step s3 running", "step s3 done", "step s4 skipped",
        }, seen);
        Assert.IsTrue(t.AnyFailed);
    }
}
