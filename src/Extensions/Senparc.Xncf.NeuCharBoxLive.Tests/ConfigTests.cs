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
/// The shipped host config (the NcbLive section of the demo host's appsettings.json, kept as TestData/appsettings.live.json) binds into LiveOptions as written: TimeSpans, the positional Pacing record, the origins, no stray keys.
/// </summary>
[TestClass]
public class ConfigTests
{
    // The NCF repo has no host; TestData/appsettings.live.json is a copy of the host's "NcbLive" section.
    static IConfiguration Shipped() => new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "TestData", "appsettings.live.json"), optional: false, reloadOnChange: false)
        .Build();

    [TestMethod]
    public void ShippedAppsettings_NcbLiveSection_BindsIntoLiveOptions()
    {
        var section = Shipped().GetSection(LiveOptions.Section);
        Assert.IsTrue(section.Exists(), "the NcbLive section");
        // A key LiveOptions has no property for (a typo) fails here instead of being ignored.
        var o = section.Get<LiveOptions>(b => b.ErrorOnUnknownConfiguration = true)!;

        Assert.IsTrue(o.Enabled);
        Assert.AreEqual("https://www.neuchar.com/19194/", o.Endpoint);
        Assert.AreEqual("gpt-5-chat", o.PlanModel);
        Assert.AreEqual("gpt-5-chat", o.RunModel);
        Assert.AreEqual("required", o.PlanToolMode);
        Assert.AreEqual(20, o.PerCodeDaily);
        Assert.AreEqual(300, o.GlobalDaily);
        // Leo lifted the per-run caps in the shipped file for now (1541fff, 2026-11-08); the code keeps its defaults.
        Assert.AreEqual(60, o.MaxCalls);
        Assert.AreEqual(60, o.MaxCallsWhatIf);
        Assert.AreEqual(400, o.MaxChars);
        Assert.AreEqual(TimeSpan.FromMinutes(30), o.MaxWork);
        Assert.AreEqual(1500, o.MaxCompletionTokens);
        Assert.AreEqual(TimeSpan.FromMinutes(10), o.ApprovalWait);
        Assert.AreEqual(TimeSpan.FromMinutes(45), o.SessionIdle);
        Assert.AreEqual(6, o.HistoryTurns);
        Assert.AreEqual(1_000_000, o.DailyTokenBudget);
        Assert.AreEqual(200, o.MaxSessions);
        Assert.AreEqual(30, o.AccessCheckPerMinute);
        CollectionAssert.AreEqual(new[] { "https://demo.neucharbox.com" }, o.ExtraOrigins);
        Assert.AreEqual(new FastForwardPacing(0.1, 0.6, 25), o.Pacing);
        o.Validate();

        // The file writes out every option, at the code's defaults (ExtraOrigins and the lifted caps aside), so the two
        // can't drift apart.
        var lifted = new[] { nameof(LiveOptions.MaxCalls), nameof(LiveOptions.MaxCallsWhatIf), nameof(LiveOptions.MaxWork), nameof(LiveOptions.SessionIdle) };
        var defaults = new LiveOptions();
        foreach (var p in typeof(LiveOptions).GetProperties())
        {
            Assert.IsTrue(section.GetSection(p.Name).Exists(), $"NcbLive:{p.Name} is in the shipped file");
            if (p.Name != nameof(LiveOptions.ExtraOrigins) && !lifted.Contains(p.Name))
                Assert.AreEqual(p.GetValue(defaults), p.GetValue(o), $"NcbLive:{p.Name} matches LiveOptions' default");
        }
    }

    [TestMethod]
    public void ShippedAppsettings_AddNcbLive_RegistersTheBoundOptions()
    {
        using var sp = new ServiceCollection().AddNcbLive(Shipped()).BuildServiceProvider();
        var o = sp.GetRequiredService<LiveOptions>();
        Assert.AreEqual(new FastForwardPacing(0.1, 0.6, 25), o.Pacing);
        Assert.AreEqual(TimeSpan.FromMinutes(45), o.SessionIdle);
        Assert.AreEqual(30, sp.GetRequiredService<AccessCheckRateLimiter>().Limit);
        CollectionAssert.AreEqual(new[] { "https://demo.neucharbox.com" }, o.ExtraOrigins);
    }
}
