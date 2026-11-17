/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentTemplate.cs
    File description: AgentTemplate related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.14.0-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.19.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.19.3 Updated module behavior and related support.

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
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AgentTemplate))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class AgentTemplate : EntityBase<int>
    {
        /// <summary>
        /// name
        /// </summary>
        [Required]
        public string Name { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string SystemMessage { get; private set; }

        /// <summary>
        /// whether enabled
        /// </summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string PromptCode { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public HookRobotType HookRobotType { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string HookRobotParameter { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; private set; }

        public string Avastar { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string FunctionCallNames { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string McpEndpoints { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int? KnowledgeBaseId { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public AgentModelBindingMode ModelBinding { get; private set; } = AgentModelBindingMode.InheritPromptRange;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int? AiModelId { get; private set; }

        ///Legacy comment translated to English.
        public string SkillBindingsJson { get; private set; }

        ///Legacy comment translated to English.
        public string WorkspacePolicyJson { get; private set; }

        ///Legacy comment translated to English.
        public string ConnectorBindingsJson { get; private set; }

        public IReadOnlyList<AgentSkillBinding> GetSkillBindings()
            => DeserializeOrEmpty<AgentSkillBinding>(SkillBindingsJson);

        public WorkspacePolicy GetWorkspacePolicy()
            => DeserializeOrDefault(WorkspacePolicyJson, new WorkspacePolicy());

        public void SetSkillBindings(IEnumerable<AgentSkillBinding> bindings)
            => SkillBindingsJson = JsonSerializer.Serialize(bindings ?? Array.Empty<AgentSkillBinding>());

        public void SetWorkspacePolicy(WorkspacePolicy policy)
            => WorkspacePolicyJson = JsonSerializer.Serialize(policy ?? new WorkspacePolicy());

        public IReadOnlyList<AgentConnectorBinding> GetConnectorBindings()
            => DeserializeOrEmpty<AgentConnectorBinding>(ConnectorBindingsJson);

        public void SetConnectorBindings(IEnumerable<AgentConnectorBinding> bindings)
            => ConnectorBindingsJson = JsonSerializer.Serialize(bindings ?? Array.Empty<AgentConnectorBinding>());

        private static IReadOnlyList<T> DeserializeOrEmpty<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<T>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch (JsonException)
            {
                return Array.Empty<T>();
            }
        }

        private static T DeserializeOrDefault<T>(string json, T fallback)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return fallback;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json) ?? fallback;
            }
            catch (JsonException)
            {
                return fallback;
            }
        }

        ///Legacy comment translated to English.
        [NotMapped]
        public bool IsHuman => HumanParticipantConstants.IsHuman(PromptCode);

        ///Legacy comment translated to English.
        [NotMapped]
        public bool IsSystemAgent => IsHuman
            || string.Equals(Name, "PromptCatalyzer", StringComparison.OrdinalIgnoreCase);

        ///Legacy comment translated to English.
        [NotMapped]
        public string SystemAgentKind => IsHuman
            ? HumanParticipantConstants.Name
            : string.Equals(Name, "PromptCatalyzer", StringComparison.OrdinalIgnoreCase)
                ? "PromptCatalyzer"
                : null;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="name">Endpointname</param>
        /// <param name="url">Endpoint URL</param>
        ///Legacy comment translated to English.
        public bool AddMcpEndpoint(string name, string url)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                return false;
            }

            var endpointsDict = GetMcpEndpointsDict();
            
            //Legacy comment translated to English.
            endpointsDict[name] = new Dictionary<string, string>
            {
                { "url", url }
            };

            //Legacy comment translated to English.
            McpEndpoints = JsonSerializer.Serialize(endpointsDict);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="name">Endpointname</param>
        ///Legacy comment translated to English.
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

            //Legacy comment translated to English.
            endpointsDict.Remove(name);

            //Legacy comment translated to English.
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
            SkillBindingsJson = agentTemplateDto.SkillBindingsJson;
            WorkspacePolicyJson = agentTemplateDto.WorkspacePolicyJson;
            ConnectorBindingsJson = agentTemplateDto.ConnectorBindingsJson;
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
    ///Legacy comment translated to English.
    /// </summary>
    public enum AgentModelBindingMode
    {
        ///Legacy comment translated to English.
        InheritPromptRange = 0,

        ///Legacy comment translated to English.
        FollowGroupTask = 1,

        ///Legacy comment translated to English.
        ManualAiModel = 2
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum HookRobotType
    {
        None = 0,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        WeChatMp = 1,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        WeChatWorkRobot = 2
    }
}
