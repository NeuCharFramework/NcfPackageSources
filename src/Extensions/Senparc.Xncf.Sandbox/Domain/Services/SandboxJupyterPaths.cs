/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxJupyterPaths.cs
    文件功能描述：Jupyter 反向代理路径约定

    创建标识：Senparc - 20260808

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxJupyterPaths
{
    /// <summary>
    /// 对外代理前缀（相对站点根）。完整形态：/sandbox-jupyter/{sessionId}/lab
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
