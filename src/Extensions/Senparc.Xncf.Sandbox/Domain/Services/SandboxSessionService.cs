/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxSessionService.cs
    文件功能描述：SandboxSessionService.cs 相关实现

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 完善沙箱会话状态服务与生命周期处理

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Sandbox.Domain.Services;

public class SandboxSessionService : ServiceBase<SandboxSession>
{
    public SandboxSessionService(IRepositoryBase<SandboxSession> repo, IServiceProvider serviceProvider)
        : base(repo, serviceProvider)
    {
    }

    public Task<SandboxSession?> GetBySessionIdAsync(string sessionId)
    {
        return GetObjectAsync(z => z.SessionId == sessionId);
    }

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

    public Task<int> CountActiveByUserAsync(int ownerUserId)
    {
        return GetCountAsync(z =>
            z.OwnerUserId == ownerUserId
            && (z.Status == SandboxSessionStatus.Creating || z.Status == SandboxSessionStatus.Running));
    }

    public Task<int> CountActiveGlobalAsync()
    {
        return GetCountAsync(z =>
            z.Status == SandboxSessionStatus.Creating || z.Status == SandboxSessionStatus.Running);
    }

    public async Task DeletePermanentlyAsync(SandboxSession session)
    {
        await RepositoryBase.DeleteAsync(session, softDelete: false).ConfigureAwait(false);
    }

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
