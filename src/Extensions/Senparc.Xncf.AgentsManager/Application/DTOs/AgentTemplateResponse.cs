/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentTemplateResponse.cs
    文件功能描述：AgentTemplateResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.14.0-preview9 新增 Agent 模板知识库关联与管理统计

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class AgentTemplate_GetListResponse
    {
        public PagedList<AgentTemplateSimpleStatusDto> List { get; set; }
    }

    public class AgentTemplate_GetItemResponse
    {
        public AgentTemplateDto AgentTemplate { get; set; }
    }

    public class AgentTemplate_GetItemStatusResponse
    {
        public AgentTemplateStatusDto AgentTemplateStatus { get; set; }
    }

    public class KnowledgeBaseOptionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsEmbedded { get; set; }
    }
}
