/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ChatGroupMember.cs

    File description: ChatGroupMember related implementation

    

    

    Created by: Senparc - 20240616

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroupMember))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class ChatGroupMember : EntityBase<int>
    {
        /// <summary>
        /// UID
        /// </summary>
        [Required]
        public string UID { get; private set; }

        /// <summary>
        /// AgentTemplateId
        /// </summary>
        [Required]
        [ForeignKey(nameof(AgentTemplate))]
        public int AgentTemplateId { get; private set; }

        [InverseProperty(nameof(AgentTemplate.ChatGroupMembers))]
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AgentTemplate AgentTemplate { get; private set; }

        /// <summary>
        /// ChatGroupId
        /// </summary>
        [Required]
        public int ChatGroupId { get; private set; }

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //public ChatGroup ChatGroup { get; private set; }

        //[InverseProperty(nameof(ChatGroupHistory.FromChatGroupMember))]
        //public List<ChatGroupHistory> FromChatGroupHistories { get; set; }

        //[InverseProperty(nameof(ChatGroupHistory.ToChatGroupMember))]
        //public List<ChatGroupHistory> ToChatGroupHistories { get; set; }


        private ChatGroupMember() { }

        public ChatGroupMember(int agentTemplateId, AgentTemplate agentTemplate, int chatGroupId/* ChatGroup chatGroup*/)
        {
            ResetUID();
            AgentTemplateId = agentTemplateId;
            AgentTemplate = agentTemplate;
            ChatGroupId = chatGroupId;
            //ChatGroup = chatGroup;
        }

        public ChatGroupMember(ChatGroupMemberDto chatGroupMemerDto)
        {
            UID = chatGroupMemerDto.UID;
            AgentTemplateId = chatGroupMemerDto.AgentTemplateId;
            AgentTemplate = chatGroupMemerDto.AgentTemplate;
            ChatGroupId = chatGroupMemerDto.ChatGroupId;
            //ChatGroup = chatGroupMemerDto.ChatGroup;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void ResetUID()
        {
            UID = Guid.NewGuid().ToString("N");
        }
    }
}
