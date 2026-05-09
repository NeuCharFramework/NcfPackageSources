using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class ChatGroup_GetListResponse
    {
        public PagedList<ChatGroupDto> ChatGroupDtoList { get; set; }
    }

    public class ChatGroup_GetItemResponse
    {
        public ChatGroupDto ChatGroupDto { get; set; }
        public List<AgentTemplateDto> AgentTemplateDtoList { get; set; }
    }


    public class ChatGroup_SetGroupChatResponse
    {
        public string Logs { get; set; }
        public ChatGroupDto ChatGroupDto { get; set; }
    }
}
