/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ModuleTests.cs
    文件功能描述：测试数据库层：表结构与迁移、EfLiveStore 读写、保留清理、申请话术与运行记录导出


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 设置表加入后更新模型与迁移断言（SQLite、SQL Server 各多一个 AddSettings 迁移）

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Tests;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>The module beyond the store contract: schema, migrations, admin-page queries and helpers, retention timer.</summary>
[TestClass]
public class ModuleTests
{
    static readonly DateTimeOffset T0 = new(2026, 11, 7, 15, 0, 0, TimeSpan.Zero);
    static readonly DateOnly Day = new(2026, 11, 7);

    // ---- schema and migrations ----

    [TestMethod]
    public void Schema_ApplyColumnsMatchTheApplyCaps_IndexesAndNoForeignKeys()
    {
        using var db = new TestDb();
        using var ctx = db.NewContext();
        var app = ctx.Model.FindEntityType(typeof(ApplicationEntity))!;
        Assert.AreEqual(LiveApiController.MaxName, app.FindProperty(nameof(ApplicationEntity.Name))!.GetMaxLength());
        Assert.AreEqual(LiveApiController.MaxWouldTry, app.FindProperty(nameof(ApplicationEntity.WouldTry))!.GetMaxLength());
        Assert.AreEqual(LiveApiController.MaxScene, app.FindProperty(nameof(ApplicationEntity.Scene))!.GetMaxLength());
        Assert.AreEqual(LiveApiController.MaxEmail, app.FindProperty(nameof(ApplicationEntity.Email))!.GetMaxLength());
        Assert.IsTrue(app.FindProperty(nameof(ApplicationEntity.Name))!.IsNullable);
        Assert.IsTrue(app.FindProperty(nameof(ApplicationEntity.CodeId))!.IsNullable);

        var code = ctx.Model.FindEntityType(typeof(InviteCodeEntity))!;
        var unique = code.GetIndexes().Single(i => i.Properties.Single().Name == nameof(InviteCodeEntity.Normalized));
        Assert.IsTrue(unique.IsUnique);
        Assert.AreEqual(LiveApiController.MaxEmail, code.FindProperty(nameof(InviteCodeEntity.Label))!.GetMaxLength(),
            "a code made on approval is labelled with the email");

        var run = ctx.Model.FindEntityType(typeof(RunEntity))!;
        Assert.IsTrue(run.GetIndexes().Any(i => i.Properties.Single().Name == nameof(RunEntity.Day)));
        Assert.IsTrue(run.FindProperty(nameof(RunEntity.FinishedUtc))!.IsNullable, "a run with no end yet");

        Assert.AreEqual(0, ctx.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()).Count(),
            "CodeId is stored as given: no foreign key");
        CollectionAssert.AreEquivalent(
            new[] { "Senparc_NeuCharBoxLive_InviteCode", "Senparc_NeuCharBoxLive_Application", "Senparc_NeuCharBoxLive_Run", "Senparc_NeuCharBoxLive_Settings" },
            ctx.Model.GetEntityTypes().Select(t => t.GetTableName()).ToList());
    }

    [TestMethod]
    public async Task Migrate_Sqlite_CreatesTheTablesTheStoreUses()
    {
        using var db = new TestDb(create: false);
        using (var ctx = db.NewContext())
        {
            CollectionAssert.AreEqual(new[] { "Init", "AddSettings" }, ctx.Database.GetMigrations().Select(m => m.Split('_', 2)[1]).ToList());
            await ctx.Database.MigrateAsync();
        }
        await StoreContract.Invites(db.Store);
        await StoreContract.Runs(db.Store);
    }

    [TestMethod]
    public void Migrations_Sqlite_MatchTheModel()
    {
        using var db = new TestDb(create: false);
        using var ctx = db.NewContext();
        AssertSnapshotMatches(ctx);
    }

    [TestMethod]
    public void Migrations_SqlServer_MatchTheModel()
    {
        // Model only: the connection is never opened.
        var options = new DbContextOptionsBuilder<NeuCharBoxLiveSenparcEntities_SqlServer>()
            .UseSqlServer("Server=unused;Database=unused;Trusted_Connection=True").Options;
        using var ctx = new NeuCharBoxLiveSenparcEntities_SqlServer(options);
        Assert.AreEqual(2, ctx.Database.GetMigrations().Count());
        AssertSnapshotMatches(ctx);
    }

    static void AssertSnapshotMatches(DbContext ctx)
    {
        var snapshot = ctx.GetService<IMigrationsAssembly>().ModelSnapshot;
        Assert.IsNotNull(snapshot, $"no model snapshot for {ctx.GetType().Name}");
        var snapshotModel = snapshot.Model is IMutableModel m ? m.FinalizeModel() : snapshot.Model;
        snapshotModel = ctx.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);
        var diff = ctx.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(), ctx.GetService<IDesignTimeModel>().Model.GetRelationalModel());
        Assert.AreEqual(0, diff.Count, "the model changed without a migration: " + string.Join("; ", diff.Select(d => d.ToString())));
        CollectionAssert.AreEquivalent(
            new[] { "Senparc_NeuCharBoxLive_InviteCode", "Senparc_NeuCharBoxLive_Application", "Senparc_NeuCharBoxLive_Run", "Senparc_NeuCharBoxLive_Settings" },
            snapshotModel.GetEntityTypes().Select(t => t.GetTableName()).ToList(), "the snapshot holds only this module's tables");
    }

    // ---- store details beyond the contract ----

    [TestMethod]
    public async Task UnknownIds_AreKeyNotFound_LikeTheInMemoryStore()
    {
        using var db = new TestDb();
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => db.Store.SetLimitAsync(99, 3));
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => db.Store.TouchAsync(99, T0));
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => db.Store.SetStatusAsync(99, "declined", null));
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => db.Store.FinishAsync(99, End(1, 1)));
    }

    [TestMethod]
    public async Task Created_IsStoredInUtc()
    {
        using var db = new TestDb();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var c = await db.Store.CreateAsync("x", 1);
        var found = await db.Store.FindAsync(InviteCodes.Normalize(c.Code));
        Assert.AreEqual(TimeSpan.Zero, found!.Created.Offset);
        Assert.IsTrue(found.Created >= before && found.Created <= DateTimeOffset.UtcNow.AddSeconds(1), found.Created.ToString("O"));
    }

    [TestMethod]
    public async Task Totals_PerModel_ForADayAndAllTime()
    {
        using var db = new TestDb();
        var s = db.Store;
        var a = await s.StartAsync(Start("gpt-5-chat", Day));
        var b = await s.StartAsync(Start("gpt-5-chat", Day) with { WhatIf = true, WhatIfDevice = "lamp" });
        var c = await s.StartAsync(Start("other-model", Day));
        var old = await s.StartAsync(Start("gpt-5-chat", Day.AddDays(-1)));
        _ = await s.StartAsync(Start("gpt-5-chat", Day)); // still running: counted, no tokens yet
        await s.FinishAsync(a, End(100, 10));
        await s.FinishAsync(b, End(200, 20));
        await s.FinishAsync(c, End(7, 3));
        await s.FinishAsync(old, End(1000, 1));

        var today = await s.TotalsAsync(Day);
        CollectionAssert.AreEqual(new[]
        {
            new RunTotals("gpt-5-chat", 3, 1, 300, 30),
            new RunTotals("other-model", 1, 0, 7, 3),
        }, today.ToList());
        var all = await s.TotalsAsync(null);
        Assert.AreEqual(new RunTotals("gpt-5-chat", 4, 1, 1300, 31), all[0]);
        Assert.AreEqual(1331L, all[0].Tokens);
        Assert.AreEqual(0, (await s.TotalsAsync(Day.AddDays(1))).Count);
    }

    [TestMethod]
    public async Task UsesOn_CountsRunsPerCodeThatDay_WithoutWhatIfs()
    {
        using var db = new TestDb();
        var s = db.Store;
        await s.StartAsync(Start("m", Day));
        await s.StartAsync(Start("m", Day));
        await s.StartAsync(Start("m", Day) with { WhatIf = true });
        await s.StartAsync(Start("m", Day) with { Code = "NCB-BBBB-3333" });
        await s.StartAsync(Start("m", Day.AddDays(-1)));
        var uses = await s.UsesOnAsync(Day);
        Assert.AreEqual(2, uses["NCB-AAAA-2222"]);
        Assert.AreEqual(2, uses["ncb-aaaa-2222"], "looked up case-insensitively");
        Assert.AreEqual(1, uses["NCB-BBBB-3333"]);
        Assert.AreEqual(2, uses.Count);
    }

    // ---- retention timer ----

    [TestMethod]
    public async Task RetentionService_BlanksAtStartThenEverySixHours_AndSurvivesAFailedPass()
    {
        var tp = new FakeTimeProvider(T0);
        var store = new CutoffRecorder { FailFirst = true };
        using var svc = new RetentionService(store, tp, NullLogger<RetentionService>.Instance);
        await svc.StartAsync(CancellationToken.None);

        await WaitFor(() => store.Cutoffs.Count >= 1, "the first pass");
        Assert.AreEqual(T0 - TimeSpan.FromDays(30), store.Cutoffs[0], "30 days of text are kept");

        tp.Advance(TimeSpan.FromHours(6) - TimeSpan.FromSeconds(1));
        await Task.Delay(50);
        Assert.AreEqual(1, store.Cutoffs.Count, "nothing before 6 h");
        tp.Advance(TimeSpan.FromSeconds(1));
        await WaitFor(() => store.Cutoffs.Count >= 2, "the pass after 6 h, though the first one threw");
        Assert.AreEqual(T0 + TimeSpan.FromHours(6) - TimeSpan.FromDays(30), store.Cutoffs[1]);

        await svc.StopAsync(CancellationToken.None);
        Assert.IsTrue(svc.ExecuteTask!.IsCompleted);
        Assert.IsFalse(svc.ExecuteTask.IsFaulted);
    }

    sealed class CutoffRecorder : IRunStore
    {
        public bool FailFirst;
        public readonly List<DateTimeOffset> Cutoffs = new();

        public Task BlankTextOlderThanAsync(DateTimeOffset cutoff)
        {
            lock (Cutoffs) Cutoffs.Add(cutoff);
            if (FailFirst) { FailFirst = false; throw new InvalidOperationException("no such table"); }
            return Task.CompletedTask;
        }

        public Task<long> StartAsync(RunStart s) => throw new NotSupportedException();
        public Task FinishAsync(long runId, RunEnd e) => throw new NotSupportedException();
        public Task<int> CountTodayAsync(DateOnly day, string? code) => throw new NotSupportedException();
        public Task<long> TokensTodayAsync(DateOnly day) => throw new NotSupportedException();
        public Task<IReadOnlyList<RunRow>> ListAsync(int take) => throw new NotSupportedException();
    }

    static async Task WaitFor(Func<bool> cond, string what)
    {
        for (var i = 0; i < 200 && !cond(); i++) await Task.Delay(25);
        Assert.IsTrue(cond(), "timed out waiting for " + what);
    }

    // ---- admin helpers ----

    [TestMethod]
    public void InviteMessage_UsesTheCodesOwnDailyLimit_AndTheName()
    {
        var code = new InviteCode(5, "NCB-7KQ4-M2XD", "a@example.com", T0, 7, false, 0, null);
        Assert.AreEqual(
            "Hi Bea, here's your NeuCharBox live AI invite code: NCB-7KQ4-M2XD. Open https://leozheng265.github.io/neucharbox-demo/, " +
            "try a guided request, then press \"Try your own request (live AI)\". It's good for 7 requests a day.",
            InviteMessage.For(" Bea ", code));
        StringAssert.StartsWith(InviteMessage.For(null, code), "Hi there, ");
        StringAssert.StartsWith(InviteMessage.For("  ", code with { DailyLimit = 20 }), "Hi there, ");
        StringAssert.EndsWith(InviteMessage.For(null, code with { DailyLimit = 20 }), "It's good for 20 requests a day.");
    }

    [TestMethod]
    public void InviteMessage_OnlyAPlainNameIsUsed()
    {
        var code = new InviteCode(5, "NCB-7KQ4-M2XD", "a@example.com", T0, 7, false, 0, null);
        foreach (var bad in new[] { "Bea https://evil.example/x", "bea@example.com", "Bea\nSend money to", "Bea\r\nX", new string('a', 61), "Bea 2" })
        {
            StringAssert.StartsWith(InviteMessage.For(bad, code), "Hi there, here's", bad);
            Assert.AreEqual("there", InviteMessage.Greeting(bad));
        }
        foreach (var good in new[] { "Bea", "Mary-Jane O'Neil", "José Núñez", "李明" })
            Assert.AreEqual(good, InviteMessage.Greeting("  " + good + " "));
    }

    [TestMethod]
    public async Task SqliteConnections_HaveSecureDeleteOn()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ncb-secdel-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<NeuCharBoxLiveSenparcEntities_Sqlite>().UseSqlite($"Data Source={path}").Options;
            using (var ctx = new NeuCharBoxLiveSenparcEntities_Sqlite(options))
            {
                await ctx.Database.EnsureCreatedAsync();
                var conn = ctx.Database.GetDbConnection();
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA secure_delete;";
                Assert.AreEqual(1L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var f in new[] { path, path + "-wal", path + "-shm", path + "-journal" }) try { File.Delete(f); } catch { }
        }
    }

    [TestMethod]
    public void RunsCsv_QuotesFields_GuardsFormulas_AndLeavesARunningRowsEndEmpty()
    {
        var done = new RunRow(2, Start("gpt-5-chat", Day) with { Text = "=HYPERLINK(\"x\"), then \"dim\"\nlamp" },
            new RunEnd("ok", "{\"steps\":[1]}", "I dimmed it, quietly.", 4, 400, 80, 1234, T0.AddSeconds(30)));
        var running = new RunRow(1, Start("gpt-5-chat", Day) with { Text = "+1 lamp", WhatIf = true, WhatIfDevice = "lamp" }, null);
        var lines = RunsCsv.Write(new[] { done, running }).Split("\r\n");

        Assert.AreEqual(string.Join(",", RunsCsv.Header), lines[0]);
        StringAssert.StartsWith(lines[1], "2,2026-11-07T15:00:00Z,2026-11-07,NCB-AAAA-2222,home,gpt-5-chat,false,,ok,4,400,80,480,1234,2026-11-07T15:00:30Z,");
        StringAssert.EndsWith(lines[1],
            ",\"'=HYPERLINK(\"\"x\"\"), then \"\"dim\"\"\nlamp\",\"{\"\"steps\"\":[1]}\",\"I dimmed it, quietly.\"",
            "quoted, quotes doubled, the formula guarded, the newline kept inside the field");
        Assert.AreEqual("1,2026-11-07T15:00:00Z,2026-11-07,NCB-AAAA-2222,home,gpt-5-chat,true,lamp,,,,,,,,'+1 lamp,,", lines[2]);
        Assert.AreEqual("", lines[3], "ends with a line break");
        Assert.AreEqual(4, lines.Length);
    }

    [TestMethod]
    public void RunStatus_EndedShowsOutcome_UnendedIsRunningOnlyIfThisProcessStartedIt()
    {
        var row = new RunRow(1, Start("m", Day), null);
        Assert.AreEqual("running", RunStatus.Of(row, hostStarted: T0.AddMinutes(-1)));
        Assert.AreEqual("unfinished", RunStatus.Of(row, hostStarted: T0.AddMinutes(1)),
            "started before this host did: the server stopped mid-run and nothing can end it now");
        Assert.AreEqual("failed", RunStatus.Of(row with { End = End(1, 1) with { Outcome = "failed" } }, T0.AddMinutes(1)));
    }

    static RunStart Start(string model, DateOnly day) =>
        new("NCB-AAAA-2222", "home", "Dim the lamp", false, null, model, day, T0);

    static RunEnd End(int prompt, int completion) =>
        new("ok", null, null, 1, prompt, completion, 10, T0.AddSeconds(5));
}
