/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveCorsMiddleware.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：/live/api 的 CORS（每个请求按运行时快照的来源构建策略，自行应答预检）


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增 LiveCorsMiddleware：不注册具名策略、不替换 ICorsPolicyProvider，宿主自己的 CORS 不受影响；支持私有网络预检

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// CORS for /live/api (contract Transport), inside <see cref="LiveApiPipeline"/>'s branch. Each request with an Origin
/// gets a policy built from that moment's <see cref="ILiveRuntime.Current"/> origins (<see cref="LiveCors.PolicyFor"/>),
/// so an origin added in the settings works on the next request, with no restart. It goes through
/// <see cref="ICorsService"/> directly, so the host's CORS options and policy provider are never touched.
/// <para>
/// A preflight (OPTIONS with Origin and Access-Control-Request-Method) is answered here with 204 and goes no further: an
/// allowed origin gets the Allow-Origin, -Methods, -Headers and Max-Age headers, and Access-Control-Allow-Private-Network
/// when it sent Access-Control-Request-Private-Network: true (Chrome's private network access, for a public demo calling
/// a site on localhost or the LAN); any other origin gets none of them. Other requests go on, and an allowed origin's
/// Allow-Origin is added when the response starts (errors and the SSE stream included).
/// </para>
/// </summary>
public sealed class LiveCorsMiddleware(RequestDelegate next)
{
    public const string RequestPrivateNetwork = "Access-Control-Request-Private-Network";
    public const string AllowPrivateNetwork = "Access-Control-Allow-Private-Network";

    /// <summary>The runtime and the CORS service are taken per request, so building the host's pipeline resolves nothing.</summary>
    public Task InvokeAsync(HttpContext ctx, ILiveRuntime runtime, ICorsService cors)
    {
        if (!ctx.Request.Headers.ContainsKey(HeaderNames.Origin)) return next(ctx);

        var result = cors.EvaluatePolicy(ctx, LiveCors.PolicyFor(runtime.Current.Origins));
        if (result.IsPreflightRequest)
        {
            cors.ApplyResult(result, ctx.Response);
            if (result.IsOriginAllowed
                && string.Equals(ctx.Request.Headers[RequestPrivateNetwork].ToString().Trim(), "true", StringComparison.OrdinalIgnoreCase))
                ctx.Response.Headers[AllowPrivateNetwork] = "true";
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }

        ctx.Response.OnStarting(() =>
        {
            cors.ApplyResult(result, ctx.Response);
            return Task.CompletedTask;
        });
        return next(ctx);
    }
}
