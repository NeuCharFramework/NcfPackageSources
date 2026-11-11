/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ConfigTests.cs
    文件功能描述：测试 NcbLive 配置段的绑定、问题检测与配置有误时关闭 Live


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增测试：旧版独立宿主的 NcbLive 键被忽略并只记一条警告（只列键名）；配置段无法绑定时用默认值且 Live 关闭

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 默认值改为不设上限；问题检查的用例改用试点时期的上限

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;
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
    public void LiveOptions_Defaults_NoCutOffs()
    {
        var o = new LiveOptions();
        // No request is cut short and no session is dropped for being idle unless the host sets a bound (0 = none).
        Assert.AreEqual(0, o.MaxCalls);
        Assert.AreEqual(0, o.MaxCallsWhatIf);
        Assert.AreEqual(TimeSpan.Zero, o.MaxWork);
        Assert.AreEqual(TimeSpan.Zero, o.SessionIdle);
        Assert.AreEqual("UTC", o.UsageTimeZone);
        Assert.AreEqual("required", o.PlanToolMode);
        Assert.AreEqual(400, o.MaxChars);
        Assert.AreEqual(1500, o.MaxCompletionTokens);
        Assert.AreEqual(TimeSpan.Zero, o.ApprovalWait);
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
        Assert.AreEqual(0, Fakes.Pilot.Options(o => o.SessionIdle = TimeSpan.FromMinutes(40)).Problems().Count, "exactly MaxWork + ApprovalWait");
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.SessionIdle) },
            Fakes.Pilot.Options(o => o.SessionIdle = TimeSpan.FromMinutes(40) - TimeSpan.FromSeconds(1)).Problems().ToList());
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.SessionIdle) },
            Fakes.Pilot.Options(o => o.ApprovalWait = TimeSpan.FromMinutes(40)).Problems().ToList());
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.MaxSessions) }, new LiveOptions { MaxSessions = 0 }.Problems().ToList());
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.AccessCheckPerMinute) },
            new LiveOptions { AccessCheckPerMinute = 0 }.Problems().ToList());
        Assert.AreEqual(0, new LiveOptions { MaxSessions = 1, AccessCheckPerMinute = 1 }.Problems().Count);
        Assert.AreEqual(0, new LiveOptions { UsageTimeZone = "America/New_York" }.Problems().Count);
        Assert.AreEqual(0, new LiveOptions { UsageTimeZone = "" }.Problems().Count, "empty is UTC");

        // An idle timeout with unbounded work or approval wait (the defaults) can never be long enough.
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.SessionIdle) }, new LiveOptions { SessionIdle = TimeSpan.FromMinutes(45) }.Problems().ToList());
        CollectionAssert.AreEqual(new[] { nameof(LiveOptions.SessionIdle) }, Fakes.Pilot.Options(o => o.ApprovalWait = TimeSpan.Zero).Problems().ToList());
        Assert.AreEqual(0, Fakes.Pilot.Options(o => o.SessionIdle = TimeSpan.Zero).Problems().Count, "no idle timeout is fine with any bounds");

        var all = new LiveOptions { SessionIdle = TimeSpan.FromSeconds(1), MaxSessions = 0, AccessCheckPerMinute = -1, UsageTimeZone = "Mars/Base" };
        CollectionAssert.AreEquivalent(new[] { "SessionIdle", "MaxSessions", "AccessCheckPerMinute", "UsageTimeZone" }, all.Problems().ToList());
    }

    static IConfiguration Config(Dictionary<string, string?> values) => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [TestMethod]
    public void Section_BindsIntoLiveOptions_AndAddNcbLiveRegistersIt()
    {
        var cfg = Config(new()
        {
            ["NcbLive:MaxWork"] = "00:05:00", ["NcbLive:ApprovalWait"] = "00:10:00", ["NcbLive:SessionIdle"] = "00:20:00", ["NcbLive:AccessCheckPerMinute"] = "7",
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

    /// <summary>
    /// The standalone pilot host's keys (the switch, models, endpoint, limits, extra origins) now live on the Settings page
    /// and in AI Kernel. Any still present are ignored, named (never their values) in one warning when the runtime is first
    /// resolved, and are not a problem: live isn't turned off for them.
    /// </summary>
    [TestMethod]
    public void PilotKeys_Ignored_OneWarningNamingThem()
    {
        var logs = new ListLoggerProvider();
        var cfg = Config(new()
        {
            ["NcbLive:Enabled"] = "true", ["NcbLive:PlanModel"] = "gpt-5-chat", ["NcbLive:Endpoint"] = "https://secret.example/19194/",
            ["NcbLive:ExtraOrigins:0"] = "https://a.example", ["NcbLive:MaxCalls"] = "7",
        });
        var services = new ServiceCollection().AddNcbLive(cfg);
        services.AddSingleton<ILoggerProvider>(logs);
        using var sp = services.BuildServiceProvider();

        Assert.AreEqual(LiveOff.NoModel, sp.GetRequiredService<ILiveRuntime>().Current.OffReason, "ignored, not a config problem");
        Assert.AreEqual(7, sp.GetRequiredService<LiveOptions>().MaxCalls, "the rest of the section still binds");
        lock (logs.Entries)
        {
            var warning = logs.Entries.Single(e => e.Level == LogLevel.Warning).Message;
            foreach (var key in new[] { "Enabled", "PlanModel", "Endpoint", "ExtraOrigins" }) StringAssert.Contains(warning, key);
            foreach (var absent in new[] { "RunModel", "PerCodeDaily", "GlobalDaily", "DailyTokenBudget" }) Assert.IsFalse(warning.Contains(absent), absent);
            Assert.IsFalse(warning.Contains("secret.example") || warning.Contains("gpt-5-chat") || warning.Contains("a.example"), "names only, never values");
            StringAssert.Contains(warning, "Settings page");
            StringAssert.Contains(warning, "AI Kernel");
            Assert.AreEqual(0, logs.Entries.Count(e => e.Level >= LogLevel.Error));
        }
        CollectionAssert.AreEquivalent(
            new[] { "Enabled", "PlanModel", "RunModel", "Endpoint", "PerCodeDaily", "GlobalDaily", "DailyTokenBudget", "ExtraOrigins" },
            NcbLiveServiceCollectionExtensions.MovedKeys.ToList());

        // None of them: no warning.
        var quiet = new ListLoggerProvider();
        var plain = new ServiceCollection().AddNcbLive(Config(new() { ["NcbLive:MaxCalls"] = "7" }));
        plain.AddSingleton<ILoggerProvider>(quiet);
        using var plainSp = plain.BuildServiceProvider();
        _ = plainSp.GetRequiredService<ILiveRuntime>();
        lock (quiet.Entries) Assert.AreEqual(0, quiet.Entries.Count);
    }

    /// <summary>
    /// A section the binder can't read (final review F1): AddNcbLive doesn't throw, LiveOptions are all the defaults, and the
    /// snapshot is off (config) with one error carrying the binder's message (it names the key). The runtime itself is untouched.
    /// </summary>
    [TestMethod]
    public void AddNcbLive_UnreadableSection_DefaultsAndLiveOffConfig_NeverThrows()
    {
        var logs = new ListLoggerProvider();
        var runtime = new StaticLiveRuntime(Fakes.Snapshots.On(new Fakes.ScriptedChatClient()));
        var services = new ServiceCollection().AddNcbLive(Config(new() { ["NcbLive:MaxWork"] = "30m", ["NcbLive:SessionIdle"] = "00:05:00" }),
            b => b.UseRuntime(runtime));
        services.AddSingleton<ILoggerProvider>(logs);
        using var sp = services.BuildServiceProvider();

        var o = sp.GetRequiredService<LiveOptions>();
        Assert.AreEqual(TimeSpan.Zero, o.MaxWork);
        Assert.AreEqual(TimeSpan.Zero, o.SessionIdle, "nothing of the section is half-bound");
        var snap = sp.GetRequiredService<ILiveRuntime>().Current;
        Assert.AreEqual(LiveOff.Config, snap.OffReason);
        Assert.IsFalse(snap.On);
        Assert.IsTrue(runtime.Current.On, "the runtime itself is untouched");
        lock (logs.Entries)
        {
            var error = logs.Entries.Single(e => e.Level >= LogLevel.Error).Message;
            StringAssert.Contains(error, "NcbLive:MaxWork");
        }

        // The module isn't serving at all: that reason says more than config, and keeps apply and access/check shut.
        runtime.Current = LiveSnapshot.Off(LiveOff.MultiTenant, LiveCors.BuiltIns);
        Assert.AreEqual(LiveOff.MultiTenant, sp.GetRequiredService<ILiveRuntime>().Current.OffReason);
        runtime.Current = LiveSnapshot.Off(LiveOff.Disabled, LiveCors.BuiltIns);
        Assert.AreEqual(LiveOff.Config, sp.GetRequiredService<ILiveRuntime>().Current.OffReason);
    }
}
