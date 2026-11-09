/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ModuleLiveRuntime.cs
    文件功能描述：模块运行时（模块已安装且开放、未启用多租户、设置已启用且选了模型时才开启 Live）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增 ModuleLiveRuntime：每 5 秒及设置保存后刷新快照，替代临时的 StaticLiveRuntime

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 模块已关闭（module_closed）时尽力读取设置，保留其额外来源与限额；读不到时用内置来源

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 NCF 报告就绪（XNCF 线程已载入）之前，后台刷新与保存后的刷新都不读数据库，快照保持 starting

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 显式刷新读库成功即视为就绪：本次运行中新安装的站点打开设置页后恢复每 5 秒轮询，无需重启

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>
/// The module's runtime: live is on only when multi-tenancy is off, the module is installed and open in NCF, the settings
/// enable it and choose a plan model, and the chosen AI Kernel models can be called. The snapshot is rebuilt every
/// <see cref="Period"/> and after every settings save (<see cref="LiveSettingsService.Changed"/>) while the hosted service
/// runs, but only once NCF reports the site ready (Ruling 14): until then it reads nothing and stays
/// <see cref="LiveOff.Starting"/>. Whatever serves visitors resolves it as <see cref="ILiveRuntime"/> (AddNcbLive wraps it
/// when the options have problems); only the module's own wiring takes this type: the hosted service, RetentionService
/// (which needs the real state, not the wrapper's) and the Settings page's refresh after a save.
/// <para>
/// The off reason is the first that applies: <see cref="LiveOff.MultiTenant"/>, <see cref="LiveOff.NotInstalled"/> (neither
/// reads the module's tables, which may not exist yet or be tenant-filtered: built-in origins only),
/// <see cref="LiveOff.ModuleClosed"/> (installed, so it reads the settings for their limits and extra origins, best effort:
/// the built-ins when they can't be read), then from the settings <see cref="LiveOff.Disabled"/>, <see cref="LiveOff.NoModel"/>
/// (no plan model), <see cref="LiveOff.ModelMissing"/> and <see cref="LiveOff.ModelUnsupported"/> (either model). A run
/// model of null is the plan model. A snapshot read from the settings has their limits and extra origins, on or off.
/// </para>
/// <para>
/// A refresh that throws (the database is down, the settings table isn't there) turns live off with
/// <see cref="LiveOff.Unavailable"/>, keeping the last limits and origins so the demo can still read health, and logs a
/// warning at most once per <see cref="WarnEvery"/>. Nothing here is an error: a module that isn't installed is a state.
/// </para>
/// </summary>
public sealed class ModuleLiveRuntime : ILiveRuntime, IHostedService
{
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan WarnEvery = TimeSpan.FromMinutes(1);

    static readonly EventId RefreshFailed = new(1401, "LiveRuntimeRefreshFailed");
    static readonly EventId StateChanged = new(1402, "LiveRuntimeStateChanged");

    readonly LiveSettingsService _settings;
    readonly AiKernelChatClients _clients;
    readonly IModuleState _module;
    readonly IAiModelSource _models;
    readonly Func<bool> _multiTenant;
    readonly Func<bool> _ncfReady;
    readonly TimeProvider _tp;
    readonly ILogger<ModuleLiveRuntime> _log;

    /// <summary>Set once <see cref="_ncfReady"/> has said true: NCF never goes back to not ready.</summary>
    volatile bool _ready;

    /// <summary><see cref="LiveCors.BuiltIns"/>, plus <see cref="LiveCors.Development"/> in Development.</summary>
    readonly IReadOnlyList<string> _baseOrigins;

    /// <summary>One refresh at a time, so an older read never overwrites a newer one.</summary>
    readonly SemaphoreSlim _gate = new(1, 1);

    readonly CancellationTokenSource _stop = new();
    volatile LiveSnapshot _current;
    Task? _loop;
    DateTimeOffset? _warnedAt;

    /// <summary>Reads nothing and never throws (the session sweep resolves the runtime at startup): it starts off, <see cref="LiveOff.Starting"/>.</summary>
    /// <param name="multiTenant">Whether NCF runs multi-tenant (SiteConfig.SenparcCoreSetting.EnableMultiTenant); read on every refresh.</param>
    /// <param name="ncfReady">
    /// Whether NCF reports the site ready (SiteConfig.NcfCoreState.AllThreadXncfLoaded: UseNcf, or StartXncfThreads once the
    /// host found the database Ready; false for good on a site in maintenance or not yet updated). The hosted service reads
    /// nothing until it is true; one that throws counts as false.
    /// </param>
    public ModuleLiveRuntime(LiveSettingsService settings, AiKernelChatClients clients, IModuleState module, IAiModelSource models,
        IHostEnvironment env, Func<bool> multiTenant, Func<bool> ncfReady, TimeProvider tp, ILogger<ModuleLiveRuntime> log)
    {
        _settings = settings;
        _clients = clients;
        _module = module;
        _models = models;
        _multiTenant = multiTenant;
        _ncfReady = ncfReady;
        _tp = tp;
        _log = log;
        _baseOrigins = env?.IsDevelopment() == true ? LiveCors.BuiltIns.Concat(LiveCors.Development).ToArray() : LiveCors.BuiltIns;
        _current = LiveSnapshot.Off(LiveOff.Starting, _baseOrigins);
    }

    public LiveSnapshot Current => _current;

    /// <summary>
    /// Rebuilds <see cref="Current"/> now. Never throws for a failed read (that is <see cref="LiveOff.Unavailable"/>); only an
    /// OperationCanceledException when <paramref name="ct"/> is cancelled, and then Current stays as it was. It doesn't wait
    /// for NCF's readiness (the hosted service does): call it when the database is known to be there, as the Settings page
    /// does right after a save.
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            LiveSnapshot next;
            try
            {
                next = await BuildAsync(ct).ConfigureAwait(false);
                // A read that worked proves the database is there. On a site installed in this run NCF never reports
                // ready until a restart, so an explicit refresh that succeeds (the Settings page's) lets the loop poll.
                _ready = true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                WarnFailed(e);
                next = _current with { Plan = null, Run = null, OffReason = LiveOff.Unavailable };
            }
            Set(next);
        }
        finally { _gate.Release(); }
    }

    async Task<LiveSnapshot> BuildAsync(CancellationToken ct)
    {
        if (_multiTenant()) return LiveSnapshot.Off(LiveOff.MultiTenant, _baseOrigins);
        switch (await _module.GetAsync(ct).ConfigureAwait(false))
        {
            case ModuleStatus.NotInstalled: return LiveSnapshot.Off(LiveOff.NotInstalled, _baseOrigins);
            case ModuleStatus.Closed: return await ClosedAsync(ct).ConfigureAwait(false);
        }

        var s = await _settings.GetAsync(ct).ConfigureAwait(false);
        var origins = OriginsOf(s);
        LiveSnapshot Off(string reason) => new(s.Limits, null, null, origins, reason);

        if (!s.Enabled) return Off(LiveOff.Disabled);
        if (s.PlanModelId is not { } planId) return Off(LiveOff.NoModel);
        var plan = _clients.Resolve(await _models.GetAsync(planId, ct).ConfigureAwait(false));
        var run = s.RunModelId is { } runId && runId != planId
            ? _clients.Resolve(await _models.GetAsync(runId, ct).ConfigureAwait(false))
            : plan;
        // model_missing comes before model_unsupported, whichever of the two models it is.
        var reason = plan.OffReason == LiveOff.ModelMissing || run.OffReason == LiveOff.ModelMissing
            ? LiveOff.ModelMissing
            : plan.OffReason ?? run.OffReason;
        return reason is null ? new LiveSnapshot(s.Limits, plan.Model, run.Model, origins, null) : Off(reason);
    }

    /// <summary>
    /// Off with <see cref="LiveOff.ModuleClosed"/>, with the settings' limits and extra origins, so a demo on an extra origin
    /// can still read health and show live as off. Best effort: settings that can't be read give the defaults and the
    /// base origins, with no warning (closing the module is a state, not a failure).
    /// </summary>
    async Task<LiveSnapshot> ClosedAsync(CancellationToken ct)
    {
        try
        {
            var s = await _settings.GetAsync(ct).ConfigureAwait(false);
            return new LiveSnapshot(s.Limits, null, null, OriginsOf(s), LiveOff.ModuleClosed);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return LiveSnapshot.Off(LiveOff.ModuleClosed, _baseOrigins);
        }
    }

    /// <summary>
    /// The base origins and the settings' extra ones, each once. The service stores extras normalized; one that still isn't
    /// an origin (a row edited by hand, a "*" that would open the API to every site) is dropped.
    /// </summary>
    IReadOnlyList<string> OriginsOf(LiveSettings s)
    {
        var extras = (s.ExtraOrigins ?? []).Select(o => LiveCors.TryNormalize(o, out var n) ? n : null).OfType<string>();
        return _baseOrigins.Concat(extras).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>Replaces the snapshot and, when live turned on or off or changed models, says so (information, not a warning).</summary>
    void Set(LiveSnapshot next)
    {
        var before = _current;
        _current = next;
        if (before.OffReason == next.OffReason && before.Plan?.Name == next.Plan?.Name && before.Run?.Name == next.Run?.Name) return;
        if (next.On)
            _log.LogInformation(StateChanged, "NeuCharBox Live is on: plan model {PlanModel}, run model {RunModel}", next.Plan!.Name, next.Run!.Name);
        else
            _log.LogInformation(StateChanged, "NeuCharBox Live is off: {OffReason}", next.OffReason);
    }

    /// <summary>At most one warning per <see cref="WarnEvery"/>: type and message only (the call runs under <see cref="_gate"/>).</summary>
    void WarnFailed(Exception e)
    {
        var now = _tp.GetUtcNow();
        if (_warnedAt is { } at && now - at < WarnEvery) return;
        _warnedAt = now;
        _log.LogWarning(RefreshFailed, "NeuCharBox Live is off (unavailable): its state can't be read: {ExceptionType}: {Message}",
            e.GetType().FullName, e.Message);
    }

    // ---- the hosted service ----

    /// <summary>
    /// Starts refreshing in the background (the first right away when NCF is ready, else at the first tick after it is) and
    /// on every settings save; it doesn't wait for the first.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _settings.Changed += OnSettingsChanged;
        _loop = Task.Run(() => LoopAsync(_stop.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _settings.Changed -= OnSettingsChanged;
        _stop.Cancel();
        if (_loop is null) return;
        try { await _loop.WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
    }

    async Task LoopAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(Period, _tp);
            do
            {
                // Until NCF is ready the database may be missing, in maintenance or on an old schema: every read would
                // fail (and EF log an error) every few seconds. Stay starting and ask again at the next tick.
                if (NcfReady()) await RefreshAsync(ct).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    /// <summary>NCF's readiness, kept once true. A check that throws is not ready.</summary>
    bool NcfReady()
    {
        if (_ready) return true;
        try { return _ready = _ncfReady(); }
        catch (Exception) { return false; }
    }

    /// <summary>
    /// Off the saving thread: Changed is raised inside SaveAsync, which must not wait for (or fail with) the refresh. Before
    /// NCF is ready a save reads nothing more either; the loop's first refresh after readiness picks it up.
    /// </summary>
    void OnSettingsChanged()
    {
        if (!NcfReady()) return;
        _ = Task.Run(async () =>
        {
            try { await RefreshAsync(_stop.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }, CancellationToken.None);
    }
}
