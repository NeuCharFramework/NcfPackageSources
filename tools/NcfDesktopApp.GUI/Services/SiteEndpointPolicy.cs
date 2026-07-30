/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SiteEndpointPolicy.cs
    文件功能描述：本地和远程 NCF 站点的统一安全地址策略

    创建标识：Senparc - 20260730
----------------------------------------------------------------*/

using System;

namespace NcfDesktopApp.GUI.Services;

internal static class SiteEndpointPolicy
{
    internal static bool TryNormalizeSiteUrl(string? siteUrl, out Uri siteUri, out string errorMessage)
    {
        siteUri = null!;
        errorMessage = string.Empty;

        if (!Uri.TryCreate(siteUrl?.Trim(), UriKind.Absolute, out var candidate) ||
            candidate.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(candidate.Host))
        {
            errorMessage = "站点地址必须是有效的 http:// 或 https:// URL。";
            return false;
        }

        if (candidate.Scheme == Uri.UriSchemeHttp && !IsLoopback(candidate))
        {
            errorMessage = "远程 NCF 站点必须使用 HTTPS；HTTP 仅允许 localhost/回环地址或本机 SSH 隧道。";
            return false;
        }

        var builder = new UriBuilder(candidate)
        {
            Path = candidate.AbsolutePath.TrimEnd('/') + "/",
            Query = string.Empty,
            Fragment = string.Empty
        };
        siteUri = builder.Uri;
        return true;
    }

    internal static bool TryCreateEndpoint(
        string? siteUrl,
        string relativePath,
        out Uri endpoint,
        out string errorMessage)
    {
        endpoint = null!;
        if (!TryNormalizeSiteUrl(siteUrl, out var baseUri, out errorMessage))
        {
            return false;
        }

        if (!Uri.TryCreate(baseUri, relativePath, out var resolved) || resolved == null ||
            !HasSameOrigin(baseUri, resolved))
        {
            errorMessage = "DesktopBridge 返回了无效或跨站的接口地址。";
            return false;
        }

        endpoint = resolved;
        return true;
    }

    internal static bool IsLoopback(Uri uri)
    {
        return uri.IsLoopback ||
               string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSameOrigin(Uri left, Uri right)
    {
        return string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase) &&
               left.Port == right.Port;
    }
}
