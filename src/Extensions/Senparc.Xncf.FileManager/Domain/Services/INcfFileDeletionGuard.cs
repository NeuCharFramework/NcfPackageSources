/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：INcfFileDeletionGuard.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview1 完善文件资源边界、安全删除策略与静态资源管理

----------------------------------------------------------------*/

using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.Domain.Services;

/// <summary>
/// Optional cross-module guard for a file deletion. FileManager owns the file
/// lifecycle, while consumers such as KnowledgeBase can register a guard to
/// prevent removal of a source that is still referenced.
/// </summary>
public interface INcfFileDeletionGuard
{
    Task EnsureCanDeleteAsync(NcfFile file);
}
