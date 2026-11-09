/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ModuleRuntimeTests.cs
    文件功能描述：测试模块运行时：安装、启用、多租户、模型与来源如何决定 Live 是否开启


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增模块运行时测试：关闭原因顺序、设置保存即生效、刷新失败、来源与依赖注入

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 模块已关闭（module_closed）时保留设置中的额外来源；未安装与多租户仍只有内置来源

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增测试：NCF 未就绪时不读数据库、配置值无法转换时模块照常注册（含 DbContext）且 Live 关闭（config）、模块状态映射

----------------------------------------------------------------*/

using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Senparc.AI;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Models;
using Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// <see cref="ModuleLiveRuntime"/>: live is on only when multi-tenancy is off, the module is installed and open, the
/// settings enable it and a model is chosen and usable. Fakes stand in for NCF's module table and AI Kernel's models;
/// the settings live in SQLite in memory. No network: the OpenAI clients are built, never called.
/// </summary>
[TestClass]
public class ModuleRuntimeTests
{
    static readonly DateTimeOffset T0 = new(2026, 11, 8, 15, 0, 0, TimeSpan.Zero);

    /// <summary>A Chat model on OpenAI with a made-up key: <see cref="AiKernelChatClients"/> builds it without a call.</summary>
    static AIModelDto Chat(int id, string model = "gpt-5-chat") => new()
    {
        Id = id,
        Alias = "live-" + id,
        AiPlatform = AiPlatform.OpenAI,
        ConfigModelType = ConfigModelType.Chat,
        Endpoint = "",
        DeploymentName = "",
        ModelId = model,
        ApiKey = "sk-test-not-a-key",
        ApiVersion = "",
    };

    /// <summary>The settings with live on and plan model 7, the rest as given.</summary>
    static LiveSettings Good(int? run = null, IReadOnlyList<string>? extra = null) =>
        LiveSettings.Defaults with { PlanModelId = 7, RunModelId = run, ExtraOrigins = extra ?? [] };

    sealed class FakeModuleState : IModuleState
    {
        int _reads;

        public ModuleStatus Status { get; set; } = ModuleStatus.Open;
        public Exception? Throw { get; set; }
        public int Reads => Volatile.Read(ref _reads);

        public Task<ModuleStatus> GetAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref _reads);
            return Throw is { } e ? Task.FromException<ModuleStatus>(e) : Task.FromResult(Status);
        }
    }

    sealed class FakeModels : IAiModelSource
    {
        public Dictionary<int, AIModelDto> Models { get; } = new();

        public Task<AIModelDto?> GetAsync(int id, CancellationToken ct) =>
            Task.FromResult(Models.TryGetValue(id, out var m) ? m : null);
    }

    sealed class FakeEnv(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Senparc.Xncf.NeuCharBoxLive.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    /// <summary>Counts the scopes opened through it: every settings read or save opens one.</summary>
    sealed class CountingScopes(IServiceScopeFactory inner) : IServiceScopeFactory
    {
        int _created;

        public int Created => Volatile.Read(ref _created);

        public IServiceScope CreateScope()
        {
            Interlocked.Increment(ref _created);
            return inner.CreateScope();
        }
    }

    /// <summary>A runtime over fakes and a SQLite settings table; model 7 exists unless a test removes it.</summary>
    sealed class Rig : IDisposable
    {
        volatile bool _ready = true;

        public Rig(string environment = "Production", bool createTables = true)
        {
            Db = new TestDb(createTables);
            Scopes = new CountingScopes(Db.Scopes);
            Settings = new LiveSettingsService(Scopes);
            Models.Models[7] = Chat(7);
            Runtime = new ModuleLiveRuntime(Settings, Clients, Module, Models, new FakeEnv(environment), () => MultiTenant, () => Ready,
                Time, Log);
        }

        public TestDb Db { get; }
        public CountingScopes Scopes { get; }
        public LiveSettingsService Settings { get; }

        /// <summary>NCF's readiness signal (its XNCF threads loaded); true unless a test says otherwise.</summary>
        public bool Ready { get => _ready; set => _ready = value; }
        public AiKernelChatClients Clients { get; } = new();
        public FakeModuleState Module { get; } = new();
        public FakeModels Models { get; } = new();
        public bool MultiTenant { get; set; }
        public FakeTimeProvider Time { get; } = new(T0);
        public ListLogger<ModuleLiveRuntime> Log { get; } = new();
        public ModuleLiveRuntime Runtime { get; }

        public async Task<LiveSnapshot> RefreshAsync()
        {
            await Runtime.RefreshAsync();
            return Runtime.Current;
        }

        public async Task SaveAsync(LiveSettings s) => Assert.AreEqual(0, (await Settings.SaveAsync(s)).Count, "valid settings");

        public int Warnings => Log.Entries.Count(e => e.Level == LogLevel.Warning);
        public int Errors => Log.Entries.Count(e => e.Level >= LogLevel.Error);

        public void Dispose() => Db.Dispose();
    }

    static void AssertOff(LiveSnapshot s, string reason)
    {
        Assert.AreEqual(reason, s.OffReason);
        Assert.IsFalse(s.On);
        Assert.IsNull(s.Plan);
        Assert.IsNull(s.Run);
    }

    // ---- the off reasons ----

    [TestMethod]
    public void Constructor_DoesNoIo_AndStartsOffStarting()
    {
        // The session sweep resolves ILiveRuntime at startup: building the runtime must never read or throw.
        using var rig = new Rig(createTables: false);
        rig.Module.Throw = new InvalidOperationException("no database yet");
        AssertOff(rig.Runtime.Current, LiveOff.Starting);
        Assert.AreEqual(0, rig.Module.Reads);
        CollectionAssert.AreEqual(LiveCors.BuiltIns.ToList(), rig.Runtime.Current.Origins.ToList());
        Assert.AreEqual(LiveLimits.Defaults, rig.Runtime.Current.Limits);
    }

    [TestMethod]
    public async Task NotInstalled_IsOff_AndHealthSaysSo()
    {
        // Not installed: the module's tables don't exist, and the runtime never reads them.
        using var rig = new Rig(createTables: false);
        rig.Module.Status = ModuleStatus.NotInstalled;
        AssertOff(await rig.RefreshAsync(), LiveOff.NotInstalled);

        var logs = new ListLoggerProvider();
        await using var h = await LiveHost.StartAsync(_ => null, builder: b => b.UseRuntime(_ => rig.Runtime),
            services: (_, s) => s.AddSingleton<ILoggerProvider>(logs));
        Assert.AreEqual(LiveOff.NotInstalled, h.Get<ILiveRuntime>().Current.OffReason);
        var health = await h.GetAsync("health");
        Assert.AreEqual(200, health.Code);
        Assert.AreEqual(false, (bool?)health.Body!["liveEnabled"], health.Body.ToJsonString());
        var c = await h.NewCodeAsync();
        var r = await h.PostAsync("sessions", new { scene = "home" }, c.Code);
        Assert.AreEqual(503, r.Code, r.Body?.ToJsonString());
        Assert.AreEqual("live_off", r.Error);

        Assert.AreEqual(0, rig.Errors, "not installed is a state, not an error");
        Assert.AreEqual(0, rig.Warnings);
        lock (logs.Entries) Assert.IsFalse(logs.Entries.Any(e => e.Level >= LogLevel.Error), string.Join("\n", logs.Entries));
    }

    [TestMethod]
    public async Task Closed_IsOff()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good());
        rig.Module.Status = ModuleStatus.Closed;
        AssertOff(await rig.RefreshAsync(), LiveOff.ModuleClosed);
        rig.Module.Status = ModuleStatus.Open;
        Assert.IsTrue((await rig.RefreshAsync()).On);
    }

    [TestMethod]
    public async Task MultiTenant_IsOff_EvenWhenAllElseIsGood()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good());
        rig.MultiTenant = true;
        AssertOff(await rig.RefreshAsync(), LiveOff.MultiTenant);
        Assert.AreEqual(0, rig.Module.Reads, "multi_tenant is the first check");

        rig.Module.Status = ModuleStatus.NotInstalled;
        AssertOff(await rig.RefreshAsync(), LiveOff.MultiTenant);

        rig.MultiTenant = false;
        rig.Module.Status = ModuleStatus.Open;
        Assert.IsTrue((await rig.RefreshAsync()).On);
    }

    [TestMethod]
    public async Task Disabled_IsOff()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good() with { Enabled = false, PerCodeDaily = 3 });
        var s = await rig.RefreshAsync();
        AssertOff(s, LiveOff.Disabled);
        Assert.AreEqual(3, s.Limits.PerCodeDaily, "an off snapshot read from the settings keeps their limits");

        // disabled comes before no_model.
        await rig.SaveAsync(LiveSettings.Defaults with { Enabled = false });
        AssertOff(await rig.RefreshAsync(), LiveOff.Disabled);
    }

    [TestMethod]
    public async Task NoPlanModel_IsOff_NoModel()
    {
        using var rig = new Rig();
        AssertOff(await rig.RefreshAsync(), LiveOff.NoModel); // no settings row: the defaults, enabled with no model

        await rig.SaveAsync(LiveSettings.Defaults with { RunModelId = 7 });
        AssertOff(await rig.RefreshAsync(), LiveOff.NoModel);
    }

    [TestMethod]
    public async Task RunModelEmpty_UsesPlanModel()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good(run: null));
        var s = await rig.RefreshAsync();
        Assert.IsTrue(s.On);
        Assert.AreSame(s.Plan!.Client, s.Run!.Client);
        Assert.AreEqual("gpt-5-chat", s.Plan.Name);
        Assert.AreEqual(s.Plan.Name, s.Run.Name);

        // A run model of its own.
        rig.Models.Models[8] = Chat(8, "gpt-5-mini");
        await rig.SaveAsync(Good(run: 8));
        s = await rig.RefreshAsync();
        Assert.IsTrue(s.On);
        Assert.AreEqual("gpt-5-chat", s.Plan!.Name);
        Assert.AreEqual("gpt-5-mini", s.Run!.Name);
        Assert.AreNotSame(s.Plan.Client, s.Run.Client);
    }

    [TestMethod]
    public async Task DeletedModel_IsOff_ModelMissing()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good());
        rig.Models.Models.Remove(7);
        AssertOff(await rig.RefreshAsync(), LiveOff.ModelMissing);

        // The run model gone, the plan model there: still model_missing.
        rig.Models.Models[7] = Chat(7);
        await rig.SaveAsync(Good(run: 9));
        AssertOff(await rig.RefreshAsync(), LiveOff.ModelMissing);

        // Not a Chat model: model_unsupported; one missing and one unsupported: model_missing comes first.
        rig.Models.Models[7] = Chat(7);
        rig.Models.Models[7].ConfigModelType = ConfigModelType.TextEmbedding;
        await rig.SaveAsync(Good());
        AssertOff(await rig.RefreshAsync(), LiveOff.ModelUnsupported);
        await rig.SaveAsync(Good(run: 9));
        AssertOff(await rig.RefreshAsync(), LiveOff.ModelMissing);

        // An unsupported platform for the run model.
        rig.Models.Models[7] = Chat(7);
        rig.Models.Models[9] = Chat(9);
        rig.Models.Models[9].AiPlatform = AiPlatform.Ollama;
        AssertOff(await rig.RefreshAsync(), LiveOff.ModelUnsupported);
    }

    [TestMethod]
    public async Task AllGood_IsOn_WithSettingsLimits()
    {
        using var rig = new Rig();
        var saved = Good() with { PerCodeDaily = 5, GlobalDaily = 6, DailyTokenBudget = 7000 };
        await rig.SaveAsync(saved);
        var s = await rig.RefreshAsync();
        Assert.IsTrue(s.On);
        Assert.IsNull(s.OffReason);
        Assert.AreEqual(saved.Limits, s.Limits);
        Assert.AreEqual(new LiveLimits(5, 6, 7000), s.Limits);
        Assert.AreEqual(0, rig.Errors);
        Assert.AreEqual(0, rig.Warnings);
    }

    // ---- refreshing ----

    [TestMethod]
    public async Task SettingsSaved_TakesEffectWithoutRestart()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good());
        await rig.Runtime.StartAsync(CancellationToken.None);
        try
        {
            await LiveHost.WaitUntil(() => Task.FromResult(rig.Runtime.Current.On), "the first refresh");

            // The fake clock never moves here, so only the Changed event can trigger the refresh.
            rig.Models.Models[8] = Chat(8, "gpt-5-mini");
            await rig.SaveAsync(Good(run: 8) with { PerCodeDaily = 2, GlobalDaily = 3, DailyTokenBudget = 4 });
            await LiveHost.WaitUntil(() => Task.FromResult(rig.Runtime.Current.Limits == new LiveLimits(2, 3, 4)), "the saved limits");
            Assert.AreEqual("gpt-5-mini", rig.Runtime.Current.Run!.Name);
            Assert.IsTrue(rig.Runtime.Current.On);

            await rig.SaveAsync(Good() with { Enabled = false });
            await LiveHost.WaitUntil(() => Task.FromResult(rig.Runtime.Current.OffReason == LiveOff.Disabled), "disabled");
        }
        finally { await rig.Runtime.StopAsync(CancellationToken.None); }
    }

    /// <summary>
    /// Final review M5: the Settings page's save awaits the runtime's refresh, so the page it redirects to already shows the
    /// new status (the Changed refresh runs in the background and could come after). Not started here: no Changed handler.
    /// </summary>
    [TestMethod]
    public async Task SettingsPageSave_AwaitsTheRefresh_SoTheNextPageShowsTheNewStatus()
    {
        using var rig = new Rig();
        var choices = new[] { new ModelChoice(7, "live-7", true) };
        AssertOff(rig.Runtime.Current, LiveOff.Starting);

        var problems = await SettingsView.SaveAsync(rig.Settings, Good() with { PerCodeDaily = 0 }, choices, refresh: rig.Runtime.RefreshAsync);
        Assert.AreEqual(1, problems.Count);
        Assert.AreEqual(0, rig.Module.Reads, "a refused save refreshes nothing");

        problems = await SettingsView.SaveAsync(rig.Settings, Good() with { PerCodeDaily = 4 }, choices, refresh: rig.Runtime.RefreshAsync);
        Assert.AreEqual(0, problems.Count);
        Assert.IsTrue(rig.Runtime.Current.On, "refreshed before SaveAsync returned");
        Assert.AreEqual(4, rig.Runtime.Current.Limits.PerCodeDaily);
    }

    [TestMethod]
    public async Task Timer_RefreshesEveryFiveSeconds()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good());
        await rig.Runtime.StartAsync(CancellationToken.None);
        try
        {
            await LiveHost.WaitUntil(() => Task.FromResult(rig.Runtime.Current.On), "the first refresh");
            rig.Module.Status = ModuleStatus.Closed;
            rig.Time.Advance(TimeSpan.FromSeconds(4.9));
            await Task.Delay(100);
            Assert.IsTrue(rig.Runtime.Current.On, "no refresh before 5 s");
            rig.Time.Advance(TimeSpan.FromSeconds(0.1));
            await LiveHost.WaitUntil(() => Task.FromResult(rig.Runtime.Current.OffReason == LiveOff.ModuleClosed), "the 5 s refresh");
        }
        finally { await rig.Runtime.StopAsync(CancellationToken.None); }

        // Stopped: neither the clock nor a save refreshes it any more.
        var reads = rig.Module.Reads;
        rig.Time.Advance(TimeSpan.FromSeconds(10));
        await rig.SaveAsync(Good());
        await Task.Delay(100);
        Assert.AreEqual(reads, rig.Module.Reads);
    }

    /// <summary>
    /// Ruling 14: until NCF reports the site ready (its XNCF threads loaded, which a site in maintenance or not yet updated
    /// never does), the hosted runtime reads nothing (no module state, no settings, not even after a save) and stays
    /// starting. The first tick after NCF is ready refreshes as usual.
    /// </summary>
    [TestMethod]
    public async Task NcfNotReady_ReadsNothing_StaysStarting_ThenRefreshesOnceReady()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good());
        rig.Ready = false;
        var scopes = rig.Scopes.Created;
        await rig.Runtime.StartAsync(CancellationToken.None);
        try
        {
            for (var i = 0; i < 3; i++)
            {
                await Task.Delay(50);
                rig.Time.Advance(ModuleLiveRuntime.Period);
            }
            await rig.SaveAsync(Good() with { PerCodeDaily = 3 }); // raises Changed
            await Task.Delay(100);
            Assert.AreEqual(0, rig.Module.Reads, "no module read before NCF is ready");
            Assert.AreEqual(scopes + 1, rig.Scopes.Created, "no settings read either: the one scope is the save's own");
            AssertOff(rig.Runtime.Current, LiveOff.Starting);
            CollectionAssert.AreEqual(LiveCors.BuiltIns.ToList(), rig.Runtime.Current.Origins.ToList());

            rig.Ready = true;
            await LiveHost.WaitUntil(() =>
            {
                rig.Time.Advance(ModuleLiveRuntime.Period);
                return Task.FromResult(rig.Runtime.Current.On);
            }, "the first refresh once NCF is ready");
            Assert.AreEqual(3, rig.Runtime.Current.Limits.PerCodeDaily);
            Assert.IsTrue(rig.Module.Reads > 0);
        }
        finally { await rig.Runtime.StopAsync(CancellationToken.None); }
    }

    [TestMethod]
    public async Task RefreshThrows_IsOff_Unavailable()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good(extra: ["https://a.example"]) with { PerCodeDaily = 4 });
        Assert.IsTrue((await rig.RefreshAsync()).On);

        rig.Module.Throw = new InvalidOperationException("the database is down");
        var s = await rig.RefreshAsync();
        AssertOff(s, LiveOff.Unavailable);
        CollectionAssert.Contains(s.Origins.ToList(), "https://a.example", "the last origins stay, so the demo can still show live as off");
        Assert.AreEqual(4, s.Limits.PerCodeDaily);
        Assert.AreEqual(1, rig.Warnings);
        StringAssert.Contains(rig.Log.Entries.Single(e => e.Level == LogLevel.Warning).Message, "the database is down");

        // At most one warning a minute.
        rig.Time.Advance(TimeSpan.FromSeconds(59));
        AssertOff(await rig.RefreshAsync(), LiveOff.Unavailable);
        Assert.AreEqual(1, rig.Warnings);
        rig.Time.Advance(TimeSpan.FromSeconds(1));
        AssertOff(await rig.RefreshAsync(), LiveOff.Unavailable);
        Assert.AreEqual(2, rig.Warnings);
        Assert.AreEqual(0, rig.Errors);

        rig.Module.Throw = null;
        Assert.IsTrue((await rig.RefreshAsync()).On, "back on once it reads again");

        // The settings table missing (installed but not migrated) is unavailable too, not an exception.
        using var bare = new Rig(createTables: false);
        AssertOff(await bare.RefreshAsync(), LiveOff.Unavailable);
        Assert.AreEqual(1, bare.Warnings);

        // So is a multi-tenancy check that throws.
        using var odd = new Rig();
        var oddRuntime = new ModuleLiveRuntime(odd.Settings, odd.Clients, odd.Module, odd.Models, new FakeEnv("Production"),
            () => throw new InvalidOperationException("no site config"), () => true, odd.Time, odd.Log);
        await oddRuntime.RefreshAsync();
        AssertOff(oddRuntime.Current, LiveOff.Unavailable);
    }

    [TestMethod]
    public async Task Origins_BuiltInsPlusExtras_AndLocalhostOnlyInDevelopment()
    {
        using var prod = new Rig("Production");
        await prod.SaveAsync(Good(extra: ["https://a.example/", "http://b.example:81"]));
        var s = await prod.RefreshAsync();
        CollectionAssert.AreEqual(LiveCors.BuiltIns.Concat(["https://a.example", "http://b.example:81"]).ToList(), s.Origins.ToList());
        CollectionAssert.DoesNotContain(s.Origins.ToList(), LiveCors.Development[0]);

        // The extras come with an off snapshot read from the settings too (module_closed reads them as well, see
        // ModuleClosed_KeepsTheSettingsOrigins_NotInstalledAndMultiTenantDoNot), but not before the settings are read.
        await prod.SaveAsync(Good(extra: ["https://a.example"]) with { Enabled = false });
        CollectionAssert.AreEqual(LiveCors.BuiltIns.Append("https://a.example").ToList(), (await prod.RefreshAsync()).Origins.ToList());
        prod.Module.Status = ModuleStatus.NotInstalled;
        CollectionAssert.AreEqual(LiveCors.BuiltIns.ToList(), (await prod.RefreshAsync()).Origins.ToList());

        using var dev = new Rig("Development");
        CollectionAssert.AreEqual(LiveCors.BuiltIns.Concat(LiveCors.Development).ToList(), dev.Runtime.Current.Origins.ToList(),
            "localhost from the start in Development");
        await dev.SaveAsync(Good(extra: ["https://a.example"]));
        CollectionAssert.AreEqual(LiveCors.BuiltIns.Concat(LiveCors.Development).Append("https://a.example").ToList(),
            (await dev.RefreshAsync()).Origins.ToList());

        // A row edited by hand: an origin that isn't one (a "*" would open the API to every site) is dropped, a repeat kept once.
        using (var ctx = prod.Db.NewContext())
        {
            var row = ctx.Settings.Single();
            row.Enabled = true;
            row.ExtraOrigins = "*\nhttps://a.example/x\nhttps://leozheng265.github.io\nhttps://c.example";
            await ctx.SaveChangesAsync();
        }
        prod.Module.Status = ModuleStatus.Open;
        s = await prod.RefreshAsync();
        Assert.IsTrue(s.On);
        CollectionAssert.AreEqual(LiveCors.BuiltIns.Append("https://c.example").ToList(), s.Origins.ToList());
    }

    /// <summary>
    /// Ruling 11: a closed module is installed, so its settings can be read; its snapshot keeps their extra origins (and
    /// limits), so a demo on an extra origin can still read health and show live as off. Not installed (the table may not
    /// exist) and multi-tenant (reads are tenant-filtered) keep the built-ins only. A closed module whose settings can't
    /// be read is still module_closed, on the built-ins, with no warning.
    /// </summary>
    [TestMethod]
    public async Task ModuleClosed_KeepsTheSettingsOrigins_NotInstalledAndMultiTenantDoNot()
    {
        using var rig = new Rig();
        await rig.SaveAsync(Good(extra: ["https://a.example"]) with { PerCodeDaily = 4 });
        rig.Module.Status = ModuleStatus.Closed;
        var s = await rig.RefreshAsync();
        AssertOff(s, LiveOff.ModuleClosed);
        CollectionAssert.AreEqual(LiveCors.BuiltIns.Append("https://a.example").ToList(), s.Origins.ToList());
        Assert.AreEqual(4, s.Limits.PerCodeDaily);

        rig.Module.Status = ModuleStatus.NotInstalled;
        s = await rig.RefreshAsync();
        AssertOff(s, LiveOff.NotInstalled);
        CollectionAssert.AreEqual(LiveCors.BuiltIns.ToList(), s.Origins.ToList());

        rig.Module.Status = ModuleStatus.Closed;
        rig.MultiTenant = true;
        s = await rig.RefreshAsync();
        AssertOff(s, LiveOff.MultiTenant);
        CollectionAssert.AreEqual(LiveCors.BuiltIns.ToList(), s.Origins.ToList());

        // Development: localhost as well as the extras.
        using var dev = new Rig("Development");
        await dev.SaveAsync(Good(extra: ["https://a.example"]));
        dev.Module.Status = ModuleStatus.Closed;
        CollectionAssert.AreEqual(LiveCors.BuiltIns.Concat(LiveCors.Development).Append("https://a.example").ToList(),
            (await dev.RefreshAsync()).Origins.ToList());

        // The settings can't be read (no table): still module_closed, not unavailable, on the built-ins and default limits.
        using var bare = new Rig(createTables: false);
        bare.Module.Status = ModuleStatus.Closed;
        s = await bare.RefreshAsync();
        AssertOff(s, LiveOff.ModuleClosed);
        CollectionAssert.AreEqual(LiveCors.BuiltIns.ToList(), s.Origins.ToList());
        Assert.AreEqual(LiveLimits.Defaults, s.Limits);
        Assert.AreEqual(0, bare.Warnings);
        Assert.AreEqual(0, bare.Errors);
    }

    // ---- the module's registrations ----

    /// <summary>A service collection with what Register.AddXncfModule adds, and the fakes registered first (it keeps them).</summary>
    static ServiceCollection ModuleServices(IModuleState? module, IAiModelSource? models, Dictionary<string, string?>? config = null,
        TestDb? db = null)
    {
        var services = new ServiceCollection();
        if (module is not null) services.AddSingleton(module);
        if (models is not null) services.AddSingleton(models);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(config ?? new()).Build();
        new Register().AddXncfModule(services, cfg, new FakeEnv("Production"));
        // The NCF host picks the context by its database type; here it is the test's SQLite one.
        if (db is not null) services.AddScoped<NeuCharBoxLiveSenparcEntities>(_ => db.NewContext());
        return services;
    }

    [TestMethod]
    public async Task Register_ResolvesTheModuleRuntime_AndAiKernelChatClients()
    {
        var module = new FakeModuleState { Status = ModuleStatus.NotInstalled };
        await using var sp = ModuleServices(module, new FakeModels()).BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        var live = sp.GetRequiredService<ILiveRuntime>();
        var concrete = sp.GetRequiredService<ModuleLiveRuntime>();
        Assert.AreSame(concrete, live, "no options problems: the module runtime itself");
        Assert.AreEqual(LiveOff.Starting, live.Current.OffReason);
        Assert.AreEqual(0, module.Reads, "resolving it reads nothing");
        Assert.IsNotNull(sp.GetRequiredService<AiKernelChatClients>());
        Assert.AreSame(sp.GetRequiredService<AiKernelChatClients>(), sp.GetRequiredService<AiKernelChatClients>());
        Assert.IsTrue(sp.GetServices<IHostedService>().Any(x => ReferenceEquals(x, concrete)), "the runtime refreshes as a hosted service");

        await concrete.RefreshAsync();
        Assert.AreEqual(LiveOff.NotInstalled, live.Current.OffReason);

        // Without fakes, NCF's own: the module table and AI Kernel's models.
        await using var ncf = ModuleServices(null, null).BuildServiceProvider();
        Assert.IsInstanceOfType<XncfModuleState>(ncf.GetRequiredService<IModuleState>());
        Assert.IsInstanceOfType<AiKernelModelSource>(ncf.GetRequiredService<IAiModelSource>());
    }

    [TestMethod]
    public async Task ConfigProblems_LiveRuntimeIsOffConfig_ThoughModuleRuntimeIsOn()
    {
        using var db = new TestDb();
        var models = new FakeModels();
        models.Models[7] = Chat(7);
        await using var sp = ModuleServices(new FakeModuleState(), models, new() { ["NcbLive:SessionIdle"] = "00:05:00" }, db)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var saved = Good(extra: ["https://a.example"]) with { PerCodeDaily = 9 };
        Assert.AreEqual(0, (await sp.GetRequiredService<LiveSettingsService>().SaveAsync(saved)).Count);

        var module = sp.GetRequiredService<ModuleLiveRuntime>();
        await module.RefreshAsync();
        Assert.IsTrue(module.Current.On, "the module runtime itself is on");

        var live = sp.GetRequiredService<ILiveRuntime>();
        Assert.AreNotSame(module, live);
        AssertOff(live.Current, LiveOff.Config);
        Assert.AreEqual(saved.Limits, live.Current.Limits, "the wrapper keeps the runtime's limits");
        CollectionAssert.AreEqual(module.Current.Origins.ToList(), live.Current.Origins.ToList(), "and its origins");
    }

    /// <summary>
    /// Final review F1: a value the binder can't convert ("30m" for a TimeSpan, "sixty" for an int). NCF swallows an
    /// exception from AddXncfModule, so throwing there would skip the module's DbContext registration and put the whole site
    /// in maintenance mode. Instead the module registers as usual, its DbContext included, the host starts, health answers
    /// liveEnabled:false, live is off with the config reason, and one error names the key.
    /// </summary>
    [DataTestMethod]
    [DataRow("NcbLive:MaxWork", "30m")]
    [DataRow("NcbLive:MaxCalls", "sixty")]
    public async Task MalformedOption_ModuleStillRegisters_HostStarts_LiveOffConfig(string key, string value)
    {
        var config = new Dictionary<string, string?> { [key] = value };
        var services = ModuleServices(new FakeModuleState(), new FakeModels(), config);
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(NeuCharBoxLiveSenparcEntities)), "the module's DbContext is registered");

        var logs = new ListLoggerProvider();
        using var host = new HostBuilder()
            .ConfigureLogging(l => l.ClearProviders().AddProvider(logs))
            .ConfigureWebHost(w => w
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IModuleState>(new FakeModuleState());
                    s.AddSingleton<IAiModelSource>(new FakeModels());
                    s.AddRouting();
                    new Register().AddXncfModule(s, new ConfigurationBuilder().AddInMemoryCollection(config).Build(), new FakeEnv("Production"));
                })
                .Configure(app =>
                {
                    new Register().UseXncfModule(app, registerService: null!);
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapControllers());
                }))
            .Build();
        await host.StartAsync();
        try
        {
            using var client = host.GetTestClient();
            var health = await client.GetFromJsonAsync<JsonObject>("/live/api/health");
            Assert.AreEqual(true, (bool?)health!["ok"]);
            Assert.AreEqual(false, (bool?)health["liveEnabled"], health.ToJsonString());
            AssertOff(host.Services.GetRequiredService<ILiveRuntime>().Current, LiveOff.Config);
            var opt = host.Services.GetRequiredService<LiveOptions>();
            Assert.AreEqual(new LiveOptions().MaxWork, opt.MaxWork, "the defaults, not a half-bound section");
            Assert.AreEqual(new LiveOptions().MaxCalls, opt.MaxCalls);
            lock (logs.Entries)
            {
                var errors = logs.Entries.Where(e => e.Level >= LogLevel.Error).ToList();
                Assert.AreEqual(1, errors.Count, string.Join("\n", logs.Entries));
                StringAssert.Contains(errors[0].Message, key, "the binder's message names the key");
            }
        }
        finally { await host.StopAsync(); }
    }

    [DataTestMethod]
    [DataRow(null, ModuleStatus.NotInstalled)]
    [DataRow(XncfModules_State.开放, ModuleStatus.Open)]
    [DataRow(XncfModules_State.关闭, ModuleStatus.Closed)]
    [DataRow(XncfModules_State.新增待审核, ModuleStatus.Closed)]
    [DataRow(XncfModules_State.更新待审核, ModuleStatus.Closed)]
    public void StatusOf_MapsNcfModuleStates(XncfModules_State? state, ModuleStatus expected) =>
        Assert.AreEqual(expected, XncfModuleState.StatusOf(state));
}
