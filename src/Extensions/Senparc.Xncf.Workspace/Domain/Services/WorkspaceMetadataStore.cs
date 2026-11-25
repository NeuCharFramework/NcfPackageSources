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

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Xncf.Workspace.Domain.Models.DatabaseModel;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.Domain.Services;

public interface IWorkspaceMetadataStore
{
    /// <summary>
    /// Determines whether a workspace metadata record is visible in the current scope.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>A task returning <see langword="true"/> when a record exists.</returns>
    Task<bool> IsVisibleAsync(string workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates workspace metadata.
    /// </summary>
    /// <param name="descriptor">The workspace descriptor to persist.</param>
    /// <param name="cancellationToken">Token used to cancel persistence.</param>
    Task UpsertAsync(WorkspaceDescriptor descriptor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a workspace's last-access timestamp when its record exists.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="lastAccessedAt">The timestamp to store.</param>
    /// <param name="cancellationToken">Token used to cancel persistence.</param>
    Task TouchAsync(string workspaceId, DateTime lastAccessedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes workspace metadata when its record exists.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    Task DeleteAsync(string workspaceId, CancellationToken cancellationToken = default);
}

public sealed class WorkspaceMetadataStore : IWorkspaceMetadataStore
{
    private readonly WorkspaceSenparcEntities _entities;

    /// <summary>
    /// Initializes metadata persistence over the Workspace database context.
    /// </summary>
    /// <param name="entities">The Workspace entity context.</param>
    public WorkspaceMetadataStore(WorkspaceSenparcEntities entities)
    {
        _entities = entities;
    }

    /// <summary>
    /// Determines whether metadata for a workspace is visible in the current database scope.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>A task returning <see langword="true"/> when a record exists.</returns>
    public Task<bool> IsVisibleAsync(string workspaceId, CancellationToken cancellationToken = default) =>
        _entities.WorkspaceRecords.AnyAsync(x => x.WorkspaceId == workspaceId, cancellationToken);

    /// <summary>
    /// Inserts workspace metadata or updates its last-access timestamp.
    /// </summary>
    /// <param name="descriptor">The workspace descriptor to persist.</param>
    /// <param name="cancellationToken">Token used to cancel persistence.</param>
    /// <returns>A task that completes after the database save.</returns>
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

    /// <summary>
    /// Updates the last-access timestamp when a workspace record exists.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="lastAccessedAt">The timestamp to persist.</param>
    /// <param name="cancellationToken">Token used to cancel persistence.</param>
    /// <returns>A task that completes after the optional database save.</returns>
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

    /// <summary>
    /// Removes workspace metadata when a record exists.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    /// <returns>A task that completes after the optional database save.</returns>
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
