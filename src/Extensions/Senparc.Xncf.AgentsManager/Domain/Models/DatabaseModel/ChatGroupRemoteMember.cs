/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: ChatGroupRemoteMember.cs
    File description: data model, DTO, and mapping definitions


    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    ///
    ///Legacy comment translated to English.
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

        ///Legacy comment translated to English.
        public ChatGroupContextSharingMode? ContextSharingMode { get; private set; }

        private ChatGroupRemoteMember() { }

        public ChatGroupRemoteMember(ChatGroupRemoteMemberDto dto)
        {
            UID = string.IsNullOrWhiteSpace(dto.UID) ? Guid.NewGuid().ToString("N") : dto.UID;
            ChatGroupId = dto.ChatGroupId;
            RemoteAgentId = dto.RemoteAgentId;
            Enable = dto.Enable;
            ContextSharingMode = dto.ContextSharingMode;
        }

        public void Update(ChatGroupRemoteMemberDto dto)
        {
            Enable = dto.Enable;
            ContextSharingMode = dto.ContextSharingMode;
        }
    }
}
