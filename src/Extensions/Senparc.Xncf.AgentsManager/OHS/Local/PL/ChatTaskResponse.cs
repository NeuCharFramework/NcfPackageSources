using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class ChatTask_GetListResponse
    {
        public PagedList<ChatTaskDto> ChatTaskList { get; set; }
    }

    public class ChatTask_GetItemResponse
    {
        public ChatTaskDto ChatTaskDto { get; set; }
    }
}
