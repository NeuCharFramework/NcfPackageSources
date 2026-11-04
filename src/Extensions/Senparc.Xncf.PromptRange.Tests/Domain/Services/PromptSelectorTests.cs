using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.PromptRange.Abstractions.References;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using RangeEntity = Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange;

namespace Senparc.Xncf.PromptRange.Tests.Domain.Services;

[TestClass]
public class PromptSelectorTests
{
    [TestMethod]
    public async Task Selector_UsesSqlFilteringPagingAndFullContentOnDemand()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection, 11);
        await context.Database.EnsureCreatedAsync();
        var range = new RangeEntity("2026.11.02.1", "Support");
        context.Add(range);
        await context.SaveChangesAsync();
        var first = CreateItem(range, "1", "First", "Hello " + new string('x', 300));
        var second = CreateItem(range, "1.1", "Second", "Other content");
        var third = CreateItem(range, "1", "Third", "Draft content");
        context.AddRange(first, second, third);
        await context.SaveChangesAsync();

        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = CreateService(context, provider);
        var options = await service.GetSelectorOptionsAsync();
        Assert.AreEqual(2, options.Count);
        Assert.IsTrue(options.All(option => option.RangeAlias == "Support"));
        var page = await service.SearchSelectorAsync(0, null!, " SUPPORT ", 1, 2);
        Assert.AreEqual(3, page.TotalCount);
        CollectionAssert.AreEqual(new[] { third.Id, second.Id }, page.Items.Select(item => item.Id).ToArray());
        var next = await service.SearchSelectorAsync(0, null!, "support", 2, 2);
        Assert.AreEqual(first.Id, next.Items.Single().Id);
        Assert.AreEqual(240, next.Items.Single().Content.Length);
        Assert.AreEqual(first.Content, (await service.GetSelectorPromptAsync(first.Id)).Content);
        var lane = await service.SearchSelectorAsync(range.Id, "1", null!, 1, 10);
        Assert.AreEqual(2, lane.TotalCount, "A parent lane must not accidentally include its child lane.");
        Assert.IsTrue(lane.Items.All(item => item.IsDraft));
        Assert.AreEqual(first.Id, (await service.SearchSelectorAsync(0, null!, "HELLO", 1, 10)).Items.Single().Id);
        Assert.AreEqual(second.Id, (await service.SearchSelectorAsync(0, null!, "SECOND", 1, 10)).Items.Single().Id);
        Assert.AreEqual(1, (await service.SearchSelectorAsync(0, null!, second.FullVersion, 1, 10)).TotalCount);
        Assert.AreEqual(0, (await service.SearchSelectorAsync(0, null!, "missing", 1, 10)).TotalCount);
    }

    [TestMethod]
    public async Task Selector_ExcludesDeletedOtherTenantAndSkillRecordsOnEveryEndpoint()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection, 11);
        await context.Database.EnsureCreatedAsync();
        var visible = new RangeEntity("2026.11.02.1", "Visible");
        var deleted = new RangeEntity("2026.11.02.2", "Deleted") { Flag = true };
        var skill = new RangeEntity("2026.11.02.3", "Skill");
        skill.ChangeKind(SkillKind.Skill);
        context.AddRange(visible, deleted, skill);
        await context.SaveChangesAsync();
        var good = CreateItem(visible, "1", "Good", "visible");
        var deletedItem = CreateItem(visible, "2", "Deleted item", "hidden");
        deletedItem.Flag = true;
        var deletedRangeItem = CreateItem(deleted, "1", "Deleted range", "hidden");
        var skillItem = CreateItem(skill, "1", "Skill item", "hidden");
        context.AddRange(good, deletedItem, deletedRangeItem, skillItem);
        await context.SaveChangesAsync();
        await using var otherContext = CreateContext(connection, 22);
        var otherRange = new RangeEntity("2026.11.02.4", "Other tenant");
        otherContext.Add(otherRange);
        await otherContext.SaveChangesAsync();
        var otherItem = CreateItem(otherRange, "1", "Other tenant", "hidden");
        otherContext.Add(otherItem);
        await otherContext.SaveChangesAsync();

        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = CreateService(context, provider);
        Assert.AreEqual(visible.Id, (await service.GetSelectorOptionsAsync()).Single().RangeId);
        Assert.AreEqual(good.Id, (await service.SearchSelectorAsync(0, null!, null!, 1, 10)).Items.Single().Id);
        foreach (var id in new[] { deletedItem.Id, deletedRangeItem.Id, skillItem.Id, otherItem.Id, int.MaxValue })
        {
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.GetSelectorPromptAsync(id));
        }
    }

    [TestMethod]
    public async Task Selector_SortsBeforePagingAndReturnsScores()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection, 11);
        await context.Database.EnsureCreatedAsync();
        var range = new RangeEntity("2026.11.02.1", "Support");
        context.Add(range);
        await context.SaveChangesAsync();
        var lower = CreateItem(range, "3", "Lower average", "content", 9.9m, 20m);
        var higher = CreateItem(range, "1", "Higher average", "content", 10.1m, 11m);
        var unrated = CreateItem(range, "2", "Unrated", "content");
        var zero = CreateItem(range, "4", "Zero", "content", 0m, 0m);
        var tied = CreateItem(range, "5", "Tied", "content", 10.1m, 11m);
        var precise = CreateItem(range, "6", "Precise", "content", 10.01m, 11m);
        context.AddRange(lower, higher, unrated, zero, tied, precise);
        await context.SaveChangesAsync();

        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = CreateService(context, provider);
        var firstPage = await service.SearchSelectorAsync(range.Id, null!, "content", 1, 2);
        Assert.AreEqual(6, firstPage.TotalCount);
        CollectionAssert.AreEqual(new[] { tied.Id, higher.Id }, firstPage.Items.Select(item => item.Id).ToArray());
        var secondPage = await service.SearchSelectorAsync(range.Id, null!, "content", 2, 2, "score");
        CollectionAssert.AreEqual(new[] { precise.Id, lower.Id }, secondPage.Items.Select(item => item.Id).ToArray());
        var lastPage = await service.SearchSelectorAsync(range.Id, null!, "content", 3, 2);
        CollectionAssert.AreEqual(new[] { zero.Id, unrated.Id }, lastPage.Items.Select(item => item.Id).ToArray());
        Assert.AreEqual(0m, lastPage.Items[0].EvalAvgScore);
        Assert.AreEqual(-1m, lastPage.Items[1].EvalAvgScore);
        Assert.AreEqual(9.9m, secondPage.Items[1].EvalAvgScore);
        Assert.AreEqual(20m, secondPage.Items[1].EvalMaxScore);
        var detail = await service.GetSelectorPromptAsync(lower.Id);
        Assert.AreEqual(9.9m, detail.EvalAvgScore);
        Assert.AreEqual(20m, detail.EvalMaxScore);
        var codePage = await service.SearchSelectorAsync(range.Id, null!, "content", 1, 2, "promptCode");
        CollectionAssert.AreEqual(new[] { higher.Id, unrated.Id }, codePage.Items.Select(item => item.Id).ToArray());
        var nextCodePage = await service.SearchSelectorAsync(range.Id, null!, "content", 2, 2, "promptCode");
        CollectionAssert.AreEqual(new[] { lower.Id, zero.Id }, nextCodePage.Items.Select(item => item.Id).ToArray());
    }

    [TestMethod]
    public async Task References_ValidateExactSegmentsAndPersistLastScoreRegardlessOfSource()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection, 11);
        await context.Database.EnsureCreatedAsync();
        var range = new RangeEntity("2026.11.04.1", "References");
        context.Add(range);
        await context.SaveChangesAsync();
        var item = CreateItem(range, "1", "Prompt", "Hello 🌟");
        var other = CreateItem(range, "2", "Other", "Other", 5, 5);
        context.AddRange(item, other);
        await context.SaveChangesAsync();
        var provider = new PromptReferenceService(new RepositoryBase<PromptItem>(new TestDbData(context)), null!);
        const string content = "prefix Hello 🌟 suffix";
        var reference = new PromptReference { PromptItemId = item.Id, PromptCode = item.FullVersion, Start = 7, Length = item.Content.Length };
        await provider.ValidateAsync(content, new[] { reference });
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => provider.ValidateAsync(content.Replace("Hello", "Hallo"), new[] { reference }));
        reference.PromptCode = range.RangeName;
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => provider.ValidateAsync(content, new[] { reference }));
        reference.PromptCode = item.FullVersion;
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => provider.ValidateAsync(content, new[] { reference, reference }));
        foreach (var score in new[] { -0.1m, 10.1m, 5.55m })
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => provider.ScoreAsync(item.Id, score));
        Assert.AreEqual(0m, (await provider.ScoreAsync(item.Id, 0)).CurrentScore);
        var human = await provider.ScoreAsync(item.Id, 9.1m);
        Assert.AreEqual(9.1m, human.HumanScore);
        item.SetReferenceScore(3.2m, true);
        await context.SaveChangesAsync();
        var robot = await provider.GetInfoAsync(item.Id);
        Assert.AreEqual(3.2m, robot.CurrentScore);
        Assert.AreEqual(9.1m, robot.HumanScore);
        Assert.AreEqual(3.2m, robot.RobotScore);
        var last = await provider.ScoreAsync(item.Id, 8.8m);
        Assert.AreEqual(8.8m, last.CurrentScore);
        Assert.AreEqual(3.2m, last.RobotScore);
        using var services = new ServiceCollection().BuildServiceProvider();
        var selector = CreateService(context, services);
        Assert.AreEqual(item.Id, (await selector.SearchSelectorAsync(0, null!, null!, 1, 10)).Items.First().Id);
        Assert.AreEqual(8.8m, (await selector.GetSelectorPromptAsync(item.Id)).CurrentScore);
        await using (var concurrent = CreateContext(connection, 11))
        {
            var concurrentItem = await concurrent.Set<PromptItem>().SingleAsync(value => value.Id == item.Id);
            concurrentItem.SetReferenceScore(2.1m, true);
            await concurrent.SaveChangesAsync();
        }
        var repeated = await provider.ScoreAsync(item.Id, 8.8m);
        Assert.AreEqual(2.1m, repeated.RobotScore);
        await context.Entry(item).ReloadAsync();
        Assert.AreEqual(8.8m, item.FinalScore, "Saving the same cached manual value must overwrite an intervening AI score.");
        Assert.AreEqual(2.1m, item.RobotScore, "An intervening source score must not be erased.");
        item.UpdateExpectedResultsJson("[null, \"\", \" \"]");
        await context.SaveChangesAsync();
        Assert.IsFalse((await provider.GetInfoAsync(item.Id)).HasBenchmark);
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => provider.ScoreWithAIAsync(item.Id, content, "answer"));
        Assert.AreEqual(8.8m, (await provider.GetInfoAsync(item.Id)).CurrentScore, "Failed AI rating must not change the current score.");
        item.UpdateExpectedResultsJson("invalid json");
        await context.SaveChangesAsync();
        Assert.IsFalse((await provider.ScoreAsync(item.Id, 8.8m)).HasBenchmark,
            "Invalid legacy benchmarks must not prevent manual scoring.");
        await using var otherTenant = CreateContext(connection, 22);
        var denied = new PromptReferenceService(new RepositoryBase<PromptItem>(new TestDbData(otherTenant)), null!);
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => denied.ScoreAsync(item.Id, 10));
        item.Flag = true;
        await context.SaveChangesAsync();
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => provider.GetInfoAsync(item.Id));
    }

    [TestMethod]
    public void ScoreMigrations_OnlyAddThreeNullableColumns()
    {
        Microsoft.EntityFrameworkCore.Migrations.Migration[] migrations =
        {
            new Senparc.Xncf.PromptRange.Domain.Migrations.Sqlite.Add_PromptCode_Scores(),
            new Senparc.Xncf.PromptRange.Domain.Migrations.SqlServer.Add_PromptCode_Scores(),
            new Senparc.Xncf.PromptRange.Domain.Migrations.MySql.Add_PromptCode_Scores(),
            new Senparc.Xncf.PromptRange.Domain.Migrations.PostgreSQL.Add_PromptCode_Scores(),
            new Senparc.Xncf.PromptRange.Domain.Migrations.Oracle.Add_PromptCode_Scores(),
            new Senparc.Xncf.PromptRange.Domain.Migrations.Dm.Add_PromptCode_Scores()
        };
        foreach (var migration in migrations)
        {
            var columns = migration.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>().ToList();
            Assert.AreEqual(3, migration.UpOperations.Count);
            Assert.AreEqual(3, columns.Count);
            CollectionAssert.AreEquivalent(new[] { "RobotScore", "HumanScore", "FinalScore" }, columns.Select(column => column.Name).ToArray());
            Assert.IsTrue(columns.All(column => column.IsNullable && column.Table == "Senparc_PromptRange_PromptItem"));
            Assert.AreEqual(3, migration.DownOperations.Count);
        }
    }

    [TestMethod]
    public async Task ExistingResultManualScore_UpdatesTheSharedPromptCodeScore()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection, 11);
        await context.Database.EnsureCreatedAsync();
        var range = new RangeEntity("2026.11.04.1", "References");
        context.Add(range);
        await context.SaveChangesAsync();
        var item = CreateItem(range, "1", "Prompt", "Prompt");
        context.Add(item);
        await context.SaveChangesAsync();
        var result = new PromptResult(new PromptResultDto { PromptItemId = item.Id, PromptItemVersion = item.FullVersion, ResultString = "Answer" });
        context.Add(result);
        await context.SaveChangesAsync();
        using var services = new ServiceCollection().BuildServiceProvider();
        var results = new PromptResultService(new RepositoryBase<PromptResult>(new TestDbData(context)), services,
            CreateService(context, services), null!, null!, null!, null!, null!);
        await results.ManualScoreAsync(result.Id, 7.6m);
        Assert.AreEqual(7.6m, item.FinalScore);
        Assert.AreEqual(7.6m, item.HumanScore);
        Assert.AreEqual(7.6m, result.FinalScore);
        var evaluator = typeof(PromptResultService).GetMethod("EvaluateScoreAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var evaluated = (Task<decimal>)evaluator.Invoke(results, new object?[] {
            new PromptItemDto(), "Answer", new List<string> { "=Answer" }, "Context"
        })!;
        Assert.AreEqual(10m, await evaluated, "Existing exact benchmark scoring must remain available without model calls.");
    }

    [TestMethod]
    public async Task Selector_RejectsInvalidFiltersAndPagination()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection, 11);
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = CreateService(context, provider);
        foreach (var (range, lane, keyword, page, size) in new[]
        {
            (-1, "", "", 1, 10), (0, "1", "", 1, 10), (0, "", new string('x', 201), 1, 10),
            (1, new string('1', 51), "", 1, 10), (0, "", "", 0, 10), (0, "", "", 1, 0),
            (0, "", "", 1, 51), (0, "", "", int.MaxValue, 50)
        })
        {
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(
                () => service.SearchSelectorAsync(range, lane, keyword, page, size));
        }
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(
            () => service.SearchSelectorAsync(0, null!, null!, 1, 10, "unknown"));
    }

    private static PromptItem CreateItem(RangeEntity range, string tactic, string name, string content,
        decimal averageScore = -1m, decimal maxScore = -1m) =>
        new(new PromptItemDto
        {
            RangeId = range.Id, RangeName = range.RangeName, Tactic = tactic, Aiming = 1,
            ParentTac = "", NickName = name, Content = content, IsDraft = true,
            EvalAvgScore = averageScore, EvalMaxScore = maxScore
        });

    private static SelectorContext CreateContext(SqliteConnection connection, int tenant)
    {
        EntitySetKeys.TryLoadSetInfo(typeof(SelectorContext));
        return new SelectorContext(new DbContextOptionsBuilder().UseSqlite(connection).Options, tenant);
    }

    private static PromptItemService CreateService(SelectorContext context, IServiceProvider provider) =>
        new(new RepositoryBase<PromptItem>(new TestDbData(context)), provider, null!, null!);

    private sealed class SelectorContext : SenparcEntitiesDbContextBase
    {
        public DbSet<PromptItem> Items => Set<PromptItem>();
        public DbSet<RangeEntity> Ranges => Set<RangeEntity>();
        public DbSet<PromptResult> Results => Set<PromptResult>();

        public SelectorContext(DbContextOptions options, int tenant) : base(options, null!)
        {
            SetMultiTenantEnable(true);
            TenantInfo = new RequestTenantInfo { Id = tenant };
            TenantInfo.TryMatch(true);
        }
    }

    private sealed class TestDbData(DbContext context) : NcfDbData
    {
        public override DbContext BaseDataContext => context;
        public override void CloseConnection() { }
    }
}
