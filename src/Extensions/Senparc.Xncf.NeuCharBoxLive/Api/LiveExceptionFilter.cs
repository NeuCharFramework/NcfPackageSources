/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveExceptionFilter.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：LiveExceptionFilter


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// Unexpected exceptions in LiveApiController never reach the host's error pages (the developer exception page would
/// send a stack trace through the tunnel):
/// <list type="bullet">
/// <item>the client left (OperationCanceledException or IOException once RequestAborted fired): nothing to send, nothing logged;</item>
/// <item>the response already started (an SSE stream): logged, and the connection is cut, since no JSON can follow;</item>
/// <item>otherwise: logged, and the contract's 500 internal with its generic message.</item>
/// </list>
/// The log has the route template and session id only: never headers (the invite code) or the body (visitor text).
/// </summary>
public sealed class LiveExceptionFilter(ILogger<LiveExceptionFilter> log) : IAsyncExceptionFilter
{
    public static readonly EventId Failed = new(7201, "LiveApiFailed");

    public Task OnExceptionAsync(ExceptionContext context)
    {
        var http = context.HttpContext;
        var e = context.Exception;
        context.ExceptionHandled = true;
        if (http.RequestAborted.IsCancellationRequested && e is OperationCanceledException or IOException)
        {
            context.Result = new EmptyResult();
            return Task.CompletedTask;
        }

        var route = context.ActionDescriptor.AttributeRouteInfo?.Template ?? context.ActionDescriptor.DisplayName ?? "?";
        var sid = context.RouteData.Values.TryGetValue("sid", out var v) ? v as string : null;
        log.LogError(Failed, e, "Live API {Route} failed (session {Sid})", route, sid ?? "-");

        if (http.Response.HasStarted)
        {
            http.Abort();
            context.Result = new EmptyResult();
            return Task.CompletedTask;
        }
        context.Result = LiveErrors.Result("internal");
        return Task.CompletedTask;
    }
}
