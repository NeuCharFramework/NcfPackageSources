/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatTask.cs
    File description: ChatTask related implementation
    -

    Created by: Senparc - 20241016

    Modified by: Senparc - 20260704
    Change description: v0.11.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel
{
    [Table(Register.DATABASE_PREFIX + nameof(ChatTask))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    /// <summary>
    /// Persists one Agent-group execution task, its execution policy snapshot, lifecycle state,
    /// and hook/result metadata.
    /// </summary>
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

        /// <summary>Gets or sets whether the task has been archived from ordinary views.</summary>
        [Required]
        public bool IsArchived { get; private set; }

        [Required]
        public DateTime StartTime { get; private set; }
        [Required]
        public DateTime EndTime { get; private set; }

        /// <summary>Gets or sets the final result or diagnostic comment.</summary>
        public string ResultComment { get; private set; }


        /// <summary>Gets or sets the hook platform used for task delivery.</summary>
        [Required]
        public HookPlatform HookPlatform { get; private set; }

        /// <summary>Gets or sets the hook platform parameter.</summary>
        public string HookPlatformParameter { get; private set; }

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private ChatTask() { }

        /// <summary>Initializes a task from its execution and hook settings.</summary>
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

        /// <summary>Initializes a task from its DTO.</summary>
        /// <param name="chatTaskDto">The task values to copy.</param>
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

        /// <summary>
        /// Changes the lifecycle status and records an end time for terminal statuses.
        /// </summary>
        /// <param name="status">The new task status.</param>
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

        /// <summary>Changes whether the task is archived.</summary>
        /// <param name="isArchived">Whether the task should be archived.</param>
        public void SetArchived(bool isArchived)
        {
            IsArchived = isArchived;
        }

    }

    /// <summary>Identifies the lifecycle state of an Agent-group task.</summary>
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
