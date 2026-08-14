/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxNcfPreviewPaths.cs
    文件功能描述：Sandbox NCF 预览反向代理路径约定

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.2.0-preview3 增加 NCF 预览沙箱工作负载

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxNcfPreviewPaths
{
    public const string ProxyPrefix = "/sandbox-preview";

    public static string GetBasePath(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("sessionId 不能为空。", nameof(sessionId));
        }
        return $"{ProxyPrefix}/{sessionId.Trim().ToLowerInvariant()}";
    }

    public static string GetEntryUrl(string sessionId) => GetBasePath(sessionId) + "/";

    public static bool TryParse(string path, out string sessionId, out string remaining)
    {
        sessionId = string.Empty;
        remaining = "/";
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(ProxyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = path[ProxyPrefix.Length..].TrimStart('/');
        if (string.IsNullOrWhiteSpace(rest)) return false;
        var slash = rest.IndexOf('/');
        sessionId = slash < 0 ? rest : rest[..slash];
        remaining = slash < 0 ? "/" : rest[slash..];
        return sessionId.Length > 0;
    }
}
