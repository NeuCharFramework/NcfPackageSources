using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
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

        public bool Score { get; private set; }

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

        /// <summary>
        /// Whether this task is a scheduled (recurring) task
        /// </summary>
        public bool IsScheduled { get; private set; }

        /// <summary>
        /// Interval in minutes between scheduled executions (null if not scheduled).
        /// Supports: fixed minutes, or encoded day-of-week/day-of-month patterns (see ScheduleType).
        /// </summary>
        public int? ScheduleIntervalMinutes { get; private set; }

        /// <summary>
        /// Schedule type: 0=interval (minutes), 1=daily, 2=weekly (ScheduleIntervalMinutes = day-of-week 1-7), 3=monthly (ScheduleIntervalMinutes = day-of-month 1-31)
        /// </summary>
        public ScheduleType ScheduleType { get; private set; }

        private ChatTask() { }

        public ChatTask(string name, int chatGroupId, int aiModelId, ChatTask_Status status, string promptCommand, string description, bool isPersonality, HookPlatform hookPlatform, string hookPlatformParameter, bool score, DateTime startTime, DateTime endTime, string resultComment, bool isScheduled = false, int? scheduleIntervalMinutes = null, ScheduleType scheduleType = ScheduleType.Interval)
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
            IsScheduled = isScheduled;
            ScheduleIntervalMinutes = scheduleIntervalMinutes;
            ScheduleType = scheduleType;
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
            Score = chatTaskDto.Score;
            StartTime = chatTaskDto.StartTime;
            EndTime = chatTaskDto.EndTime;
            ResultComment = chatTaskDto.ResultComment;
            HookPlatform = chatTaskDto.HookPlatform;
            HookPlatformParameter = chatTaskDto.HookPlatformParameter;
            IsScheduled = chatTaskDto.IsScheduled;
            ScheduleIntervalMinutes = chatTaskDto.ScheduleIntervalMinutes;
            ScheduleType = chatTaskDto.ScheduleType;
        }

        public void ChangeStatus(ChatTask_Status status)
        {
            Status = status;
            if (status == ChatTask_Status.Cancelled || status == ChatTask_Status.Finished)
            {
                EndTime = DateTime.Now;
            }
        }

    }

    public enum ChatTask_Status
    {
        Waiting = 0,
        Chatting = 1,
        Paused = 2,
        Finished = 3,
        Cancelled = 4,
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

    /// <summary>
    /// Schedule recurrence type for scheduled tasks
    /// </summary>
    public enum ScheduleType
    {
        /// <summary>
        /// Fixed interval in minutes (ScheduleIntervalMinutes = number of minutes)
        /// </summary>
        Interval = 0,
        /// <summary>
        /// Daily at the same time of day
        /// </summary>
        Daily = 1,
        /// <summary>
        /// Weekly on a specific day (ScheduleIntervalMinutes = day of week: 1=Monday … 7=Sunday)
        /// </summary>
        Weekly = 2,
        /// <summary>
        /// Monthly on a specific day (ScheduleIntervalMinutes = day of month: 1–31)
        /// </summary>
        Monthly = 3,
    }
}
