/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SettingsTests.cs
    文件功能描述：测试设置服务与设置页逻辑：保存校验、状态、试用链接与模型选项


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 设置表与 LiveSettingsService 的测试，六种数据库的迁移检查

----------------------------------------------------------------*/

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>The settings row: defaults, validation, save and read back, and the migrations that create its table.</summary>
[TestClass]
public class SettingsTests
{
    const string SettingsTable = "Senparc_NeuCharBoxLive_Settings";

    static void AssertSame(LiveSettings expected, LiveSettings actual)
    {
        Assert.AreEqual(expected.Enabled, actual.Enabled);
        Assert.AreEqual(expected.PlanModelId, actual.PlanModelId);
        Assert.AreEqual(expected.RunModelId, actual.RunModelId);
        Assert.AreEqual(expected.PerCodeDaily, actual.PerCodeDaily);
        Assert.AreEqual(expected.GlobalDaily, actual.GlobalDaily);
        Assert.AreEqual(expected.DailyTokenBudget, actual.DailyTokenBudget);
        CollectionAssert.AreEqual(expected.ExtraOrigins.ToList(), actual.ExtraOrigins.ToList());
    }

    [TestMethod]
    public async Task Settings_Missing_ReturnsDefaults()
    {
        using var db = new TestDb();
        var svc = new LiveSettingsService(db.Scopes);
        var s = await svc.GetAsync();
        AssertSame(LiveSettings.Defaults, s);
        Assert.AreEqual(0, s.ExtraOrigins.Count);
        Assert.AreEqual(new Senparc.Xncf.NeuCharBoxLive.Engine.Access.LiveLimits(20, 300, 1_000_000), s.Limits);
        Assert.IsTrue(s.Enabled);
    }

    [TestMethod]
    public async Task Settings_SaveThenGet_RoundTrips()
    {
        using var db = new TestDb();
        var svc = new LiveSettingsService(db.Scopes);
        var saved = new LiveSettings(false, 3, 4, 5, 6, 7000, ["https://a.example"]);
        Assert.AreEqual(0, (await svc.SaveAsync(saved)).Count);
        AssertSame(saved, await svc.GetAsync());

        // A second save updates the one row.
        var again = new LiveSettings(true, null, null, 9, 10, 11, ["https://b.example", "http://c.example:8080"]);
        Assert.AreEqual(0, (await svc.SaveAsync(again)).Count);
        AssertSame(again, await new LiveSettingsService(db.Scopes).GetAsync());
        using var ctx = db.NewContext();
        Assert.AreEqual(1, await ctx.Settings.CountAsync());
    }

    [TestMethod]
    public async Task Settings_Save_NormalizesOrigins()
    {
        using var db = new TestDb();
        var svc = new LiveSettingsService(db.Scopes);
        var s = LiveSettings.Defaults with { ExtraOrigins = ["https://a.example/", " http://b.example:81 "] };
        Assert.AreEqual(0, (await svc.SaveAsync(s)).Count);
        CollectionAssert.AreEqual(new[] { "https://a.example", "http://b.example:81" }, (await svc.GetAsync()).ExtraOrigins.ToList());
        using var ctx = db.NewContext();
        Assert.AreEqual("https://a.example\nhttp://b.example:81", (await ctx.Settings.SingleAsync()).ExtraOrigins, "one origin per line");
    }

    [TestMethod]
    public void Settings_Validate_RejectsBadOrigins()
    {
        foreach (var bad in new[] { "https://a.example/x", "*", "ftp://a.example" })
        {
            var problems = LiveSettingsService.Validate(LiveSettings.Defaults with { ExtraOrigins = [bad] });
            CollectionAssert.Contains(problems.ToList(), "ExtraOrigins", bad);
        }
        Assert.AreEqual(0, LiveSettingsService.Validate(LiveSettings.Defaults).Count);
        Assert.AreEqual(0, LiveSettingsService.Validate(LiveSettings.Defaults with { ExtraOrigins = ["https://a.example/"] }).Count);
    }

    [TestMethod]
    public void Settings_Validate_RejectsNonPositiveLimits()
    {
        var problems = LiveSettingsService.Validate(LiveSettings.Defaults with { PerCodeDaily = 0, GlobalDaily = -1, DailyTokenBudget = 0 });
        CollectionAssert.AreEquivalent(new[] { "PerCodeDaily", "GlobalDaily", "DailyTokenBudget" }, problems.ToList());
    }

    [TestMethod]
    public async Task Settings_Save_RaisesChanged_OnlyWhenValid()
    {
        using var db = new TestDb();
        var svc = new LiveSettingsService(db.Scopes);
        var raised = 0;
        svc.Changed += () => raised++;

        var problems = await svc.SaveAsync(LiveSettings.Defaults with { PerCodeDaily = 0 });
        Assert.AreEqual(1, problems.Count);
        Assert.AreEqual(0, raised);
        AssertSame(LiveSettings.Defaults, await svc.GetAsync());
        using (var ctx = db.NewContext()) Assert.AreEqual(0, await ctx.Settings.CountAsync(), "an invalid save writes nothing");

        Assert.AreEqual(0, (await svc.SaveAsync(LiveSettings.Defaults with { PerCodeDaily = 2 })).Count);
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void Migrations_ExistForAllSixProviders()
    {
        var asm = typeof(NeuCharBoxLiveSenparcEntities).Assembly;
        var contexts = asm.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(NeuCharBoxLiveSenparcEntities)))
            .OrderBy(t => t.Name).ToList();
        CollectionAssert.AreEqual(
            new[] { "Dm", "MySql", "Oracle", "PostgreSQL", "Sqlite", "SqlServer" },
            contexts.Select(t => t.Name.Replace("NeuCharBoxLiveSenparcEntities_", "")).ToList());

        foreach (var ctx in contexts)
        {
            var migrations = asm.GetTypes()
                .Where(t => t.GetCustomAttribute<DbContextAttribute>()?.ContextType == ctx && t.GetCustomAttribute<MigrationAttribute>() is not null)
                .Select(t => t.GetCustomAttribute<MigrationAttribute>()!.Id).OrderBy(i => i, StringComparer.Ordinal).ToList();
            Assert.IsTrue(migrations.Count >= 1, $"{ctx.Name} has no migration");

            var snapshotType = asm.GetTypes().Single(t => typeof(ModelSnapshot).IsAssignableFrom(t) && t.GetCustomAttribute<DbContextAttribute>()?.ContextType == ctx);
            var snapshot = (ModelSnapshot)Activator.CreateInstance(snapshotType, nonPublic: true)!;
            var tables = snapshot.Model.GetEntityTypes().Select(t => t.GetTableName()).ToList();
            CollectionAssert.Contains(tables, SettingsTable, $"{ctx.Name}'s snapshot has no Settings entity");

            if (ctx.Name.EndsWith("_Sqlite"))
            {
                Assert.AreEqual("20261107232352_Init", migrations[0]);
                Assert.AreEqual(2, migrations.Count);
                StringAssert.EndsWith(migrations[1], "_AddSettings");
            }
            else if (ctx.Name.EndsWith("_SqlServer"))
            {
                Assert.AreEqual("20261107232402_Init", migrations[0]);
                Assert.AreEqual(2, migrations.Count);
                StringAssert.EndsWith(migrations[1], "_AddSettings");
            }
            else
            {
                Assert.AreEqual(1, migrations.Count, $"{ctx.Name}: one initial migration that already holds the settings table");
            }
        }
    }

    [TestMethod]
    public async Task Sqlite_Migrate_CreatesSettingsTable()
    {
        using var db = new TestDb(create: false);
        using (var ctx = db.NewContext())
            await ctx.Database.MigrateAsync();
        var svc = new LiveSettingsService(db.Scopes);
        var saved = new LiveSettings(true, 1, 2, 3, 4, 5, ["https://a.example"]);
        Assert.AreEqual(0, (await svc.SaveAsync(saved)).Count);
        AssertSame(saved, await svc.GetAsync());
    }
}
