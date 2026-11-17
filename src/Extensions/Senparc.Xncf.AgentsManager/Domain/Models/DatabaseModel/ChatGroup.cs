/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ChatGroup.cs

    File description: ChatGroup related implementation





    Created by: Senparc - 20240616



    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260813

    Change description: v0.15.0-preview11 Updated module behavior and related support.



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
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroup))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class ChatGroup : EntityBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string Name { get; private set; }

        /// <summary>
        /// whether enabled
        /// </summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>
        /// status
        /// </summary>
        [Required]
        public ChatGroupState State { get; private set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public ChatGroupContextSharingMode? ContextSharingMode { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        //[ForeignKey(nameof(AdminAgentTemplate))]
        public int AdminAgentTemplateId { get; private set; }
        //[InverseProperty(nameof(AgentTemplate.AdminChatGroups))]
        public AgentTemplate AdminAgentTemplate { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        //[ForeignKey(nameof(EnterAgentTemplate))]
        public int EnterAgentTemplateId { get; private set; }

        //[InverseProperty(nameof(AgentTemplate.EnterAgentChatGroups))]
        public AgentTemplate EnterAgentTemplate { get; set; }

        //public ICollection<ChatGroupMember> ChatGroupMembers { get; set; }


        private ChatGroup() { }

        public ChatGroup(string name, bool enable, ChatGroupState state, string description, int adminAgentTemplateId, int enterAgentTemplateId)
        {
            Name = name;
            Enable = enable;
            State = state;
            Description = description;
            AdminAgentTemplateId = adminAgentTemplateId;
            EnterAgentTemplateId = enterAgentTemplateId;
        }

        public ChatGroup(ChatGroupDto chatGroupDto)
        {
            Update(chatGroupDto);
        }

        public void Update(ChatGroupDto chatGroupDto)
        {
            Name = chatGroupDto.Name;
            Enable = chatGroupDto.Enable;
            State = chatGroupDto.State;
            Description = chatGroupDto.Description;
            ContextSharingMode = chatGroupDto.ContextSharingMode;
            AdminAgentTemplateId = chatGroupDto.AdminAgentTemplateId;
            EnterAgentTemplateId = chatGroupDto.EnterAgentTemplateId;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void EnableGroup()
        {
            Enable = true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void DisableGroup()
        {
            Enable = false;
        }

        public void Start()
        {
            State = ChatGroupState.Running;
        }

        public void Finish()
        {
            State = ChatGroupState.Finished;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum ChatGroupState
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Unstart = 0,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Running = 1,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Finished = 2,
    }
}
