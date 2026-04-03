using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    ///ChatGroupMemer database entity
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroupMember))]//The prefix must be added to prevent conflicts system-wide.
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
        ///AgentTemplate (type with the same name)
        /// </summary>
        public AgentTemplate AgentTemplate { get; private set; }

        /// <summary>
        /// ChatGroupId
        /// </summary>
        [Required]
        public int ChatGroupId { get; private set; }

        ///// <summary>
        ///// ChatGroup (type with the same name)
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
        ///reset UID
        /// </summary>
        public void ResetUID()
        {
            UID = Guid.NewGuid().ToString("N");
        }
    }
}
