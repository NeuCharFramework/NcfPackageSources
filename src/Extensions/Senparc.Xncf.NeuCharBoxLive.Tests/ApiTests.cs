/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ApiTests.cs
    文件功能描述：NeuCharBox Live 测试：ApiTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 隧道守卫已移除：WrongMethod_405_WithJsonBody 去掉经隧道的用例，开发者异常页改在模块分支之后

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;
using Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;
using Senparc.Xncf.NeuCharBoxLive.Api;
using static Senparc.Xncf.NeuCharBoxLive.Tests.Fakes.ScriptedChatClient;
using ScriptTurn = System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, Microsoft.Extensions.AI.ChatResponse>;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// The live API over a TestServer (Task 10): routes, error kinds and order, SSE framing and resume, CORS. The model is
/// scripted, the pacer instant and the clock fake; every wait is bounded by a real-time guard.
/// </summary>
[TestClass]
public class ApiTests
{
    // ---- scripts ----

    static ScriptedChatClient Script(params ChatResponse[] responses) => new(responses.Select(r => (ScriptTurn)((_, _) => r)).ToArray());

    static ChatResponse LampPlan() => Tool("submit_plan", new { steps = new[] { new { id = "s1", text = "Dim the floor lamp to 20%", devices = new[] { "lamp" } } } });

    static ChatResponse SetLamp20() => Tool("set_lamp", new { brightness = 20, step = "s1" });

    static ChatResponse CannotDo() => Tool("cannot_do", new { reason = "I can't order food here.", suggestions = new[] { "Dim the lamp" } });

    static ScriptedChatClient LampOk() => Script(LampPlan(), SetLamp20(), Text("DONE"), Text("I dimmed the floor lamp to 20%."));

    static ScriptedChatClient LampWhatIf() => Script(SetLamp20(), Text("DONE"), Text("The floor lamp didn't respond, so I couldn't dim it."));

    static void AssertError(LiveHost.Reply r, int status, string kind)
    {
        Assert.AreEqual(status, r.Code, $"{kind}: {r.Body?.ToJsonString()}");
        Assert.AreEqual(kind, r.Error, r.Body?.ToJsonString());
        Assert.IsFalse(string.IsNullOrWhiteSpace((string?)r.Body!["message"]), "every error carries a message");
    }

    // ---- health and access ----

    [TestMethod]
    public async Task Health_Shape()
    {
        await using var h = await LiveHost.StartAsync();
        var r = await h.GetAsync("health");
        Assert.AreEqual(200, r.Code);
        Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse("""{"ok":true,"liveEnabled":true,"scenes":["home","custom"]}"""), r.Body), r.Body?.ToJsonString());

        h.SetLimits(l => l with { DailyTokenBudget = 0 }); // today's budget is spent
        Assert.AreEqual(false, (bool)(await h.GetAsync("health")).Body!["liveEnabled"]!);
        h.SetLimits(l => l with { DailyTokenBudget = 1_000_000 });
        h.SetLive(false);
        Assert.AreEqual(false, (bool)(await h.GetAsync("health")).Body!["liveEnabled"]!);

        await using var noRuntime = await LiveHost.StartAsync(_ => null);
        r = await noRuntime.GetAsync("health");
        Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse("""{"ok":true,"liveEnabled":false,"scenes":["home","custom"]}"""), r.Body), r.Body?.ToJsonString());

        // Planning and running on different models: both must be there.
        var model = new LiveModel(LiveHost.Model, new ScriptedChatClient());
        await using var noRun = await LiveHost.StartAsync(_ => new LiveSnapshot(LiveLimits.Defaults, model, null, LiveHost.Origins, null));
        Assert.AreEqual(false, (bool)(await noRun.GetAsync("health")).Body!["liveEnabled"]!);
        await using var noPlan = await LiveHost.StartAsync(_ => new LiveSnapshot(LiveLimits.Defaults, null, model, LiveHost.Origins, null));
        Assert.AreEqual(false, (bool)(await noPlan.GetAsync("health")).Body!["liveEnabled"]!);
    }

    [TestMethod]
    public async Task AccessCheck_OkAndBad()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var r = await h.PostAsync("access/check", new { code = c.Code });
        Assert.AreEqual(200, r.Code);
        Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse("""{"ok":true,"remainingToday":20}"""), r.Body), r.Body?.ToJsonString());

        r = await h.PostAsync("access/check", new { code = " " + c.Code.ToLowerInvariant().Replace('-', ' ') + " " });
        Assert.AreEqual(200, r.Code, "codes compare case-insensitively with spaces stripped");

        AssertError(await h.PostAsync("access/check", new { code = "NCB-2222-2222" }), 401, "bad_code");
        AssertError(await h.PostAsync("access/check", new { }), 401, "bad_code");
        AssertError(await h.PostAsync("access/check", null), 401, "bad_code");
        AssertError(await h.PostAsync("access/check", "{not json"), 400, "bad_request");

        await h.Store.SetDisabledAsync(c.Id, true);
        AssertError(await h.PostAsync("access/check", new { code = c.Code }), 401, "bad_code");
    }

    /// <summary>
    /// Contract (server final review): access/check is rate-limited per IP, counted like apply (the last
    /// X-Forwarded-For hop, which the tunnel agent adds), NcbLive:AccessCheckPerMinute a minute (default 30).
    /// </summary>
    [TestMethod]
    public async Task AccessCheck_RateLimitedPerIp()
    {
        await using var h = await LiveHost.StartAsync(config: new() { ["NcbLive:AccessCheckPerMinute"] = "3" });
        var c = await h.NewCodeAsync();
        Task<(int Status, string Body)> Check(string xff, string? code = null, string remote = "127.0.0.1") =>
            h.RawAsync("POST", "/live/api/access/check", remote, "x.ngrok-free.app",
                new Dictionary<string, string> { ["X-Forwarded-For"] = xff, ["X-Forwarded-Proto"] = "https" },
                new JsonObject { ["code"] = code ?? c.Code }.ToJsonString());

        Assert.AreEqual(200, (await Check("198.51.100.9")).Status);
        Assert.AreEqual(401, (await Check("198.51.100.9", "NCB-2222-2222")).Status, "a wrong code counts too");
        Assert.AreEqual(200, (await Check("198.51.100.9")).Status);
        var r = await Check("198.51.100.9");
        Assert.AreEqual(429, r.Status, r.Body);
        StringAssert.Contains(r.Body, "\"error\":\"rate\"");
        StringAssert.Contains(r.Body, "\"message\":");
        Assert.AreEqual(429, (await Check("198.51.100.9", "NCB-2222-2222")).Status, "refused before the code is looked up");
        Assert.AreEqual(429, (await Check("203.0.113.1, 198.51.100.9")).Status, "a spoofed first entry doesn't reset the limit");
        Assert.AreEqual(200, (await Check("198.51.100.9, 198.51.100.10")).Status, "another hop is another IP");
        Assert.AreEqual(200, (await Check("198.51.100.9", remote: "192.0.2.10")).Status, "off the tunnel agent the remote address counts");
        h.Fake.Advance(TimeSpan.FromMinutes(1));
        Assert.AreEqual(200, (await Check("198.51.100.9")).Status, "a new minute");

        await using var shipped = await LiveHost.StartAsync();
        Assert.AreEqual(30, shipped.Get<AccessCheckRateLimiter>().Limit, "the default");
    }

    // ---- sessions ----

    [TestMethod]
    public async Task Health_ListsCustom()
    {
        await using var h = await LiveHost.StartAsync();
        var r = await h.GetAsync("health");
        CollectionAssert.AreEqual(new[] { "home", "custom" }, r.Body!["scenes"]!.AsArray().Select(x => (string)x!).ToArray());
    }

    static object[] Devs(params string[] ids) => ids.Select(id => (object)new { type = System.Text.RegularExpressions.Regex.Replace(id, @"_\d+$", ""), id }).ToArray();

    [TestMethod]
    public async Task CustomSession_201_WithRoomState()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var r = await h.PostAsync("sessions", new { scene = "custom", devices = Devs("floor_lamp", "floor_lamp_2", "soil_sensor") }, c.Code);
        Assert.AreEqual(201, r.Code, r.Body?.ToJsonString());
        var b = r.Body!.AsObject();
        Assert.IsFalse(string.IsNullOrEmpty((string?)b["sessionId"]));
        Assert.AreEqual("online", (string?)b["state"]!["floor_lamp_2"]!["status"]);
        Assert.AreEqual("online", (string?)b["state"]!["floor_lamp"]!["status"]);
        Assert.AreEqual(34.0, (double)b["state"]!["soil_sensor"]!["moisture"]!);
        Assert.IsNotNull(b["state"]!["env"]);
        Assert.IsNotNull(b["clock"]);
        var expected = CustomRoom.Build(new[] { new RoomDevice("floor_lamp", "floor_lamp"), new RoomDevice("floor_lamp", "floor_lamp_2"), new RoomDevice("soil_sensor", "soil_sensor") })
            .Suggestions.Take(4).ToList();
        CollectionAssert.AreEqual(expected, b["suggestions"]!.AsArray().Select(x => (string)x!).ToList());
    }

    [TestMethod]
    public async Task CustomSession_400_BadRoom()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        AssertError(await h.PostAsync("sessions", new { scene = "custom", devices = new object[] { new { type = "toaster", id = "toaster" } } }, c.Code), 400, "bad_room");
        AssertError(await h.PostAsync("sessions", new { scene = "custom", devices = Enumerable.Range(1, 21).Select(i => (object)new { type = "floor_lamp", id = i == 1 ? "floor_lamp" : $"floor_lamp_{i}" }).ToArray() }, c.Code), 400, "bad_room");
        AssertError(await h.PostAsync("sessions", new { scene = "custom", devices = new object[] { new { type = "floor_lamp", id = "lamp_x" } } }, c.Code), 400, "bad_room");
        AssertError(await h.PostAsync("sessions", new { scene = "custom", devices = new object[] { new { type = "floor_lamp" } } }, c.Code), 400, "bad_room");
        AssertError(await h.PostAsync("sessions", new { scene = "custom" }, c.Code), 400, "bad_room");
        AssertError(await h.PostAsync("sessions", new { scene = "custom", devices = Array.Empty<object>() }, c.Code), 400, "bad_room");
        AssertError(await h.PostAsync("sessions", "{\"scene\":\"custom\",", c.Code), 400, "bad_request");
    }

    [TestMethod]
    public async Task Sessions_CreateReturnsStateClockSuggestions()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var r = await h.PostAsync("sessions", new { scene = "home" },
            edit: m => m.Headers.TryAddWithoutValidation("Authorization", "bearer " + c.Code.ToLowerInvariant()));
        Assert.AreEqual(201, r.Code, r.Body?.ToJsonString());
        var b = r.Body!.AsObject();
        Assert.IsFalse(string.IsNullOrEmpty((string?)b["sessionId"]));
        Assert.AreEqual("online", (string?)b["state"]!["lamp"]!["status"]);
        Assert.AreEqual(16.5, (double)b["state"]!["env"]!["hour"]!);
        Assert.AreEqual("Fri 16:30", (string?)b["clock"]!["label"]);
        Assert.AreEqual("2026-11-13T16:30:00", (string?)b["clock"]!["iso"]);
        Assert.AreEqual(16.5, (double)b["clock"]!["hour"]!);
        var suggestions = b["suggestions"]!.AsArray().Select(x => (string)x!).ToList();
        Assert.IsTrue(suggestions.Count is >= 2 and <= 4, $"{suggestions.Count} suggestions");
        CollectionAssert.AreEqual(ManifestLoader.Load("home").Suggestions.Take(4).ToList(), suggestions);
        CollectionAssert.AreEqual(new[] { "sessionId", "state", "clock", "suggestions", "automations" }, b.Select(kv => kv.Key).ToList());
        Assert.AreEqual(0, b["automations"]!.AsArray().Count, "a new room has no automations");
    }

    /// <summary>
    /// Contract (server final review): a code holds at most 5 live sessions, and a 6th ends its least recently active
    /// idle one, whose tab then gets 404 no_session and whose stream ends; at the server's cap, 429 global_limit.
    /// </summary>
    [TestMethod]
    public async Task Sessions_PerCodeAndGlobalCaps()
    {
        await using var h = await LiveHost.StartAsync(config: new() { ["NcbLive:MaxSessions"] = "6" });
        Assert.AreEqual(6, h.Opt.MaxSessions, "bound from config");
        var c = await h.NewCodeAsync();
        var sids = new List<string>();
        for (var i = 0; i < 5; i++) sids.Add(await h.SessionAsync(c));
        await using var sse = await h.OpenSseAsync(c, sids[0]);
        foreach (var sid in sids.Skip(1)) // every route that finds a session counts as activity: sids[0] is now the least recent
        {
            h.Fake.Advance(TimeSpan.FromSeconds(1));
            Assert.AreEqual(200, (await h.PostAsync($"sessions/{sid}/reset", new { }, c.Code)).Code);
        }

        var sixth = await h.SessionAsync(c);

        Assert.IsTrue(await sse.EndsAsync(), "the ended session's stream closes");
        AssertError(await h.PostAsync($"sessions/{sids[0]}/requests", new { text = "Dim the lamp" }, c.Code), 404, "no_session");
        AssertError(await h.GetAsync($"sessions/{sids[0]}/events", c.Code), 404, "no_session");
        await (await h.OpenSseAsync(c, sixth)).DisposeAsync(); // the new session works
        foreach (var sid in sids.Skip(1)) await (await h.OpenSseAsync(c, sid)).DisposeAsync(); // and the other four are kept

        // 5 (this code) + 1 (another) = MaxSessions: a third code is refused.
        var other = await h.NewCodeAsync();
        await h.SessionAsync(other);
        AssertError(await h.PostAsync("sessions", new { scene = "home" }, (await h.NewCodeAsync()).Code), 429, "global_limit");
    }

    // ---- a request over SSE ----

    [TestMethod]
    public async Task Request_FullRunOverSse()
    {
        await using var h = await LiveHost.StartAsync();
        h.Chat.Inner = LampOk();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);

        var rid = await h.RequestAsync(c, sid);
        var planned = await sse.UntilAsync("awaiting_approval");
        CollectionAssert.AreEqual(new[] { "planning", "plan", "awaiting_approval" }, planned.Select(e => e.Kind).ToList());
        Assert.IsTrue(planned.All(e => e.Rid == rid));

        var approve = await h.PostAsync($"sessions/{sid}/requests/{rid}/approve", new { }, c.Code);
        Assert.AreEqual(202, approve.Code, approve.Body?.ToJsonString());
        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), approve.Body));

        var run = await sse.UntilAsync("done");
        var done = run[^1];
        Assert.AreEqual("ok", (string?)done.Data["outcome"]);
        Assert.AreEqual(19, (int)done.Data["remainingToday"]!);
        Assert.IsTrue(run.Any(e => e.Kind == "state" && (string?)e.Data["path"] == "lamp.brightness" && (double)e.Data["value"]! == 0.2));
        Assert.IsTrue(run.All(e => e.Rid == rid));
    }

    [TestMethod]
    public async Task Sse_FramingAndPing()
    {
        await using var h = await LiveHost.StartAsync();
        h.Chat.Inner = Script(LampPlan());
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);
        Assert.IsTrue(sse.Response.Headers.CacheControl is { NoCache: true, NoTransform: true }, sse.Response.Headers.CacheControl?.ToString());
        Assert.AreEqual("no", string.Join(",", sse.Response.Headers.GetValues("X-Accel-Buffering")));

        var rid = await h.RequestAsync(c, sid);
        string? line;
        do line = await sse.LineAsync(); while (line == ": ping" || line == "");
        Assert.AreEqual("id: 1", line);
        Assert.AreEqual("event: planning", await sse.LineAsync());
        Assert.AreEqual($$"""data: {"rid":"{{rid}}"}""", await sse.LineAsync());
        Assert.AreEqual("", await sse.LineAsync());
        await sse.UntilAsync("awaiting_approval");

        // Nothing new: after 15 s (fake) the stream sends a comment.
        Assert.AreEqual(": ping", await sse.AdvanceUntilLineAsync(h.Fake));
        Assert.AreEqual("", await sse.LineAsync());
    }

    [TestMethod]
    public async Task Sse_ReconnectMidRun_ReplaysMissedEventsOnce()
    {
        await using var h = await LiveHost.StartAsync();
        var chat = LampOk();
        h.Chat.Inner = chat;
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);

        var first = await h.OpenSseAsync(c, sid);
        var rid = await h.RequestAsync(c, sid);
        var seen1 = await first.UntilAsync("awaiting_approval");
        var n = seen1[^1].Id;
        await first.DisposeAsync(); // the visitor's tab drops mid-run

        // The run goes on with no one reading: approve it from another tab and let it finish server-side.
        Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/requests/{rid}/approve", new { }, c.Code)).Code);
        await LiveHost.WaitUntil(async () => (await h.Store.ListAsync(10)).SingleOrDefault()?.End is not null, "the run to end");

        await using var second = await h.OpenSseAsync(c, sid, after: n);
        var seen2 = await second.UntilAsync("done");
        Assert.AreEqual(n + 1, seen2[0].Id, "the replay starts right after the last id seen");
        var all = seen1.Concat(seen2).ToList();
        CollectionAssert.AreEqual(Enumerable.Range(1, all.Count).Select(i => (long)i).ToList(), all.Select(e => e.Id).ToList(),
            "every event exactly once, in order");
        Assert.AreEqual(1, all.Count(e => e.Kind == "done"));
        Assert.AreEqual(1, all.Count(e => e.Kind == "planning"));
        Assert.AreEqual("ok", (string?)seen2[^1].Data["outcome"]);
        Assert.AreEqual(": ping", await second.AdvanceUntilLineAsync(h.Fake), "nothing follows the request's done");

        var runs = await h.Store.ListAsync(10);
        Assert.AreEqual(1, runs.Count, "one run in the run store");
        Assert.AreEqual("ok", runs[0].End!.Outcome);
        Assert.AreEqual(4, chat.Calls.Count, "each model turn ran once");

        await using var fromStart = await h.OpenSseAsync(c, sid, after: 0);
        var replay = await fromStart.UntilAsync("done");
        CollectionAssert.AreEqual(all.Select(e => e.Id).ToList(), replay.Select(e => e.Id).ToList(), "after=0 replays the whole log");
    }

    [TestMethod]
    public async Task Sse_EndsWhenSessionExpires()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);
        var sessions = h.Get<SessionManager>();
        Assert.AreEqual(1, sessions.Count);

        // An open stream alone isn't activity: the sweep (every minute, on the fake clock) expires the session and the
        // stream ends, so the session isn't kept in memory behind it.
        h.Fake.Advance(h.Opt.SessionIdle);
        await LiveHost.WaitUntil(async () =>
        {
            if (sessions.Count == 0) return true;
            h.Fake.Advance(TimeSpan.FromMinutes(1));
            await Task.Delay(50);
            return sessions.Count == 0;
        }, "the sweep to expire the session");
        Assert.IsTrue(await sse.EndsAsync(), "the stream ends when its session expires");

        var again = await h.GetAsync($"sessions/{sid}/events", c.Code);
        AssertError(again, 404, "no_session");
    }

    // ---- errors ----

    [TestMethod]
    public async Task Errors_Order()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var other = await h.NewCodeAsync();
        h.Chat.Inner = Script(LampPlan());
        var busy = await h.SessionAsync(c);
        var idle = await h.SessionAsync(c);
        var r1 = await h.RequestAsync(c, busy); // waits for approval: the session is busy
        await using (var sse = await h.OpenSseAsync(c, busy)) await sse.UntilAsync("awaiting_approval");
        string Path(string sid) => $"sessions/{sid}/requests";
        var tooLong = new { text = new string('a', 401) };

        // 1. bad_code beats everything (and a missing header is bad_code too).
        h.SetLive(false);
        AssertError(await h.PostAsync(Path("nope"), new { }, "NCB-2222-2222"), 401, "bad_code");
        AssertError(await h.PostAsync(Path("nope"), new { }), 401, "bad_code");
        AssertError(await h.PostAsync(Path(busy), new { text = "x" },
            edit: m => m.Headers.TryAddWithoutValidation("Authorization", "Basic " + c.Code)), 401, "bad_code");
        // 2. live_off: live turned off in the runtime.
        AssertError(await h.PostAsync(Path("nope"), new { }, c.Code), 503, "live_off");
        h.SetLive(true);
        // 3. no_session: unknown, or another code's session.
        AssertError(await h.PostAsync(Path("nope"), new { }, c.Code), 404, "no_session");
        AssertError(await h.PostAsync(Path(busy), new { }, other.Code), 404, "no_session");
        // 4. bad_request: missing, empty or unreadable text, even while busy.
        AssertError(await h.PostAsync(Path(busy), new { }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(Path(busy), new { text = "  \n " }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(Path(busy), new { text = 5 }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(Path(busy), "{\"text\":", c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(Path(busy), null, c.Code), 400, "bad_request");
        // 5. too_long, even while busy.
        AssertError(await h.PostAsync(Path(busy), tooLong, c.Code), 400, "too_long");
        // 6. busy beats budget and the daily limits.
        h.SetLimits(l => l with { DailyTokenBudget = 0 });
        h.SetLimits(l => l with { GlobalDaily = 1 });
        await h.Store.SetLimitAsync(c.Id, 1);
        AssertError(await h.PostAsync(Path(busy), new { text = "Dim the lamp" }, c.Code), 409, "busy");
        // 7. budget beats the daily limits.
        AssertError(await h.PostAsync(Path(idle), new { text = "Dim the lamp" }, c.Code), 429, "budget");
        // 8. global_limit beats code_limit.
        h.SetLimits(l => l with { DailyTokenBudget = 1_000_000 });
        AssertError(await h.PostAsync(Path(idle), new { text = "Dim the lamp" }, c.Code), 429, "global_limit");
        // 9. code_limit: the limit changed mid-session (the session was made at 20) and applies at once.
        h.SetLimits(l => l with { GlobalDaily = 300 });
        AssertError(await h.PostAsync(Path(idle), new { text = "Dim the lamp" }, c.Code), 429, "code_limit");
        await h.Store.SetLimitAsync(c.Id, 20);
        h.Chat.Inner = Script(CannotDo());
        Assert.AreEqual(202, (await h.PostAsync(Path(idle), new { text = "Dim the lamp" }, c.Code)).Code);
        Assert.AreEqual(2, (await h.Store.ListAsync(10)).Count, "refused requests reserve nothing");
        Assert.IsNotNull(r1);

        // POST sessions: bad_code → live_off → budget → bad_request.
        h.SetLive(false);
        h.SetLimits(l => l with { DailyTokenBudget = 0 });
        AssertError(await h.PostAsync("sessions", new { scene = "mars" }, "NCB-2222-2222"), 401, "bad_code");
        AssertError(await h.PostAsync("sessions", new { scene = "mars" }, c.Code), 503, "live_off");
        h.SetLive(true);
        AssertError(await h.PostAsync("sessions", new { scene = "mars" }, c.Code), 429, "budget");
        h.SetLimits(l => l with { DailyTokenBudget = 1_000_000 });
        AssertError(await h.PostAsync("sessions", new { scene = "mars" }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync("sessions", new { }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync("sessions", "[1,2", c.Code), 400, "bad_request");

        // The stream checks the code, then the session.
        AssertError(await h.GetAsync($"sessions/{busy}/events", "NCB-2222-2222"), 401, "bad_code");
        AssertError(await h.GetAsync("sessions/nope/events", c.Code), 404, "no_session");
        AssertError(await h.GetAsync($"sessions/{busy}/events?after=abc", c.Code), 400, "bad_request");
        AssertError(await h.GetAsync($"sessions/{busy}/events?after=-1", c.Code), 400, "bad_request");
    }

    [TestMethod]
    public async Task Request_CodeDisabledBeforeAdmit_401()
    {
        // The code passes the route's check, then is disabled before admission: AdmitRequestAsync throws
        // ArgumentException for it, and the route answers 401 bad_code, not 500.
        InviteCode? code = null;
        string? sid = null;
        var disableNext = false;
        await using var h = await LiveHost.StartAsync(services: (host, s) =>
        {
            var inner = s.Last(d => d.ServiceType == typeof(Func<RequestRunner>));
            s.Remove(inner);
            s.AddSingleton<Func<RequestRunner>>(sp =>
            {
                var make = (Func<RequestRunner>)inner.ImplementationFactory!(sp);
                return () =>
                {
                    if (disableNext) // runs inside StartRequestAsync, after the route's check, before admission
                    {
                        disableNext = false;
                        sp.GetRequiredService<SessionManager>().Get(sid!, code! with { Disabled = true });
                    }
                    return make();
                };
            });
        });
        code = await h.NewCodeAsync();
        sid = await h.SessionAsync(code);
        disableNext = true;
        AssertError(await h.PostAsync($"sessions/{sid}/requests", new { text = "Dim the lamp" }, code.Code), 401, "bad_code");
        Assert.AreEqual(0, (await h.Store.ListAsync(10)).Count, "nothing reserved");

        h.Chat.Inner = Script(CannotDo());
        Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/requests", new { text = "Dim the lamp" }, code.Code)).Code,
            "the session isn't left busy");
    }

    [TestMethod]
    public async Task TooLong_400_At401Chars()
    {
        await using var h = await LiveHost.StartAsync();
        h.Chat.Inner = Script(CannotDo(), CannotDo(), CannotDo());
        var c = await h.NewCodeAsync();
        var a = await h.SessionAsync(c);
        var b = await h.SessionAsync(c);
        var e = await h.SessionAsync(c);
        string Path(string sid) => $"sessions/{sid}/requests";
        var emoji = string.Concat(Enumerable.Repeat("\U0001F4A1", 200)); // 400 UTF-16 units

        AssertError(await h.PostAsync(Path(a), new { text = new string('a', 401) }, c.Code), 400, "too_long");
        AssertError(await h.PostAsync(Path(a), new { text = " " + new string('a', 401) + " " }, c.Code), 400, "too_long");
        AssertError(await h.PostAsync(Path(a), new { text = emoji + "a" }, c.Code), 400, "too_long");
        Assert.AreEqual(202, (await h.PostAsync(Path(a), new { text = new string('a', 400) }, c.Code)).Code);
        Assert.AreEqual(202, (await h.PostAsync(Path(b), new { text = "   " + new string('a', 400) + "\n  " }, c.Code)).Code);
        Assert.AreEqual(202, (await h.PostAsync(Path(e), new { text = emoji }, c.Code)).Code);
    }

    // ---- apply ----

    [TestMethod]
    public async Task Apply_HoneypotRateAndValidation()
    {
        await using var h = await LiveHost.StartAsync();
        var apps = h.Get<IApplicationStore>();
        Task<(int Status, string Body)> Apply(string json, string xff, string remote = "127.0.0.1") =>
            h.RawAsync("POST", "/live/api/apply", remote, "x.ngrok-free.app",
                new Dictionary<string, string> { ["X-Forwarded-For"] = xff, ["X-Forwarded-Proto"] = "https" }, json);
        static string Form(string email = "ada@example.com", string interest = "lab", string website = "", string name = "Ada",
            string wouldTry = "Water the plant", string scene = "home") =>
            new JsonObject
            {
                ["email"] = email, ["name"] = name, ["interest"] = interest, ["wouldTry"] = wouldTry,
                ["scene"] = scene, ["website"] = website,
            }.ToJsonString();

        // The honeypot: still 202, nothing stored.
        var r = await Apply(Form(website: "http://spam.example"), "198.51.100.1");
        Assert.AreEqual(202, r.Status);
        Assert.AreEqual("{}", r.Body);
        Assert.AreEqual(0, (await apps.ListAsync()).Count);

        r = await Apply(Form(), "198.51.100.1");
        Assert.AreEqual(202, r.Status);
        Assert.AreEqual("{}", r.Body);
        var stored = (await apps.ListAsync()).Single();
        Assert.AreEqual(("ada@example.com", "Ada", "lab", "Water the plant", "home", "new", (long?)null),
            (stored.Email, stored.Name, stored.Interest, stored.WouldTry, stored.Scene, stored.Status, stored.CodeId));
        Assert.AreEqual(h.Fake.GetUtcNow(), stored.Submitted);

        // Validation: email has an @ and is at most 200 characters; interest is one of the five.
        foreach (var bad in new[]
                 {
                     Form(email: "ada.example.com"), Form(email: new string('a', 189) + "@example.com"), Form(interest: "spam"),
                     """{"email":"ada@example.com","wouldTry":"x","scene":"home","website":""}""",
                 })
        {
            var b = await Apply(bad, "198.51.100.2");
            Assert.AreEqual(400, b.Status, bad);
            StringAssert.Contains(b.Body, "\"error\":\"bad_request\"");
        }
        Assert.AreEqual(400, (await Apply("{\"email\":", "198.51.100.3")).Status);
        Assert.AreEqual(202, (await Apply(Form(email: new string('a', 188) + "@example.com"), "198.51.100.3")).Status, "200 characters is allowed");
        foreach (var interest in new[] { "home", "warehouse", "business", "other" })
            Assert.AreEqual(202, (await Apply(Form(interest: interest), "198.51.100.4")).Status, interest);

        // The other fields are capped as the admin's columns are (after trim): name 120, wouldTry 400, scene 32.
        foreach (var (bad, what) in new[]
                 {
                     (Form(name: new string('n', 121)), "name"), (Form(wouldTry: new string('w', 401)), "wouldTry"),
                     (Form(scene: new string('s', 33)), "scene"),
                 })
            Assert.AreEqual(400, (await Apply(bad, "198.51.100.5")).Status, what);
        Assert.AreEqual(202, (await Apply(Form(name: " " + new string('n', 120) + " ", wouldTry: new string('w', 400),
            scene: new string('s', 32)), "198.51.100.5")).Status, "at the caps");

        // 5 per IP per hour. The IP is the hop ngrok adds (the last X-Forwarded-For entry): earlier ones are the client's.
        for (var i = 0; i < 5; i++) Assert.AreEqual(202, (await Apply(Form(), "198.51.100.9")).Status);
        r = await Apply(Form(), "198.51.100.9");
        Assert.AreEqual(429, r.Status);
        StringAssert.Contains(r.Body, "\"error\":\"rate\"");
        Assert.AreEqual(429, (await Apply(Form(), "203.0.113.1, 198.51.100.9")).Status, "a spoofed first entry doesn't reset the limit");
        Assert.AreEqual(202, (await Apply(Form(), "198.51.100.9, 198.51.100.10")).Status, "another hop is another IP");
        Assert.AreEqual(202, (await Apply(Form(website: "x"), "198.51.100.9")).Status, "the honeypot answers before the limit");
        // Not through the local tunnel agent: X-Forwarded-For is ignored and the remote address counts.
        Assert.AreEqual(202, (await Apply(Form(), "198.51.100.9", remote: "192.0.2.10")).Status);
        h.Fake.Advance(TimeSpan.FromHours(1));
        Assert.AreEqual(202, (await Apply(Form(), "198.51.100.9")).Status, "a new hour");
    }

    // ---- what-if, decide, reset ----

    [TestMethod]
    public async Task WhatIf_OnlyLatestFinished()
    {
        await using var h = await LiveHost.StartAsync();
        h.Chat.Inner = LampOk();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);
        string Path(string rid) => $"sessions/{sid}/requests/{rid}/whatif";
        var lamp = new { device = "lamp" };

        var rid = await h.RequestAsync(c, sid);
        await sse.UntilAsync("awaiting_approval");
        AssertError(await h.PostAsync(Path("rnope"), lamp, c.Code), 404, "no_request");
        AssertError(await h.PostAsync(Path(rid), lamp, c.Code), 409, "not_waiting");
        Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/requests/{rid}/approve", new { }, c.Code)).Code);
        Assert.AreEqual("ok", (string?)(await sse.UntilAsync("done"))[^1].Data["outcome"]);

        AssertError(await h.PostAsync(Path(rid), new { device = "toaster" }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(Path(rid), new { }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(Path(rid), "{\"device\"", c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(Path(rid), lamp, "NCB-2222-2222"), 401, "bad_code");
        AssertError(await h.PostAsync($"sessions/nope/requests/{rid}/whatif", lamp, c.Code), 404, "no_session");
        h.SetLive(false);
        AssertError(await h.PostAsync(Path(rid), lamp, c.Code), 503, "live_off");
        h.SetLive(true);
        h.SetLimits(l => l with { DailyTokenBudget = 0 });
        AssertError(await h.PostAsync(Path(rid), lamp, c.Code), 429, "budget");
        h.SetLimits(l => l with { DailyTokenBudget = 1_000_000 });

        h.Chat.Inner = LampWhatIf();
        var r = await h.PostAsync(Path(rid), lamp, c.Code);
        Assert.AreEqual(202, r.Code, r.Body?.ToJsonString());
        var wid = (string)r.Body!["requestId"]!;
        Assert.AreNotEqual(rid, wid);
        var replay = await sse.UntilAsync("done");
        Assert.AreEqual("room", replay[0].Kind);
        Assert.AreEqual("automations", replay[1].Kind, "the replayed room's automations list follows the room");
        Assert.AreEqual("plan", replay[2].Kind);
        Assert.IsFalse(replay.Any(e => e.Kind == "awaiting_approval"), "a what-if replays without asking");
        Assert.IsTrue(replay.All(e => e.Rid == wid));
        AssertError(await h.PostAsync(Path(wid), lamp, c.Code), 409, "not_waiting");
        Assert.AreEqual(1, (await h.Store.CountTodayAsync(h.Get<UsageDay>().For(h.Fake.GetUtcNow()), c.Code)), "what-ifs don't count");
    }

    [TestMethod]
    public async Task Decide_UnknownRid404_NotWaiting409()
    {
        await using var h = await LiveHost.StartAsync();
        h.Chat.Inner = Script(LampPlan());
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);

        foreach (var verb in new[] { "approve", "decline" })
        {
            AssertError(await h.PostAsync($"sessions/{sid}/requests/rnope/{verb}", new { }, c.Code), 404, "no_request");
            AssertError(await h.PostAsync($"sessions/{sid}/requests/rnope/{verb}", new { }, "NCB-2222-2222"), 401, "bad_code");
            AssertError(await h.PostAsync($"sessions/nope/requests/rnope/{verb}", new { }, c.Code), 404, "no_session");
        }

        var rid = await h.RequestAsync(c, sid);
        await sse.UntilAsync("awaiting_approval");
        var r = await h.PostAsync($"sessions/{sid}/requests/{rid}/decline", null, c.Code);
        Assert.AreEqual(202, r.Code, r.Body?.ToJsonString());
        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), r.Body));
        Assert.AreEqual("declined", (string?)(await sse.UntilAsync("done"))[^1].Data["outcome"]);
        AssertError(await h.PostAsync($"sessions/{sid}/requests/{rid}/approve", new { }, c.Code), 409, "not_waiting");
        AssertError(await h.PostAsync($"sessions/{sid}/requests/{rid}/decline", new { }, c.Code), 409, "not_waiting");
    }

    [TestMethod]
    public async Task Reset_200_And409MidRequest()
    {
        await using var h = await LiveHost.StartAsync();
        h.Chat.Inner = LampOk();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);
        var reset = $"sessions/{sid}/reset";

        var rid = await h.RequestAsync(c, sid);
        await sse.UntilAsync("awaiting_approval");
        AssertError(await h.PostAsync(reset, new { }, c.Code), 409, "busy");
        await h.PostAsync($"sessions/{sid}/requests/{rid}/approve", new { }, c.Code);
        await sse.UntilAsync("done"); // the lamp is at 20% now

        AssertError(await h.PostAsync(reset, new { }, "NCB-2222-2222"), 401, "bad_code");
        AssertError(await h.PostAsync("sessions/nope/reset", new { }, c.Code), 404, "no_session");
        var r = await h.PostAsync(reset, new { }, c.Code);
        Assert.AreEqual(200, r.Code, r.Body?.ToJsonString());
        var b = r.Body!.AsObject();
        CollectionAssert.AreEqual(new[] { "state", "clock", "automations" }, b.Select(kv => kv.Key).ToList());
        Assert.AreEqual(0, b["automations"]!.AsArray().Count);
        Assert.AreEqual(0, (double)b["state"]!["lamp"]!["brightness"]!);
        Assert.AreEqual(false, (bool)b["state"]!["lamp"]!["on"]!);
        Assert.AreEqual(16.5, (double)b["state"]!["env"]!["hour"]!);
        Assert.AreEqual("Fri 16:30", (string?)b["clock"]!["label"]);
        Assert.AreEqual(": ping", await sse.AdvanceUntilLineAsync(h.Fake), "reset emits no event");
    }

    // ---- edits and the clock ----

    [TestMethod]
    public async Task Set_202_400_401_404_409()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        var set = $"sessions/{sid}/set";

        var r = await h.PostAsync(set, new { path = "soil.moisture", value = 55 }, c.Code);
        Assert.AreEqual(202, r.Code, r.Body?.ToJsonString());
        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), r.Body));

        AssertError(await h.PostAsync(set, new { path = "lamp.status", value = "fault" }, c.Code), 400, "bad_value");
        AssertError(await h.PostAsync(set, new { path = "soil.moisture", value = 101 }, c.Code), 400, "bad_value");
        AssertError(await h.PostAsync(set, new { path = "soil.moisture", value = "wet" }, c.Code), 400, "bad_value");
        AssertError(await h.PostAsync(set, "{not json", c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(set, new { value = 5 }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(set, new { path = "soil.moisture" }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(set, new JsonArray(), c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(set, new { path = "soil.moisture", value = 5 }), 401, "bad_code");
        AssertError(await h.PostAsync(set, new { path = "soil.moisture", value = 5 }, "NCB-2222-2222"), 401, "bad_code");
        AssertError(await h.PostAsync("sessions/nope/set", new { path = "soil.moisture", value = 5 }, c.Code), 404, "no_session");

        h.Get<SessionManager>().Get(sid, c)!.Room.Set("lamp.status", JsonValue.Create("fault"));
        AssertError(await h.PostAsync(set, new { path = "lamp.brightness", value = 0.5 }, c.Code), 409, "device_unavailable");
    }

    [TestMethod]
    public async Task Set_RateLimitedPerSession()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        var other = await h.SessionAsync(c);

        for (var i = 0; i < 10; i++)
            Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/set", new { path = "soil.moisture", value = i }, c.Code)).Code);
        AssertError(await h.PostAsync($"sessions/{sid}/set", new { path = "soil.moisture", value = 1 }, c.Code), 429, "rate");
        AssertError(await h.PostAsync($"sessions/{sid}/advance", new { hours = 1 }, c.Code), 429, "rate");
        Assert.AreEqual(202, (await h.PostAsync($"sessions/{other}/set", new { path = "soil.moisture", value = 1 }, c.Code)).Code,
            "another session has its own budget");
        h.Fake.Advance(TimeSpan.FromSeconds(5));
        Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/set", new { path = "soil.moisture", value = 1 }, c.Code)).Code,
            "a new window");
    }

    [TestMethod]
    public async Task Set_IdleEdit_StreamsOnSse()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);

        Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/set", new { path = "soil.moisture", value = 25 }, c.Code)).Code);
        var e = await sse.NextAsync();
        Assert.AreEqual("state", e.Kind);
        Assert.AreEqual("edit", (string?)e.Data["src"]);
        Assert.IsNull(e.Data["rid"], "outside a request there is no rid");
        Assert.AreEqual("soil.moisture", (string?)e.Data["path"]);
        Assert.AreEqual(25, (double)e.Data["value"]!);
    }

    [TestMethod]
    public async Task Advance_200WithClock_And409MidRequest()
    {
        await using var h = await LiveHost.StartAsync();
        h.Chat.Inner = LampOk();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);
        var advance = $"sessions/{sid}/advance";

        var rid = await h.RequestAsync(c, sid);
        await sse.UntilAsync("awaiting_approval");
        AssertError(await h.PostAsync(advance, new { hours = 1 }, c.Code), 409, "busy");
        await h.PostAsync($"sessions/{sid}/requests/{rid}/approve", new { }, c.Code);
        await sse.UntilAsync("done");

        AssertError(await h.PostAsync(advance, new { hours = 1 }, "NCB-2222-2222"), 401, "bad_code");
        AssertError(await h.PostAsync("sessions/nope/advance", new { hours = 1 }, c.Code), 404, "no_session");
        foreach (var bad in new object[] { 0, -1, 200, 168.5 })
            AssertError(await h.PostAsync(advance, new { hours = bad }, c.Code), 400, "bad_value");
        AssertError(await h.PostAsync(advance, new { hours = "2" }, c.Code), 400, "bad_value");
        AssertError(await h.PostAsync(advance, new { }, c.Code), 400, "bad_request");
        AssertError(await h.PostAsync(advance, "{not json", c.Code), 400, "bad_request");

        var before = (double)(await h.PostAsync($"sessions/{sid}/reset", new { }, c.Code)).Body!["clock"]!["hour"]!;
        var r = await h.PostAsync(advance, new { hours = 1 }, c.Code);
        Assert.AreEqual(200, r.Code, r.Body?.ToJsonString());
        CollectionAssert.AreEqual(new[] { "clock" }, r.Body!.AsObject().Select(kv => kv.Key).ToList());
        Assert.AreEqual(before + 1, (double)r.Body["clock"]!["hour"]!, 1e-6);
        Assert.AreEqual("Fri 17:30", (string?)r.Body["clock"]!["label"]);
    }

    [TestMethod]
    public async Task Custom_SetAndAdvance_UseTheCatalog()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var r = await h.PostAsync("sessions", new { scene = "custom", devices = Devs("soil_sensor", "water_pump") }, c.Code);
        Assert.AreEqual(201, r.Code, r.Body?.ToJsonString());
        var sid = (string)r.Body!["sessionId"]!;
        var set = $"sessions/{sid}/set";

        var ok = await h.PostAsync(set, new { path = "soil_sensor.moisture", value = 40 }, c.Code);
        Assert.AreEqual(202, ok.Code, ok.Body?.ToJsonString());
        Assert.AreEqual(202, (await h.PostAsync(set, new { path = "water_pump.running", value = false }, c.Code)).Code);
        AssertError(await h.PostAsync(set, new { path = "soil_sensor.status", value = "fault" }, c.Code), 400, "bad_value");
        AssertError(await h.PostAsync(set, new { path = "soil.moisture", value = 25 }, c.Code), 400, "bad_value");

        // The clock plays the catalog's drift: soil dries 0.0833 per hour.
        var adv = await h.PostAsync($"sessions/{sid}/advance", new { hours = 12 }, c.Code);
        Assert.AreEqual(200, adv.Code, adv.Body?.ToJsonString());
        Assert.AreEqual("Sat 04:30", (string?)adv.Body!["clock"]!["label"]);
        Assert.AreEqual(39, (double)h.Get<SessionManager>().Get(sid, c)!.Room.Get("soil_sensor.moisture")!, 0.01);
    }

    // ---- error copy ----

    [TestMethod]
    public void ErrorMessages_PassGuard()
    {
        var expected = new Dictionary<string, (int, string)>
        {
            ["bad_request"] = (400, "That request couldn't be read."),
            ["bad_room"] = (400, "That room can't be set up. Check its devices and try again."),
            ["too_long"] = (400, "Keep it under 400 characters."),
            ["bad_code"] = (401, "That invite code isn't valid."),
            ["no_session"] = (404, "This live session has ended."),
            ["no_request"] = (404, "That request isn't in this session."),
            ["no_automation"] = (404, "That automation isn't in this room any more."),
            ["bad_value"] = (400, "That value isn't allowed for this device."),
            ["device_unavailable"] = (409, "That device can't be changed right now."),
            ["busy"] = (409, "Wait for the current request to finish."),
            ["not_waiting"] = (409, "Nothing is waiting for an answer."),
            ["code_limit"] = (429, "That's today's live requests for this code. Try again tomorrow."),
            ["global_limit"] = (429, "Live AI has hit today's limit. Try again tomorrow."),
            ["budget"] = (429, "Live AI is resting for today."),
            ["rate"] = (429, "Too many tries from here. Try again later."), // apply, access/check, set, advance and automations
            ["live_off"] = (503, "Live AI is offline right now."),
            ["internal"] = (500, "Something went wrong on our side. Try again in a moment."),
        };
        CollectionAssert.AreEquivalent(expected.Keys.ToList(), LiveErrors.All.Keys.ToList(), "every contract kind, and only those");
        foreach (var (kind, (status, message)) in expected)
        {
            var e = LiveErrors.All[kind];
            Assert.AreEqual(kind, e.Kind);
            Assert.AreEqual(status, e.Status, kind);
            Assert.AreEqual(message, e.Message, kind);
            Assert.IsFalse(string.IsNullOrWhiteSpace(e.Message), kind);
            Assert.AreEqual(0, CopyGuard.Find(e.Message).Count, kind);
        }
    }

    // ---- CORS ----

    [TestMethod]
    public void Cors_TryNormalize_OnlyOrigins()
    {
        CollectionAssert.AreEqual(new[] { "https://leozheng265.github.io" }, LiveCors.BuiltIns.ToList());
        CollectionAssert.AreEqual(new[] { "http://localhost:8765" }, LiveCors.Development.ToList());
        foreach (var (raw, want) in new[] { ("https://demo.neucharbox.com/", "https://demo.neucharbox.com"),
                     (" https://LEOZHENG265.github.io ", "https://LEOZHENG265.github.io"), ("http://localhost:8765", "http://localhost:8765") })
        {
            Assert.IsTrue(LiveCors.TryNormalize(raw, out var origin), raw);
            Assert.AreEqual(want, origin, raw);
        }
        // A wildcard would open the API to every site (CORS treats "*" as any origin); a path or scheme isn't an origin.
        foreach (var bad in new[] { "*", "https://*.github.io", "https://demo.neucharbox.com/live", "ftp://demo.neucharbox.com", "null",
                     "demo.neucharbox.com", " ", "", null!, "https://demo.neucharbox.com?x=1", "https://demo.neucharbox.com/#top",
                     "https://user@demo.neucharbox.com" })
        {
            Assert.IsFalse(LiveCors.TryNormalize(bad, out var origin), bad);
            Assert.AreEqual("", origin, bad);
        }
    }

    [TestMethod]
    public async Task Cors_PreflightAndSseOrigin()
    {
        // The snapshot's origins: the built-ins, the local demo and one more.
        await using var h = await LiveHost.StartAsync(host => host.OnSnapshot(origins: LiveHost.Origins.Append("https://demo.neucharbox.com").ToArray()));
        static string[] Values(HttpResponseMessage m, string header) =>
            m.Headers.TryGetValues(header, out var v) ? v.SelectMany(x => x.Split(',')).Select(x => x.Trim()).ToArray() : Array.Empty<string>();

        async Task<HttpResponseMessage> Preflight(string path, string origin, string method)
        {
            var req = new HttpRequestMessage(HttpMethod.Options, path);
            req.Headers.TryAddWithoutValidation("Origin", origin);
            req.Headers.TryAddWithoutValidation("Access-Control-Request-Method", method);
            req.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "authorization,ngrok-skip-browser-warning");
            var resp = await h.Client.SendAsync(req);
            Assert.AreEqual(0, Values(resp, "Access-Control-Allow-Credentials").Length, "never credentials");
            return resp;
        }

        foreach (var origin in new[] { "https://leozheng265.github.io", "http://localhost:8765", "https://demo.neucharbox.com" })
        foreach (var (path, method) in new[] { ("/live/api/health", "GET"), ("/live/api/sessions/x/requests", "POST"), ("/live/api/sessions/x/events", "GET") })
        {
            using var p = await Preflight(path, origin, method);
            Assert.AreEqual(204, (int)p.StatusCode, $"{origin} {path}");
            CollectionAssert.AreEqual(new[] { origin }, Values(p, "Access-Control-Allow-Origin"));
            CollectionAssert.AreEquivalent(new[] { "GET", "POST", "OPTIONS" }, Values(p, "Access-Control-Allow-Methods"));
            CollectionAssert.AreEquivalent(new[] { "authorization", "content-type", "accept", "ngrok-skip-browser-warning" },
                Values(p, "Access-Control-Allow-Headers").Select(x => x.ToLowerInvariant()).ToList());
            CollectionAssert.AreEqual(new[] { "600" }, Values(p, "Access-Control-Max-Age"));
        }

        // Off the list: another site, a sandboxed or file:// page ("null"), a lookalike host, the wrong scheme or port.
        foreach (var origin in new[] { "https://evil.example", "null", "https://leozheng265.github.io.evil.example",
                     "https://evil.leozheng265.github.io", "http://leozheng265.github.io", "http://localhost:8766", "https://leozheng265.github.io/" })
        {
            using var evil = await Preflight("/live/api/health", origin, "GET");
            Assert.AreEqual(0, Values(evil, "Access-Control-Allow-Origin").Length, $"{origin} gets no Allow-Origin");
            var get = await h.GetAsync("health", edit: m => m.Headers.TryAddWithoutValidation("Origin", origin));
            Assert.AreEqual(0, Values(get.Message, "Access-Control-Allow-Origin").Length, $"{origin} on a GET");
        }

        var health = await h.GetAsync("health", edit: m => m.Headers.TryAddWithoutValidation("Origin", "https://leozheng265.github.io"));
        CollectionAssert.AreEqual(new[] { "https://leozheng265.github.io" }, Values(health.Message, "Access-Control-Allow-Origin"));
        Assert.AreEqual(0, Values(health.Message, "Access-Control-Allow-Credentials").Length);

        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using (var sse = await h.OpenSseAsync(c, sid, origin: "https://leozheng265.github.io"))
            CollectionAssert.AreEqual(new[] { "https://leozheng265.github.io" }, Values(sse.Response, "Access-Control-Allow-Origin"),
                "the SSE response carries the Allow-Origin");
        await using (var sse = await h.OpenSseAsync(c, sid, origin: "https://leozheng265.github.io"))
            Assert.AreEqual(0, Values(sse.Response, "Access-Control-Allow-Credentials").Length);
        await using (var sse = await h.OpenSseAsync(c, sid, origin: "https://evil.example"))
            Assert.AreEqual(0, Values(sse.Response, "Access-Control-Allow-Origin").Length);
    }

    // ---- before the module is installed: the store can't be read ----

    [TestMethod]
    public async Task BeforeInstall_StoreUnreadable_HealthIs200LiveOff_OtherRoutesAreLiveOff_Not500()
    {
        await using var h = await LiveHost.StartAsync(services: (_, s) => s.AddSingleton<Faults>(), builder: b => b.UseStores<FaultyStore>());
        h.Get<Faults>().On = m => new InvalidOperationException("SQLite Error 1: 'no such table: Senparc_NeuCharBoxLive_Run'");

        var r = await h.GetAsync("health");
        Assert.AreEqual(200, r.Code, r.Body?.ToJsonString());
        Assert.AreEqual(true, (bool)r.Body!["ok"]!);
        Assert.AreEqual(false, (bool)r.Body["liveEnabled"]!);

        AssertError(await h.PostAsync("access/check", new { code = "NCB-2222-2222" }), 503, "live_off");
        AssertError(await h.PostAsync("sessions", new { scene = "home" }, "NCB-2222-2222"), 503, "live_off");
        AssertError(await h.PostAsync("sessions/x/requests", new { text = "Dim the lamp" }, "NCB-2222-2222"), 503, "live_off");
        AssertError(await h.PostAsync("apply", new { email = "a@example.com", interest = "home", wouldTry = "x", scene = "home" }), 503, "live_off");

        // After install the same host works again, with no restart.
        h.Get<Faults>().On = _ => null;
        h.Fake.Advance(TimeSpan.FromSeconds(6));
        Assert.AreEqual(true, (bool)(await h.GetAsync("health")).Body!["liveEnabled"]!);
        var c = await h.NewCodeAsync();
        Assert.AreEqual(200, (await h.PostAsync("access/check", new { code = c.Code })).Code);
    }

    // ---- unexpected errors ----

    [TestMethod]
    public async Task UnexpectedError_500Internal_NoStackTrace()
    {
        // The developer exception page (on in Development) sits between the module's branch and routing. A broken store
        // must still answer the contract's 500 internal, with nothing of the exception in it.
        await using var h = await LiveHost.StartAsync(services: (_, s) => s.AddSingleton<Faults>(),
            builder: b => b.UseStores<FaultyStore>(), beforeRouting: app => app.UseDeveloperExceptionPage());
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        h.Get<Faults>().On = m => m == nameof(IInviteStore.FindAsync)
            ? new InvalidOperationException("the store broke in Senparc.Xncf.NeuCharBoxLive.Tests.Fakes.FaultyStore")
            : null;

        foreach (var (method, path, json) in new (string, string, string?)[]
                 {
                     ("POST", "/live/api/sessions", """{"scene":"home"}"""), ("GET", $"/live/api/sessions/{sid}/events", null),
                     ("POST", "/live/api/access/check", """{"code":"x"}"""), ("POST", $"/live/api/sessions/{sid}/requests", """{"text":"hi"}"""),
                 })
        foreach (var accept in new[] { "text/html", "application/json", "*/*" })
        {
            var r = await h.RawAsync(method, path, "127.0.0.1", "x.ngrok-free.app", new Dictionary<string, string>
            {
                ["X-Forwarded-For"] = "203.0.113.9", ["Authorization"] = "Bearer " + c.Code, ["Accept"] = accept,
            }, json);
            Assert.AreEqual(500, r.Status, $"{method} {path} ({accept})");
            foreach (var leak in new[] { "   at ", ".cs:line", "NcbLive.", "the store broke" })
                Assert.IsFalse(r.Body.Contains(leak), $"{path} leaks '{leak}': {r.Body}");
            var body = JsonNode.Parse(r.Body)!;
            Assert.AreEqual("internal", (string?)body["error"]);
            Assert.AreEqual("Something went wrong on our side. Try again in a moment.", (string?)body["message"]);
        }
    }

    [TestMethod]
    public async Task StoreArgumentErrors_AreInternal_NotBadCode()
    {
        // Only admission's own disabled-code guard means bad_code; a store's argument errors are server errors.
        await using var h = await LiveHost.StartAsync(services: (_, s) => s.AddSingleton<Faults>(), builder: b => b.UseStores<FaultyStore>());
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        var faults = h.Get<Faults>();
        foreach (var e in new Exception[] { new ArgumentNullException("c"), new ArgumentOutOfRangeException("c"), new ArgumentException("no such code", "id") })
        {
            faults.On = m => m == nameof(IInviteStore.TouchAsync) ? e : null; // TouchAsync runs inside admission
            AssertError(await h.PostAsync($"sessions/{sid}/requests", new { text = "Dim the lamp" }, c.Code), 500, "internal");
        }
        faults.On = _ => null;
        h.Chat.Inner = Script(CannotDo());
        Assert.AreEqual(202, (await h.PostAsync($"sessions/{sid}/requests", new { text = "Dim the lamp" }, c.Code)).Code,
            "nothing was left reserved or busy");
    }

    sealed class Lifetime : IHttpRequestLifetimeFeature
    {
        public CancellationToken RequestAborted { get; set; }
        public bool Aborted { get; private set; }
        public void Abort() => Aborted = true;
    }

    sealed class StartedResponse : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }

    static ExceptionContext FilterContext(Exception e, bool aborted, bool started, out Lifetime lifetime)
    {
        var http = new DefaultHttpContext();
        lifetime = new Lifetime { RequestAborted = aborted ? new CancellationToken(true) : CancellationToken.None };
        http.Features.Set<IHttpRequestLifetimeFeature>(lifetime);
        if (started) http.Features.Set<IHttpResponseFeature>(new StartedResponse());
        http.Request.Headers.Authorization = "Bearer NCB-SECRET-CODE";
        var route = new RouteData();
        route.Values["sid"] = "abc123";
        var action = new ActionDescriptor { AttributeRouteInfo = new AttributeRouteInfo { Template = "live/api/sessions/{sid}/events" } };
        return new ExceptionContext(new ActionContext(http, route, action), new List<IFilterMetadata>()) { Exception = e };
    }

    [TestMethod]
    public async Task ExceptionFilter_ClientAbortsQuiet_StartedStreamsCut_ElseInternal()
    {
        var log = new ListLogger<LiveExceptionFilter>();
        var filter = new LiveExceptionFilter(log);

        foreach (var e in new Exception[] { new OperationCanceledException(), new IOException("connection reset") })
        {
            var ctx = FilterContext(e, aborted: true, started: true, out var lt);
            await filter.OnExceptionAsync(ctx);
            Assert.IsTrue(ctx.ExceptionHandled, e.GetType().Name);
            Assert.IsInstanceOfType<EmptyResult>(ctx.Result);
            Assert.IsFalse(lt.Aborted, "the client already left");
        }
        Assert.AreEqual(0, log.Entries.Count, "a client leaving isn't an error");

        var started = FilterContext(new InvalidOperationException("boom"), aborted: false, started: true, out var cut);
        await filter.OnExceptionAsync(started);
        Assert.IsTrue(started.ExceptionHandled);
        Assert.IsTrue(cut.Aborted, "a started stream can't carry a JSON error, so the connection is cut");

        var fresh = FilterContext(new InvalidOperationException("boom"), aborted: false, started: false, out var kept);
        await filter.OnExceptionAsync(fresh);
        Assert.IsTrue(fresh.ExceptionHandled);
        Assert.IsFalse(kept.Aborted);
        var result = (ContentResult)fresh.Result!;
        Assert.AreEqual(500, result.StatusCode);
        Assert.AreEqual("internal", (string?)JsonNode.Parse(result.Content!)!["error"]);

        Assert.AreEqual(2, log.Entries.Count(x => x.Level == LogLevel.Error));
        foreach (var entry in log.Entries)
        {
            StringAssert.Contains(entry.Message, "live/api/sessions/{sid}/events");
            StringAssert.Contains(entry.Message, "abc123");
            Assert.IsFalse(entry.Message.Contains("NCB-SECRET-CODE"), "no headers in the log");
        }
    }

    [TestMethod]
    public async Task Sse_EndsOnShutdown()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        await using var sse = await h.OpenSseAsync(c, sid);
        h.Get<IHostApplicationLifetime>().StopApplication();
        Assert.IsTrue(await sse.EndsAsync(), "open streams end when the host stops, so shutdown doesn't wait on them");
    }

    /// <summary>Kestrel's BadHttpRequestException while reading a body (a broken chunk, a body over its limit).</summary>
    sealed class BrokenBody : Stream
    {
        static Exception Bad() => new BadHttpRequestException("Unexpected end of request content.", 400);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw Bad();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => throw Bad();
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => throw Bad();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [TestMethod]
    public async Task BodyReadFailure_IsBadRequest()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        foreach (var (path, code) in new (string, string?)[]
                 {
                     ("/live/api/access/check", null), ("/live/api/apply", null), ("/live/api/sessions", c.Code),
                     ($"/live/api/sessions/{sid}/requests", c.Code),
                 })
        {
            var headers = new Dictionary<string, string>();
            if (code is not null) headers["Authorization"] = "Bearer " + code;
            var r = await h.RawAsync("POST", path, "127.0.0.1", "localhost:5660", headers, body: new BrokenBody());
            Assert.AreEqual(400, r.Status, path);
            StringAssert.Contains(r.Body, "\"error\":\"bad_request\"", path);
        }
    }

    // ---- wrong method ----

    /// <summary>
    /// Contract (server final review): a wrong method on a live route is 405 with the JSON body
    /// <c>{"error":"bad_request","message":…}</c>, never an empty body. Real MVC routing makes the 405 (with Allow).
    /// </summary>
    [TestMethod]
    public async Task WrongMethod_405_WithJsonBody()
    {
        await using var h = await LiveHost.StartAsync();
        var c = await h.NewCodeAsync();
        var sid = await h.SessionAsync(c);
        foreach (var (method, path, allow) in new[]
                 {
                     ("GET", "apply", "POST"), ("GET", "access/check", "POST"), ("POST", "health", "GET"),
                     ("GET", "sessions", "POST"), ("GET", $"sessions/{sid}/requests", "POST"), ("POST", $"sessions/{sid}/events", "GET"),
                     ("GET", $"sessions/{sid}/reset", "POST"),
                     ("GET", $"sessions/{sid}/set", "POST"), ("GET", $"sessions/{sid}/advance", "POST"),("GET", $"sessions/{sid}/requests/r1/approve", "POST"),
                 })
        {
            var r = await h.SendAsync(new HttpMethod(method), path, method == "POST" ? new { } : null, c.Code);
            var what = $"{method} {path}";
            Assert.AreEqual(405, r.Code, what);
            Assert.AreEqual("application/json", r.Message.Content.Headers.ContentType?.MediaType, what);
            Assert.AreEqual("bad_request", r.Error, what);
            Assert.AreEqual(LiveErrors.All["bad_request"].Message, (string?)r.Body!["message"], what);
            CollectionAssert.Contains(r.Message.Content.Headers.Allow.ToList(), allow, what);
        }

        // Any method reaches routing.
        var put = await h.RawAsync("PUT", "/live/api/sessions", "127.0.0.1", "localhost:5660");
        Assert.AreEqual(405, put.Status);
        StringAssert.Contains(put.Body, "\"error\":\"bad_request\"");
        var head = await h.RawAsync("HEAD", "/live/api/health", "127.0.0.1", "localhost:5660");
        Assert.AreEqual((405, ""), head, "a HEAD response has no body");

        // Unmatched paths are still the JSON 404, and the right method still works.
        AssertError(await h.GetAsync("nope"), 404, "no_request");
        Assert.AreEqual(200, (await h.GetAsync("health")).Code);
    }

    // Startup with options that can't work together: RuntimeTests.ConfigProblem_HostStarts_AndHealthIsOff (the host
    // starts with live off instead of failing).
}
