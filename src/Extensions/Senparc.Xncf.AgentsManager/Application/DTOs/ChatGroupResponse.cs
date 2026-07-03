/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupResponse.cs
    文件功能描述：ChatGroupResponse 相关实现
    
    
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
