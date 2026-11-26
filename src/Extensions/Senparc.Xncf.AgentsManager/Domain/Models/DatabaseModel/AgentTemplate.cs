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

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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
    /// Specifies how an agent template selects the AI model used for execution.
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
        /// Inherits the model selected by the referenced PromptRange.
        /// </summary>
        [Required]
        public string SystemMessage { get; private set; }

        /// <summary>
        /// whether enabled
        /// </summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>
        /// Follows the model selected by the containing chat group task.
        /// </summary>
        public string PromptCode { get; private set; }

        /// <summary>
        /// Uses the explicitly assigned AI model identifier.
        /// </summary>
        public HookRobotType HookRobotType { get; private set; }

        /// <summary>Gets or sets the hook-specific parameter.</summary>
        public string HookRobotParameter { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; private set; }

        public string Avastar { get; set; }

        /// <summary>Gets or sets the configured Function names.</summary>
        public string FunctionCallNames { get; private set; }

        /// <summary>Gets or sets serialized MCP endpoint settings.</summary>
        public string McpEndpoints { get; private set; }

        /// <summary>Gets or sets the optional knowledge-base identifier.</summary>
        public int? KnowledgeBaseId { get; private set; }

        /// <summary>Gets or sets the model-binding mode.</summary>
        [Required]
        public AgentModelBindingMode ModelBinding { get; private set; } = AgentModelBindingMode.InheritPromptRange;

        /// <summary>Gets or sets the manually bound AI model identifier.</summary>
        public int? AiModelId { get; private set; }

        /// <summary>Gets or sets serialized skill bindings.</summary>
        public string SkillBindingsJson { get; private set; }

        /// <summary>Gets or sets serialized workspace policy.</summary>
        public string WorkspacePolicyJson { get; private set; }

        /// <summary>Gets or sets serialized connector bindings.</summary>
        public string ConnectorBindingsJson { get; private set; }

        /// <summary>
        /// Deserializes skill bindings, returning an empty list for empty or invalid JSON.
        /// </summary>
        /// <returns>The configured skill bindings.</returns>
        public IReadOnlyList<AgentSkillBinding> GetSkillBindings()
            => DeserializeOrEmpty<AgentSkillBinding>(SkillBindingsJson);

        /// <summary>
        /// Deserializes the workspace policy, returning a default policy when absent or invalid.
        /// </summary>
        /// <returns>The configured workspace policy.</returns>
        public WorkspacePolicy GetWorkspacePolicy()
            => DeserializeOrDefault(WorkspacePolicyJson, new WorkspacePolicy());

        /// <summary>
        /// Serializes skill bindings into the persisted JSON field.
        /// </summary>
        /// <param name="bindings">The bindings to store.</param>
        public void SetSkillBindings(IEnumerable<AgentSkillBinding> bindings)
            => SkillBindingsJson = JsonSerializer.Serialize(bindings ?? Array.Empty<AgentSkillBinding>());

        /// <summary>
        /// Serializes the workspace policy into the persisted JSON field.
        /// </summary>
        /// <param name="policy">The policy to store.</param>
        public void SetWorkspacePolicy(WorkspacePolicy policy)
            => WorkspacePolicyJson = JsonSerializer.Serialize(policy ?? new WorkspacePolicy());

        /// <summary>
        /// Deserializes connector bindings, returning an empty list for empty or invalid JSON.
        /// </summary>
        /// <returns>The configured connector bindings.</returns>
        public IReadOnlyList<AgentConnectorBinding> GetConnectorBindings()
            => DeserializeOrEmpty<AgentConnectorBinding>(ConnectorBindingsJson);

        /// <summary>
        /// Serializes connector bindings into the persisted JSON field.
        /// </summary>
        /// <param name="bindings">The bindings to store.</param>
        public void SetConnectorBindings(IEnumerable<AgentConnectorBinding> bindings)
            => ConnectorBindingsJson = JsonSerializer.Serialize(bindings ?? Array.Empty<AgentConnectorBinding>());

        /// <summary>
        /// Deserializes a JSON array into a read-only list and suppresses malformed or empty input.
        /// </summary>
        /// <typeparam name="T">The element type expected in the JSON array.</typeparam>
        /// <param name="json">The serialized array text.</param>
        /// <returns>A non-null list, or an empty list when the input is absent or invalid.</returns>
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

        /// <summary>
        /// Deserializes a JSON value and returns the supplied fallback for absent or malformed input.
        /// </summary>
        /// <typeparam name="T">The value type expected in the JSON text.</typeparam>
        /// <param name="json">The serialized value text.</param>
        /// <param name="fallback">The value returned when deserialization cannot produce a result.</param>
        /// <returns>The deserialized value or <paramref name="fallback"/>.</returns>
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

        /// <summary>
        /// Indicates whether this template represents the reserved Human participant.
        /// </summary>
        [NotMapped]
        public bool IsHuman => HumanParticipantConstants.IsHuman(PromptCode);

        /// <summary>
        /// Indicates whether this template is a reserved system participant.
        /// </summary>
        [NotMapped]
        public bool IsSystemAgent => IsHuman
            || string.Equals(Name, "PromptCatalyzer", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the reserved system participant kind, or <see langword="null"/> for ordinary agents.
        /// </summary>
        [NotMapped]
        public string SystemAgentKind => IsHuman
            ? HumanParticipantConstants.Name
            : string.Equals(Name, "PromptCatalyzer", StringComparison.OrdinalIgnoreCase)
                ? "PromptCatalyzer"
                : null;

        /// <summary>
        /// Deserializes the MCP endpoint name-to-configuration map.
        /// </summary>
        /// <returns>The endpoint configuration map, or an empty map for invalid JSON.</returns>
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
        /// Adds or replaces an MCP endpoint configuration.
        /// </summary>
        /// <param name="name">The endpoint name.</param>
        /// <param name="url">The endpoint URL.</param>
        /// <returns><see langword="true"/> when both values are non-empty and the endpoint is stored.</returns>
        public bool AddMcpEndpoint(string name, string url)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                return false;
            }

            var endpointsDict = GetMcpEndpointsDict();
            
            endpointsDict[name] = new Dictionary<string, string>
            {
                { "url", url }
            };

            McpEndpoints = JsonSerializer.Serialize(endpointsDict);
            return true;
        }

        /// <summary>
        /// Removes an MCP endpoint configuration by name.
        /// </summary>
        /// <param name="name">The endpoint name.</param>
        /// <returns><see langword="true"/> when an endpoint was removed.</returns>
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

            endpointsDict.Remove(name);

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

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private AgentTemplate() { }

        /// <summary>
        /// Initializes an agent template and its model-binding policy.
        /// </summary>
        /// <param name="name">The display name.</param>
        /// <param name="systemMessage">The system prompt or PromptRange reference.</param>
        /// <param name="enable">Whether the template is enabled.</param>
        /// <param name="description">The template description.</param>
        /// <param name="promptCode">The prompt code/reference.</param>
        /// <param name="hookRobotType">The hook/robot type.</param>
        /// <param name="hookRobotParameter">Hook-specific parameter.</param>
        /// <param name="avastar">Optional avatar.</param>
        /// <param name="functionCallNames">Optional function names.</param>
        /// <param name="mcpEndpoints">Optional MCP endpoint configuration.</param>
        /// <param name="knowledgeBaseId">Optional knowledge-base identifier.</param>
        /// <param name="modelBinding">The model binding mode.</param>
        /// <param name="aiModelId">The manually bound model identifier.</param>
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

        /// <summary>
        /// Enables the agent template.
        /// </summary>
        /// <returns><see langword="true"/> after the state is enabled.</returns>
        public bool EnableAgent()
        {
            Enable = true;
            return true;
        }

        /// <summary>
        /// Disables the agent template.
        /// </summary>
        /// <returns><see langword="true"/> after the state is disabled.</returns>
        public bool DisableAgent()
        {
            Enable = false;
            return true;
        }

        /// <summary>
        /// Replaces editable template fields from a DTO and reapplies model binding rules.
        /// </summary>
        /// <param name="agentTemplateDto">The source DTO.</param>
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

        /// <summary>
        /// Updates the model-binding mode and clears manual model identity unless manual binding is selected.
        /// </summary>
        /// <param name="modelBinding">The desired model-binding mode.</param>
        /// <param name="aiModelId">The model identifier used for manual binding.</param>
        public void UpdateModelBinding(AgentModelBindingMode modelBinding, int? aiModelId)
        {
            SetModelBinding(modelBinding, aiModelId);
        }

        /// <summary>
        /// Applies the model-binding mode and retains a manual model identifier only when
        /// <see cref="AgentModelBindingMode.ManualAiModel"/> is selected.
        /// </summary>
        /// <param name="modelBinding">The requested binding mode.</param>
        /// <param name="aiModelId">The manually selected model identifier.</param>
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
    /// Identifies an optional external robot hook used by an agent template.
    /// </summary>
    public enum AgentModelBindingMode
    {
        /// Uses no external robot hook.
        InheritPromptRange = 0,

        /// Uses a Weixin public-account hook.
        FollowGroupTask = 1,

        /// Uses a Weixin Work robot hook.
        ManualAiModel = 2
    }

    /// <summary>
    /// Identifies the optional external robot or model-binding mode used by an agent template.
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
