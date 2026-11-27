/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxSessionService.cs
    File description: SandboxSessionService.cs related implementation

    Created by: Senparc - 20260822

    Modified by: Senparc - 20260822
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Sandbox.Domain.Services;

public class SandboxSessionService : ServiceBase<SandboxSession>
{
    /// <summary>
    /// Initializes persistence operations for Sandbox sessions.
    /// </summary>
    /// <param name="repo">The Sandbox session repository.</param>
    /// <param name="serviceProvider">The dependency provider used by the base service.</param>
    public SandboxSessionService(IRepositoryBase<SandboxSession> repo, IServiceProvider serviceProvider)
        : base(repo, serviceProvider)
    {
    }

    /// <summary>
    /// Loads one Sandbox session by its public session identifier.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A task containing the session or <see langword="null"/>.</returns>
    public Task<SandboxSession?> GetBySessionIdAsync(string sessionId)
    {
        return GetObjectAsync(z => z.SessionId == sessionId);
    }

    /// <summary>
    /// Lists sessions currently creating, running, or stopping.
    /// </summary>
    /// <returns>Active sessions ordered newest first.</returns>
    public async Task<List<SandboxSession>> GetActiveSessionsAsync()
    {
        var list = await GetObjectListAsync(
                0,
                0,
                z => z.Status == SandboxSessionStatus.Creating
                     || z.Status == SandboxSessionStatus.Running
                     || z.Status == SandboxSessionStatus.Stopping,
                z => z.AddTime,
                OrderingType.Descending)
            .ConfigureAwait(false);
        return list.ToList();
    }

    /// <summary>
    /// Counts active sessions owned by one user.
    /// </summary>
    /// <param name="ownerUserId">The session owner identifier.</param>
    /// <returns>The active-session count.</returns>
    public Task<int> CountActiveByUserAsync(int ownerUserId)
    {
        return GetCountAsync(z =>
            z.OwnerUserId == ownerUserId
            && (z.Status == SandboxSessionStatus.Creating || z.Status == SandboxSessionStatus.Running));
    }

    /// <summary>
    /// Counts active sessions across all users.
    /// </summary>
    /// <returns>The global active-session count.</returns>
    public Task<int> CountActiveGlobalAsync()
    {
        return GetCountAsync(z =>
            z.Status == SandboxSessionStatus.Creating || z.Status == SandboxSessionStatus.Running);
    }

    /// <summary>
    /// Permanently deletes a Sandbox session record.
    /// </summary>
    /// <param name="session">The session entity to delete.</param>
    /// <returns>A task that completes after deletion.</returns>
    public async Task DeletePermanentlyAsync(SandboxSession session)
    {
        await RepositoryBase.DeleteAsync(session, softDelete: false).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists running/creating sessions whose expiration is at or before a supplied UTC time.
    /// </summary>
    /// <param name="utcNow">The UTC cutoff timestamp.</param>
    /// <returns>Expired sessions ordered by expiration time.</returns>
    public async Task<List<SandboxSession>> GetExpiredRunningAsync(DateTime utcNow)
    {
        var list = await GetObjectListAsync(
                0,
                0,
                z => (z.Status == SandboxSessionStatus.Running || z.Status == SandboxSessionStatus.Creating)
                     && z.ExpiresAtUtc <= utcNow,
                z => z.ExpiresAtUtc,
                OrderingType.Ascending)
            .ConfigureAwait(false);
        return list.ToList();
    }
}
