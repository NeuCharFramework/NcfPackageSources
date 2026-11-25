/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceRecord.cs
    File description: Workspace metadata entity

    Created by: Senparc - 20261021

    Modified by: Senparc - 20261021
    Change description: v0.1.1 persist Workspace ownership and storage information

    Modified by: Senparc - 20261103
    Change description: v0.2.2 maintain the Workspace metadata entity

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(WorkspaceRecord))]
public class WorkspaceRecord : EntityBase<int>
{
    [Required, MaxLength(100)]
    public string WorkspaceId { get; private set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; private set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string PhysicalPath { get; private set; } = string.Empty;

    [Required]
    public long OwnerId { get; private set; }

    [Required, MaxLength(30)]
    public string OwnerType { get; private set; } = "User";

    public int? AgentTemplateId { get; private set; }

    [MaxLength(30)]
    public string StorageProvider { get; private set; } = "FileSystem";

    [MaxLength(30)]
    public string Status { get; private set; } = "Active";

    public DateTime LastAccessedAt { get; private set; }

    /// <summary>
    /// Initializes an empty record for ORM materialization.
    /// </summary>
    protected WorkspaceRecord()
    {
    }

    /// <summary>
    /// Creates a persisted metadata record from a workspace descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor containing identity, ownership, and storage metadata.</param>
    public WorkspaceRecord(WorkspaceDescriptor descriptor)
    {
        WorkspaceId = descriptor.Id;
        Name = descriptor.Name;
        PhysicalPath = descriptor.RootPath;
        OwnerId = descriptor.OwnerId;
        OwnerType = descriptor.OwnerType;
        AgentTemplateId = descriptor.AgentTemplateId;
        StorageProvider = descriptor.StorageProvider;
        LastAccessedAt = descriptor.LastAccessedUtc;
    }

    /// <summary>
    /// Updates the last-access timestamp.
    /// </summary>
    /// <param name="lastAccessedAt">The timestamp to store.</param>
    public void Touch(DateTime lastAccessedAt) => LastAccessedAt = lastAccessedAt;
}
