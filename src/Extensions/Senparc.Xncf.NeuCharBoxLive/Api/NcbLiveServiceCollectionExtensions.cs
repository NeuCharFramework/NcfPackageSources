/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcbLiveServiceCollectionExtensions.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：NcbLiveServiceCollectionExtensions


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
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
    /// </summary>
    public NcbLiveBuilder UseRuntime<TRuntime>() where TRuntime : class, ILiveRuntime
    {
        Runtime = (s => s.AddSingleton<TRuntime>(), sp => sp.GetRequiredService<TRuntime>());
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
    /// the stores, access, sessions, a runner per request, the session sweep, LiveApiController and the CORS policy
    /// <see cref="LiveCors.Policy"/>. It never throws for the options: when <see cref="LiveOptions.Problems"/> names any,
    /// they are logged (as an error, by property name, when the runtime is first resolved) and every snapshot the engine
    /// reads is off with <see cref="LiveOff.Config"/>.
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
        // The origins are the runtime's when the policy is first built (at startup); Task 6 makes them follow the snapshot.
        s.AddCors();
        s.AddOptions<CorsOptions>().Configure<ILiveRuntime>((o, runtime) =>
            o.AddPolicy(LiveCors.Policy, p => LiveCors.Build(p, runtime.Current.Origins)));
        return s;
    }

    /// <summary>No runtime given: live is off, no_model, on the built-in origins.</summary>
    static (Action<IServiceCollection>, Func<IServiceProvider, ILiveRuntime>) DefaultRuntime()
    {
        var off = new StaticLiveRuntime(LiveSnapshot.Off(LiveOff.NoModel, LiveCors.BuiltIns));
        return (_ => { }, _ => off);
    }

    /// <summary>Options with problems: every snapshot is off (config), on the runtime's own origins.</summary>
    sealed class ConfigOffRuntime(ILiveRuntime inner) : ILiveRuntime
    {
        public LiveSnapshot Current => LiveSnapshot.Off(LiveOff.Config, inner.Current.Origins);
    }

    /// <summary>
    /// The tunnel guard, then, for /live/api, the JSON body for a 405 and CORS. Call it first, right after building the
    /// app: nothing may run before the guard. It also builds SessionManager (and so the runtime), so options with
    /// problems are logged at startup, not on a visitor's first call.
    /// </summary>
    public static IApplicationBuilder UseNcbLiveGuard(this IApplicationBuilder app)
    {
        _ = app.ApplicationServices.GetRequiredService<SessionManager>();
        app.UseMiddleware<TunnelGuardMiddleware>();
        app.UseWhen(ctx => TunnelGuardMiddleware.IsLiveApi(ctx.Request.Path), branch =>
        {
            branch.Use(JsonMethodNotAllowedAsync);
            branch.UseCors(LiveCors.Policy);
        });
        return app;
    }

    /// <summary>
    /// A wrong method on a live route: routing's 405 (it sets Allow, writes nothing) gets the contract's JSON body,
    /// <c>{"error":"bad_request","message":…}</c>, keeping the 405 (contract, server final review). It runs around the
    /// rest of the pipeline, so it holds wherever the host puts routing. A HEAD response stays empty.
    /// </summary>
    static async Task JsonMethodNotAllowedAsync(HttpContext ctx, RequestDelegate next)
    {
        await next(ctx);
        if (ctx.Response.StatusCode == StatusCodes.Status405MethodNotAllowed && !ctx.Response.HasStarted
            && !HttpMethods.IsHead(ctx.Request.Method))
            await LiveErrors.WriteAsync(ctx, "bad_request", StatusCodes.Status405MethodNotAllowed);
    }

    /// <summary>
    /// The end of the live API: any /live/api request that routing found no endpoint for gets a JSON 404
    /// (<c>no_request</c>), so it never falls through to an NCF page, fallback or default file. Add it after the
    /// routing and endpoint calls (UseRouting, MapControllers, MapRazorPages), before anything terminal.
    /// </summary>
    public static IApplicationBuilder UseNcbLiveApiFallback(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            if (TunnelGuardMiddleware.IsLiveApi(ctx.Request.Path) && ctx.GetEndpoint() is null)
            {
                await LiveErrors.WriteAsync(ctx, "no_request");
                return;
            }
            await next(ctx);
        });
    }

    internal static void Forward<TStore>(IServiceCollection s) where TStore : class, IInviteStore, IApplicationStore, IRunStore
    {
        s.AddSingleton<IInviteStore>(sp => sp.GetRequiredService<TStore>());
        s.AddSingleton<IApplicationStore>(sp => sp.GetRequiredService<TStore>());
        s.AddSingleton<IRunStore>(sp => sp.GetRequiredService<TStore>());
    }
}
