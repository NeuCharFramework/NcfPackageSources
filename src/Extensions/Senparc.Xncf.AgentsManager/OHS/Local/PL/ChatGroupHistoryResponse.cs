using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class ChatGroupHistory_GetListResponse
    {
        public PagedList<ChatGroupHistoryDto> ChatGroupHistories { get; set; }
    }
}
