/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveCorsMiddleware.cs
    文件功能描述：/live/api 的 CORS（每个请求按运行时快照的来源构建策略，自行应答预检）


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增 LiveCorsMiddleware：不注册具名策略、不替换 ICorsPolicyProvider，宿主自己的 CORS 不受影响；支持私有网络预检

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 使用中间件自己的 CorsService 实例，不再从宿主容器解析 ICorsService（AddNcbLive 不再调用 AddCors）

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// CORS for /live/api (contract Transport), inside <see cref="LiveApiPipeline"/>'s branch. Each request with an Origin
/// gets a policy built from that moment's <see cref="ILiveRuntime.Current"/> origins (<see cref="LiveCors.PolicyFor"/>),
/// so an origin added in the settings works on the next request, with no restart. It evaluates that policy with a
/// <see cref="CorsService"/> of its own (empty options, no policies), so nothing CORS is registered in, or read from, the
/// host's container: the host's CORS options, policy provider and services are never touched.
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

    /// <summary>Built on the first request with an Origin (a race builds two, which is harmless: it holds no state).</summary>
    CorsService? _cors;

    /// <summary>The runtime is taken per request and the CORS service built on first use, so building the host's pipeline resolves nothing.</summary>
    public Task InvokeAsync(HttpContext ctx, ILiveRuntime runtime)
    {
        if (!ctx.Request.Headers.ContainsKey(HeaderNames.Origin)) return next(ctx);

        var cors = _cors ??= new CorsService(Options.Create(new CorsOptions()),
            ctx.RequestServices.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance);
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
