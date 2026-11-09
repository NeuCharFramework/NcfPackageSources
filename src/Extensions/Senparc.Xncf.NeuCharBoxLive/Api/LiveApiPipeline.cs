/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveApiPipeline.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：模块自行接入的 /live/api 分支（CORS、契约的 JSON 404 与 405）


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增 LiveApiPipeline：由 Register.UseXncfModule 在宿主 UseRouting 之前接入，取代 UseNcbLiveGuard 与 UseNcbLiveApiFallback

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// The module's own branch for /live/api, added from <see cref="Register.UseXncfModule"/>, which NCF calls before the
/// host's UseRouting, so the host needs no edit. Only requests whose path is /live/api or below it enter the branch:
/// <see cref="LiveCorsMiddleware"/> (CORS on the runtime snapshot's origins, preflights answered there), then the
/// contract's JSON bodies for a 405 and for a 404 no route matched. Every other path goes on exactly as the host built it.
/// </summary>
public static class LiveApiPipeline
{
    public const string Prefix = "/live/api";

    static volatile bool _hooked;

    /// <summary>True once <see cref="Use"/> has run in this process. False means the host never called UseXncfModules.</summary>
    public static bool Hooked => _hooked;

    /// <summary>/live/api or below it, in any case (routing is case-insensitive). /live/apix is not.</summary>
    public static bool IsLiveApi(PathString path) => path.StartsWithSegments(Prefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Adds the branch to <paramref name="app"/> and returns it. Call it before the host's UseRouting.</summary>
    public static IApplicationBuilder Use(IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseWhen(ctx => IsLiveApi(ctx.Request.Path), branch =>
        {
            branch.UseMiddleware<LiveCorsMiddleware>();
            branch.Use(JsonErrorsAsync);
        });
        _hooked = true;
        return app;
    }

    /// <summary>
    /// Runs the rest of the pipeline (routing, the endpoints and whatever the host adds), then gives an empty error the
    /// contract's body, keeping its status. A 405 from routing (it sets Allow, writes nothing) becomes
    /// <c>{"error":"bad_request",…}</c> (contract, server final review). A 404 with no endpoint (no route matched, and
    /// NCF hosts map no fallback) becomes <c>{"error":"no_request",…}</c>, so a wrong path is never a 405 or the host's
    /// page. A response already started, or a HEAD's, is left alone.
    /// </summary>
    static async Task JsonErrorsAsync(HttpContext ctx, RequestDelegate next)
    {
        await next(ctx);
        if (ctx.Response.HasStarted || HttpMethods.IsHead(ctx.Request.Method)) return;
        if (ctx.Response.StatusCode == StatusCodes.Status405MethodNotAllowed)
            await LiveErrors.WriteAsync(ctx, "bad_request", StatusCodes.Status405MethodNotAllowed);
        else if (ctx.Response.StatusCode == StatusCodes.Status404NotFound && ctx.GetEndpoint() is null)
            await LiveErrors.WriteAsync(ctx, "no_request");
    }
}
