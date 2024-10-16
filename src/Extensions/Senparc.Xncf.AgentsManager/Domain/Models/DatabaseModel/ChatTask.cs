﻿using Senparc.Ncf.Core.Models;
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

        public Status Status { get; private set; }
        [Required]
        public string PromptCommand { get; private set; }

        public string Description { get; private set; }

        [Required]
        public bool IsPersonality { get; private set; }

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

        public ChatTask(string name, int chatGroupId, int aiModelId, Status status, string promptCommand, string description, bool isPersonality, HookPlatform hookPlatform, string hookPlatformParameter)
        {
            Name = name;
            ChatGroupId = chatGroupId;
            AiModelId = aiModelId;
            Status = status;
            PromptCommand = promptCommand;
            Description = description;
            IsPersonality = isPersonality;
            HookPlatform = hookPlatform;
            HookPlatformParameter = hookPlatformParameter;
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
            HookPlatform = chatTaskDto.HookPlatform;
            HookPlatformParameter = chatTaskDto.HookPlatformParameter;
        }
    }

    public enum Status
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
        None=0,
        WeChat_MP=1,
        WeChat_Work=2
    }
}