/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：StoreContract.cs
    文件功能描述：NeuCharBox Live 测试：StoreContract


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// What every store must do, run against InMemoryLiveStore (InMemoryStoreTests) and the module's EfLiveStore
/// (EfLiveStoreTests runs it against EfLiveStore). It uses only Senparc.Xncf.NeuCharBoxLive.Engine and MSTest. Each method expects an empty store.
/// Times are whole seconds in UTC, so a database round trip keeps them equal.
/// </summary>
public static class StoreContract
{
    public const string CodePattern = "^NCB-[2-9A-HJKMNP-Z]{4}-[2-9A-HJKMNP-Z]{4}$";

    static readonly DateTimeOffset T0 = new(2026, 11, 7, 15, 0, 0, TimeSpan.Zero);
    static readonly DateOnly Day = new(2026, 11, 7);
    const string CodeA = "NCB-AAAA-2222", CodeB = "NCB-BBBB-3333";

    public static async Task Invites(IInviteStore s)
    {
        var a = await s.CreateAsync("alice@example.com", 20);
        var b = await s.CreateAsync("bob", 5);
        StringAssert.Matches(a.Code, new System.Text.RegularExpressions.Regex(CodePattern));
        StringAssert.Matches(b.Code, new System.Text.RegularExpressions.Regex(CodePattern));
        Assert.AreNotEqual(a.Id, b.Id);
        Assert.AreNotEqual(a.Code, b.Code);
        Assert.AreEqual("alice@example.com", a.Label);
        Assert.AreEqual(20, a.DailyLimit);
        Assert.IsFalse(a.Disabled);
        Assert.AreEqual(0, a.UsesTotal);
        Assert.IsNull(a.LastUsed);
        Assert.AreNotEqual(default, a.Created);

        var found = await s.FindAsync(InviteCodes.Normalize(a.Code));
        Assert.IsNotNull(found);
        Assert.AreEqual(a.Id, found.Id);
        Assert.AreEqual(a.Code, found.Code, "the code is kept in its canonical form");
        found = await s.FindAsync(InviteCodes.Normalize(" " + a.Code.ToLowerInvariant().Replace('-', ' ') + " "));
        Assert.AreEqual(a.Id, found?.Id, "typed in lower case with spaces");
        Assert.IsNull(await s.FindAsync(InviteCodes.Normalize("NCB-0000-0000")));
        Assert.IsNull(await s.FindAsync(""));

        CollectionAssert.AreEqual(new[] { b.Id, a.Id }, (await s.ListAsync()).Select(c => c.Id).ToList(), "newest first");

        await s.SetLimitAsync(a.Id, 3);
        await s.SetDisabledAsync(a.Id, true);
        await s.TouchAsync(a.Id, T0.AddMinutes(5));
        await s.TouchAsync(a.Id, T0.AddMinutes(9));
        var a2 = await s.FindAsync(InviteCodes.Normalize(a.Code));
        Assert.IsNotNull(a2, "a disabled code is still found; the caller refuses it");
        Assert.AreEqual(3, a2.DailyLimit);
        Assert.IsTrue(a2.Disabled);
        Assert.AreEqual(2, a2.UsesTotal);
        Assert.AreEqual(T0.AddMinutes(9), a2.LastUsed);

        var b2 = (await s.ListAsync()).Single(c => c.Id == b.Id);
        Assert.AreEqual(5, b2.DailyLimit, "other codes are untouched");
        Assert.IsFalse(b2.Disabled);
        Assert.AreEqual(0, b2.UsesTotal);
        Assert.IsNull(b2.LastUsed);

        await s.SetDisabledAsync(a.Id, false);
        Assert.IsFalse((await s.FindAsync(InviteCodes.Normalize(a.Code)))!.Disabled);
    }

    public static async Task Applications(IApplicationStore s)
    {
        var first = new Application(0, "a@example.com", null, "home", "Water the plant while I'm away", "home", T0, "new", null);
        var second = new Application(0, "b@example.com", "Bea", "lab", "Watch the incubator", "lab", T0.AddMinutes(1), "new", null);
        var id1 = await s.AddAsync(first);
        var id2 = await s.AddAsync(second);
        Assert.AreNotEqual(id1, id2);

        var list = await s.ListAsync();
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(second with { Id = id2 }, list[0], "newest first");
        Assert.AreEqual(first with { Id = id1 }, list[1]);

        await s.SetStatusAsync(id1, "approved", 42);
        await s.SetStatusAsync(id2, "declined", null);
        list = await s.ListAsync();
        Assert.AreEqual(second with { Id = id2, Status = "declined" }, list[0]);
        Assert.AreEqual(first with { Id = id1, Status = "approved", CodeId = 42 }, list[1]);
    }

    public static async Task TryApprove(IApplicationStore s)
    {
        var id = await s.AddAsync(new Application(0, "a@example.com", null, "home", "Water the plant", "home", T0, "new", null));
        Assert.IsTrue(await s.TryApproveAsync(id, 7), "first approval wins");
        Assert.IsFalse(await s.TryApproveAsync(id, 8), "second approval changes nothing");
        var a = (await s.ListAsync()).Single();
        Assert.AreEqual("approved", a.Status);
        Assert.AreEqual(7L, a.CodeId, "the winner's code stays");
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => s.TryApproveAsync(id + 99, 1));

        // Two at once: exactly one wins.
        var id2 = await s.AddAsync(new Application(0, "b@example.com", null, "home", "Water the plant", "home", T0, "new", null));
        var results = await Task.WhenAll(Enumerable.Range(10, 8).Select(c => s.TryApproveAsync(id2, c)));
        Assert.AreEqual(1, results.Count(r => r));
    }

    public static async Task Runs(IRunStore s)
    {
        var yesterday = Day.AddDays(-1);
        // Inserted oldest first, so "newest first" is the same by id and by start time.
        var old = await s.StartAsync(Start(CodeA, "Yesterday's lamp", yesterday, T0.AddDays(-1)));
        var a1 = await s.StartAsync(Start(CodeA, "Dim the lamp", Day, T0));
        var a2 = await s.StartAsync(Start(CodeA, "Close the blinds", Day, T0.AddSeconds(1)));
        var w = await s.StartAsync(Start(CodeA, "Dim the lamp", Day, T0.AddSeconds(2)) with { WhatIf = true, WhatIfDevice = "lamp" });
        var b1 = await s.StartAsync(Start(CodeB, "Water the plant", Day, T0.AddSeconds(3)));
        Assert.AreEqual(5, new[] { old, a1, a2, w, b1 }.Distinct().Count());

        Assert.AreEqual(2, await s.CountTodayAsync(Day, CodeA), "running runs count; what-ifs don't");
        Assert.AreEqual(1, await s.CountTodayAsync(Day, CodeB));
        Assert.AreEqual(3, await s.CountTodayAsync(Day, null));
        Assert.AreEqual(1, await s.CountTodayAsync(yesterday, CodeA));
        Assert.AreEqual(0, await s.CountTodayAsync(Day, "NCB-CCCC-4444"));
        Assert.AreEqual(0L, await s.TokensTodayAsync(Day), "no run has finished");

        var end = new RunEnd("ok", "{\"steps\":[]}", "I dimmed the lamp.", 4, 400, 80, 1234, T0.AddSeconds(30));
        await s.FinishAsync(a1, end);
        await s.FinishAsync(w, end with { Outcome = "failed", PromptTokens = 1000, CompletionTokens = 200 });
        await s.FinishAsync(old, end with { PromptTokens = 5000 });
        Assert.AreEqual(480L + 1200L, await s.TokensTodayAsync(Day), "what-ifs' tokens count");
        Assert.AreEqual(5080L, await s.TokensTodayAsync(yesterday));
        Assert.AreEqual(2, await s.CountTodayAsync(Day, CodeA), "finished runs still count");

        var list = await s.ListAsync(10);
        CollectionAssert.AreEqual(new[] { b1, w, a2, a1, old }, list.Select(r => r.Id).ToList(), "newest first");
        var row = list.Single(r => r.Id == a1);
        Assert.AreEqual(Start(CodeA, "Dim the lamp", Day, T0), row.Start);
        Assert.AreEqual(end, row.End);
        Assert.IsNull(list.Single(r => r.Id == a2).End, "still running");
        var wRow = list.Single(r => r.Id == w);
        Assert.IsTrue(wRow.Start.WhatIf);
        Assert.AreEqual("lamp", wRow.Start.WhatIfDevice);

        CollectionAssert.AreEqual(new[] { b1, w }, (await s.ListAsync(2)).Select(r => r.Id).ToList());
    }

    public static async Task Retention(IRunStore s)
    {
        var cutoff = T0.AddDays(-30);
        var oldDay = Day.AddDays(-31);
        var old = await s.StartAsync(Start(CodeA, "Old request", oldDay, T0.AddDays(-31)));
        var oldRunning = await s.StartAsync(Start(CodeA, "Old unfinished request", oldDay, T0.AddDays(-31).AddMinutes(1)));
        var recent = await s.StartAsync(Start(CodeA, "Recent request", Day, T0));
        var end = new RunEnd("ok", "{\"steps\":[\"Dim the lamp\"]}", "I dimmed the lamp.", 4, 400, 80, 1234, T0.AddDays(-31).AddSeconds(30));
        await s.FinishAsync(old, end);
        await s.FinishAsync(recent, end with { Finished = T0.AddSeconds(30) });

        await s.BlankTextOlderThanAsync(cutoff);

        var rows = await s.ListAsync(10);
        var o = rows.Single(r => r.Id == old);
        Assert.IsTrue(string.IsNullOrEmpty(o.Start.Text), "request text blanked (\"\" or null)");
        Assert.IsNull(o.End!.PlanJson);
        Assert.IsNull(o.End.Summary);
        Assert.AreEqual(end with { PlanJson = null, Summary = null }, o.End, "outcome, calls, tokens, latency kept");
        Assert.AreEqual(Start(CodeA, "", oldDay, T0.AddDays(-31)) with { Text = o.Start.Text }, o.Start, "the rest of the start kept");

        var u = rows.Single(r => r.Id == oldRunning);
        Assert.IsTrue(string.IsNullOrEmpty(u.Start.Text));
        Assert.IsNull(u.End);

        var n = rows.Single(r => r.Id == recent);
        Assert.AreEqual("Recent request", n.Start.Text);
        Assert.AreEqual("{\"steps\":[\"Dim the lamp\"]}", n.End!.PlanJson);
        Assert.AreEqual("I dimmed the lamp.", n.End.Summary);

        Assert.AreEqual(2, await s.CountTodayAsync(oldDay, CodeA), "rows are kept");
        Assert.AreEqual(480L, await s.TokensTodayAsync(oldDay));
    }

    static RunStart Start(string code, string text, DateOnly day, DateTimeOffset started) =>
        new(code, "home", text, false, null, "gpt-5-chat", day, started);
}
