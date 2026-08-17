/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentTemplate.cs
    文件功能描述：AgentTemplate 相关实现
    
    
    创建标识：Senparc - 20240616
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.14.0-preview9 新增 Agent 模板知识库关联与管理统计

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel
{
    /// <summary>
    /// Agent模板信息
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AgentTemplate))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AgentTemplate : EntityBase<int>
    {
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        public string Name { get; private set; }

        /// <summary>
        /// 系统消息
        /// </summary>
        [Required]
        public string SystemMessage { get; private set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>
        /// PromptRange 的代号
        /// </summary>
        public string PromptCode { get; private set; }

        /// <summary>
        /// 第三方机器人平台类型
        /// </summary>
        public HookRobotType HookRobotType { get; private set; }

        /// <summary>
        /// 第三方机器人平台参数
        /// </summary>
        public string HookRobotParameter { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; }

        public string Avastar { get; set; }

        /// <summary>
        /// Function Call 名称列表，多个用逗号分隔
        /// </summary>
        public string FunctionCallNames { get; private set; }

        /// <summary>
        /// McpEndpoints，多个用逗号分隔
        /// </summary>
        public string McpEndpoints { get; private set; }

        /// <summary>
        /// 绑定的 KnowledgeBase ID。跨 XNCF 数据库上下文，不建立数据库外键。
        /// </summary>
        public int? KnowledgeBaseId { get; private set; }

        /// <summary>
        /// 模型绑定方式。默认沿用 PromptRange 中的模型配置，保持升级前行为。
        /// </summary>
        [Required]
        public AgentModelBindingMode ModelBinding { get; private set; } = AgentModelBindingMode.InheritPromptRange;

        /// <summary>
        /// 手动绑定的 AIModel ID。跨 XNCF 数据库上下文，不建立数据库外键。
        /// </summary>
        public int? AiModelId { get; private set; }

        /// <summary>是否为系统保留的 Human 参与者。该属性不落库。</summary>
        [NotMapped]
        public bool IsHuman => HumanParticipantConstants.IsHuman(PromptCode);

        /// <summary>是否为系统自动维护的 Agent。</summary>
        [NotMapped]
        public bool IsSystemAgent => IsHuman
            || string.Equals(Name, "PromptCatalyzer", StringComparison.OrdinalIgnoreCase);

        /// <summary>系统 Agent 类型，仅用于管理界面标识。</summary>
        [NotMapped]
        public string SystemAgentKind => IsHuman
            ? HumanParticipantConstants.Name
            : string.Equals(Name, "PromptCatalyzer", StringComparison.OrdinalIgnoreCase)
                ? "PromptCatalyzer"
                : null;

        /// <summary>
        /// 获取McpEndpoints的JSON对象
        /// </summary>
        /// <returns>包含所有Endpoints的Dictionary对象</returns>
        public Dictionary<string, Dictionary<string, string>> GetMcpEndpointsDict()
        {
            if (string.IsNullOrEmpty(McpEndpoints))
            {
                return new Dictionary<string, Dictionary<string, string>>();
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(McpEndpoints);
            }
            catch
            {
                return new Dictionary<string, Dictionary<string, string>>();
            }
        }

        /// <summary>
        /// 添加一个MCP Endpoint
        /// </summary>
        /// <param name="name">Endpoint名称</param>
        /// <param name="url">Endpoint URL</param>
        /// <returns>是否添加成功</returns>
        public bool AddMcpEndpoint(string name, string url)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                return false;
            }

            var endpointsDict = GetMcpEndpointsDict();
            
            // 添加或更新Endpoint
            endpointsDict[name] = new Dictionary<string, string>
            {
                { "url", url }
            };

            // 序列化回字符串
            McpEndpoints = JsonSerializer.Serialize(endpointsDict);
            return true;
        }

        /// <summary>
        /// 移除一个MCP Endpoint
        /// </summary>
        /// <param name="name">Endpoint名称</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveMcpEndpoint(string name)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(McpEndpoints))
            {
                return false;
            }

            var endpointsDict = GetMcpEndpointsDict();
            
            if (!endpointsDict.ContainsKey(name))
            {
                return false;
            }

            // 移除Endpoint
            endpointsDict.Remove(name);

            // 如果为空则设为null，否则序列化回字符串
            McpEndpoints = endpointsDict.Count > 0 
                ? JsonSerializer.Serialize(endpointsDict)
                : null;
                
            return true;
        }

        //[InverseProperty(nameof(ChatGroupMember.AgentTemplate))]
        public ICollection<ChatGroupMember> ChatGroupMembers { get; private set; }

        //[InverseProperty(nameof(ChatGroup.AdminAgentTemplate))]
        public ICollection<ChatGroup> AdminChatGroups { get; private set; }

        //[InverseProperty(nameof(ChatGroup.EnterAgentTemplate))]
        public ICollection<ChatGroup> EnterAgentChatGroups { get; private set; }

        [InverseProperty(nameof(ChatGroupHistory.FromAgentTemplate))]
        public ICollection<ChatGroupHistory> FromChatGroupHistories { get; set; }

        [InverseProperty(nameof(ChatGroupHistory.ToAgentTemplate))]
        public ICollection<ChatGroupHistory> ToChatGroupHistoies { get; set; }

        private AgentTemplate() { }

        public AgentTemplate(string name, string systemMessage, bool enable, string description, string promptCode, HookRobotType hookRobotType, string hookRobotParameter, string avastar = null, string functionCallNames = null, string mcpEndpoints = null, int? knowledgeBaseId = null, AgentModelBindingMode modelBinding = AgentModelBindingMode.InheritPromptRange, int? aiModelId = null)
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
            SetModelBinding(modelBinding, aiModelId);
        }

        public bool EnableAgent()
        {
            Enable = true;
            return true;
        }

        public bool DisableAgent()
        {
            Enable = false;
            return true;
        }

        public void UpdateFromDto(AgentTemplateDto agentTemplateDto)
        {
            Name = agentTemplateDto.Name;
            SystemMessage = agentTemplateDto.SystemMessage;
            Enable = agentTemplateDto.Enable;
            Description = agentTemplateDto.Description;
            PromptCode = agentTemplateDto.PromptCode;
            HookRobotType = agentTemplateDto.HookRobotType;
            HookRobotParameter = agentTemplateDto.HookRobotParameter;
            FunctionCallNames = agentTemplateDto.FunctionCallNames;
            Avastar = agentTemplateDto.Avastar;
            McpEndpoints = agentTemplateDto.McpEndpoints;
            KnowledgeBaseId = agentTemplateDto.KnowledgeBaseId;
            SetModelBinding(agentTemplateDto.ModelBinding, agentTemplateDto.AiModelId);
        }

        public void UpdateModelBinding(AgentModelBindingMode modelBinding, int? aiModelId)
        {
            SetModelBinding(modelBinding, aiModelId);
        }

        private void SetModelBinding(AgentModelBindingMode modelBinding, int? aiModelId)
        {
            ModelBinding = Enum.IsDefined(modelBinding)
                ? modelBinding
                : AgentModelBindingMode.InheritPromptRange;
            AiModelId = ModelBinding == AgentModelBindingMode.ManualAiModel
                ? aiModelId
                : null;
        }
    }

    /// <summary>
    /// Agent 的运行模型来源。
    /// </summary>
    public enum AgentModelBindingMode
    {
        /// <summary>沿用 PromptRange Prompt 版本绑定的模型。</summary>
        InheritPromptRange = 0,

        /// <summary>沿用 Group 或 Workflow 节点的本次任务模型。</summary>
        FollowGroupTask = 1,

        /// <summary>固定使用 Agent 手动绑定的 AIModel。</summary>
        ManualAiModel = 2
    }

    /// <summary>
    /// 第三方机器人平台类型
    /// </summary>
    public enum HookRobotType
    {
        None = 0,
        /// <summary>
        /// 微信公众号
        /// </summary>
        WeChatMp = 1,
        /// <summary>
        /// 企业微信机器人
        /// </summary>
        WeChatWorkRobot = 2
    }
}
