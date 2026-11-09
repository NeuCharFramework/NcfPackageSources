/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveCors.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：LiveCors


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// The CORS policy for /live/api (spec 4.4, contract Transport): the demo's origins plus NcbLive:ExtraOrigins, the
/// methods and headers the demo sends (Authorization and ngrok-skip-browser-warning make even GET health preflighted),
/// and a 10-minute preflight cache. No credentials: the invite code travels in the Authorization header.
/// </summary>
public static class LiveCors
{
    public const string Policy = "NcbLive";

    public static IReadOnlyList<string> Origins { get; } = new[] { "https://leozheng265.github.io", "http://localhost:8765" };
    public static IReadOnlyList<string> Methods { get; } = new[] { "GET", "POST", "OPTIONS" };
    public static IReadOnlyList<string> Headers { get; } = new[] { "Authorization", "Content-Type", "Accept", "ngrok-skip-browser-warning" };
    public static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(600);

    /// <summary>
    /// The allowlist plus the extra origins, without a trailing slash. Throws InvalidOperationException for an extra
    /// entry that isn't an http(s) origin: a "*" would open the API to every site.
    /// </summary>
    public static string[] AllowedOrigins(IEnumerable<string>? extra)
    {
        var list = new List<string>(Origins);
        foreach (var raw in extra ?? Array.Empty<string>())
        {
            var o = (raw ?? "").Trim().TrimEnd('/');
            if (o.Length == 0) continue;
            if (!Uri.TryCreate(o, UriKind.Absolute, out var u) || u.Scheme is not ("http" or "https")
                || u.AbsolutePath != "/" || u.Query.Length > 0 || u.Fragment.Length > 0 || u.UserInfo.Length > 0
                || o.Contains('*'))
                throw new InvalidOperationException($"NcbLive:ExtraOrigins entry '{raw}' is not an http(s) origin.");
            list.Add(o);
        }
        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static void Build(CorsPolicyBuilder p, IEnumerable<string> origins) => p
        .WithOrigins(origins.ToArray())
        .WithMethods(Methods.ToArray())
        .WithHeaders(Headers.ToArray())
        .SetPreflightMaxAge(MaxAge);
}
