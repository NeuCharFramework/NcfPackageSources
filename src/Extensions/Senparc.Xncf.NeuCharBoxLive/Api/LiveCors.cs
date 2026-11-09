/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveCors.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：LiveCors


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 移除具名策略 NcbLive，改为 PolicyFor(origins)：LiveCorsMiddleware 每个请求按快照的来源构建策略

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// The CORS policy for /live/api (spec 4.4, contract Transport): the runtime snapshot's origins, the methods and headers
/// the demo sends (Authorization and ngrok-skip-browser-warning make even GET health preflighted), and a 10-minute
/// preflight cache. No credentials: the invite code travels in the Authorization header. It is never registered with the
/// host's CORS options: <see cref="LiveCorsMiddleware"/> builds it per request.
/// </summary>
public static class LiveCors
{
    /// <summary>The published demo, allowed in every snapshot.</summary>
    public static IReadOnlyList<string> BuiltIns { get; } = new[] { "https://leozheng265.github.io" };

    /// <summary>The demo served from localhost while it is being worked on; a host adds it to its snapshot's origins.</summary>
    public static IReadOnlyList<string> Development { get; } = new[] { "http://localhost:8765" };

    public static IReadOnlyList<string> Methods { get; } = new[] { "GET", "POST", "OPTIONS" };
    public static IReadOnlyList<string> Headers { get; } = new[] { "Authorization", "Content-Type", "Accept", "ngrok-skip-browser-warning" };
    public static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(600);

    /// <summary>
    /// <paramref name="raw"/> as an origin: trimmed, without a trailing slash. False (and an empty origin) for anything
    /// that isn't an http(s) origin: a path, query, fragment or user info, or a "*", which would open the API to every
    /// site. Never throws.
    /// </summary>
    public static bool TryNormalize(string raw, out string origin)
    {
        origin = "";
        var o = (raw ?? "").Trim().TrimEnd('/');
        if (o.Length == 0 || o.Contains('*')) return false;
        if (!Uri.TryCreate(o, UriKind.Absolute, out var u) || u.Scheme is not ("http" or "https")
            || u.AbsolutePath != "/" || u.Query.Length > 0 || u.Fragment.Length > 0 || u.UserInfo.Length > 0)
            return false;
        origin = o;
        return true;
    }

    /// <summary>The policy for these origins (a snapshot's): <see cref="Methods"/>, <see cref="Headers"/>, <see cref="MaxAge"/>.</summary>
    public static CorsPolicy PolicyFor(IEnumerable<string> origins) => new CorsPolicyBuilder()
        .WithOrigins(origins.ToArray())
        .WithMethods(Methods.ToArray())
        .WithHeaders(Headers.ToArray())
        .SetPreflightMaxAge(MaxAge)
        .Build();
}
