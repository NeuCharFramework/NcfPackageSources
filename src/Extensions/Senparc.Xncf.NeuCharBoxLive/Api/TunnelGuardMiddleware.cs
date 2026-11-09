/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：TunnelGuardMiddleware.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：TunnelGuardMiddleware


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// The tunnel guard (spec 4.4, global constraints). A request is local only when it comes from loopback with no
/// X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto or Forwarded header and a localhost Host. Anything else (the
/// ngrok agent connects from loopback but adds X-Forwarded-For) passes only as GET, POST or OPTIONS on a clean live-API
/// path; everything else is an empty 404. It must be the host's very first middleware, so nothing runs before it.
/// </summary>
/// <remarks>
/// "Clean" matters because the path goes on to the rest of the NCF pipeline: Kestrel decodes %5C to '\', and on Windows
/// the static files middleware splits on it, so /live/api/..%5C..%5Ccss%5Csite.css would serve wwwroot\css\site.css.
/// </remarks>
public sealed class TunnelGuardMiddleware(RequestDelegate next)
{
    public const string ApiPrefix = "/live/api";

    static readonly string[] LocalHosts = { "localhost", "127.0.0.1", "::1", "[::1]" };
    static readonly string[] TunnelHeaders = { "X-Forwarded-For", "X-Forwarded-Host", "X-Forwarded-Proto", "Forwarded" };

    public Task InvokeAsync(HttpContext ctx)
    {
        if (IsLocal(ctx) || (IsAllowedMethod(ctx.Request.Method) && IsCleanLiveApi(ctx.Request.Path))) return next(ctx);
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }

    /// <summary>/live/api or below it, in any case (routing is case-insensitive). /live/apix is not. Used for the CORS branch.</summary>
    public static bool IsLiveApi(PathString path) => path.StartsWithSegments(ApiPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>GET, POST or OPTIONS: the only methods the API answers.</summary>
    public static bool IsAllowedMethod(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsPost(method) || HttpMethods.IsOptions(method);

    /// <summary>
    /// /live/api (ASCII, any case), then zero or more "/" + a segment of [A-Za-z0-9_-]+, then an optional "/". So no
    /// '.', '\', '%', ':', non-ASCII or empty segments: every live route's segments (ids included) fit that.
    /// </summary>
    public static bool IsCleanLiveApi(PathString path)
    {
        var p = path.Value;
        if (p is null || p.Length < ApiPrefix.Length || !Ascii.EqualsIgnoreCase(p.AsSpan(0, ApiPrefix.Length), ApiPrefix)) return false;
        var rest = p.AsSpan(ApiPrefix.Length);
        if (rest.Length == 0) return true;            // /live/api
        if (rest[0] != '/') return false;             // /live/apix
        rest = rest[1..];
        if (rest.Length == 0) return true;            // /live/api/
        if (rest[^1] == '/') rest = rest[..^1];       // one trailing slash after a segment
        if (rest.Length == 0) return false;           // /live/api//
        var segment = 0;
        foreach (var c in rest)
        {
            if (c == '/')
            {
                if (segment == 0) return false;       // an empty segment
                segment = 0;
            }
            else if (char.IsAsciiLetterOrDigit(c) || c is '_' or '-') segment++;
            else return false;
        }
        return segment > 0;
    }

    public static bool IsLocal(HttpContext ctx)
    {
        var ip = ctx.Connection.RemoteIpAddress;
        if (ip is null) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (!IPAddress.IsLoopback(ip)) return false;
        foreach (var header in TunnelHeaders)
            if (ctx.Request.Headers.ContainsKey(header)) return false;
        var host = ctx.Request.Host.Host;
        foreach (var local in LocalHosts)
            if (string.Equals(host, local, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
