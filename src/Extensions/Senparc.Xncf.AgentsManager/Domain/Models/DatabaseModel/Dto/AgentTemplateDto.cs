/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentTemplateDto.cs
    文件功能描述：AgentTemplateDto 相关实现

    创建标识：Senparc - 20240616

    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.14.0-preview9 新增 Agent 模板知识库关联与管理统计

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 AgentTemplate 模型绑定、空输出 Token 重试与 Human-in-the-Loop

----------------------------------------------------------------*/

using Microsoft.Identity.Client;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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

        public string Avastar { get; set; }

        /// <summary>
        /// 可调用的函数名称列表,以逗号分隔
        /// </summary>
        public string FunctionCallNames { get; set; }

        /// <summary>
        /// Function Calling 绑定。FunctionCallNames 仍保留为旧版插件类名的兼容字段。
        /// </summary>
        public List<AgentFunctionBindingDto> FunctionBindings { get; set; } = new();

        /// <summary>
        /// McpEndpoints，多个用逗号分隔
        /// </summary>
        public string McpEndpoints { get; set; }

        public int? KnowledgeBaseId { get; set; }

        /// <summary>
        /// 模型绑定方式：0=从 PromptRange 继承，1=跟随组任务，2=手动选择 AIModel。
        /// </summary>
        public AgentModelBindingMode ModelBinding { get; set; } = AgentModelBindingMode.InheritPromptRange;

        /// <summary>
        /// 当 <see cref="ModelBinding"/> 为手动选择时使用的 AIModel ID。
        /// </summary>
        public int? AiModelId { get; set; }

        /// <summary>是否为系统保留的 Human 参与者。</summary>
        public bool IsHuman { get; set; }

        /// <summary>是否为系统自动维护的 Agent，例如 Human、PromptCatalyzer。</summary>
        public bool IsSystemAgent { get; set; }

        public string SystemAgentKind { get; set; }

        public string KnowledgeBaseName { get; set; }

        public int CompletedConversationRounds { get; set; }

        public int CompletedTaskCount { get; set; }

        public long PromptTokens { get; set; }

        public long CompletionTokens { get; set; }

        public long TotalTokens { get; set; }

        public double AverageResponseMilliseconds { get; set; }

        public DateTime? LastActiveTime { get; set; }

        public AgentTemplateDto() { }

        public AgentTemplateDto(string name, string systemMessage, bool enable, string description, string promptCode = null, HookRobotType hookRobotType = default, string hookRobotParameter = null, string avastar = null, string functionCallNames = null, string mcpEndpoints = null, int? knowledgeBaseId = null, AgentModelBindingMode modelBinding = AgentModelBindingMode.InheritPromptRange, int? aiModelId = null)
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
            KnowledgeBaseId = knowledgeBaseId;
            ModelBinding = modelBinding;
            AiModelId = aiModelId;
        }
    }

    public class AgentTemplateDto_UpdateOrCreate:AgentTemplateDto {

    }

    public class AgentFunctionBindingDto
    {
        /// <summary>plugin、function 或 workflow。</summary>
        public string Kind { get; set; }

        /// <summary>绑定的稳定键：插件类型、moduleUid::functionKey 或 workflowId。</summary>
        public string Key { get; set; }

        /// <summary>仅用于管理界面显示，不作为权限依据。</summary>
        public string Name { get; set; }

        public string Description { get; set; }
        public string ModuleUid { get; set; }
        public string FunctionKey { get; set; }
        public int? WorkflowId { get; set; }
    }

    public class AgentTemplateSimpleStatusDto : AgentTemplateDto
    {
        public int ChattingCount { get; set; }
        public float Score { get; set; }
        public bool HasPublishedA2A { get; set; }
        public bool PublishedA2AEnabled { get; set; }
    }

    public class AgentTemplateStatusDto
    {
        public AgentTemplateDto AgentTemplateDto { get; set; }

        public PromptItemDto PromptItemDto { get; set; }
        public PromptRangeDto PromptRangeDto { get; set; }

        public AIModelDto AIModelDto { get; set; }
    }
}
