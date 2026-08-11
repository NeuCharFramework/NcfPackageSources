using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System.Collections.Generic;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class RemoteAgent_GetListResponse
    {
        public PagedList<RemoteAgentDto> List { get; set; }
    }

    public class RemoteAgent_GetItemResponse
    {
        public RemoteAgentDto RemoteAgentDto { get; set; }
    }

    public class RemoteAgent_SetResponse
    {
        public RemoteAgentDto RemoteAgentDto { get; set; }
    }
}
