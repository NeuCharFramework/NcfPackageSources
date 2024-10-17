using Microsoft.Identity.Client;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// Agent模板信息
    /// </summary>
    public class AgentTemplateDto : DtoBase<int>
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get;  set; }

        /// <summary>
        /// 系统消息（PromptCode）
        /// </summary>
        public string SystemMessage { get;  set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get;  set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get;  set; }

        /// <summary>
        /// PromptRange 的代号
        /// </summary>
        public string PromptCode { get;  set; }

        /// <summary>
        /// 第三方机器人平台类型
        /// </summary>
        public HookRobotType HookRobotType { get;  set; }

        /// <summary>
        /// 第三方机器人平台参数
        /// </summary>
        public string HookRobotParameter { get; set; }

        public AgentTemplateDto() { }

        public AgentTemplateDto(string name, string systemMessage, bool enable, string description, string promptCode = null, HookRobotType hookRobotType = default, string hookRobotParameter = null)
        {
            Name = name;
            SystemMessage = systemMessage;
            Enable = enable;
            Description = description;
            PromptCode = promptCode;
            HookRobotType = hookRobotType;
            HookRobotParameter = hookRobotParameter;
        }
    }

    public class AgentTemplateDto_UpdateOrCreate:AgentTemplateDto {

    }

    public class AgentTemplateSimpleStatusDto : AgentTemplateDto
    {
        public int ChattingCount { get; set; }
        public float Score { get; set; }
    }

    public class AgentTemplateStatusDto
    {
        public AgentTemplateDto AgentTemplateDto { get; set; }

        public PromptItemDto PromptItemDto { get; set; }
        public PromptRangeDto PromptRangeDto { get; set; }

        public AIModelDto AIModelDto { get; set; }
    }
}