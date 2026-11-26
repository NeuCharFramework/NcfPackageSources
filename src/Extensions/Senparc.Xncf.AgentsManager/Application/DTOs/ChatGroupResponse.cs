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

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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
    /// <summary>Contains the paged Agent-group list returned by administration queries.</summary>
    public class ChatGroup_GetListResponse
    {
        /// <summary>Gets or sets the paged group DTO collection.</summary>
        public PagedList<ChatGroupDto> ChatGroupDtoList { get; set; }
    }

    /// <summary>Contains one group and the Agent templates used by its roles.</summary>
    public class ChatGroup_GetItemResponse
    {
        /// <summary>Gets or sets the group DTO.</summary>
        public ChatGroupDto ChatGroupDto { get; set; }
        /// <summary>Gets or sets all Agent templates available to the group editor.</summary>
        public List<AgentTemplateDto> AgentTemplateDtoList { get; set; }

        /// <summary>Gets or sets role-specific Agent template projections.</summary>
        public List<ChatGroupRoleAgentDto> RoleAgentTemplateDtoList { get; set; } = new List<ChatGroupRoleAgentDto>();

        /// <summary>Gets or sets remote Agent members assigned to the group.</summary>
        public List<ChatGroupRemoteMemberDto> RemoteMemberDtoList { get; set; } = new List<ChatGroupRemoteMemberDto>();
    }

    /// <summary>Associates a group role name with an Agent template.</summary>
    public class ChatGroupRoleAgentDto
    {
        /// <summary>Gets or sets the role name.</summary>
        public string RoleName { get; set; }
        /// <summary>Gets or sets the Agent template DTO.</summary>
        public AgentTemplateDto AgentTemplateDto { get; set; }
    }


    /// <summary>Contains logs and the saved group returned by group-chat setup.</summary>
    public class ChatGroup_SetGroupChatResponse
    {
        /// <summary>Gets or sets operation logs.</summary>
        public string Logs { get; set; }
        /// <summary>Gets or sets the saved group DTO.</summary>
        public ChatGroupDto ChatGroupDto { get; set; }
    }
}
