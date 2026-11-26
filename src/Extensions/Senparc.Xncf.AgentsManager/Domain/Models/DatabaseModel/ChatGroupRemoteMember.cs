/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupRemoteMember.cs
    File description: data model, DTO, and mapping definitions

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// Associates a ChatGroup with a configured remote agent and its context-sharing policy.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroupRemoteMember))]
    [Serializable]
    public class ChatGroupRemoteMember : EntityBase<int>
    {
        [Required]
        public string UID { get; private set; }

        [Required]
        public int ChatGroupId { get; private set; }

        [Required]
        [ForeignKey(nameof(RemoteAgent))]
        public int RemoteAgentId { get; private set; }

        public RemoteAgent RemoteAgent { get; private set; }

        [Required]
        public bool Enable { get; private set; }

        /// Gets or sets the optional context-sharing mode for this remote member.
        public ChatGroupContextSharingMode? ContextSharingMode { get; private set; }

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private ChatGroupRemoteMember() { }

        /// <summary>
        /// Initializes a remote ChatGroup member from a DTO.
        /// </summary>
        /// <param name="dto">The group, agent, and sharing settings.</param>
        public ChatGroupRemoteMember(ChatGroupRemoteMemberDto dto)
        {
            UID = string.IsNullOrWhiteSpace(dto.UID) ? Guid.NewGuid().ToString("N") : dto.UID;
            ChatGroupId = dto.ChatGroupId;
            RemoteAgentId = dto.RemoteAgentId;
            Enable = dto.Enable;
            ContextSharingMode = dto.ContextSharingMode;
        }

        /// <summary>
        /// Updates the enabled state and context-sharing mode.
        /// </summary>
        /// <param name="dto">The replacement member settings.</param>
        public void Update(ChatGroupRemoteMemberDto dto)
        {
            Enable = dto.Enable;
            ContextSharingMode = dto.ContextSharingMode;
        }
    }
}
