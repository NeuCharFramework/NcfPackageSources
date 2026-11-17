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

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

/// <summary>
///Legacy comment translated to English.
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

    public void MarkFailed(string message)
    {
        Status = SandboxSessionStatus.Failed;
        StatusMessage = Truncate(message, 1000);
        Touch();
    }

    public void MarkStopping()
    {
        Status = SandboxSessionStatus.Stopping;
        Touch();
    }

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

    public bool CanDeleteRecord()
    {
        return Status is (SandboxSessionStatus.Stopped
            or SandboxSessionStatus.Expired
            or SandboxSessionStatus.Failed)
            && string.IsNullOrWhiteSpace(RuntimeHandle);
    }

    public void SetAlias(string? alias)
    {
        var normalized = (alias ?? string.Empty).Trim();
        Alias = normalized.Length == 0 ? null : Truncate(normalized, 128);
        Touch();
    }

    public void SetExtraPorts(string? extraPorts)
    {
        var normalized = (extraPorts ?? string.Empty).Trim();
        ExtraPorts = normalized.Length == 0 ? null : Truncate(normalized, 500);
        Touch();
    }

    public void SetExpiresAtUtc(DateTime newExpiresAtUtc)
    {
        ExpiresAtUtc = newExpiresAtUtc;
        Touch();
    }

    public void Touch()
    {
        LastActivityAtUtc = DateTime.UtcNow;
        LastUpdateTime = DateTime.Now;
    }

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
