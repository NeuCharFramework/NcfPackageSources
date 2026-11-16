/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: WorkspaceMetadataStore.cs
    File description: Workspace metadata persistence
    
    
    Created by: Senparc - 20261021
    
    Modified by: Senparc - 20261021
    Change description: v0.1.1 save and update Workspace metadata

    Modified by: Senparc - 20261102
    Change description: add tenant-scoped Workspace visibility queries

    Modified by: Senparc - 20261103
    Change description: v0.2.2 add streamed large-file Workspace uploads with tenant-aware ownership checks

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Xncf.Workspace.Domain.Models.DatabaseModel;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.Domain.Services;

public interface IWorkspaceMetadataStore
{
    Task<bool> IsVisibleAsync(string workspaceId, CancellationToken cancellationToken = default);
    Task UpsertAsync(WorkspaceDescriptor descriptor, CancellationToken cancellationToken = default);
    Task TouchAsync(string workspaceId, DateTime lastAccessedAt, CancellationToken cancellationToken = default);
    Task DeleteAsync(string workspaceId, CancellationToken cancellationToken = default);
}

public sealed class WorkspaceMetadataStore : IWorkspaceMetadataStore
{
    private readonly WorkspaceSenparcEntities _entities;

    public WorkspaceMetadataStore(WorkspaceSenparcEntities entities)
    {
        _entities = entities;
    }

    public Task<bool> IsVisibleAsync(string workspaceId, CancellationToken cancellationToken = default) =>
        _entities.WorkspaceRecords.AnyAsync(x => x.WorkspaceId == workspaceId, cancellationToken);

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

    public async Task DeleteAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        var record = await _entities.WorkspaceRecords
            .SingleOrDefaultAsync(x => x.WorkspaceId == workspaceId, cancellationToken)
            .ConfigureAwait(false);
        if (record == null)
        {
            return;
        }

        _entities.WorkspaceRecords.Remove(record);
        await _entities.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
