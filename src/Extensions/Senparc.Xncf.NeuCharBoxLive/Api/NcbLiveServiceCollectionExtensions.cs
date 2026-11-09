/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcbLiveServiceCollectionExtensions.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：NcbLiveServiceCollectionExtensions


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
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
    internal Func<IServiceProvider, string, IChatClient?>? ChatFactory { get; private set; }
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
    /// The chat client for a model id (PlanModel, RunModel), or null when there is none (no key): live is then off.
    /// Called once per model id; the client is kept.
    /// </summary>
    public NcbLiveBuilder UseChatClient(Func<IServiceProvider, string, IChatClient?> factory)
    {
        ChatFactory = factory;
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
    /// The live server: LiveOptions from section NcbLive (one plain singleton instance), the stores, access, sessions,
    /// a runner per request, the session sweep, LiveApiController and the CORS policy <see cref="LiveCors.Policy"/>.
    /// Throws InvalidOperationException for an NcbLive:ExtraOrigins entry that isn't an origin.
    /// </summary>
    public static IServiceCollection AddNcbLive(this IServiceCollection s, IConfiguration cfg, Action<NcbLiveBuilder>? configure = null)
    {
        var opt = cfg.GetSection(LiveOptions.Section).Get<LiveOptions>() ?? new LiveOptions();
        var origins = LiveCors.AllowedOrigins(opt.ExtraOrigins);
        var b = new NcbLiveBuilder();
        configure?.Invoke(b);

        s.AddLogging();
        s.AddSingleton(opt);
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

        var factory = b.ChatFactory;
        s.AddSingleton<Func<string, IChatClient?>>(sp => new ChatClients(sp, factory).For);
        s.AddSingleton<Func<RequestRunner>>(sp =>
        {
            var chat = sp.GetRequiredService<Func<string, IChatClient?>>();
            // Routes refuse with live_off before a request starts without a client, so this never throws for a visitor.
            IChatClient ChatFor(string model) =>
                chat(model) ?? throw new InvalidOperationException($"no chat client for model '{model}': live is off");
            return () => new RequestRunner(ChatFor, sp.GetRequiredService<LiveOptions>(), sp.GetRequiredService<IPacer>(),
                sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<IRunStore>(), sp.GetRequiredService<ITokenMeter>(),
                sp.GetRequiredService<ILogger<RequestRunner>>());
        });

        s.AddHostedService<SessionSweepService>();
        s.AddControllers().AddApplicationPart(typeof(LiveApiController).Assembly);
        s.AddCors(o => o.AddPolicy(LiveCors.Policy, p => LiveCors.Build(p, origins)));
        return s;
    }

    /// <summary>
    /// The tunnel guard, then, for /live/api, the JSON body for a 405 and CORS. Call it first, right after building the
    /// app: nothing may run before the guard. It also builds SessionManager, so options that can't work together fail
    /// here, not on a visitor.
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

    /// <summary>The UseChatClient factory's client per model id, made on first use and kept (a null client too).</summary>
    sealed class ChatClients(IServiceProvider sp, Func<IServiceProvider, string, IChatClient?>? factory)
    {
        readonly object _gate = new();
        readonly Dictionary<string, IChatClient?> _made = new(StringComparer.Ordinal);

        public IChatClient? For(string model)
        {
            if (factory is null) return null;
            lock (_gate)
            {
                if (!_made.TryGetValue(model, out var c)) _made[model] = c = factory(sp, model);
                return c;
            }
        }
    }
}
