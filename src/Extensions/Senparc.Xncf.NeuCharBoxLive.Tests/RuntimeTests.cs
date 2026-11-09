/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RuntimeTests.cs
    文件功能描述：NeuCharBox Live 测试：RuntimeTests（每个请求一份 LiveSnapshot）


    创建标识：Senparc - 20261108

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;
using Senparc.Xncf.NeuCharBoxLive.Api;
using static Senparc.Xncf.NeuCharBoxLive.Tests.Fakes.ScriptedChatClient;
using ScriptTurn = System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, Microsoft.Extensions.AI.ChatResponse>;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// The engine reads its on/off switch, models, limits and origins from <see cref="ILiveRuntime.Current"/>; a request
/// keeps the snapshot it started with. Options that can't work together turn live off instead of stopping the host.
/// </summary>
[TestClass]
public class RuntimeTests
{
    static ScriptedChatClient Script(params ChatResponse[] responses) => new(responses.Select(r => (ScriptTurn)((_, _) => r)).ToArray());

    static ChatResponse LampPlan() => Tool("submit_plan", new { steps = new[] { new { id = "s1", text = "Dim the floor lamp to 20%", devices = new[] { "lamp" } } } });

    static ChatResponse CannotDo() => Tool("cannot_do", new { reason = "I can't order food here.", suggestions = new[] { "Dim the lamp" } });

    static ScriptedChatClient LampOk() =>
        Script(LampPlan(), Tool("set_lamp", new { brightness = 20, step = "s1" }), Text("DONE"), Text("I dimmed the floor lamp to 20%."));

    static void AssertError(LiveHost.Reply r, int status, string kind)
    {
        Assert.AreEqual(status, r.Code, $"{kind}: {r.Body?.ToJsonString()}");
        Assert.AreEqual(kind, r.Error, r.Body?.ToJsonString());
    }

    static async Task AssertHealth(LiveHost h, bool liveEnabled)
    {
        var r = await h.GetAsync("health");
        Assert.AreEqual(200, r.Code);
        Assert.AreEqual(true, (bool?)r.Body!["ok"]);
        Assert.AreEqual(liveEnabled, (bool?)r.Body!["liveEnabled"], r.Body.ToJsonString());
    }

    sealed class ListLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Logger(this);

        public void Dispose() { }

        sealed class Logger(ListLoggerProvider p) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (p.Entries) p.Entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }

    [TestMethod]
    public async Task ConfigProblem_HostStarts_AndHealthIsOff()
    {
        // SessionIdle shorter than MaxWork + ApprovalWait used to throw at startup; now the host starts with live off.
        var logs = new ListLoggerProvider();
        await using var h = await LiveHost.StartAsync(config: new() { ["NcbLive:SessionIdle"] = "00:05:00" },
            services: (_, s) => s.AddSingleton<ILoggerProvider>(logs));

        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.SessionIdle) }, h.Opt.Problems().ToList());
        Assert.AreEqual(LiveOff.Config, h.Get<ILiveRuntime>().Current.OffReason, "every snapshot is off(config)");
        Assert.IsTrue(h.Runtime.Current.On, "the registered runtime itself is on: AddNcbLive wraps it");
        await AssertHealth(h, false);
        var c = await h.NewCodeAsync();
        AssertError(await h.PostAsync("sessions", new { scene = "home" }, c.Code), 503, "live_off");
        lock (logs.Entries)
            Assert.IsTrue(logs.Entries.Any(e => e.Level >= LogLevel.Error && e.Message.Contains(nameof(LiveOptions.SessionIdle))),
                "the problem is logged by property name");

        // The wrapper keeps the runtime's origins, so the demo can still read health and show live as off.
        CollectionAssert.AreEqual(h.Runtime.Current.Origins.ToList(), h.Get<ILiveRuntime>().Current.Origins.ToList());
    }

    [TestMethod]
    public async Task Health_Off_WhenSnapshotHasOffReason()
    {
        await using var h = await LiveHost.StartAsync(_ => LiveSnapshot.Off(LiveOff.NoModel, LiveHost.Origins));
        var r = await h.GetAsync("health");
        Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse("""{"ok":true,"liveEnabled":false,"scenes":["home","custom"]}"""), r.Body), r.Body?.ToJsonString());
        var c = await h.NewCodeAsync();
        AssertError(await h.PostAsync("sessions", new { scene = "home" }, c.Code), 503, "live_off");

        // Models present but an off reason: still off. Cleared: on.
        h.Runtime.Current = h.OnSnapshot() with { OffReason = LiveOff.Disabled };
        await AssertHealth(h, false);
        AssertError(await h.PostAsync("sessions", new { scene = "home" }, c.Code), 503, "live_off");
        h.Runtime.Current = h.OnSnapshot();
        await AssertHealth(h, true);
        Assert.AreEqual(201, (await h.PostAsync("sessions", new { scene = "home" }, c.Code)).Code);

        // No runtime at all: AddNcbLive's default is off with no_model on the built-in origins.
        await using var none = await LiveHost.StartAsync(_ => null);
        var snap = none.Get<ILiveRuntime>().Current;
        Assert.AreEqual(LiveOff.NoModel, snap.OffReason);
        Assert.IsFalse(snap.On);
        CollectionAssert.AreEqual(LiveCors.BuiltIns.ToList(), snap.Origins.ToList());
        await AssertHealth(none, false);
    }

    [TestMethod]
    public async Task Request_KeepsItsSnapshot_WhenRuntimeChangesMidRequest()
    {
        await using var h = await LiveHost.StartAsync();
        var a = LampOk();
        var b = Script(CannotDo());
        h.Runtime.Current = h.OnSnapshot(a, model: "model-a");
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);

        var first = await h.RequestAsync(c, sid);
        await sse.UntilAsync("awaiting_approval");
        h.Runtime.Current = h.OnSnapshot(b, model: "model-b");
        Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/requests/{first}/approve", new { }, c.Code)).Code);
        Assert.AreEqual("ok", (string?)(await sse.UntilAsync("done"))[^1].Data["outcome"]);

        Assert.AreEqual(4, a.Calls.Count, "plan, run, DONE, summary: all on model A");
        Assert.IsTrue(a.Calls.All(x => x.Options?.ModelId == "model-a"), string.Join(",", a.Calls.Select(x => x.Options?.ModelId)));
        Assert.AreEqual(0, b.Calls.Count, "nothing of the first request went to model B");

        var second = await h.RequestAsync(c, sid);
        Assert.AreEqual("cannot_do", (string?)(await sse.UntilAsync("done"))[^1].Data["outcome"]);
        Assert.AreEqual(4, a.Calls.Count);
        Assert.AreEqual(1, b.Calls.Count, "the next request uses the new snapshot");
        Assert.AreEqual("model-b", b.Calls[0].Options?.ModelId);

        var runs = await h.Store.ListAsync(10); // newest first
        Assert.AreEqual(2, runs.Count);
        Assert.AreEqual("model-a", runs[1].Start.Model, "the run log names the model the request started on");
        Assert.AreEqual("model-b", runs[0].Start.Model);
        Assert.IsNotNull(second);
    }

    [TestMethod]
    public async Task Limits_ComeFromSnapshot()
    {
        await using var h = await LiveHost.StartAsync();
        h.Runtime.Current = h.OnSnapshot(limits: new LiveLimits(PerCodeDaily: 20, GlobalDaily: 1, DailyTokenBudget: 1_000_000));
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);
        Assert.AreEqual(1, (int?)(await h.PostAsync("access/check", new { code = c.Code })).Body!["remainingToday"], "min(code 20, global 1)");

        h.Chat.Inner = Script(CannotDo());
        await h.RequestAsync(c, sid);
        var done = (await sse.UntilAsync("done"))[^1];
        Assert.AreEqual(0, (int?)done.Data["remainingToday"], "the request's remaining uses its snapshot's GlobalDaily");
        AssertError(await h.PostAsync($"sessions/{sid}/requests", new { text = "Dim the lamp" }, c.Code), 429, "global_limit");
        await AssertHealth(h, true);

        // The cannot_do call was metered (120 tokens): a budget of 1 is spent.
        h.Runtime.Current = h.OnSnapshot(limits: new LiveLimits(20, 300, 1));
        await AssertHealth(h, false);
        AssertError(await h.PostAsync($"sessions/{sid}/requests", new { text = "Dim the lamp" }, c.Code), 429, "budget");
    }

    [TestMethod]
    public void Snapshot_OnNeedsBothModelsAndNoOffReason()
    {
        var m = new LiveModel("m", new ScriptedChatClient());
        Assert.IsTrue(new LiveSnapshot(LiveLimits.Defaults, m, m, LiveCors.BuiltIns, null).On);
        Assert.IsFalse(new LiveSnapshot(LiveLimits.Defaults, m, null, LiveCors.BuiltIns, null).On);
        Assert.IsFalse(new LiveSnapshot(LiveLimits.Defaults, null, m, LiveCors.BuiltIns, null).On);
        Assert.IsFalse(new LiveSnapshot(LiveLimits.Defaults, m, m, LiveCors.BuiltIns, LiveOff.Disabled).On);

        var off = LiveSnapshot.Off(LiveOff.ModelMissing, LiveCors.Development);
        Assert.IsFalse(off.On);
        Assert.AreEqual(LiveOff.ModelMissing, off.OffReason);
        Assert.AreEqual(LiveLimits.Defaults, off.Limits);
        Assert.AreEqual(new LiveLimits(20, 300, 1_000_000), LiveLimits.Defaults);
        Assert.IsNull(off.Plan);
        Assert.IsNull(off.Run);
        CollectionAssert.AreEqual(LiveCors.Development.ToList(), off.Origins.ToList());

        var rt = new StaticLiveRuntime(off);
        Assert.AreSame(off, rt.Current);
        rt.Current = off with { OffReason = LiveOff.Unavailable };
        Assert.AreEqual(LiveOff.Unavailable, rt.Current.OffReason);
    }
}
