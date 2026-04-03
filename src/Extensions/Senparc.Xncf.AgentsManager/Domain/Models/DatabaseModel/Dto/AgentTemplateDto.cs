using Microsoft.Identity.Client;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    ///Agent template information
    /// </summary>
    public class AgentTemplateDto : DtoBase<int>
    {
        /// <summary>
        ///name
        /// </summary>
        public string Name { get;  set; }

        /// <summary>
        /// System message (PromptCode)
        /// </summary>
        public string SystemMessage { get;  set; }

        /// <summary>
        /// Whether to enable
        /// </summary>
        public bool Enable { get;  set; }

        /// <summary>
        /// describe
        /// </summary>
        public string Description { get;  set; }

        /// <summary>
        ///Code name for PromptRange
        /// </summary>
        public string PromptCode { get;  set; }

        /// <summary>
        /// Third-party robot platform type
        /// </summary>
        public HookRobotType HookRobotType { get;  set; }

        /// <summary>
        /// Third-party robot platform parameters
        /// </summary>
        public string HookRobotParameter { get; set; }

        public string Avastar { get; set; }

        /// <summary>
        /// List of callable function names, separated by commas
        /// </summary>
        public string FunctionCallNames { get; set; }

        /// <summary>
        /// McpEndpoints, multiple separated by commas
        /// </summary>
        public string McpEndpoints { get; set; }

        public AgentTemplateDto() { }

        public AgentTemplateDto(string name, string systemMessage, bool enable, string description, string promptCode = null, HookRobotType hookRobotType = default, string hookRobotParameter = null, string avastar = null, string functionCallNames = null, string mcpEndpoints = null)
        {
            Name = name;
            SystemMessage = systemMessage;
            Enable = enable;
            Description = description;
            PromptCode = promptCode;
            HookRobotType = hookRobotType;
            HookRobotParameter = hookRobotParameter;
            Avastar = avastar;
            FunctionCallNames = functionCallNames;
            McpEndpoints = mcpEndpoints;
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