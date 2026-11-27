/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WorkspaceAssetCapabilityStore.cs
    File description: Provides w orkspacea ssetc apabilitys tore functionality.

    Created by: Senparc - 20261123

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

----------------------------------------------------------------*/

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceAssetCapabilityStore.cs
    File description: Short-lived, file-scoped capabilities for sandbox workspace previews

    Created by: Senparc - 20261123



    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record WorkspaceAssetCapability(
    string Id, string Token, int SessionId, string WorkspaceId, string Path,
    string ETag, DateTimeOffset ExpiresAt);

public sealed class WorkspaceAssetCapabilityStore
{
    private const int LifetimeMinutes = 10;
    private readonly ConcurrentDictionary<string, WorkspaceAssetCapability> _capabilities = new();
    private readonly TimeProvider _time;

    /// <summary>
    /// Initializes the short-lived in-memory capability store.
    /// </summary>
    /// <param name="timeProvider">Optional clock used for expiration and deterministic tests.</param>
    public WorkspaceAssetCapabilityStore(TimeProvider timeProvider = null)
    {
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Creates a ten-minute capability for one normalized workspace path and
    /// removes expired or excessive older capabilities.
    /// </summary>
    /// <param name="sessionId">The chat session owning the capability.</param>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="path">The workspace-relative path.</param>
    /// <param name="etag">The expected file fingerprint, or an empty value.</param>
    /// <returns>The generated capability identifier, token, path, and expiration.</returns>
    /// <exception cref="ArgumentException">Thrown when the session/workspace or path is invalid.</exception>
    public WorkspaceAssetCapability Create(int sessionId, string workspaceId, string path, string etag)
    {
        if (sessionId <= 0 || string.IsNullOrWhiteSpace(workspaceId))
            throw new ArgumentException("The capability must belong to an active Workspace session.");
        WorkspaceAssetPreviewBuilder.NormalizePath(path);
        var now = _time.GetUtcNow();
        var capability = new WorkspaceAssetCapability(
            Guid.NewGuid().ToString("N"),
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
            sessionId,
            workspaceId,
            path,
            etag ?? string.Empty,
            now.AddMinutes(LifetimeMinutes));
        foreach (var item in _capabilities)
        {
            if (item.Value.ExpiresAt <= now) _capabilities.TryRemove(item.Key, out _);
        }
        _capabilities[capability.Id] = capability;
        if (_capabilities.Count > 4096)
        {
            foreach (var item in _capabilities.OrderBy(item => item.Value.ExpiresAt).Take(256))
                _capabilities.TryRemove(item.Key, out _);
        }
        return capability;
    }

    /// <summary>
    /// Validates a capability identifier/token pair, removes expired entries,
    /// and returns the stored capability when the constant-time token comparison succeeds.
    /// </summary>
    /// <param name="id">The capability identifier.</param>
    /// <param name="token">The capability token.</param>
    /// <param name="capability">Receives the validated capability.</param>
    /// <returns><see langword="true"/> when the capability is valid and unexpired.</returns>
    public bool TryGet(string id, string token, out WorkspaceAssetCapability capability)
    {
        capability = null;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(token)
            || !_capabilities.TryGetValue(id, out var stored))
            return false;
        if (stored.ExpiresAt <= _time.GetUtcNow())
        {
            _capabilities.TryRemove(id, out _);
            return false;
        }
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(stored.Token),
            Encoding.ASCII.GetBytes(token));
        if (!matches) return false;
        capability = stored;
        return true;
    }
}
