/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupMember.cs
    File description: ChatGroupMember related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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
    /// Persists the membership of an Agent template in a ChatGroup.
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
        /// Gets the linked Agent template.
        /// </summary>
        public AgentTemplate AgentTemplate { get; private set; }

        /// <summary>
        /// ChatGroupId
        /// </summary>
        [Required]
        public int ChatGroupId { get; private set; }

        ///// <summary>
        ///// </summary>
        //public ChatGroup ChatGroup { get; private set; }

        //[InverseProperty(nameof(ChatGroupHistory.FromChatGroupMember))]
        //public List<ChatGroupHistory> FromChatGroupHistories { get; set; }

        //[InverseProperty(nameof(ChatGroupHistory.ToChatGroupMember))]
        //public List<ChatGroupHistory> ToChatGroupHistories { get; set; }


        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private ChatGroupMember() { }

        /// <summary>Initializes a ChatGroup membership and generates its UID.</summary>
        /// <param name="agentTemplateId">The Agent-template identifier.</param>
        /// <param name="agentTemplate">The linked Agent template.</param>
        /// <param name="chatGroupId">The ChatGroup identifier.</param>
        public ChatGroupMember(int agentTemplateId, AgentTemplate agentTemplate, int chatGroupId/* ChatGroup chatGroup*/)
        {
            ResetUID();
            AgentTemplateId = agentTemplateId;
            AgentTemplate = agentTemplate;
            ChatGroupId = chatGroupId;
            //ChatGroup = chatGroup;
        }

        /// <summary>Maps a membership DTO to the persisted entity.</summary>
        /// <param name="chatGroupMemerDto">The source membership DTO.</param>
        public ChatGroupMember(ChatGroupMemberDto chatGroupMemerDto)
        {
            UID = chatGroupMemerDto.UID;
            AgentTemplateId = chatGroupMemerDto.AgentTemplateId;
            AgentTemplate = chatGroupMemerDto.AgentTemplate;
            ChatGroupId = chatGroupMemerDto.ChatGroupId;
            //ChatGroup = chatGroupMemerDto.ChatGroup;
        }

        /// <summary>
        /// Generates a new stable membership UID.
        /// </summary>
        public void ResetUID()
        {
            UID = Guid.NewGuid().ToString("N");
        }
    }
}
