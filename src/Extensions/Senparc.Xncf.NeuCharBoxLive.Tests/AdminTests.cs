/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminTests.cs
    文件功能描述：测试后台页面逻辑：资源文件中英文键齐全、状态文案、页面引用的键存在、申请与邀请码的管理


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 设置页（SettingsView）、后台页面文本资源与时区的测试

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增测试：AI Kernel 读不到时模型列表为空并给出提示，不抛错

----------------------------------------------------------------*/

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>The Settings page's pure logic, the admin pages' text resources, and admin times in the usage zone.</summary>
[TestClass]
public class AdminTests
{
    static string ModuleDir([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "Senparc.Xncf.NeuCharBoxLive"));

    static HashSet<string> ResxKeys(string file) =>
        XDocument.Load(Path.Combine(ModuleDir(), "Resources", file)).Root!.Elements("data").Select(d => (string)d.Attribute("name")!).ToHashSet();

    static AIModelDto Model(int id, ConfigModelType type, AiPlatform platform, string? alias = null) => new()
    {
        Id = id, Alias = alias ?? "", ModelId = "m" + id, ConfigModelType = type, AiPlatform = platform, ApiKey = "sk-secret-not-real",
    };

    // ---- the try-it link ----

    [DataTestMethod]
    [DataRow("http://localhost:5000", "https://leozheng265.github.io/neucharbox-demo/?liveApi=http%3A%2F%2Flocalhost%3A5000")]
    [DataRow("http://127.0.0.1:5660", "https://leozheng265.github.io/neucharbox-demo/?liveApi=http%3A%2F%2F127.0.0.1%3A5660")]
    [DataRow("https://localhost:5081", null)]
    [DataRow("https://my-site.example", null)]
    public void TryItLink_OnlyForHttpLoopback(string origin, string? expected) => Assert.AreEqual(expected, SettingsView.TryItLink(origin));

    // ---- status ----

    static LiveSnapshot Snap(string? off) => off is null
        ? new LiveSnapshot(LiveLimits.Defaults, new LiveModel("m", null!), new LiveModel("m", null!), [], null)
        : LiveSnapshot.Off(off, []);

    [TestMethod]
    public void StatusKey_PicksOnOffAndBudget()
    {
        Assert.AreEqual("Status.On", SettingsView.StatusKey(Snap(null), false));
        Assert.AreEqual("Status.Off.budget", SettingsView.StatusKey(Snap(null), true));
        Assert.AreEqual("Status.Off.no_model", SettingsView.StatusKey(Snap(LiveOff.NoModel), true), "the off reason wins over the budget");
        Assert.AreEqual("Status.Off.config", SettingsView.StatusKey(Snap(LiveOff.Config), false));
    }

    [TestMethod]
    public void StatusKey_EveryOffReason_HasResxKeyInEnglishAndChinese()
    {
        var codes = typeof(LiveOff).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!).ToList();
        var keys = codes.Select(c => "Status.Off." + c).Append("Status.Off.budget").Append("Status.On").ToList();
        Assert.IsTrue(keys.Count >= 12);
        var neutral = ResxKeys("NeuCharBoxLiveResource.resx");
        var zh = ResxKeys("NeuCharBoxLiveResource.zh-CN.resx");
        foreach (var k in keys)
        {
            Assert.IsTrue(neutral.Contains(k), "neutral: " + k);
            Assert.IsTrue(zh.Contains(k), "zh-CN: " + k);
        }
        foreach (var c in codes)
            Assert.AreEqual("Status.Off." + c, SettingsView.StatusKey(LiveSnapshot.Off(c, []), false));
    }

    // ---- the model picker ----

    [TestMethod]
    public void Choices_OnlyChatModels_UnsupportedMarked()
    {
        var choices = SettingsView.Choices(new[]
        {
            Model(1, ConfigModelType.Chat, AiPlatform.OpenAI, "Fast one"),
            Model(2, ConfigModelType.TextEmbedding, AiPlatform.OpenAI),
            Model(3, ConfigModelType.Chat, AiPlatform.HuggingFace),
        });
        Assert.AreEqual(2, choices.Count);
        Assert.AreEqual(new ModelChoice(1, "Fast one", true), choices[0]);
        Assert.AreEqual(new ModelChoice(3, "m3", false), choices[1], "no alias: the model id is the label");
        foreach (var c in choices) Assert.IsFalse(c.ToString()!.Contains("sk-secret"), "a choice never carries the key");
    }

    [TestMethod]
    public void ModelProblems_NamesUnknownAndUnsupportedModels()
    {
        var choices = new[] { new ModelChoice(1, "a", true), new ModelChoice(3, "c", false) };
        Assert.AreEqual(0, SettingsView.ModelProblems(LiveSettings.Defaults, choices).Count);
        Assert.AreEqual(0, SettingsView.ModelProblems(LiveSettings.Defaults with { PlanModelId = 1, RunModelId = 1 }, choices).Count);
        CollectionAssert.AreEqual(new[] { "PlanModelId" }, SettingsView.ModelProblems(LiveSettings.Defaults with { PlanModelId = 3 }, choices).ToList());
        CollectionAssert.AreEqual(new[] { "RunModelId" }, SettingsView.ModelProblems(LiveSettings.Defaults with { PlanModelId = 1, RunModelId = 99 }, choices).ToList());
        CollectionAssert.AreEqual(new[] { "PlanModelId", "RunModelId" },
            SettingsView.ModelProblems(LiveSettings.Defaults with { PlanModelId = 2, RunModelId = 3 }, choices).ToList());
    }

    [TestMethod]
    public async Task Save_UnsupportedModel_IsRejected_AndNothingSaved()
    {
        using var db = new TestDb();
        var svc = new LiveSettingsService(db.Scopes);
        var raised = 0;
        svc.Changed += () => raised++;
        var choices = new[] { new ModelChoice(1, "a", true), new ModelChoice(3, "c", false) };

        var problems = await SettingsView.SaveAsync(svc, LiveSettings.Defaults with { PlanModelId = 3, PerCodeDaily = 0 }, choices);

        CollectionAssert.AreEquivalent(new[] { "PlanModelId", "PerCodeDaily" }, problems.ToList(), "model and limit problems together");
        Assert.AreEqual(0, raised);
        using (var ctx = db.NewContext()) Assert.AreEqual(0, ctx.Settings.Count());

        Assert.AreEqual(0, (await SettingsView.SaveAsync(svc, LiveSettings.Defaults with { PlanModelId = 1 }, choices)).Count);
        Assert.AreEqual(1, raised);
        Assert.AreEqual(1, (await svc.GetAsync()).PlanModelId);
    }

    sealed class UnreadableModels : IAiModelSource
    {
        public Task<AIModelDto?> GetAsync(int id, CancellationToken ct) => throw new InvalidOperationException("no such table: Senparc_AIKernel_AIModel");
        public Task<IReadOnlyList<AIModelDto>> ListAsync(CancellationToken ct) => throw new InvalidOperationException("no such table: Senparc_AIKernel_AIModel");
    }

    sealed class ListedModels(params AIModelDto[] models) : IAiModelSource
    {
        public Task<AIModelDto?> GetAsync(int id, CancellationToken ct) => Task.FromResult(models.FirstOrDefault(m => m.Id == id));
        public Task<IReadOnlyList<AIModelDto>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<AIModelDto>>(models);
    }

    /// <summary>
    /// Final review F4: AI Kernel not installed or its read failing must not break the module's home page: an empty picker,
    /// a flag the page turns into a status line (in English and Chinese), and a warning with the exception's type only.
    /// </summary>
    [TestMethod]
    public async Task LoadChoices_AiKernelUnreadable_EmptyPickerAndFlag_NeverThrows()
    {
        var log = new Fakes.ListLogger<AdminTests>();
        var (choices, unreadable) = await SettingsView.LoadChoicesAsync(new UnreadableModels(), log);
        Assert.AreEqual(0, choices.Count);
        Assert.IsTrue(unreadable);
        var warning = log.Entries.Single();
        Assert.AreEqual(Microsoft.Extensions.Logging.LogLevel.Warning, warning.Level);
        Assert.IsNull(warning.Exception);
        StringAssert.Contains(warning.Message, nameof(InvalidOperationException));

        (choices, unreadable) = await SettingsView.LoadChoicesAsync(new ListedModels(Model(1, ConfigModelType.Chat, AiPlatform.OpenAI)));
        Assert.AreEqual(1, choices.Count);
        Assert.IsFalse(unreadable);

        // A request that was cancelled isn't reported as AI Kernel being unreadable.
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => SettingsView.LoadChoicesAsync(new UnreadableModels(), null, cancelled.Token));

        foreach (var file in new[] { "NeuCharBoxLiveResource.resx", "NeuCharBoxLiveResource.zh-CN.resx" })
            Assert.IsTrue(ResxKeys(file).Contains("Settings.AiKernelUnreadable"), file);
        var page = File.ReadAllText(Path.Combine(ModuleDir(), "Areas", "Admin", "Pages", "NeuCharBoxLive", "Settings.cshtml"));
        StringAssert.Contains(page, "@XR[\"Settings.AiKernelUnreadable\"]", "the page shows the status line");
    }

    [TestMethod]
    public void ParseOrigins_OnePerLine_BlanksDropped()
    {
        CollectionAssert.AreEqual(new[] { "https://a.example", "http://b.example:81" },
            SettingsView.ParseOrigins(" https://a.example \r\n\r\nhttp://b.example:81\n ").ToList());
        Assert.AreEqual(0, SettingsView.ParseOrigins(null).Count);
    }

    // ---- resources ----

    [TestMethod]
    public void Resx_ZhCN_HasEveryNeutralKey()
    {
        var neutral = ResxKeys("NeuCharBoxLiveResource.resx");
        var zh = ResxKeys("NeuCharBoxLiveResource.zh-CN.resx");
        Assert.IsTrue(neutral.Count > 60, "the admin text is in the resx: " + neutral.Count);
        CollectionAssert.AreEqual(new string[0], neutral.Except(zh).OrderBy(k => k).ToArray(), "keys missing from zh-CN");
        CollectionAssert.AreEqual(new string[0], zh.Except(neutral).OrderBy(k => k).ToArray(), "zh-CN keys the neutral file lacks");
        // Neutral is English: no CJK in any value.
        var cjk = new Regex(@"[㐀-鿿]");
        foreach (var d in XDocument.Load(Path.Combine(ModuleDir(), "Resources", "NeuCharBoxLiveResource.resx")).Root!.Elements("data"))
            Assert.IsFalse(cjk.IsMatch((string)d.Element("value")!), "neutral value is not English: " + d.Attribute("name"));
    }

    [TestMethod]
    public void Pages_UseOnlyKeysThatExist()
    {
        var neutral = ResxKeys("NeuCharBoxLiveResource.resx");
        var files = Directory.EnumerateFiles(Path.Combine(ModuleDir(), "Areas"), "*.cshtml*", SearchOption.AllDirectories)
            .Concat(new[] { Path.Combine(ModuleDir(), "Register.Area.cs") }).ToList();
        Assert.IsTrue(files.Count >= 9);
        var used = new HashSet<string>();
        foreach (var file in files)
            foreach (Match m in Regex.Matches(File.ReadAllText(file), "(?:XR\\[|NeuCharBoxLiveResource\\.Get\\()\\s*\"([^\"]+)\""))
            {
                used.Add(m.Groups[1].Value);
                Assert.IsTrue(neutral.Contains(m.Groups[1].Value), $"{Path.GetFileName(file)} uses missing key {m.Groups[1].Value}");
            }
        Assert.IsTrue(used.Count > 40, "the pages use the resx: " + used.Count);
    }

    [TestMethod]
    public void Pages_HaveNoHardCodedEnglishHeadings()
    {
        // Titles and headings were the visible hard-coded English; they all go through XR now.
        foreach (var page in new[] { "Applications", "Codes", "Runs", "Settings" })
        {
            var text = File.ReadAllText(Path.Combine(ModuleDir(), "Areas", "Admin", "Pages", "NeuCharBoxLive", page + ".cshtml"));
            Assert.IsFalse(Regex.IsMatch(text, "<h3>[A-Za-z]"), page + " has a hard-coded heading");
            Assert.IsFalse(text.Contains("ViewData[\"Title\"] = \"NeuCharBox"), page + " has a hard-coded title");
        }
    }

    // ---- times in the usage zone ----

    [TestMethod]
    public void AdminTime_UsesTheUsageZone()
    {
        var t = new DateTimeOffset(2026, 11, 8, 3, 30, 0, TimeSpan.Zero);
        Assert.AreEqual("2026-11-08 03:30", AdminTime.InZone16(t, TimeZoneInfo.Utc));
        var shanghai = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        Assert.AreEqual("2026-11-08 11:30", AdminTime.InZone16(t, shanghai));
        Assert.AreEqual("2026-11-07 22:30", AdminTime.InZone16(t, TimeZoneInfo.FindSystemTimeZoneById("America/New_York")));
    }
}
