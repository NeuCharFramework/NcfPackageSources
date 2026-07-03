/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatTaskDto.cs
    文件功能描述：ChatTaskDto 数据传输对象定义
    
    
    创建标识：Senparc - 20241016
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
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


        public bool Score { get; set; }

        public bool IsArchived { get; set; }

        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 对于对话结果的评价
        /// </summary>
        public string ResultComment { get; set; }


        /// <summary>
        /// 进行 WebHook 的平台
        /// </summary>
        [Required]
        public HookPlatform HookPlatform { get; set; }

        /// <summary>
        /// 进行 WebHook 的平台参数
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
    /// 用于缓存的当前运行任务信息
    /// </summary>
    public class RunningChatTaskDto
    {
        public ChatTaskDto ChatTaskDto { get; set; }
        public bool Cancel { get; set; }
        public int MessageCount { get; set; }
    }
}
