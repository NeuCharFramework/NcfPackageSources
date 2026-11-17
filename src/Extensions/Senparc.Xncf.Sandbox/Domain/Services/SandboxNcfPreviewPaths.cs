/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SandboxNcfPreviewPaths.cs
    File description: Related implementation.

    Created by: Senparc - 20260814

    Modified by: Senparc - 20260815
    Change description: v0.2.0 add the NCF preview sandbox workload

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

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
