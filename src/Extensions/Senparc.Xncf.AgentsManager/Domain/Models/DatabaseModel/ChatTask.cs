/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatTask.cs
    文件功能描述：ChatTask 相关实现
    
    
    创建标识：Senparc - 20241016
    
    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

-

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 AgentTemplate 模型绑定、空输出 Token 重试与 Human-in-the-Loop

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel
{
    [Table(Register.DATABASE_PREFIX + nameof(ChatTask))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class ChatTask : EntityBase<int>
    {
        [Required, MaxLength(150)]
        public string Name { get; private set; }
        [Required]
        public int ChatGroupId { get; private set; }
        [Required]
        public int AiModelId { get; private set; }

        public ChatTask_Status Status { get; private set; }
        [Required]
        public string PromptCommand { get; private set; }

        public string Description { get; private set; }

        [Required]
        public bool IsPersonality { get; private set; }

        [Required]
        public bool ExecutionPolicyCaptured { get; private set; }

        [Required]
        public bool RequireHumanApproval { get; private set; }

        [Required]
        public HumanInTheLoopLevel HumanInTheLoopLevel { get; private set; }

        [Required]
        public ToolPermissionMode PluginToolPermission { get; private set; }

        [Required]
        public ToolPermissionMode McpToolPermission { get; private set; }

        [Required]
        public bool IncludeHumanParticipant { get; private set; }

        [Required]
        public int ChatMaxRound { get; private set; }

        public bool Score { get; private set; }

        /// <summary>
        /// 是否已归档
        /// </summary>
        [Required]
        public bool IsArchived { get; private set; }

        [Required]
        public DateTime StartTime { get; private set; }
        [Required]
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// 对于对话结果的评价
        /// </summary>
        public string ResultComment { get; private set; }


        /// <summary>
        /// 进行 WebHook 的平台
        /// </summary>
        [Required]
        public HookPlatform HookPlatform { get; private set; }

        /// <summary>
        /// 进行 WebHook 的平台参数
        /// </summary>
        public string HookPlatformParameter { get; private set; }

        private ChatTask() { }

        public ChatTask(string name, int chatGroupId, int aiModelId, ChatTask_Status status, string promptCommand, string description, bool isPersonality, HookPlatform hookPlatform, string hookPlatformParameter, bool score, DateTime startTime, DateTime endTime, string resultComment)
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
            IsArchived = false;
        }

        public ChatTask(ChatTaskDto chatTaskDto)
        {
            Name = chatTaskDto.Name;
            ChatGroupId = chatTaskDto.ChatGroupId;
            AiModelId = chatTaskDto.AiModelId;
            Status = chatTaskDto.Status;
            PromptCommand = chatTaskDto.PromptCommand;
            Description = chatTaskDto.Description;
            IsPersonality = chatTaskDto.IsPersonality;
            ExecutionPolicyCaptured = chatTaskDto.ExecutionPolicyCaptured;
            RequireHumanApproval = chatTaskDto.RequireHumanApproval;
            HumanInTheLoopLevel = chatTaskDto.HumanInTheLoopLevel;
            PluginToolPermission = chatTaskDto.PluginToolPermission;
            McpToolPermission = chatTaskDto.McpToolPermission;
            IncludeHumanParticipant = chatTaskDto.IncludeHumanParticipant;
            ChatMaxRound = chatTaskDto.ChatMaxRound;
            Score = chatTaskDto.Score;
            StartTime = chatTaskDto.StartTime;
            EndTime = chatTaskDto.EndTime;
            ResultComment = chatTaskDto.ResultComment;
            HookPlatform = chatTaskDto.HookPlatform;
            HookPlatformParameter = chatTaskDto.HookPlatformParameter;
            IsArchived = chatTaskDto.IsArchived;
        }

        public void ChangeStatus(ChatTask_Status status)
        {
            Status = status;
            if (status == ChatTask_Status.Cancelled
                || status == ChatTask_Status.Finished
                || status == ChatTask_Status.Failed)
            {
                EndTime = DateTime.Now;
            }
        }

        public void SetArchived(bool isArchived)
        {
            IsArchived = isArchived;
        }

    }

    public enum ChatTask_Status
    {
        Waiting = 0,
        Chatting = 1,
        Paused = 2,
        Finished = 3,
        Cancelled = 4,
        Failed = 5,
    }

    public enum HookPlatform
    {
        /// <summary>
        /// 
        /// </summary>
        None = 0,
        WeChat_MP = 1,
        WeChat_Work = 2
    }
}
