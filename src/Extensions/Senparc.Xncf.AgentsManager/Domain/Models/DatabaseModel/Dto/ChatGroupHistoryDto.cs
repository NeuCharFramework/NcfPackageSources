/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupHistoryDto.cs
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

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// Transfers one ChatGroup interaction history record and its token/latency usage metrics.
    /// </summary>
    public class ChatGroupHistoryDto : DtoBase
    {
        public int ChatGroupId { get; set; }
        public int ChatTaskId { get; set; }

        public ChatGroup ChatGroup { get; set; }

        public int? FromAgentTemplateId { get; set; }

        public AgentTemplate FromAgentTemplate { get; set; }

        public int? ToAgentTemplateId { get; set; }

        public AgentTemplate ToAgentTemplate { get; set; }

        public string FromParticipantKey { get; set; }

        public string FromParticipantKind { get; set; }

        public string FromParticipantName { get; set; }

        public string RemoteContextId { get; set; }

        public string RemoteTaskId { get; set; }

        //public int? FromChatGroupMemberId { get; set; }

        //public ChatGroupMember FromChatGroupMember { get; set; }

        //public int? ToChatGroupMemberId { get; set; }

        //public ChatGroupMember ToChatGroupMember { get; set; }

        public string Message { get; set; }

        public MessageType MessageType { get; set; }

        public Status Status { get; set; }

        public int PromptTokens { get; set; }

        public int CompletionTokens { get; set; }

        public int TotalTokens { get; set; }

        public int ResponseMilliseconds { get; set; }

        public int RoundIndex { get; set; }

        public string ResponseId { get; set; }

        /// <summary>Initializes an empty history DTO.</summary>
        public ChatGroupHistoryDto() { }

        /// <summary>Initializes a history DTO from participant and message data.</summary>
        public ChatGroupHistoryDto(int chatGroupId,int chatTaskId, ChatGroup chatGroup, int fromAgentTemplateId, AgentTemplate fromAgentTemplate, int toAgentTemplateId, AgentTemplate toAgentTemplate, /*int fromChatGroupMemberId, ChatGroupMember fromChatGroupMember, int toChatGroupMemberId, ChatGroupMember toChatGroupMember,*/ string message, MessageType messageType, Status status)
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

            if (ChatUsageRemarkCodec.TryDecodeMessage(AdminRemark, out var usage))
            {
                PromptTokens = usage.PromptTokens;
                CompletionTokens = usage.CompletionTokens;
                TotalTokens = usage.TotalTokens;
                ResponseMilliseconds = usage.ResponseMilliseconds;
                RoundIndex = usage.RoundIndex;
                ResponseId = usage.ResponseId;
            }
        }

        /// <summary>Maps a persisted history entity and decodes its usage metadata.</summary>
        /// <param name="chatGroupHistory">The source history entity.</param>
        public ChatGroupHistoryDto(ChatGroupHistory chatGroupHistory)
        {
            ChatGroupId = chatGroupHistory.ChatGroupId;
            ChatTaskId = chatGroupHistory.ChatTaskId;
            ChatGroup = chatGroupHistory.ChatGroup;
            FromAgentTemplateId = chatGroupHistory.FromAgentTemplateId;
            FromAgentTemplate = chatGroupHistory.FromAgentTemplate;
            ToAgentTemplateId = chatGroupHistory.ToAgentTemplateId;
            ToAgentTemplate = chatGroupHistory.ToAgentTemplate;
            FromParticipantKey = chatGroupHistory.FromParticipantKey;
            FromParticipantKind = chatGroupHistory.FromParticipantKind;
            FromParticipantName = chatGroupHistory.FromParticipantName;
            RemoteContextId = chatGroupHistory.RemoteContextId;
            RemoteTaskId = chatGroupHistory.RemoteTaskId;
            //FromChatGroupMemberId = chatGroupHistory.FromChatGroupMemberId;
            //FromChatGroupMember = chatGroupHistory.FromChatGroupMember;
            //ToChatGroupMemberId = chatGroupHistory.ToChatGroupMemberId;
            //ToChatGroupMember = chatGroupHistory.ToChatGroupMember;
            Message = chatGroupHistory.Message;
            MessageType = chatGroupHistory.MessageType;
            Status = chatGroupHistory.Status;

            if (ChatUsageRemarkCodec.TryDecodeMessage(chatGroupHistory.AdminRemark, out var usage))
            {
                PromptTokens = usage.PromptTokens;
                CompletionTokens = usage.CompletionTokens;
                TotalTokens = usage.TotalTokens;
                ResponseMilliseconds = usage.ResponseMilliseconds;
                RoundIndex = usage.RoundIndex;
                ResponseId = usage.ResponseId;
            }
        }
    }
}
