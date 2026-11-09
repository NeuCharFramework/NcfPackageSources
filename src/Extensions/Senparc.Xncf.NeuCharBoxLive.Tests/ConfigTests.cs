/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ConfigTests.cs
    文件功能描述：NeuCharBox Live 测试：ConfigTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;
using Senparc.Xncf.NeuCharBoxLive.Api;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// LiveOptions holds engine tuning only: its code defaults are the pilot's values, and <see cref="LiveOptions.Problems"/>
/// names settings that can't work together without ever throwing. On/off, models, limits and origins come from the
/// runtime's snapshot (RuntimeTests).
/// </summary>
[TestClass]
public class ConfigTests
{
    [TestMethod]
    public void LiveOptions_Defaults_MatchPilot()
    {
        var o = new LiveOptions();
        Assert.AreEqual(60, o.MaxCalls);
        Assert.AreEqual(60, o.MaxCallsWhatIf);
        Assert.AreEqual(TimeSpan.FromMinutes(30), o.MaxWork);
        Assert.AreEqual(TimeSpan.FromMinutes(45), o.SessionIdle);
        Assert.AreEqual("UTC", o.UsageTimeZone);
        Assert.AreEqual("required", o.PlanToolMode);
        Assert.AreEqual(400, o.MaxChars);
        Assert.AreEqual(1500, o.MaxCompletionTokens);
        Assert.AreEqual(TimeSpan.FromMinutes(10), o.ApprovalWait);
        Assert.AreEqual(6, o.HistoryTurns);
        Assert.AreEqual(200, o.MaxSessions);
        Assert.AreEqual(30, o.AccessCheckPerMinute);
        Assert.AreEqual(new FastForwardPacing(0.1, 0.6, 25), o.Pacing);
        CollectionAssert.AreEqual(Array.Empty<string>(), o.Problems().ToList());

        // These moved to the runtime's snapshot (models, limits, origins, on/off) or to the interim wiring (endpoint).
        foreach (var gone in new[] { "Enabled", "Endpoint", "PlanModel", "RunModel", "PerCodeDaily", "GlobalDaily", "DailyTokenBudget", "ExtraOrigins" })
            Assert.IsNull(typeof(LiveOptions).GetProperty(gone), gone);
    }

    [TestMethod]
    public void LiveOptions_UnknownZone_IsAProblem_NotAnException()
        => CollectionAssert.Contains(new LiveOptions { UsageTimeZone = "Mars/Base" }.Problems().ToList(), "UsageTimeZone");

    [TestMethod]
    public void LiveOptions_Problems_NamesEachBadSetting()
    {
        // A request can be quiet for its work time plus one approval wait; its session must outlive that.
        Assert.AreEqual(0, new LiveOptions { SessionIdle = TimeSpan.FromMinutes(40) }.Problems().Count, "exactly MaxWork + ApprovalWait");
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.SessionIdle) },
            new LiveOptions { SessionIdle = TimeSpan.FromMinutes(40) - TimeSpan.FromSeconds(1) }.Problems().ToList());
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.SessionIdle) },
            new LiveOptions { ApprovalWait = TimeSpan.FromMinutes(40) }.Problems().ToList());
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.MaxSessions) }, new LiveOptions { MaxSessions = 0 }.Problems().ToList());
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.AccessCheckPerMinute) },
            new LiveOptions { AccessCheckPerMinute = 0 }.Problems().ToList());
        Assert.AreEqual(0, new LiveOptions { MaxSessions = 1, AccessCheckPerMinute = 1 }.Problems().Count);
        Assert.AreEqual(0, new LiveOptions { UsageTimeZone = "America/New_York" }.Problems().Count);
        Assert.AreEqual(0, new LiveOptions { UsageTimeZone = "" }.Problems().Count, "empty is UTC");

        var all = new LiveOptions { SessionIdle = TimeSpan.Zero, MaxSessions = 0, AccessCheckPerMinute = -1, UsageTimeZone = "Mars/Base" };
        CollectionAssert.AreEquivalent(new[] { "SessionIdle", "MaxSessions", "AccessCheckPerMinute", "UsageTimeZone" }, all.Problems().ToList());
    }

    static IConfiguration Config(Dictionary<string, string?> values) => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [TestMethod]
    public void Section_BindsIntoLiveOptions_AndAddNcbLiveRegistersIt()
    {
        var cfg = Config(new()
        {
            ["NcbLive:MaxWork"] = "00:05:00", ["NcbLive:SessionIdle"] = "00:20:00", ["NcbLive:AccessCheckPerMinute"] = "7",
            ["NcbLive:Pacing:IdleHourSeconds"] = "0.2", ["NcbLive:Pacing:EventSeconds"] = "0.5", ["NcbLive:Pacing:WeekCapSeconds"] = "10",
            ["NcbLive:UsageTimeZone"] = "America/New_York",
        });
        var bound = cfg.GetSection(LiveOptions.Section).Get<LiveOptions>(b => b.ErrorOnUnknownConfiguration = true)!;
        Assert.AreEqual(TimeSpan.FromMinutes(5), bound.MaxWork);
        Assert.AreEqual(new FastForwardPacing(0.2, 0.5, 10), bound.Pacing);

        using var sp = new ServiceCollection().AddNcbLive(cfg).BuildServiceProvider();
        var o = sp.GetRequiredService<LiveOptions>();
        Assert.AreEqual(TimeSpan.FromMinutes(20), o.SessionIdle);
        Assert.AreEqual(new FastForwardPacing(0.2, 0.5, 10), o.Pacing);
        Assert.AreEqual(7, sp.GetRequiredService<AccessCheckRateLimiter>().Limit);
        var day = sp.GetRequiredService<UsageDay>();
        Assert.AreEqual(new DateOnly(2026, 11, 8), day.For(DateTimeOffset.Parse("2026-11-09T00:30Z")), "days flip at midnight in New York");
        Assert.AreEqual(LiveOff.NoModel, sp.GetRequiredService<ILiveRuntime>().Current.OffReason, "no runtime: off, no_model");
    }

    [TestMethod]
    public void AddNcbLive_UnknownZone_UsesUtc_AndLiveIsOff()
    {
        var cfg = Config(new() { ["NcbLive:UsageTimeZone"] = "Mars/Base" });
        var runtime = new StaticLiveRuntime(new LiveSnapshot(LiveLimits.Defaults, new LiveModel("m", new Fakes.ScriptedChatClient()),
            new LiveModel("m", new Fakes.ScriptedChatClient()), LiveCors.BuiltIns, null));
        using var sp = new ServiceCollection().AddNcbLive(cfg, b => b.UseRuntime(runtime)).BuildServiceProvider();
        Assert.AreSame(TimeZoneInfo.Utc, sp.GetRequiredService<UsageDay>().Zone);
        var snap = sp.GetRequiredService<ILiveRuntime>().Current;
        Assert.AreEqual(LiveOff.Config, snap.OffReason);
        Assert.IsFalse(snap.On);
        Assert.IsTrue(runtime.Current.On, "the runtime itself is untouched");
    }

    sealed class ContainerRuntime(LiveOptions opt) : ILiveRuntime
    {
        public LiveOptions Options { get; } = opt;
        public LiveSnapshot Current { get; } = LiveSnapshot.Off(LiveOff.Starting, LiveCors.BuiltIns);
    }

    [TestMethod]
    public void UseRuntime_OfType_IsBuiltByTheContainer()
    {
        using var sp = new ServiceCollection().AddNcbLive(Config(new()), b => b.UseRuntime<ContainerRuntime>()).BuildServiceProvider();
        var rt = sp.GetRequiredService<ILiveRuntime>();
        Assert.AreSame(sp.GetRequiredService<ContainerRuntime>(), rt);
        Assert.AreSame(sp.GetRequiredService<LiveOptions>(), ((ContainerRuntime)rt).Options);
        Assert.AreEqual(LiveOff.Starting, rt.Current.OffReason);
    }
}
