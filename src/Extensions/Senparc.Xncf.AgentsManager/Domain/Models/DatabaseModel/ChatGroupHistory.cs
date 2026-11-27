/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupHistory.cs
    File description: Related implementation.

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// Persists one message exchanged between Agent participants during a group task.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroupHistory))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class ChatGroupHistory : EntityBase<int>
    {
        [Required]
        public int ChatGroupId { get; private set; }

        public ChatGroup ChatGroup { get; private set; }


        [Required]
        public int ChatTaskId { get; private set; }
        public ChatTask ChatTask { get; private set; }


        [ForeignKey(nameof(FromAgentTemplate))]
        public int? FromAgentTemplateId { get; private set; }
        [InverseProperty(nameof(AgentTemplate.FromChatGroupHistories))]
        public AgentTemplate FromAgentTemplate { get; private set; }

        [ForeignKey(nameof(ToAgentTemplate))]
        public int? ToAgentTemplateId { get; private set; }

        [InverseProperty(nameof(AgentTemplate.ToChatGroupHistoies))]
        public AgentTemplate ToAgentTemplate { get; private set; }

        /// <summary>Gets the participant key that emitted the message.</summary>
        public string FromParticipantKey { get; private set; }

        /// <summary>Gets the kind of the emitting participant.</summary>
        public string FromParticipantKind { get; private set; }

        /// <summary>Gets the display name of the emitting participant.</summary>
        public string FromParticipantName { get; private set; }

        /// <summary>Gets the remote context identifier, when applicable.</summary>
        public string RemoteContextId { get; private set; }

        /// <summary>Gets the remote task identifier, when applicable.</summary>
        public string RemoteTaskId { get; private set; }

        //[ForeignKey(nameof(FromChatGroupMember))]
        //public int? FromChatGroupMemberId { get; private set; }

        //public ChatGroupMember FromChatGroupMember { get; private set; }

        //[ForeignKey(nameof(ToChatGroupMember))]
        //public int? ToChatGroupMemberId { get; private set; }

        //public ChatGroupMember ToChatGroupMember { get; private set; }

        [Required]
        public string Message { get; private set; }

        [Required]
        public MessageType MessageType { get; private set; }

        [Required,DeniedValues(Status.Waiting)]
        public Status Status { get;private set; }

        public int MyProperty { get; set; }

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private ChatGroupHistory() { }

        /// <summary>
        /// Initializes a persisted group-history message with its sender/recipient agents and status.
        /// </summary>
        public ChatGroupHistory(int chatGroupId,int chatTaskId, ChatGroup chatGroup, int? fromAgentTemplateId, AgentTemplate fromAgentTemplate, int? toAgentTemplateId, AgentTemplate toAgentTemplate, /*int? fromChatGroupMemberId, ChatGroupMember fromChatGroupMember, int? toChatGroupMemberId, ChatGroupMember toChatGroupMember,*/ string message, MessageType messageType, Status status)
        {
            ChatGroupId = chatGroupId;
            ChatTaskId = chatTaskId;
            ChatGroup = chatGroup;
            FromAgentTemplateId = fromAgentTemplateId;
            FromAgentTemplate = fromAgentTemplate;
            ToAgentTemplateId = toAgentTemplateId;
            ToAgentTemplate = toAgentTemplate;
            //FromChatGroupMemberId = fromChatGroupMemberId;
            //FromChatGroupMember = fromChatGroupMember;
            //ToChatGroupMemberId = toChatGroupMemberId;
            //ToChatGroupMember = toChatGroupMember;
            Message = message;
            MessageType = messageType;
            Status = status;
        }

        /// <summary>Initializes a history entity from its DTO.</summary>
        /// <param name="chatGroupHistoryDto">The history values to copy.</param>
        public ChatGroupHistory(ChatGroupHistoryDto chatGroupHistoryDto)
        {
            ChatGroupId = chatGroupHistoryDto.ChatGroupId;
            ChatTaskId = chatGroupHistoryDto.ChatTaskId;
            ChatGroup = chatGroupHistoryDto.ChatGroup;
            FromAgentTemplateId = chatGroupHistoryDto.FromAgentTemplateId;
            FromAgentTemplate = chatGroupHistoryDto.FromAgentTemplate;
            ToAgentTemplateId = chatGroupHistoryDto.ToAgentTemplateId;
            ToAgentTemplate = chatGroupHistoryDto.ToAgentTemplate;
            FromParticipantKey = chatGroupHistoryDto.FromParticipantKey;
            FromParticipantKind = chatGroupHistoryDto.FromParticipantKind;
            FromParticipantName = chatGroupHistoryDto.FromParticipantName;
            RemoteContextId = chatGroupHistoryDto.RemoteContextId;
            RemoteTaskId = chatGroupHistoryDto.RemoteTaskId;
            //FromChatGroupMemberId = chatGroupHistoryDto.FromChatGroupMemberId;
            //FromChatGroupMember = chatGroupHistoryDto.FromChatGroupMember;
            //ToChatGroupMemberId = chatGroupHistoryDto.ToChatGroupMemberId;
            //ToChatGroupMember = chatGroupHistoryDto.ToChatGroupMember;
            Message = chatGroupHistoryDto.Message;
            MessageType = chatGroupHistoryDto.MessageType;
            Status = chatGroupHistoryDto.Status;
            AdminRemark = chatGroupHistoryDto.AdminRemark;
            Remark = chatGroupHistoryDto.Remark;
        }
    }

    public enum MessageType
    {
        Text,
        Image,
        Voice,
        Video
    }

    public enum Status
    { 
        Waiting=0,
        Chatting=1,
        Paused=2,
        Finished=3,
        Cancelled=4,
    }
}
