/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupHistoryDto.cs
    文件功能描述：ChatGroupHistoryDto 数据传输对象定义
    
    
    创建标识：Senparc - 20240616
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// ChatGroupHistory 数据库实体 DTO
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

        public ChatGroupHistoryDto() { }

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

        public ChatGroupHistoryDto(ChatGroupHistory chatGroupHistory)
        {
            ChatGroupId = chatGroupHistory.ChatGroupId;
            ChatTaskId = chatGroupHistory.ChatTaskId;
            ChatGroup = chatGroupHistory.ChatGroup;
            FromAgentTemplateId = chatGroupHistory.FromAgentTemplateId;
            FromAgentTemplate = chatGroupHistory.FromAgentTemplate;
            ToAgentTemplateId = chatGroupHistory.ToAgentTemplateId;
            ToAgentTemplate = chatGroupHistory.ToAgentTemplate;
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
