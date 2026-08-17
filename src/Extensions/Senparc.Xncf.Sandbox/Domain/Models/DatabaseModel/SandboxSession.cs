/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxSession.cs
    文件功能描述：沙箱会话持久化实体

    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

/// <summary>
/// 沙箱会话。运行时句柄（容器 ID 等）可重建探测，但以本表为编排真相来源之一。
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

    public void Extend(DateTime newExpiresAtUtc)
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
            LastActivityAtUtc = new DateTimeOffset(LastActivityAtUtc, TimeSpan.Zero)
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
