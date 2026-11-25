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

    public WorkspaceAssetCapabilityStore(TimeProvider timeProvider = null)
    {
        _time = timeProvider ?? TimeProvider.System;
    }

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
