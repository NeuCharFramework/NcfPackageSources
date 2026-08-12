/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseFileDeletionGuard.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview8 完善知识库文件删除保护、召回测试与管理界面

----------------------------------------------------------------*/

using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.KnowledgeBase.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services;

/// <summary>
/// Keeps FileManager from deleting a knowledge-base source while its chunks
/// are still attached to one or more knowledge bases.
/// </summary>
public sealed class KnowledgeBaseFileDeletionGuard : INcfFileDeletionGuard
{
    private readonly KnowledgeBaseItemService _knowledgeBaseItemService;

    public KnowledgeBaseFileDeletionGuard(KnowledgeBaseItemService knowledgeBaseItemService)
    {
        _knowledgeBaseItemService = knowledgeBaseItemService;
    }

    public async Task EnsureCanDeleteAsync(NcfFile file)
    {
        if (file.ResourceScope != NcfFileResourceScope.KnowledgeBase)
        {
            return;
        }

        var references = await _knowledgeBaseItemService.GetFullListAsync(item => item.NcfFileId == file.Id);
        var knowledgeBaseCount = references.Select(item => item.KnowledgeBasesId).Distinct().Count();
        if (knowledgeBaseCount > 0)
        {
            throw new InvalidOperationException($"文件“{file.FileName}”仍关联到 {knowledgeBaseCount} 个知识库。请先在知识库的“配置”中取消关联，再删除文件。");
        }
    }
}
