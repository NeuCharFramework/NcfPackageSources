/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ModuleLiveRuntime.cs
    文件功能描述：NeuCharBox Live 领域服务：模块运行时（模块已安装且开放、未启用多租户、设置已启用且选了模型时才开启 Live）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增 ModuleLiveRuntime：每 5 秒及设置保存后刷新快照，替代临时的 StaticLiveRuntime

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
/// runs. Resolve it as <see cref="ILiveRuntime"/>, never as this type: AddNcbLive wraps it when the options have problems.
/// <para>
/// The off reason is the first that applies: <see cref="LiveOff.MultiTenant"/>, <see cref="LiveOff.NotInstalled"/>,
/// <see cref="LiveOff.ModuleClosed"/> (none of these reads the module's tables, which may not exist yet), then from the
/// settings <see cref="LiveOff.Disabled"/>, <see cref="LiveOff.NoModel"/> (no plan model), <see cref="LiveOff.ModelMissing"/>
/// and <see cref="LiveOff.ModelUnsupported"/> (either model). A run model of null is the plan model. A snapshot read from
/// the settings has their limits and extra origins, on or off.
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
    readonly TimeProvider _tp;
    readonly ILogger<ModuleLiveRuntime> _log;

    /// <summary><see cref="LiveCors.BuiltIns"/>, plus <see cref="LiveCors.Development"/> in Development.</summary>
    readonly IReadOnlyList<string> _baseOrigins;

    /// <summary>One refresh at a time, so an older read never overwrites a newer one.</summary>
    readonly SemaphoreSlim _gate = new(1, 1);

    readonly CancellationTokenSource _stop = new();
    volatile LiveSnapshot _current;
    Task? _loop;
    DateTimeOffset? _warnedAt;

    /// <summary>Reads nothing and never throws (the CORS options resolve the runtime at startup): it starts off, <see cref="LiveOff.Starting"/>.</summary>
    /// <param name="multiTenant">Whether NCF runs multi-tenant (SiteConfig.SenparcCoreSetting.EnableMultiTenant); read on every refresh.</param>
    public ModuleLiveRuntime(LiveSettingsService settings, AiKernelChatClients clients, IModuleState module, IAiModelSource models,
        IHostEnvironment env, Func<bool> multiTenant, TimeProvider tp, ILogger<ModuleLiveRuntime> log)
    {
        _settings = settings;
        _clients = clients;
        _module = module;
        _models = models;
        _multiTenant = multiTenant;
        _tp = tp;
        _log = log;
        _baseOrigins = env?.IsDevelopment() == true ? LiveCors.BuiltIns.Concat(LiveCors.Development).ToArray() : LiveCors.BuiltIns;
        _current = LiveSnapshot.Off(LiveOff.Starting, _baseOrigins);
    }

    public LiveSnapshot Current => _current;

    /// <summary>
    /// Rebuilds <see cref="Current"/>. Never throws for a failed read (that is <see cref="LiveOff.Unavailable"/>); only an
    /// OperationCanceledException when <paramref name="ct"/> is cancelled, and then Current stays as it was.
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
            case ModuleStatus.Closed: return LiveSnapshot.Off(LiveOff.ModuleClosed, _baseOrigins);
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

    /// <summary>Starts refreshing in the background (the first right away) and on every settings save; it doesn't wait for the first.</summary>
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
            do await RefreshAsync(ct).ConfigureAwait(false);
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    /// <summary>Off the saving thread: Changed is raised inside SaveAsync, which must not wait for (or fail with) the refresh.</summary>
    void OnSettingsChanged() => _ = Task.Run(async () =>
    {
        try { await RefreshAsync(_stop.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
    }, CancellationToken.None);
}
