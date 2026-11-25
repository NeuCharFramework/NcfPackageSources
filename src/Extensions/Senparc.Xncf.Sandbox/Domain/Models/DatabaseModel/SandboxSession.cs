/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxSession.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

/// <summary>
/// Persists the lifecycle, runtime, ownership, quota, and access state of one Sandbox session.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(SandboxSession))]
[Serializable]
public class SandboxSession : EntityBase<int>
{
    [Required]
    [MaxLength(64)]
    public string SessionId { get; private set; } = string.Empty;

    public int OwnerUserId { get; private set; }

    [Required]
    [MaxLength(64)]
    public string TemplateKey { get; private set; } = string.Empty;

    public SandboxRuntimeKind RuntimeKind { get; private set; }

    public SandboxSessionStatus Status { get; private set; }

    [MaxLength(128)]
    public string? RuntimeHandle { get; private set; }

    public int? HostPort { get; private set; }

    [MaxLength(500)]
    public string? AccessUrl { get; private set; }

    [MaxLength(128)]
    public string? AccessToken { get; private set; }

    public double CpuLimit { get; private set; }

    public int MemoryMb { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime LastActivityAtUtc { get; private set; }

    [MaxLength(1000)]
    public string? StatusMessage { get; private set; }

    [MaxLength(128)]
    public string? Alias { get; private set; }

    [MaxLength(500)]
    public string? ExtraPorts { get; private set; }

    private SandboxSession()
    {
    }

    /// <summary>
    /// Creates a new session in the <see cref="SandboxSessionStatus.Creating"/> state.
    /// </summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <param name="ownerUserId">The owning administrator/user identifier.</param>
    /// <param name="templateKey">The sandbox template key.</param>
    /// <param name="runtimeKind">The selected runtime kind.</param>
    /// <param name="cpuLimit">The configured CPU limit.</param>
    /// <param name="memoryMb">The configured memory limit in megabytes.</param>
    /// <param name="expiresAtUtc">The UTC expiration time.</param>
    public SandboxSession(
        string sessionId,
        int ownerUserId,
        string templateKey,
        SandboxRuntimeKind runtimeKind,
        double cpuLimit,
        int memoryMb,
        DateTime expiresAtUtc)
    {
        SessionId = sessionId;
        OwnerUserId = ownerUserId;
        TemplateKey = templateKey;
        RuntimeKind = runtimeKind;
        CpuLimit = cpuLimit;
        MemoryMb = memoryMb;
        ExpiresAtUtc = expiresAtUtc;
        LastActivityAtUtc = DateTime.UtcNow;
        Status = SandboxSessionStatus.Creating;
        Flag = false;
        AddTime = DateTime.Now;
        LastUpdateTime = DateTime.Now;
    }

    /// <summary>
    /// Marks the session running and records runtime/access details.
    /// </summary>
    /// <param name="runtimeHandle">The isolated runtime handle.</param>
    /// <param name="hostPort">Optional host port.</param>
    /// <param name="accessUrl">Optional access URL.</param>
    /// <param name="accessToken">Optional access token.</param>
    /// <param name="message">Optional status message.</param>
    public void MarkRunning(string runtimeHandle, int? hostPort, string? accessUrl, string? accessToken, string? message = null)
    {
        RuntimeHandle = runtimeHandle;
        HostPort = hostPort;
        AccessUrl = accessUrl;
        AccessToken = accessToken;
        Status = SandboxSessionStatus.Running;
        StatusMessage = message;
        Touch();
    }

    /// <summary>
    /// Marks the session failed and stores a bounded failure message.
    /// </summary>
    /// <param name="message">The failure message.</param>
    public void MarkFailed(string message)
    {
        Status = SandboxSessionStatus.Failed;
        StatusMessage = Truncate(message, 1000);
        Touch();
    }

    /// <summary>
    /// Marks the session as stopping.
    /// </summary>
    public void MarkStopping()
    {
        Status = SandboxSessionStatus.Stopping;
        Touch();
    }

    /// <summary>
    /// Marks the session stopped and clears runtime/access handles.
    /// </summary>
    /// <param name="message">Optional status message.</param>
    public void MarkStopped(string? message = null)
    {
        Status = SandboxSessionStatus.Stopped;
        StatusMessage = message;
        RuntimeHandle = null;
        HostPort = null;
        AccessUrl = null;
        AccessToken = null;
        Touch();
    }

    /// <summary>
    /// Marks the session expired and clears runtime/access handles.
    /// </summary>
    /// <param name="message">Optional expiration message.</param>
    public void MarkExpired(string? message = null)
    {
        Status = SandboxSessionStatus.Expired;
        StatusMessage = message ?? "TTL expired";
        RuntimeHandle = null;
        HostPort = null;
        AccessUrl = null;
        AccessToken = null;
        Touch();
    }

    /// <summary>
    /// Determines whether the persisted record is safe to delete permanently.
    /// </summary>
    /// <returns><see langword="true"/> only for terminal states without a runtime handle.</returns>
    public bool CanDeleteRecord()
    {
        return Status is (SandboxSessionStatus.Stopped
            or SandboxSessionStatus.Expired
            or SandboxSessionStatus.Failed)
            && string.IsNullOrWhiteSpace(RuntimeHandle);
    }

    /// <summary>
    /// Sets a trimmed, length-bounded display alias.
    /// </summary>
    /// <param name="alias">The desired alias.</param>
    public void SetAlias(string? alias)
    {
        var normalized = (alias ?? string.Empty).Trim();
        Alias = normalized.Length == 0 ? null : Truncate(normalized, 128);
        Touch();
    }

    /// <summary>
    /// Sets a trimmed, length-bounded display string for extra port mappings.
    /// </summary>
    /// <param name="extraPorts">The extra-port display value.</param>
    public void SetExtraPorts(string? extraPorts)
    {
        var normalized = (extraPorts ?? string.Empty).Trim();
        ExtraPorts = normalized.Length == 0 ? null : Truncate(normalized, 500);
        Touch();
    }

    /// <summary>
    /// Updates the session expiration time.
    /// </summary>
    /// <param name="newExpiresAtUtc">The new UTC expiration time.</param>
    public void SetExpiresAtUtc(DateTime newExpiresAtUtc)
    {
        ExpiresAtUtc = newExpiresAtUtc;
        Touch();
    }

    /// <summary>
    /// Updates activity and modification timestamps.
    /// </summary>
    public void Touch()
    {
        LastActivityAtUtc = DateTime.UtcNow;
        LastUpdateTime = DateTime.Now;
    }

    /// <summary>
    /// Maps persistence state into the public Sandbox session information contract.
    /// </summary>
    /// <returns>A public session snapshot.</returns>
    public SandboxSessionInfo ToInfo()
    {
        return new SandboxSessionInfo
        {
            SessionId = SessionId,
            OwnerUserId = OwnerUserId,
            TemplateKey = TemplateKey,
            RuntimeKind = RuntimeKind,
            Status = Status,
            AccessUrl = AccessUrl,
            StatusMessage = StatusMessage,
            HostPort = HostPort,
            CreatedAtUtc = new DateTimeOffset(AddTime.ToUniversalTime()),
            ExpiresAtUtc = new DateTimeOffset(ExpiresAtUtc, TimeSpan.Zero),
            IsTtlUnlimited = SandboxTtlPolicy.IsUnlimited(ExpiresAtUtc),
            LastActivityAtUtc = new DateTimeOffset(LastActivityAtUtc, TimeSpan.Zero),
            Alias = Alias,
            ExtraPorts = ExtraPorts
        };
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value;
        }

        return value[..max];
    }
}
