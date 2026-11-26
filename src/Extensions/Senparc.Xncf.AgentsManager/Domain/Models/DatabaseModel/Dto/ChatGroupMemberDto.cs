/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupMemberDto.cs
    File description: ChatGroupMemberDto related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// Transfers one Agent-template membership link to a chat group.
    /// </summary>
    public class ChatGroupMemberDto : DtoBase
    {
        /// <summary>
        /// UID
        /// </summary>
        public string UID { get;  set; }

        /// <summary>
        /// AgentTemplateId
        /// </summary>
        public int AgentTemplateId { get;  set; }

        /// <summary>
        /// Gets or sets the mapped Agent-template entity.
        /// </summary>
        public AgentTemplate AgentTemplate { get;  set; }

        /// <summary>
        /// chatgroupid
        /// </summary>
        public int ChatGroupId { get;  set; }

        ///// <summary>
        ///// </summary>
        //public ChatGroup ChatGroup { get;  set; }

        /// <summary>
        /// Initializes an empty ChatGroup membership DTO for binding.
        /// </summary>
        public ChatGroupMemberDto() { }

        /// <summary>
        /// Initializes a ChatGroup membership DTO from identifiers.
        /// </summary>
        /// <param name="uID">The stable membership UID.</param>
        /// <param name="chatGroupId">The ChatGroup identifier.</param>
        /// <param name="agentTemplateId">The Agent template identifier.</param>
        public ChatGroupMemberDto(string uID, int chatGroupId, int agentTemplateId /*,AgentTemplate agentTemplate  ChatGroup chatGroup*/)
        {
            UID = uID;
            ChatGroupId = chatGroupId;
            AgentTemplateId = agentTemplateId;
            //AgentTemplate = agentTemplate;
            //ChatGroup = chatGroup;
        }

        /// <summary>
        /// Projects a persisted ChatGroup membership entity into a DTO.
        /// </summary>
        /// <param name="chatGroupMemer">The membership entity to project.</param>
        public ChatGroupMemberDto(ChatGroupMember chatGroupMemer)
        {
            UID = chatGroupMemer.UID;
            AgentTemplateId = chatGroupMemer.AgentTemplateId;
            AgentTemplate = chatGroupMemer.AgentTemplate;
            ChatGroupId = chatGroupMemer.ChatGroupId;
            //ChatGroup = chatGroupMemer.ChatGroup;
        }
    }
}
