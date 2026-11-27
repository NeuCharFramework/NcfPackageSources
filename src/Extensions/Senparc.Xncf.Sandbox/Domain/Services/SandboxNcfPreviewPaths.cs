/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxNcfPreviewPaths.cs
    File description: Related implementation.

    Created by: Senparc - 20260814

    Modified by: Senparc - 20260815
    Change description: v0.2.0 add the NCF preview sandbox workload

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

public static class SandboxNcfPreviewPaths
{
    public const string ProxyPrefix = "/sandbox-preview";

    /// <summary>
    /// Builds the reverse-proxy base path for a Sandbox preview session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The normalized preview base path.</returns>
    public static string GetBasePath(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("sessionId 不能为空。", nameof(sessionId));
        }
        return $"{ProxyPrefix}/{sessionId.Trim().ToLowerInvariant()}";
    }

    /// <summary>
    /// Builds the preview entry URL for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The trailing-slash preview URL path.</returns>
    public static string GetEntryUrl(string sessionId) => GetBasePath(sessionId) + "/";

    /// <summary>
    /// Parses a proxy path into session identifier and backend-relative remainder.
    /// </summary>
    /// <param name="path">The incoming proxy path.</param>
    /// <param name="sessionId">Receives the parsed session identifier.</param>
    /// <param name="remaining">Receives the remaining backend path.</param>
    /// <returns><see langword="true"/> when the path contains a session segment.</returns>
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
