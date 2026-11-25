/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceCatalog.cs
    File description: aggregate Workspace usage by administrator and clear content within permission boundaries

    Created by: Senparc - 20261109

    Modified by: Senparc - 20261109
    Change description: regular administrators manage their own Workspaces; super administrators can view and clear other Workspaces in the current tenant

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

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
    /// <summary>Creates an empty usage report for an actor and quota.</summary>
    /// <param name="actor">The resolved workspace actor.</param>
    /// <param name="quotaBytes">The configured workspace quota.</param>
    /// <returns>An empty report preserving actor scope and quota.</returns>
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
    /// Must remain consistent with Senparc.Ncf.Service.Config.SYSROLE_ADMINISTRATOR_ROLE_CODE.
    /// </summary>
    public const string SuperAdminRoleCode = "administrator";

    /// <summary>
    /// Resolves an authenticated administrator identity and its super-administrator role.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <param name="actor">Receives the resolved actor or a zero-valued actor.</param>
    /// <returns><see langword="true"/> when a positive NameIdentifier is present.</returns>
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

    /// <summary>
    /// Checks role claims for the configured super-administrator role code.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns><see langword="true"/> when the role is present.</returns>
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

    /// <summary>
    /// Creates a workspace owned by the authenticated actor.
    /// </summary>
    /// <param name="actor">The workspace actor.</param>
    /// <param name="name">The workspace display name.</param>
    /// <param name="cancellationToken">Token used to cancel creation.</param>
    /// <returns>A task containing the created workspace descriptor.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the actor identity is invalid.</exception>
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
            "Administrator created a Workspace: ActorId={ActorId}, Name={Name}",
            actor.UserId,
            name.Trim());
        return _storage.CreateAsync(new WorkspaceCreateRequest(actor.UserId, name.Trim()), cancellationToken);
    }

    /// <summary>
    /// Aggregates usage for the actor's own workspaces and, for super administrators,
    /// visible workspaces owned by other administrators.
    /// </summary>
    /// <param name="actor">The workspace actor.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration and usage reads.</param>
    /// <returns>A task containing scoped workspace usage and summaries.</returns>
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
                    "Skipped a Workspace owned by another administrator: ActorId={ActorId}, WorkspaceId={WorkspaceId}, OwnerId={OwnerId}",
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
                "Workspace usage: WorkspaceId={WorkspaceId}, Scope={Scope}, Files={FileCount}, Directories={DirectoryCount}, Bytes={UsedBytes}",
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
            "Workspace usage summary: ActorId={ActorId}, SuperAdmin={IsSuperAdmin}, OwnCount={OwnCount}, OtherCount={OtherCount}, OwnBytes={OwnBytes}, OtherBytes={OtherBytes}",
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

    /// <summary>
    /// Clears workspace content after enforcing ownership or super-administrator scope.
    /// </summary>
    /// <param name="actor">The workspace actor.</param>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    /// <returns>A task containing the cleared workspace result.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the workspace is not visible to the actor.</exception>
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
                    "Rejected clearing another administrator's Workspace: ActorId={ActorId}, WorkspaceId={WorkspaceId}, OwnerId={OwnerId}",
                    actor.UserId,
                    workspaceId,
                    descriptor.OwnerId);
            }
            else
            {
                _logger.LogInformation(
                    "Workspace clear request did not match a visible Workspace: ActorId={ActorId}, WorkspaceId={WorkspaceId}",
                    actor.UserId,
                    workspaceId);
            }

            throw new DirectoryNotFoundException("Workspace does not exist.");
        }

        if (clearingOtherOwner)
        {
            _logger.LogWarning(
                "Super administrator cleared another administrator's Workspace: ActorId={ActorId}, WorkspaceId={WorkspaceId}, OwnerId={OwnerId}, Name={Name}",
                actor.UserId,
                descriptor.Id,
                descriptor.OwnerId,
                descriptor.Name);
        }
        else
        {
            _logger.LogInformation(
                "Administrator cleared their own Workspace content: ActorId={ActorId}, WorkspaceId={WorkspaceId}, Name={Name}",
                actor.UserId,
                descriptor.Id,
                descriptor.Name);
        }

        await _storage.ClearContentAsync(descriptor.Id, cancellationToken).ConfigureAwait(false);
        return new WorkspaceClearResult(descriptor.Id, descriptor.Name, clearingOtherOwner, descriptor.OwnerId);
    }

    /// <summary>
    /// Ensures the actor contains a positive administrator identifier.
    /// </summary>
    /// <param name="actor">The actor to validate.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the actor identity is invalid.</exception>
    private static void EnsureActor(WorkspaceActor actor)
    {
        if (actor.UserId <= 0)
        {
            throw new UnauthorizedAccessException("The current administrator identity is invalid.");
        }
    }
}

internal static class WorkspaceUsageSummaryMath
{
    public static readonly WorkspaceUsageSummary Empty = new(0, 0, 0, 0);

    /// <summary>
    /// Sums file, directory, byte, and workspace counts from usage items.
    /// </summary>
    /// <param name="items">The usage items to aggregate.</param>
    /// <returns>The aggregate usage summary.</returns>
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

    /// <summary>
    /// Adds two usage summaries with checked arithmetic.
    /// </summary>
    /// <param name="left">The first summary.</param>
    /// <param name="right">The second summary.</param>
    /// <returns>The combined summary.</returns>
    public static WorkspaceUsageSummary Add(WorkspaceUsageSummary left, WorkspaceUsageSummary right) =>
        new(
            checked(left.WorkspaceCount + right.WorkspaceCount),
            checked(left.FileCount + right.FileCount),
            checked(left.DirectoryCount + right.DirectoryCount),
            checked(left.UsedBytes + right.UsedBytes));
}
