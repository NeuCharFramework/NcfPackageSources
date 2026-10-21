/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkspaceMetadataStore.cs
    文件功能描述：工作区元数据持久化
    
    
    创建标识：Senparc - 20261021
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.1 保存和更新工作区元数据

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Xncf.Workspace.Domain.Models.DatabaseModel;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.Domain.Services;

public interface IWorkspaceMetadataStore
{
    Task UpsertAsync(WorkspaceDescriptor descriptor, CancellationToken cancellationToken = default);
    Task TouchAsync(string workspaceId, DateTime lastAccessedAt, CancellationToken cancellationToken = default);
}

public sealed class WorkspaceMetadataStore : IWorkspaceMetadataStore
{
    private readonly WorkspaceSenparcEntities _entities;

    public WorkspaceMetadataStore(WorkspaceSenparcEntities entities)
    {
        _entities = entities;
    }

    public async Task UpsertAsync(WorkspaceDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        var record = await _entities.WorkspaceRecords
            .SingleOrDefaultAsync(x => x.WorkspaceId == descriptor.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record == null)
        {
            _entities.WorkspaceRecords.Add(new WorkspaceRecord(descriptor));
        }
        else
        {
            record.Touch(descriptor.LastAccessedUtc);
        }

        await _entities.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task TouchAsync(string workspaceId, DateTime lastAccessedAt, CancellationToken cancellationToken = default)
    {
        var record = await _entities.WorkspaceRecords
            .SingleOrDefaultAsync(x => x.WorkspaceId == workspaceId, cancellationToken)
            .ConfigureAwait(false);
        if (record == null)
        {
            return;
        }

        record.Touch(lastAccessedAt);
        await _entities.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
