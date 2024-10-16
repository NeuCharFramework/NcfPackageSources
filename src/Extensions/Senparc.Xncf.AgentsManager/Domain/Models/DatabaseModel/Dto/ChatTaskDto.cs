using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Status Status { get; set; }
        [Required]
        public string PromptCommand { get; set; }

        public string Description { get; set; }

        [Required]
        public bool IsPersonality { get; set; }

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

        public ChatTaskDto(string name, int chatGroupId, int aiModelId, Status status, string promptCommand, string description, bool isPersonality, HookPlatform hookPlatform, string hookPlatformParameter)
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

        public ChatTaskDto(ChatTask chatTask)
        {
            Name = chatTask.Name;
            ChatGroupId = chatTask.ChatGroupId;
            AiModelId = chatTask.AiModelId;
            Status = chatTask.Status;
            PromptCommand = chatTask.PromptCommand;
            Description = chatTask.Description;
            IsPersonality = chatTask.IsPersonality;
            HookPlatform = chatTask.HookPlatform;
            HookPlatformParameter = chatTask.HookPlatformParameter;
        }
    }
}
