/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxJupyterPaths.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxJupyterPaths
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public const string ProxyPrefix = "/sandbox-jupyter";

    public static string GetBaseUrl(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("sessionId 不能为空。", nameof(sessionId));
        }

        return $"{ProxyPrefix}/{sessionId.Trim().ToLowerInvariant()}/";
    }

    public static string GetLabEntryUrl(string sessionId) => GetBaseUrl(sessionId) + "lab";

    public static string GetDirectLabEntryUrl(string sessionId, int hostPort, string accessToken)
    {
        if (hostPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(hostPort), "hostPort 必须在 1 到 65535 之间。");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("accessToken 不能为空。", nameof(accessToken));
        }

        return $"http://127.0.0.1:{hostPort}{GetLabEntryUrl(sessionId)}?token={Uri.EscapeDataString(accessToken)}";
    }

    public static bool TryParse(string path, out string sessionId, out string remaining)
    {
        sessionId = string.Empty;
        remaining = "/";

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith(ProxyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = normalized[ProxyPrefix.Length..];
        if (!rest.StartsWith('/'))
        {
            return false;
        }

        rest = rest.TrimStart('/');
        if (rest.Length == 0)
        {
            return false;
        }

        var slash = rest.IndexOf('/');
        if (slash <= 0)
        {
            sessionId = rest;
            remaining = "/";
            return sessionId.Length > 0;
        }

        sessionId = rest[..slash];
        remaining = rest[slash..];
        if (string.IsNullOrEmpty(remaining))
        {
            remaining = "/";
        }

        return sessionId.Length > 0;
    }
}
