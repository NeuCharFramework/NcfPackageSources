/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkspaceRecord.cs
    文件功能描述：工作区元数据实体
    
    
    创建标识：Senparc - 20261021
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.1 持久化工作区所有者与存储信息

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

    protected WorkspaceRecord()
    {
    }

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

    public void Touch(DateTime lastAccessedAt) => LastAccessedAt = lastAccessedAt;
}
