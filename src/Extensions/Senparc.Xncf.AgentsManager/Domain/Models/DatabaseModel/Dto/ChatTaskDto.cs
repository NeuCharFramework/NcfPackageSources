using Senparc.Ncf.Core.Models;
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
            StartTime = chatTask.StartTime;
            EndTime = chatTask.EndTime;
            ResultComment = chatTask.ResultComment;
            HookPlatform = chatTask.HookPlatform;
            HookPlatformParameter = chatTask.HookPlatformParameter;
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
