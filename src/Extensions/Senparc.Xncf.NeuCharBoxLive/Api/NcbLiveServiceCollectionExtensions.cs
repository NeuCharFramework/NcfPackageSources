/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcbLiveServiceCollectionExtensions.cs
    文件功能描述：AddNcbLive 服务注册扩展，及可替换存储、运行时、节奏器的构建器


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增 UseRuntime(factory)；配置有问题时的关闭快照保留运行时的限额与来源

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 移除 UseNcbLiveGuard（隧道守卫）与 UseNcbLiveApiFallback，改由 LiveApiPipeline 接入；AddCors 不再注册具名策略

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>What AddNcbLive plugs in. Every method returns the builder.</summary>
public sealed class NcbLiveBuilder
{
    internal Action<IServiceCollection>? Stores { get; private set; }
    internal (Action<IServiceCollection> Register, Func<IServiceProvider, ILiveRuntime> Resolve)? Runtime { get; private set; }
    internal IPacer? Pacer { get; private set; }
    internal TimeProvider? Time { get; private set; }

    /// <summary>
    /// One <typeparamref name="TStore"/> singleton, built by the container (so it can take IServiceScopeFactory),
    /// behind IInviteStore, IApplicationStore and IRunStore. The default is InMemoryLiveStore.
    /// </summary>
    public NcbLiveBuilder UseStores<TStore>() where TStore : class, IInviteStore, IApplicationStore, IRunStore
    {
        Stores = s =>
        {
            s.AddSingleton<TStore>();
            NcbLiveServiceCollectionExtensions.Forward<TStore>(s);
        };
        return this;
    }

    /// <summary>
    /// The runtime the engine reads its snapshot from (on/off, models, limits, origins): one <typeparamref name="TRuntime"/>
    /// singleton, built by the container. Without a runtime, live is off (no_model) on <see cref="LiveCors.BuiltIns"/>.
    /// The concrete type is registered too (for the host's own wiring, such as a hosted service), but it bypasses the
    /// config-off wrapper: everything else resolves <see cref="ILiveRuntime"/>.
    /// </summary>
    public NcbLiveBuilder UseRuntime<TRuntime>() where TRuntime : class, ILiveRuntime
    {
        Runtime = (s => s.AddSingleton<TRuntime>(), sp => sp.GetRequiredService<TRuntime>());
        return this;
    }

    /// <summary>
    /// The same, with the <typeparamref name="TRuntime"/> singleton built by <paramref name="factory"/>: for a runtime whose
    /// constructor takes something the container shouldn't hold as a service (the module's multi-tenancy check, a Func&lt;bool&gt;).
    /// </summary>
    public NcbLiveBuilder UseRuntime<TRuntime>(Func<IServiceProvider, TRuntime> factory) where TRuntime : class, ILiveRuntime
    {
        ArgumentNullException.ThrowIfNull(factory);
        Runtime = (s => s.AddSingleton(factory), sp => sp.GetRequiredService<TRuntime>());
        return this;
    }

    /// <summary>The same, with a runtime already built (a host's fixed snapshot, or a test's <see cref="StaticLiveRuntime"/>).</summary>
    public NcbLiveBuilder UseRuntime(ILiveRuntime instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        Runtime = (_ => { }, _ => instance);
        return this;
    }

    public NcbLiveBuilder UsePacer(IPacer pacer)
    {
        Pacer = pacer;
        return this;
    }

    public NcbLiveBuilder UseTimeProvider(TimeProvider tp)
    {
        Time = tp;
        return this;
    }
}

public static class NcbLiveServiceCollectionExtensions
{
    /// <summary>
    /// The live server: LiveOptions from section NcbLive (one plain singleton instance), the runtime
    /// (<see cref="NcbLiveBuilder.UseRuntime{TRuntime}"/>) behind ILiveRuntime, the UsageDay of NcbLive:UsageTimeZone,
    /// the stores, access, sessions, a runner per request, the session sweep, LiveApiController and the CORS services. It
    /// never throws for the options: when <see cref="LiveOptions.Problems"/> names any, they are logged (as an error, by
    /// property name, when the runtime is first resolved, at the latest by the session sweep at startup) and every snapshot
    /// the engine reads is off with <see cref="LiveOff.Config"/>. The HTTP side (CORS, the JSON 404 and 405) is
    /// <see cref="LiveApiPipeline.Use"/>, which the module adds from its UseXncfModule.
    /// </summary>
    public static IServiceCollection AddNcbLive(this IServiceCollection s, IConfiguration cfg, Action<NcbLiveBuilder>? configure = null)
    {
        var opt = cfg.GetSection(LiveOptions.Section).Get<LiveOptions>() ?? new LiveOptions();
        var problems = opt.Problems();
        UsageDay.TryFromId(opt.UsageTimeZone, out var usageDay); // UTC when unknown (then also a problem)
        var b = new NcbLiveBuilder();
        configure?.Invoke(b);

        s.AddLogging();
        s.AddSingleton(opt);
        s.AddSingleton(usageDay);
        var (registerRuntime, resolveRuntime) = b.Runtime ?? DefaultRuntime();
        registerRuntime(s);
        s.AddSingleton<ILiveRuntime>(sp =>
        {
            var runtime = resolveRuntime(sp);
            if (problems.Count == 0) return runtime;
            sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(NcbLiveServiceCollectionExtensions).FullName!).LogError(
                "NeuCharBox Live is off: these {Section} settings can't work (as they are, or together): {Problems}. Fix them and restart.",
                LiveOptions.Section, string.Join(", ", problems));
            return new ConfigOffRuntime(runtime);
        });
        // Not TryAddSingleton: in this repo Senparc.Ncf.Utility defines a type Microsoft.Extensions.DependencyInjection.Extensions, which
        // clashes with the namespace the TryAdd* extensions live in (CS0434).
        if (b.Time is { } tp) s.AddSingleton<TimeProvider>(tp);
        else if (!s.Any(d => d.ServiceType == typeof(TimeProvider))) s.AddSingleton(TimeProvider.System);
        if (b.Pacer is { } pacer) s.AddSingleton<IPacer>(pacer);
        else if (!s.Any(d => d.ServiceType == typeof(IPacer))) s.AddSingleton<IPacer, RealPacer>();

        (b.Stores ?? (x =>
        {
            x.AddSingleton(sp => new InMemoryLiveStore(sp.GetRequiredService<TimeProvider>()));
            Forward<InMemoryLiveStore>(x);
        }))(s);

        s.AddSingleton<ITokenMeter, TokenMeter>();
        s.AddSingleton<AccessService>();
        s.AddSingleton<ApplyRateLimiter>();
        s.AddSingleton<AccessCheckRateLimiter>();
        s.AddSingleton<EditRateLimiter>();
        s.AddSingleton<SessionManager>();

        // A request takes its models from its snapshot (SessionManager refuses an off one with live_off first).
        s.AddSingleton<Func<RequestRunner>>(sp => () => new RequestRunner(sp.GetRequiredService<LiveOptions>(),
            sp.GetRequiredService<IPacer>(), sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<IRunStore>(),
            sp.GetRequiredService<ITokenMeter>(), sp.GetRequiredService<UsageDay>(), sp.GetRequiredService<ILogger<RequestRunner>>()));

        s.AddHostedService<SessionSweepService>();
        s.AddControllers().AddApplicationPart(typeof(LiveApiController).Assembly);
        // Only so ICorsService exists (AddCors adds nothing a host already has). No named policy and no policy provider:
        // either would touch the host's own CORS. LiveCorsMiddleware builds the policy per request from the snapshot.
        s.AddCors();
        return s;
    }

    /// <summary>No runtime given: live is off, no_model, on the built-in origins.</summary>
    static (Action<IServiceCollection>, Func<IServiceProvider, ILiveRuntime>) DefaultRuntime()
    {
        var off = new StaticLiveRuntime(LiveSnapshot.Off(LiveOff.NoModel, LiveCors.BuiltIns));
        return (_ => { }, _ => off);
    }

    /// <summary>Options with problems: every snapshot is off (config), with the runtime's own limits and origins.</summary>
    sealed class ConfigOffRuntime(ILiveRuntime inner) : ILiveRuntime
    {
        public LiveSnapshot Current => inner.Current with { Plan = null, Run = null, OffReason = LiveOff.Config };
    }

    internal static void Forward<TStore>(IServiceCollection s) where TStore : class, IInviteStore, IApplicationStore, IRunStore
    {
        s.AddSingleton<IInviteStore>(sp => sp.GetRequiredService<TStore>());
        s.AddSingleton<IApplicationStore>(sp => sp.GetRequiredService<TStore>());
        s.AddSingleton<IRunStore>(sp => sp.GetRequiredService<TStore>());
    }
}
