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

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 NcbLive 配置段无法绑定（如 MaxWork 为 "30m"）时用默认值并关闭 Live（config），不再抛出；旧版独立宿主的键只记一条警告；不再注册任何 CORS 服务（改用 AddMvcCore）；多租户、未安装、已关闭时保留运行时自己的关闭原因

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
    /// Keys of the standalone pilot host's NcbLive section that the module no longer reads: the switch, the models, the
    /// daily limits and the extra origins are on the Settings page, the endpoint (and its key) on the AI Kernel model.
    /// </summary>
    public static readonly IReadOnlyList<string> MovedKeys =
        ["Enabled", "PlanModel", "RunModel", "Endpoint", "PerCodeDaily", "GlobalDaily", "DailyTokenBudget", "ExtraOrigins"];

    /// <summary>
    /// The live server: LiveOptions from section NcbLive (one plain singleton instance), the runtime
    /// (<see cref="NcbLiveBuilder.UseRuntime{TRuntime}"/>) behind ILiveRuntime, the UsageDay of NcbLive:UsageTimeZone,
    /// the stores, access, sessions, a runner per request, the session sweep and LiveApiController (MVC core only; no
    /// CORS service of any kind: <see cref="LiveCorsMiddleware"/> has its own). It never throws for the options, so the
    /// module's registration (its DbContexts included) always completes: a section the binder can't read (a value it can't
    /// convert, such as "30m" for a TimeSpan) gives the defaults, and that, or <see cref="LiveOptions.Problems"/>, is
    /// logged as an error (with the binder's message, which names the key, or by property name) when the runtime is first
    /// resolved, at the latest by the session sweep at startup; every snapshot the engine reads is then off with
    /// <see cref="LiveOff.Config"/>. Any <see cref="MovedKeys"/> still present are named in one warning at the same time.
    /// The HTTP side (CORS, the JSON 404 and 405) is <see cref="LiveApiPipeline.Use"/>, which the module adds from its UseXncfModule.
    /// </summary>
    public static IServiceCollection AddNcbLive(this IServiceCollection s, IConfiguration cfg, Action<NcbLiveBuilder>? configure = null)
    {
        LiveOptions opt;
        string? unreadable = null;
        try
        {
            opt = cfg.GetSection(LiveOptions.Section).Get<LiveOptions>() ?? new LiveOptions();
        }
        catch (Exception e)
        {
            // Never throw from here: NCF swallows an exception from AddXncfModule, and the module's registration (its
            // DbContexts too) would stop halfway.
            opt = new LiveOptions();
            unreadable = e.Message;
        }
        var problems = opt.Problems();
        var moved = MovedKeys.Where(k => cfg.GetSection(LiveOptions.Section + ":" + k).Exists()).ToList();
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
            if (moved.Count == 0 && unreadable is null && problems.Count == 0) return runtime;
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(NcbLiveServiceCollectionExtensions).FullName!);
            // Names only, never values (Endpoint could carry a key).
            if (moved.Count > 0)
                log.LogWarning("NeuCharBox Live ignores these {Section} settings: {Keys}. They moved to the module's Settings page (switch, " +
                    "models, daily limits, extra origins) and to the AI Kernel model (endpoint and key); remove them from the configuration.",
                    LiveOptions.Section, string.Join(", ", moved));
            if (unreadable is not null)
                log.LogError("NeuCharBox Live is off: the {Section} section can't be read ({Error}), so every setting in it is at its " +
                    "default. Fix it and restart.", LiveOptions.Section, unreadable);
            else if (problems.Count > 0)
                log.LogError("NeuCharBox Live is off: these {Section} settings can't work (as they are, or together): {Problems}. Fix them and restart.",
                    LiveOptions.Section, string.Join(", ", problems));
            return unreadable is null && problems.Count == 0 ? runtime : new ConfigOffRuntime(runtime);
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
        // MVC core is all LiveApiController needs (it writes its own JSON). Not AddControllers: that also adds the CORS
        // services (and API explorer, data annotations) to a host that may not have them. An NCF host has full MVC anyway.
        s.AddMvcCore().AddApplicationPart(typeof(LiveApiController).Assembly);
        return s;
    }

    /// <summary>No runtime given: live is off, no_model, on the built-in origins.</summary>
    static (Action<IServiceCollection>, Func<IServiceProvider, ILiveRuntime>) DefaultRuntime()
    {
        var off = new StaticLiveRuntime(LiveSnapshot.Off(LiveOff.NoModel, LiveCors.BuiltIns));
        return (_ => { }, _ => off);
    }

    /// <summary>
    /// Options with problems: every snapshot is off (config), with the runtime's own limits and origins. A runtime that says
    /// the module isn't serving at all (<see cref="LiveOff.NotServing"/>) keeps its own reason, which says more and keeps
    /// apply and access/check shut (Ruling 13).
    /// </summary>
    sealed class ConfigOffRuntime(ILiveRuntime inner) : ILiveRuntime
    {
        public LiveSnapshot Current
        {
            get
            {
                var s = inner.Current;
                return s with { Plan = null, Run = null, OffReason = LiveOff.NotServing(s.OffReason) ? s.OffReason : LiveOff.Config };
            }
        }
    }

    internal static void Forward<TStore>(IServiceCollection s) where TStore : class, IInviteStore, IApplicationStore, IRunStore
    {
        s.AddSingleton<IInviteStore>(sp => sp.GetRequiredService<TStore>());
        s.AddSingleton<IApplicationStore>(sp => sp.GetRequiredService<TStore>());
        s.AddSingleton<IRunStore>(sp => sp.GetRequiredService<TStore>());
    }
}
