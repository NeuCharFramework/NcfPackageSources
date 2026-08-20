/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ImportFilesRequest.cs
    文件功能描述：ImportFilesRequest 相关实现
    
    
    创建标识：Senparc - 20251225
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request
{
    public class ImportFilesRequest
    {
        public int knowledgeBaseId { get; set; }
        public List<int> fileIds { get; set; }
    }
}
