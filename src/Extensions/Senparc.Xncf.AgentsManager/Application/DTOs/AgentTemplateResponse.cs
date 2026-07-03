/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentTemplateResponse.cs
    文件功能描述：AgentTemplateResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
}
