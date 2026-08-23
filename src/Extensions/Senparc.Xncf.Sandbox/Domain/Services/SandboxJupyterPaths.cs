/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxJupyterPaths.cs
    文件功能描述：Jupyter 反向代理路径约定

    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxJupyterPaths
{
    /// <summary>
    /// Jupyter 的 base_url 前缀。完整形态：/sandbox-jupyter/{sessionId}/lab
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
