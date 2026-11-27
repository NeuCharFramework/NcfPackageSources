/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroup.cs
    File description: ChatGroup related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

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
    /// Stores an Agent group configuration, lifecycle state, and administrator/contact agents.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroup))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class ChatGroup : EntityBase<int>
    {
        /// <summary>Gets the administrator-facing group name.</summary>
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

        /// <summary>Gets the configured context-sharing mode.</summary>
        public ChatGroupContextSharingMode? ContextSharingMode { get; private set; }

        /// <summary>Gets the administrator Agent template identifier.</summary>
        [Required]
        //[ForeignKey(nameof(AdminAgentTemplate))]
        public int AdminAgentTemplateId { get; private set; }
        //[InverseProperty(nameof(AgentTemplate.AdminChatGroups))]
        public AgentTemplate AdminAgentTemplate { get; set; }

        /// <summary>Gets the entry/contact Agent template identifier.</summary>
        [Required]
        //[ForeignKey(nameof(EnterAgentTemplate))]
        public int EnterAgentTemplateId { get; private set; }

        //[InverseProperty(nameof(AgentTemplate.EnterAgentChatGroups))]
        public AgentTemplate EnterAgentTemplate { get; set; }

        //public ICollection<ChatGroupMember> ChatGroupMembers { get; set; }


        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private ChatGroup() { }

        /// <summary>
        /// Initializes a group with its lifecycle state and administrator/contact Agent templates.
        /// </summary>
        public ChatGroup(string name, bool enable, ChatGroupState state, string description, int adminAgentTemplateId, int enterAgentTemplateId)
        {
            Name = name;
            Enable = enable;
            State = state;
            Description = description;
            AdminAgentTemplateId = adminAgentTemplateId;
            EnterAgentTemplateId = enterAgentTemplateId;
        }

        /// <summary>Initializes a group from its DTO.</summary>
        /// <param name="chatGroupDto">The group values to copy.</param>
        public ChatGroup(ChatGroupDto chatGroupDto)
        {
            Update(chatGroupDto);
        }

        /// <summary>Applies editable group values from a DTO.</summary>
        /// <param name="chatGroupDto">The replacement group values.</param>
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

        /// <summary>Marks the group as enabled.</summary>
        public void EnableGroup()
        {
            Enable = true;
        }

        /// <summary>Marks the group as disabled.</summary>
        public void DisableGroup()
        {
            Enable = false;
        }

        /// <summary>Marks the group as running.</summary>
        public void Start()
        {
            State = ChatGroupState.Running;
        }

        /// <summary>Marks the group as finished.</summary>
        public void Finish()
        {
            State = ChatGroupState.Finished;
        }
    }

    /// <summary>Identifies the lifecycle state of an Agent group.</summary>
    public enum ChatGroupState
    {
        /// <summary>The group has not started.</summary>
        Unstart = 0,
        /// <summary>The group is currently running.</summary>
        Running = 1,
        /// <summary>The group has finished.</summary>
        Finished = 2,
    }
}
