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
