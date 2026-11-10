/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceCatalog.cs
    文件功能描述：按管理员身份汇总工作区用量，并执行受权限约束的内容清空


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：普通管理员只管理自己的工作区；超级管理员可查看并清空当前租户内其他工作区

----------------------------------------------------------------*/

using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.Domain.Services;

public enum WorkspaceOwnershipScope
{
    Own = 0,
    Other = 1
}

public sealed record WorkspaceActor(int UserId, bool IsSuperAdmin);

public sealed record WorkspaceUsageItem(
    string Id,
    string Name,
    int OwnerId,
    string OwnerType,
    DateTime CreatedUtc,
    DateTime LastAccessedUtc,
    WorkspaceOwnershipScope Scope,
    WorkspaceUsageStatistics Usage);

public sealed record WorkspaceUsageSummary(
    int WorkspaceCount,
    int FileCount,
    int DirectoryCount,
    long UsedBytes);

public sealed record WorkspaceUsageReport(
    bool IsSuperAdmin,
    int ActorId,
    long QuotaBytes,
    IReadOnlyList<WorkspaceUsageItem> OwnWorkspaces,
    IReadOnlyList<WorkspaceUsageItem> OtherWorkspaces,
    WorkspaceUsageSummary OwnSummary,
    WorkspaceUsageSummary OtherSummary,
    WorkspaceUsageSummary VisibleSummary)
{
    public static WorkspaceUsageReport Empty(WorkspaceActor actor, long quotaBytes) =>
        new(
            actor.IsSuperAdmin,
            actor.UserId,
            quotaBytes,
            [],
            [],
            WorkspaceUsageSummaryMath.Empty,
            WorkspaceUsageSummaryMath.Empty,
            WorkspaceUsageSummaryMath.Empty);
}

public sealed record WorkspaceClearResult(
    string WorkspaceId,
    string Name,
    bool ClearedOtherOwner,
    int OwnerId);

public static class WorkspaceActors
{
    /// <summary>
    /// 与 Senparc.Ncf.Service.Config.SYSROLE_ADMINISTRATOR_ROLE_CODE 保持一致。
    /// </summary>
    public const string SuperAdminRoleCode = "administrator";

    public static bool TryResolve(ClaimsPrincipal? user, out WorkspaceActor actor)
    {
        actor = new WorkspaceActor(0, false);
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var userId) || userId <= 0)
        {
            return false;
        }

        actor = new WorkspaceActor(userId, HasSuperAdminRole(user));
        return true;
    }

    public static bool HasSuperAdminRole(ClaimsPrincipal user) =>
        user.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(role => string.Equals(role, SuperAdminRoleCode, StringComparison.OrdinalIgnoreCase));
}

public interface IWorkspaceCatalog
{
    Task<WorkspaceDescriptor> CreateOwnAsync(
        WorkspaceActor actor,
        string name,
        CancellationToken cancellationToken = default);

    Task<WorkspaceUsageReport> GetReportAsync(
        WorkspaceActor actor,
        CancellationToken cancellationToken = default);

    Task<WorkspaceClearResult> ClearContentAsync(
        WorkspaceActor actor,
        string workspaceId,
        CancellationToken cancellationToken = default);
}

public sealed class WorkspaceCatalog : IWorkspaceCatalog
{
    private readonly IWorkspaceStorage _storage;
    private readonly WorkspaceOptions _options;
    private readonly ILogger<WorkspaceCatalog> _logger;

    public WorkspaceCatalog(
        IWorkspaceStorage storage,
        IOptions<WorkspaceOptions> options,
        ILogger<WorkspaceCatalog> logger)
    {
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    public Task<WorkspaceDescriptor> CreateOwnAsync(
        WorkspaceActor actor,
        string name,
        CancellationToken cancellationToken = default)
    {
        EnsureActor(actor);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workspace name is required.", nameof(name));
        }

        _logger.LogInformation(
            "管理员创建自己的工作区: ActorId={ActorId}, Name={Name}",
            actor.UserId,
            name.Trim());
        return _storage.CreateAsync(new WorkspaceCreateRequest(actor.UserId, name.Trim()), cancellationToken);
    }

    public async Task<WorkspaceUsageReport> GetReportAsync(
        WorkspaceActor actor,
        CancellationToken cancellationToken = default)
    {
        EnsureActor(actor);
        var own = new List<WorkspaceUsageItem>();
        var other = new List<WorkspaceUsageItem>();
        foreach (var workspace in await _storage.ListWorkspacesAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isOwn = workspace.OwnerId == actor.UserId;
            if (!isOwn && !actor.IsSuperAdmin)
            {
                _logger.LogDebug(
                    "跳过不属于当前管理员的工作区: ActorId={ActorId}, WorkspaceId={WorkspaceId}, OwnerId={OwnerId}",
                    actor.UserId,
                    workspace.Id,
                    workspace.OwnerId);
                continue;
            }

            var usage = await _storage.GetUsageAsync(workspace.Id, cancellationToken).ConfigureAwait(false);
            var item = new WorkspaceUsageItem(
                workspace.Id,
                workspace.Name,
                workspace.OwnerId,
                workspace.OwnerType,
                workspace.CreatedUtc,
                workspace.LastAccessedUtc,
                isOwn ? WorkspaceOwnershipScope.Own : WorkspaceOwnershipScope.Other,
                usage);
            _logger.LogDebug(
                "工作区用量: WorkspaceId={WorkspaceId}, Scope={Scope}, Files={FileCount}, Directories={DirectoryCount}, Bytes={UsedBytes}",
                item.Id,
                item.Scope,
                usage.FileCount,
                usage.DirectoryCount,
                usage.UsedBytes);
            if (isOwn)
            {
                own.Add(item);
            }
            else
            {
                other.Add(item);
            }
        }

        var ownSummary = WorkspaceUsageSummaryMath.From(own);
        var otherSummary = WorkspaceUsageSummaryMath.From(other);
        var visibleSummary = WorkspaceUsageSummaryMath.Add(ownSummary, otherSummary);
        _logger.LogInformation(
            "工作区用量汇总: ActorId={ActorId}, SuperAdmin={IsSuperAdmin}, OwnCount={OwnCount}, OtherCount={OtherCount}, OwnBytes={OwnBytes}, OtherBytes={OtherBytes}",
            actor.UserId,
            actor.IsSuperAdmin,
            ownSummary.WorkspaceCount,
            otherSummary.WorkspaceCount,
            ownSummary.UsedBytes,
            otherSummary.UsedBytes);
        return new WorkspaceUsageReport(
            actor.IsSuperAdmin,
            actor.UserId,
            _options.MaxWorkspaceBytes,
            own,
            other,
            ownSummary,
            otherSummary,
            visibleSummary);
    }

    public async Task<WorkspaceClearResult> ClearContentAsync(
        WorkspaceActor actor,
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        EnsureActor(actor);
        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            throw new ArgumentException("Workspace id is required.", nameof(workspaceId));
        }

        var descriptor = await _storage.GetAsync(workspaceId, cancellationToken).ConfigureAwait(false);
        var clearingOtherOwner = descriptor != null && descriptor.OwnerId != actor.UserId;
        if (descriptor == null || (clearingOtherOwner && !actor.IsSuperAdmin))
        {
            if (clearingOtherOwner && descriptor != null)
            {
                _logger.LogWarning(
                    "拒绝清空他人工作区: ActorId={ActorId}, WorkspaceId={WorkspaceId}, OwnerId={OwnerId}",
                    actor.UserId,
                    workspaceId,
                    descriptor.OwnerId);
            }
            else
            {
                _logger.LogInformation(
                    "清空请求未命中可见工作区: ActorId={ActorId}, WorkspaceId={WorkspaceId}",
                    actor.UserId,
                    workspaceId);
            }

            throw new DirectoryNotFoundException("Workspace does not exist.");
        }

        if (clearingOtherOwner)
        {
            _logger.LogWarning(
                "超级管理员清空其他管理员的工作区: ActorId={ActorId}, WorkspaceId={WorkspaceId}, OwnerId={OwnerId}, Name={Name}",
                actor.UserId,
                descriptor.Id,
                descriptor.OwnerId,
                descriptor.Name);
        }
        else
        {
            _logger.LogInformation(
                "清空自己的工作区内容: ActorId={ActorId}, WorkspaceId={WorkspaceId}, Name={Name}",
                actor.UserId,
                descriptor.Id,
                descriptor.Name);
        }

        await _storage.ClearContentAsync(descriptor.Id, cancellationToken).ConfigureAwait(false);
        return new WorkspaceClearResult(descriptor.Id, descriptor.Name, clearingOtherOwner, descriptor.OwnerId);
    }

    private static void EnsureActor(WorkspaceActor actor)
    {
        if (actor.UserId <= 0)
        {
            throw new UnauthorizedAccessException("当前管理员身份无效。");
        }
    }
}

internal static class WorkspaceUsageSummaryMath
{
    public static readonly WorkspaceUsageSummary Empty = new(0, 0, 0, 0);

    public static WorkspaceUsageSummary From(IReadOnlyList<WorkspaceUsageItem> items)
    {
        var fileCount = 0;
        var directoryCount = 0;
        var usedBytes = 0L;
        foreach (var item in items)
        {
            fileCount = checked(fileCount + item.Usage.FileCount);
            directoryCount = checked(directoryCount + item.Usage.DirectoryCount);
            usedBytes = checked(usedBytes + item.Usage.UsedBytes);
        }

        return new WorkspaceUsageSummary(items.Count, fileCount, directoryCount, usedBytes);
    }

    public static WorkspaceUsageSummary Add(WorkspaceUsageSummary left, WorkspaceUsageSummary right) =>
        new(
            checked(left.WorkspaceCount + right.WorkspaceCount),
            checked(left.FileCount + right.FileCount),
            checked(left.DirectoryCount + right.DirectoryCount),
            checked(left.UsedBytes + right.UsedBytes));
}
