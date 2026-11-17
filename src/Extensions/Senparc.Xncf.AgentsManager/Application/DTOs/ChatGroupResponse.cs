/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: ChatGroupResponse.cs
    File description: ChatGroupResponse related implementation


    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Application.Dtos
{
    public class ChatGroup_GetListResponse
    {
        public PagedList<ChatGroupDto> ChatGroupDtoList { get; set; }
    }

    public class ChatGroup_GetItemResponse
    {
        public ChatGroupDto ChatGroupDto { get; set; }
        public List<AgentTemplateDto> AgentTemplateDtoList { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public List<ChatGroupRoleAgentDto> RoleAgentTemplateDtoList { get; set; } = new List<ChatGroupRoleAgentDto>();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<ChatGroupRemoteMemberDto> RemoteMemberDtoList { get; set; } = new List<ChatGroupRemoteMemberDto>();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class ChatGroupRoleAgentDto
    {
        public string RoleName { get; set; }
        public AgentTemplateDto AgentTemplateDto { get; set; }
    }


    public class ChatGroup_SetGroupChatResponse
    {
        public string Logs { get; set; }
        public ChatGroupDto ChatGroupDto { get; set; }
    }
}
