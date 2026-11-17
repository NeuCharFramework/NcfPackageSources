/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatTaskDto.cs
    File description: Related implementation.

    Created by: Senparc - 20241016

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260704
    Change description: v0.11.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto
{
    public class ChatTaskDto : DtoBase<int>
    {
        [Required, MaxLength(150)]
        public string Name { get; set; }
        [Required]
        public int ChatGroupId { get; set; }
        [Required]
        public int AiModelId { get; set; }

        public ChatTask_Status Status { get; set; }
        [Required]
        public string PromptCommand { get; set; }

        public string Description { get; set; }

        [Required]
        public bool IsPersonality { get; set; }

        public bool ExecutionPolicyCaptured { get; set; }

        public bool RequireHumanApproval { get; set; }

        public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; }

        public ToolPermissionMode PluginToolPermission { get; set; }

        public ToolPermissionMode McpToolPermission { get; set; }

        public bool IncludeHumanParticipant { get; set; }

        public int ChatMaxRound { get; set; } = ChatGroupService.DefaultChatMaxRound;


        public bool Score { get; set; }

        public bool IsArchived { get; set; }

        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ResultComment { get; set; }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public HookPlatform HookPlatform { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string HookPlatformParameter { get; set; }

        public int TotalPromptTokens { get; set; }

        public int TotalCompletionTokens { get; set; }

        public int TotalTokens { get; set; }

        public int TotalRounds { get; set; }

        public double AverageResponseMilliseconds { get; set; }

        public int MaxResponseMilliseconds { get; set; }

        public ChatTaskDto() { }

        public ChatTaskDto(string name, int chatGroupId, int aiModelId, ChatTask_Status status, string promptCommand, string description, bool isPersonality, HookPlatform hookPlatform, string hookPlatformParameter, bool score, DateTime startTime, DateTime endTime, string resultComment)
        {
            Name = name;
            ChatGroupId = chatGroupId;
            AiModelId = aiModelId;
            Status = status;
            PromptCommand = promptCommand;
            Description = description;
            IsPersonality = isPersonality;
            Score = score;
            IsArchived = false;
            StartTime = startTime;
            EndTime = endTime;
            ResultComment = resultComment;
            HookPlatform = hookPlatform;
            HookPlatformParameter = hookPlatformParameter;
        }

        public ChatTaskDto(ChatTask chatTask)
        {
            Name = chatTask.Name;
            ChatGroupId = chatTask.ChatGroupId;
            AiModelId = chatTask.AiModelId;
            Status = chatTask.Status;
            PromptCommand = chatTask.PromptCommand;
            Description = chatTask.Description;
            IsPersonality = chatTask.IsPersonality;
            ExecutionPolicyCaptured = chatTask.ExecutionPolicyCaptured;
            RequireHumanApproval = chatTask.RequireHumanApproval;
            HumanInTheLoopLevel = chatTask.HumanInTheLoopLevel;
            PluginToolPermission = chatTask.PluginToolPermission;
            McpToolPermission = chatTask.McpToolPermission;
            IncludeHumanParticipant = chatTask.IncludeHumanParticipant;
            ChatMaxRound = chatTask.ChatMaxRound;
            Score = chatTask.Score;
            IsArchived = chatTask.IsArchived;
            StartTime = chatTask.StartTime;
            EndTime = chatTask.EndTime;
            ResultComment = chatTask.ResultComment;
            HookPlatform = chatTask.HookPlatform;
            HookPlatformParameter = chatTask.HookPlatformParameter;

            var aggregate = ChatUsageRemarkCodec.DecodeAggregateOrDefault(chatTask.AdminRemark);
            TotalPromptTokens = aggregate.PromptTokens;
            TotalCompletionTokens = aggregate.CompletionTokens;
            TotalTokens = aggregate.TotalTokens;
            TotalRounds = aggregate.MessageCount;
            AverageResponseMilliseconds = aggregate.AverageResponseMilliseconds;
            MaxResponseMilliseconds = aggregate.MaxResponseMilliseconds;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class RunningChatTaskDto
    {
        public ChatTaskDto ChatTaskDto { get; set; }
        public bool Cancel { get; set; }
        public int MessageCount { get; set; }
    }
}
