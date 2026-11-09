/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AccessTests.cs
    文件功能描述：NeuCharBox Live 测试：AccessTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.RegularExpressions;
using Microsoft.Extensions.Time.Testing;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>Invite codes, daily limits, the token kill switch, the apply rate limit and retention, on a FakeTimeProvider.</summary>
[TestClass]
public class AccessTests
{
    // 2026-11-07 10:00 in New York (EST, UTC−5).
    readonly FakeTimeProvider Fake = new(new DateTimeOffset(2026, 11, 7, 15, 0, 0, TimeSpan.Zero));
    readonly InMemoryLiveStore Store;
    readonly LiveOptions Opt = new();

    /// <summary>Usage days reset at midnight in New York here: the boundary cases below are written for it.</summary>
    readonly UsageDay Days = NewYork();

    /// <summary>The limits come from the runtime's snapshot; <see cref="Budget"/> changes them.</summary>
    readonly StaticLiveRuntime Runtime = new(Fakes.Snapshots.On(new Fakes.ScriptedChatClient()));

    public AccessTests() => Store = new InMemoryLiveStore(Fake);

    // ---- helpers ----

    static UsageDay NewYork()
    {
        Assert.IsTrue(UsageDay.TryFromId("America/New_York", out var ny));
        return ny;
    }

    LiveLimits Limits => Runtime.Current.Limits;

    void Budget(long tokens) => Runtime.Current = Runtime.Current with { Limits = Limits with { DailyTokenBudget = tokens } };

    DateOnly Day => Days.For(Fake.GetUtcNow());

    AccessService Access(IRunStore? runs = null, ITokenMeter? meter = null)
    {
        runs ??= Store;
        return new AccessService(Store, runs, meter ?? new TokenMeter(runs, Runtime, Fake), Runtime, Days, Fake);
    }

    RunStart Run(InviteCode c, bool whatIf = false) =>
        new(c.Code, "home", "Dim the lamp", whatIf, whatIf ? "lamp" : null, Fakes.Snapshots.Model, Day, Fake.GetUtcNow());

    async Task Seed(InviteCode c, int n, bool whatIf = false)
    {
        for (var i = 0; i < n; i++) await Store.StartAsync(Run(c, whatIf));
    }

    /// <summary>Counts take a real 20 ms, so two admissions that aren't serialised both read the same count.</summary>
    sealed class SlowRuns(IRunStore inner) : IRunStore
    {
        public Task<long> StartAsync(RunStart s) => inner.StartAsync(s);
        public Task FinishAsync(long runId, RunEnd e) => inner.FinishAsync(runId, e);

        public async Task<int> CountTodayAsync(DateOnly day, string? code)
        {
            var n = await inner.CountTodayAsync(day, code);
            await Task.Delay(20);
            return n;
        }

        public Task<long> TokensTodayAsync(DateOnly day) => inner.TokensTodayAsync(day);
        public Task<IReadOnlyList<RunRow>> ListAsync(int take) => inner.ListAsync(take);
        public Task BlankTextOlderThanAsync(DateTimeOffset cutoff) => inner.BlankTextOlderThanAsync(cutoff);
    }

    sealed class CountingRuns(IRunStore inner) : IRunStore
    {
        int _tokenReads;
        public int TokenReads => Volatile.Read(ref _tokenReads);

        public Task<long> StartAsync(RunStart s) => inner.StartAsync(s);
        public Task FinishAsync(long runId, RunEnd e) => inner.FinishAsync(runId, e);
        public Task<int> CountTodayAsync(DateOnly day, string? code) => inner.CountTodayAsync(day, code);

        public Task<long> TokensTodayAsync(DateOnly day)
        {
            Interlocked.Increment(ref _tokenReads);
            return inner.TokensTodayAsync(day);
        }

        public Task<IReadOnlyList<RunRow>> ListAsync(int take) => inner.ListAsync(take);
        public Task BlankTextOlderThanAsync(DateTimeOffset cutoff) => inner.BlankTextOlderThanAsync(cutoff);
    }

    async Task FinishWithTokens(long id, int prompt, int completion) =>
        await Store.FinishAsync(id, new RunEnd("ok", null, null, 1, prompt, completion, 100, Fake.GetUtcNow()));

    // ---- limits and admission ----

    [TestMethod]
    public async Task Limits_CodeGlobalBudgetOrder()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        var b = await Store.CreateAsync("b@example.com", 400);
        var access = Access();

        await Seed(a, 20);
        Assert.AreEqual(new AdmitResult("code_limit", null), await access.AdmitRequestAsync(a, Run(a)));
        Assert.IsNotNull((await access.AdmitRequestAsync(b, Run(b))).RunId, "another code still gets in");

        await Seed(b, 279);
        Assert.AreEqual(300, await Store.CountTodayAsync(Day, null));
        Assert.AreEqual(new AdmitResult("global_limit", null), await access.AdmitRequestAsync(b, Run(b)));
        Assert.AreEqual("global_limit", (await access.AdmitRequestAsync(a, Run(a))).Error, "the global cap is checked before the code's");

        await FinishWithTokens((await Store.ListAsync(1))[0].Id, 900_000, 100_000);
        Fake.Advance(TimeSpan.FromSeconds(5)); // the meter reads the store at most every 5 s
        Assert.AreEqual(new AdmitResult("budget", null), await access.AdmitRequestAsync(a, Run(a)), "the kill switch is checked first");
        Assert.AreEqual("budget", (await access.AdmitRequestAsync(b, Run(b))).Error);

        Assert.AreEqual(300, await Store.CountTodayAsync(Day, null), "no refusal reserved a slot");
        Assert.AreEqual(1, (await Store.FindAsync(InviteCodes.Normalize(b.Code)))!.UsesTotal, "only the admitted request touched its code");
        Assert.AreEqual(0, (await Store.FindAsync(InviteCodes.Normalize(a.Code)))!.UsesTotal);
    }

    [TestMethod]
    public async Task Admit_ReservesTheRunAndTouchesTheCode()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        var start = Run(a);
        var r = await Access().AdmitRequestAsync(a, start);

        Assert.IsNull(r.Error);
        var row = (await Store.ListAsync(10)).Single();
        Assert.AreEqual(row.Id, r.RunId);
        Assert.AreEqual(start, row.Start);
        Assert.IsNull(row.End, "the run is reserved, not finished");
        var touched = (await Store.FindAsync(InviteCodes.Normalize(a.Code)))!;
        Assert.AreEqual(1, touched.UsesTotal);
        Assert.AreEqual(Fake.GetUtcNow(), touched.LastUsed);
    }

    [TestMethod]
    public async Task Admit_ConcurrentAtLimit_OnlyOnePasses()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        await Seed(a, 19);
        var access = Access(new SlowRuns(Store), new Fakes.StubMeter());

        var results = await Task.WhenAll(Task.Run(() => access.AdmitRequestAsync(a, Run(a))), Task.Run(() => access.AdmitRequestAsync(a, Run(a))));

        Assert.AreEqual(1, results.Count(r => r.RunId is not null && r.Error is null));
        Assert.AreEqual(1, results.Count(r => r.Error == "code_limit" && r.RunId is null));
        Assert.AreEqual(20, await Store.CountTodayAsync(Day, a.Code));

        // Eight at once with three left: exactly three get in.
        var b = await Store.CreateAsync("b@example.com", 20);
        await Seed(b, 17);
        var many = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => access.AdmitRequestAsync(b, Run(b)))));
        Assert.AreEqual(3, many.Count(r => r.RunId is not null));
        Assert.AreEqual(5, many.Count(r => r.Error == "code_limit"));
        Assert.AreEqual(20, await Store.CountTodayAsync(Day, b.Code));
        Assert.AreEqual(3, many.Where(r => r.RunId is not null).Select(r => r.RunId).Distinct().Count());
    }

    [TestMethod]
    public async Task Admit_ConcurrentAtGlobalCap_OnlyOnePasses()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        var b = await Store.CreateAsync("b@example.com", 20);
        var bulk = await Store.CreateAsync("bulk", 1000);
        await Seed(bulk, Limits.GlobalDaily - 1);
        var access = Access(new SlowRuns(Store), new Fakes.StubMeter());

        var results = await Task.WhenAll(Task.Run(() => access.AdmitRequestAsync(a, Run(a))), Task.Run(() => access.AdmitRequestAsync(b, Run(b))));

        Assert.AreEqual(1, results.Count(r => r.RunId is not null));
        Assert.AreEqual(1, results.Count(r => r.Error == "global_limit"));
        Assert.AreEqual(Limits.GlobalDaily, await Store.CountTodayAsync(Day, null));
    }

    /// <summary>
    /// A request is admitted on its own snapshot's limits (SessionManager passes them); CheckAsync and an admission
    /// without limits read the runtime's current ones.
    /// </summary>
    [TestMethod]
    public async Task Admit_UsesTheLimitsPassed_ElseTheRuntimes()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        var access = Access();
        await Seed(a, 2);
        Runtime.Current = Runtime.Current with { Limits = Limits with { GlobalDaily = 2 } };
        Assert.AreEqual(0, (await access.CheckAsync(a.Code)).Remaining, "the runtime's GlobalDaily");
        Assert.AreEqual("global_limit", (await access.AdmitRequestAsync(a, Run(a))).Error);
        Assert.IsNull((await access.AdmitRequestAsync(a, Run(a), LiveLimits.Defaults)).Error, "the request's own limits");
        Assert.AreEqual("budget", (await access.AdmitRequestAsync(a, Run(a), LiveLimits.Defaults with { DailyTokenBudget = 0 })).Error);
        Assert.IsFalse(access.BudgetSpent(Day));
        Assert.IsTrue(access.BudgetSpent(Day, LiveLimits.Defaults with { DailyTokenBudget = 0 }));
        Budget(0);
        Assert.IsTrue(access.BudgetSpent(Day), "the runtime's budget");
    }

    [TestMethod]
    public async Task Limits_WhatIfDoesNotCount()
    {
        var a = await Store.CreateAsync("a@example.com", 2);
        await Seed(a, 1);
        await Seed(a, 5, whatIf: true);
        var access = Access();

        Assert.AreEqual(1, await Store.CountTodayAsync(Day, a.Code));
        Assert.AreEqual(1, (await access.CheckAsync(a.Code)).Remaining);
        Assert.IsNull((await access.AdmitRequestAsync(a, Run(a))).Error);
        Assert.AreEqual("code_limit", (await access.AdmitRequestAsync(a, Run(a))).Error);
    }

    [TestMethod]
    public async Task Limits_ResetAtEasternMidnight()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        var access = Access();
        for (var i = 0; i < 20; i++) Assert.IsNull((await access.AdmitRequestAsync(a, Run(a))).Error);
        Assert.AreEqual("code_limit", (await access.AdmitRequestAsync(a, Run(a))).Error);

        Fake.SetUtcNow(DateTimeOffset.Parse("2026-11-08T04:59:59Z")); // 23:59:59 in New York
        Assert.AreEqual(0, (await access.CheckAsync(a.Code)).Remaining);
        Assert.AreEqual("code_limit", (await access.AdmitRequestAsync(a, Run(a))).Error);

        Fake.SetUtcNow(DateTimeOffset.Parse("2026-11-08T05:00:00Z")); // midnight in New York
        Assert.AreEqual(20, (await access.CheckAsync(a.Code)).Remaining);
        Assert.IsNull((await access.AdmitRequestAsync(a, Run(a))).Error);
    }

    // ---- check ----

    [TestMethod]
    public async Task Check_RemainingUsesCodeLimit()
    {
        var a = await Store.CreateAsync("a@example.com", 2);
        var access = Access();
        await Seed(a, 1);
        Assert.AreEqual(new AccessResult(true, (await Store.FindAsync(InviteCodes.Normalize(a.Code)))!, 1, null), await access.CheckAsync(a.Code),
            "the code's own DailyLimit, not PerCodeDaily");

        var b = await Store.CreateAsync("b@example.com", 20);
        var bulk = await Store.CreateAsync("bulk", 1000);
        await Seed(bulk, 298); // 299 of 300 today
        Assert.AreEqual(1, (await access.CheckAsync(b.Code)).Remaining, "the global cap");
        Assert.AreEqual(1, (await access.CheckAsync(a.Code)).Remaining);

        await Seed(bulk, 5); // past the cap
        var full = await access.CheckAsync(b.Code);
        Assert.IsTrue(full.Ok, "a valid code with nothing left is still a good code");
        Assert.AreEqual(0, full.Remaining, "never negative");
    }

    [TestMethod]
    public async Task Check_BearerCaseAndSpaces()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        var access = Access();
        foreach (var given in new[] { a.Code, "Bearer " + a.Code, "bearer " + a.Code.ToLowerInvariant(), "  BEARER\t" + a.Code + " ",
                     " " + a.Code.ToLowerInvariant().Replace("-", " ") + " " })
        {
            var r = await access.CheckAsync(given);
            Assert.IsTrue(r.Ok, given);
            Assert.AreEqual(a.Id, r.Code!.Id, given);
            Assert.AreEqual(20, r.Remaining);
            Assert.IsNull(r.Error);
        }
    }

    [TestMethod]
    public async Task DisabledCode_IsBadCode()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        await Store.SetDisabledAsync(a.Id, true);
        var access = Access();
        var bad = new AccessResult(false, null, 0, "bad_code");

        Assert.AreEqual(bad, await access.CheckAsync(a.Code));
        Assert.AreEqual(bad, await access.CheckAsync("Bearer " + a.Code));
        foreach (var given in new[] { null, "", "   ", "Bearer", "Bearer ", "NCB-0000-0000", "Basic " + a.Code })
            Assert.AreEqual(bad, await access.CheckAsync(given), given ?? "null");

        await Store.SetDisabledAsync(a.Id, false);
        Assert.IsTrue((await access.CheckAsync(a.Code)).Ok, "re-enabled");
    }

    // ---- fail closed when the store can't be read (before the module is installed) ----

    sealed class BrokenRuns(IRunStore inner) : IRunStore
    {
        public bool Broken = true;
        public int Reads;

        public Task<long> StartAsync(RunStart s) => inner.StartAsync(s);
        public Task FinishAsync(long runId, RunEnd e) => inner.FinishAsync(runId, e);
        public Task<int> CountTodayAsync(DateOnly day, string? code) => inner.CountTodayAsync(day, code);

        public Task<long> TokensTodayAsync(DateOnly day)
        {
            Reads++;
            return Broken ? throw new InvalidOperationException("no such table: Senparc_NeuCharBoxLive_Run") : inner.TokensTodayAsync(day);
        }

        public Task<IReadOnlyList<RunRow>> ListAsync(int take) => inner.ListAsync(take);
        public Task BlankTextOlderThanAsync(DateTimeOffset cutoff) => inner.BlankTextOlderThanAsync(cutoff);
    }

    sealed class WarnLog : Microsoft.Extensions.Logging.ILogger<TokenMeter>
    {
        public int Warnings;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel level) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel level, Microsoft.Extensions.Logging.EventId id, TState state,
            Exception? e, Func<TState, Exception?, string> f)
        {
            if (level == Microsoft.Extensions.Logging.LogLevel.Warning) Warnings++;
        }
    }

    [TestMethod]
    public void TokenMeter_StoreUnreadable_CountsAsSpent_WarnsThrottled_ThenRecovers()
    {
        var runs = new BrokenRuns(Store);
        var log = new WarnLog();
        var meter = new TokenMeter(runs, Runtime, Fake, log);

        Assert.IsTrue(meter.Spent(Day), "a failed read at the day's start counts as spent, and does not throw");
        Assert.AreEqual(1, log.Warnings);
        for (var i = 0; i < 20; i++) Assert.IsTrue(meter.Spent(Day));
        Assert.AreEqual(1, runs.Reads, "within 5 s the failure is cached; the store is not hammered");
        Fake.Advance(TimeSpan.FromSeconds(6));
        Assert.IsTrue(meter.Spent(Day), "a failed re-read is still spent");
        Assert.AreEqual(2, runs.Reads);
        Assert.AreEqual(1, log.Warnings, "the warning is throttled");
        Fake.Advance(TimeSpan.FromMinutes(2));
        Assert.IsTrue(meter.Spent(Day));
        Assert.AreEqual(2, log.Warnings, "and repeats after a minute");

        // Install happened: the next read works and live turns on without a restart.
        runs.Broken = false;
        Fake.Advance(TimeSpan.FromSeconds(6));
        Assert.IsFalse(meter.Spent(Day));
        meter.Add(Day, Convert.ToInt32(Limits.DailyTokenBudget));
        Assert.IsTrue(meter.Spent(Day), "tokens added after recovery count against the budget");
    }

    [TestMethod]
    public void TokenMeter_StoreUnreadableAtReread_KeepsFailingClosed()
    {
        var runs = new BrokenRuns(Store) { Broken = false };
        var meter = new TokenMeter(runs, Runtime, Fake);
        Assert.IsFalse(meter.Spent(Day));
        runs.Broken = true;
        Fake.Advance(TimeSpan.FromSeconds(6));
        Assert.IsTrue(meter.Spent(Day), "a store that stops answering counts as spent, not as the last good number");
    }

    // ---- the token kill switch ----

    [TestMethod]
    public async Task TokenMeter_StorePlusAddsSinceLastRead_ReadsAtMostEvery5s()
    {
        Budget(1000);
        var a = await Store.CreateAsync("a@example.com", 20);
        var runs = new CountingRuns(Store);
        var meter = new TokenMeter(runs, Runtime, Fake);
        await FinishWithTokens(await Store.StartAsync(Run(a)), 600, 300);

        Assert.IsFalse(meter.Spent(Day), "900 of 1000");
        Assert.AreEqual(1, runs.TokenReads);

        // Another run is logged: the meter sees it at its next read, 5 s after the last one.
        await FinishWithTokens(await Store.StartAsync(Run(a)), 150, 50);
        Fake.Advance(TimeSpan.FromSeconds(4.9));
        for (var i = 0; i < 50; i++) Assert.IsFalse(meter.Spent(Day), "the cached 900");
        Assert.AreEqual(1, runs.TokenReads, "within 5 s the store is read once");
        Fake.Advance(TimeSpan.FromSeconds(0.1));
        Assert.IsTrue(meter.Spent(Day), "re-read: 1100 of 1000");
        Assert.AreEqual(2, runs.TokenReads);

        // A new day in New York reads the store at once; tokens added since that read count without another read.
        Fake.SetUtcNow(DateTimeOffset.Parse("2026-11-08T05:00:00Z"));
        Assert.IsFalse(meter.Spent(Day));
        Assert.AreEqual(3, runs.TokenReads);
        meter.Add(Day, 999);
        Assert.IsFalse(meter.Spent(Day), "999");
        meter.Add(Day.AddDays(-1), 5000);
        Assert.IsFalse(meter.Spent(Day), "yesterday's tokens don't count today");
        meter.Add(Day, 1);
        Assert.IsTrue(meter.Spent(Day), "reaching the budget counts as spent");
        Assert.AreEqual(3, runs.TokenReads);
    }

    [TestMethod]
    public async Task TokenMeter_InFlightAddsSurviveRereads()
    {
        Budget(1000);
        var a = await Store.CreateAsync("a@example.com", 20);
        var meter = new TokenMeter(Store, Runtime, Fake);
        await FinishWithTokens(await Store.StartAsync(Run(a)), 600, 300);
        Assert.IsFalse(meter.Spent(Day), "900 logged");

        // A run still going has used 100 tokens; the store won't hold them until it ends.
        var running = await Store.StartAsync(Run(a));
        meter.Add(Day, 60);
        meter.Add(Day, 40);
        Assert.IsTrue(meter.Spent(Day), "900 + 100 in flight");
        Fake.Advance(TimeSpan.FromSeconds(5));
        Assert.IsTrue(meter.Spent(Day), "a re-read (still 900 in the store) keeps the in-flight 100");
        Fake.Advance(TimeSpan.FromSeconds(5));
        Assert.IsTrue(meter.Spent(Day));

        // When it ends its tokens are in the store too: they count once, not twice.
        Budget(1001);
        await FinishWithTokens(running, 80, 20);
        Fake.Advance(TimeSpan.FromSeconds(5));
        Assert.IsFalse(meter.Spent(Day), "1000, not 1100");

        // A run that ends elsewhere (no Add here) still shows through the store.
        await FinishWithTokens(await Store.StartAsync(Run(a)), 1, 0);
        Fake.Advance(TimeSpan.FromSeconds(5));
        Assert.IsTrue(meter.Spent(Day), "1001 in the store");
    }

    /// <summary>
    /// Just after midnight a request that computed its day a moment earlier can still ask about yesterday. That late call
    /// must not roll the meter back: today's in-flight tokens would be dropped and today read again as a new day.
    /// </summary>
    [TestMethod]
    public void TokenMeter_LateCallForYesterday_DoesNotRollTheDayBack()
    {
        Budget(1000);
        var runs = new CountingRuns(Store);
        var meter = new TokenMeter(runs, Runtime, Fake);
        Fake.SetUtcNow(DateTimeOffset.Parse("2026-11-08T05:00:01Z")); // 00:00:01 in New York
        var today = Day;
        var yesterday = today.AddDays(-1);
        Assert.IsFalse(meter.Spent(today));
        meter.Add(today, 1000); // in flight: the store doesn't hold it yet
        Assert.IsTrue(meter.Spent(today));
        var reads = runs.TokenReads;

        Assert.IsTrue(meter.Spent(yesterday), "a late call gets today's answer");
        Assert.AreEqual(reads, runs.TokenReads, "and starts no day");
        Assert.IsTrue(meter.Spent(today), "today's in-flight tokens are still counted");
        Assert.AreEqual(reads, runs.TokenReads, "today is not read again as a new day");
    }

    [TestMethod]
    public async Task Admit_RefusesAMismatchedOrDisabledCode()
    {
        var a = await Store.CreateAsync("a@example.com", 20);
        var b = await Store.CreateAsync("b@example.com", 20);
        var access = Access();

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => access.AdmitRequestAsync(a, Run(b)), "logged under another code");
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => access.AdmitRequestAsync(a with { Disabled = true }, Run(a)));
        Assert.AreEqual(0, await Store.CountTodayAsync(Day, null), "nothing reserved");
        Assert.AreEqual(0, (await Store.FindAsync(InviteCodes.Normalize(a.Code)))!.UsesTotal);
        Assert.IsNull((await access.AdmitRequestAsync(a, Run(a))).Error, "the lock was released");
    }

    // ---- applications ----

    [TestMethod]
    public void ApplyRate_FivePerHourPerIp()
    {
        var rate = new ApplyRateLimiter(Fake);
        for (var i = 0; i < 5; i++) Assert.IsTrue(rate.TryAcquire("203.0.113.9"), $"#{i + 1}");
        Assert.IsFalse(rate.TryAcquire("203.0.113.9"), "the 6th in the hour");
        Assert.IsFalse(rate.TryAcquire("203.0.113.9"), "a refusal doesn't free a slot");
        Assert.IsTrue(rate.TryAcquire("198.51.100.7"), "per IP");

        Fake.Advance(TimeSpan.FromMinutes(61));
        Assert.IsTrue(rate.TryAcquire("203.0.113.9"), "61 minutes later");

        // Sliding, not a fixed hour: 2 more at +61, 2 at +91; at +122 the two from +61 have gone, not the ones from +91.
        Assert.IsTrue(rate.TryAcquire("203.0.113.9"));
        Fake.Advance(TimeSpan.FromMinutes(30));
        Assert.IsTrue(rate.TryAcquire("203.0.113.9"));
        Assert.IsTrue(rate.TryAcquire("203.0.113.9"));
        Assert.IsTrue(rate.TryAcquire("203.0.113.9"));
        Assert.IsFalse(rate.TryAcquire("203.0.113.9"), "5 in the last hour");
        Fake.Advance(TimeSpan.FromMinutes(31));
        Assert.IsTrue(rate.TryAcquire("203.0.113.9"));
        Assert.IsTrue(rate.TryAcquire("203.0.113.9"));
        Assert.IsFalse(rate.TryAcquire("203.0.113.9"), "the three from +91 are still in the hour");
    }

    [TestMethod]
    public void AccessCheckRate_PerIpPerMinute_FromOptions()
    {
        var rate = new AccessCheckRateLimiter(Opt, Fake);
        Assert.AreEqual(30, rate.Limit);
        Assert.AreEqual(TimeSpan.FromMinutes(1), rate.Window);
        for (var i = 0; i < 30; i++) Assert.IsTrue(rate.TryAcquire("203.0.113.9"), $"#{i + 1}");
        Assert.IsFalse(rate.TryAcquire("203.0.113.9"), "the 31st in the minute");
        Assert.IsTrue(rate.TryAcquire("198.51.100.7"), "per IP");
        Fake.Advance(TimeSpan.FromMinutes(1));
        Assert.IsTrue(rate.TryAcquire("203.0.113.9"), "a minute later");
        Assert.AreEqual(2, new AccessCheckRateLimiter(new LiveOptions { AccessCheckPerMinute = 2 }, Fake).Limit, "configurable");
    }

    /// <summary>
    /// Both limiters track at most MaxTracked IPs. Full of IPs still inside the window, a new IP is refused (under a
    /// flood from that many addresses, refusing newcomers protects the store) until IPs leave the window.
    /// </summary>
    [TestMethod]
    public void RateLimiters_TrackAtMostMaxTrackedIps()
    {
        Assert.AreEqual(IpRateLimiter.DefaultMaxTracked, new ApplyRateLimiter(Fake).MaxTracked);
        Assert.AreEqual(IpRateLimiter.DefaultMaxTracked, new AccessCheckRateLimiter(Opt, Fake).MaxTracked);

        var rate = new IpRateLimiter(2, TimeSpan.FromMinutes(1), Fake, maxTracked: 3);
        foreach (var ip in new[] { "a", "b", "c" }) Assert.IsTrue(rate.TryAcquire(ip), ip);
        Assert.IsFalse(rate.TryAcquire("d"), "full of IPs still in the window: a new one is refused");
        Assert.AreEqual(3, rate.Tracked);
        Assert.IsTrue(rate.TryAcquire("a"), "a tracked IP keeps its own limit");
        Fake.Advance(TimeSpan.FromSeconds(30));
        Assert.IsTrue(rate.TryAcquire("b"));
        Fake.Advance(TimeSpan.FromSeconds(30)); // a's and c's hits have left the window; b's second hasn't
        Assert.IsTrue(rate.TryAcquire("d"), "room again once IPs leave the window");
        Assert.IsTrue(rate.TryAcquire("e"));
        Assert.IsFalse(rate.TryAcquire("f"), "b, d and e are all in the window");
        Assert.AreEqual(3, rate.Tracked);

        // A flood of new addresses never grows the map past the cap.
        var flood = new IpRateLimiter(1, TimeSpan.FromMinutes(1), Fake, maxTracked: 100);
        for (var i = 0; i < 10_000; i++) flood.TryAcquire($"2001:db8::{i:x}");
        Assert.AreEqual(100, flood.Tracked);
    }

    // ---- codes ----

    [TestMethod]
    public void Code_FormatAndAlphabet()
    {
        var format = new Regex(StoreContract.CodePattern);
        var seen = new HashSet<char>();
        var codes = new HashSet<string>();
        for (var i = 0; i < 1000; i++)
        {
            var c = InviteCodes.Generate();
            StringAssert.Matches(c, format);
            Assert.IsFalse(c[4..].IndexOfAny("01ILO".ToCharArray()) >= 0, c);
            seen.UnionWith(c[4..].Replace("-", ""));
            codes.Add(c);
        }
        Assert.AreEqual(InviteCodes.Alphabet, string.Concat(seen.Order()), "every character of the alphabet is used, and only those");
        Assert.AreEqual(31, InviteCodes.Alphabet.Length);
        Assert.IsTrue(codes.Count >= 999, "8 random characters don't repeat in 1000 codes");
    }

    [TestMethod]
    public void Code_NormalizeCaseAndSpaces()
    {
        Assert.AreEqual(InviteCodes.Normalize("NCB-7KQ4-M2XD"), InviteCodes.Normalize(" ncb-7kq4 m2xd "));
        Assert.AreEqual("NCB7KQ4M2XD", InviteCodes.Normalize("NCB-7KQ4-M2XD"));
        Assert.AreEqual("NCB7KQ4M2XD", InviteCodes.Normalize("\tncb 7kq4–m2xd\n"), "tabs, newlines and a pasted en dash");
        Assert.AreEqual("", InviteCodes.Normalize("  - "));
        Assert.AreEqual("", InviteCodes.Normalize(null!));
    }

    // ---- retention ----

    [TestMethod]
    public async Task Retention_BlanksTextAfter30Days()
    {
        var day = Days.For(Fake.GetUtcNow());
        var old = await Store.StartAsync(new RunStart("NCB-AAAA-2222", "home", "Movie night", false, null, "gpt-5-chat", day, Fake.GetUtcNow()));
        await Store.FinishAsync(old, new RunEnd("ok", "{\"steps\":[]}", "I dimmed the lamp.", 5, 500, 100, 900, Fake.GetUtcNow()));
        Fake.Advance(TimeSpan.FromSeconds(2));
        var mid = await Store.StartAsync(new RunStart("NCB-AAAA-2222", "home", "Lamp off", false, null, "gpt-5-chat", day, Fake.GetUtcNow()));
        Fake.Advance(TimeSpan.FromDays(30) - TimeSpan.FromSeconds(1)); // old is now 30 days and 1 s old, mid 1 s under 30 days

        await Store.BlankTextOlderThanAsync(Fake.GetUtcNow() - TimeSpan.FromDays(30));

        var rows = await Store.ListAsync(10);
        var o = rows.Single(r => r.Id == old);
        Assert.AreEqual("", o.Start.Text);
        Assert.IsNull(o.End!.PlanJson);
        Assert.IsNull(o.End.Summary);
        Assert.AreEqual(500, o.End.PromptTokens);
        Assert.AreEqual(100, o.End.CompletionTokens);
        Assert.AreEqual(5, o.End.Calls);
        Assert.AreEqual(900L, o.End.LatencyMs);
        Assert.AreEqual("Lamp off", rows.Single(r => r.Id == mid).Start.Text, "1 s under 30 days old: kept");
    }
}
