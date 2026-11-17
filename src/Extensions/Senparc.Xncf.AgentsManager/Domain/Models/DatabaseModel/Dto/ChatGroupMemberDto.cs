/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupMemberDto.cs
    File description: ChatGroupMemberDto related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public AgentTemplate AgentTemplate { get;  set; }

        /// <summary>
        /// chatgroupid
        /// </summary>
        public int ChatGroupId { get;  set; }

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //public ChatGroup ChatGroup { get;  set; }

        public ChatGroupMemberDto() { }

        public ChatGroupMemberDto(string uID, int chatGroupId, int agentTemplateId /*,AgentTemplate agentTemplate  ChatGroup chatGroup*/)
        {
            UID = uID;
            ChatGroupId = chatGroupId;
            AgentTemplateId = agentTemplateId;
            //AgentTemplate = agentTemplate;
            //ChatGroup = chatGroup;
        }

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
