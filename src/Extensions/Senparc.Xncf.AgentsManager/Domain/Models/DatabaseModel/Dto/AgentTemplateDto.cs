/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentTemplateDto.cs
    File description: AgentTemplateDto related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.14.0-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

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

using Microsoft.Identity.Client;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.Workspace.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AgentTemplateDto : DtoBase<int>
    {
        /// <summary>
        /// name
        /// </summary>
        public string Name { get;  set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string SystemMessage { get;  set; }

        /// <summary>
        /// whether enabled
        /// </summary>
        public bool Enable { get;  set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get;  set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string PromptCode { get;  set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public HookRobotType HookRobotType { get;  set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string HookRobotParameter { get; set; }

        public string Avastar { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string FunctionCallNames { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<AgentFunctionBindingDto> FunctionBindings { get; set; } = new();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string McpEndpoints { get; set; }

        public int? KnowledgeBaseId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AgentModelBindingMode ModelBinding { get; set; } = AgentModelBindingMode.InheritPromptRange;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int? AiModelId { get; set; }

        public string SkillBindingsJson { get; set; }

        public string WorkspacePolicyJson { get; set; }

        public string ConnectorBindingsJson { get; set; }

        public List<AgentSkillBinding> SkillBindings
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SkillBindingsJson))
                {
                    return new List<AgentSkillBinding>();
                }

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<AgentSkillBinding>>(SkillBindingsJson) ?? new();
                }
                catch (System.Text.Json.JsonException)
                {
                    return new List<AgentSkillBinding>();
                }
            }
            set => SkillBindingsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new());
        }

        public WorkspacePolicy WorkspacePolicy
        {
            get => string.IsNullOrWhiteSpace(WorkspacePolicyJson)
                ? new WorkspacePolicy()
                : System.Text.Json.JsonSerializer.Deserialize<WorkspacePolicy>(WorkspacePolicyJson) ?? new WorkspacePolicy();
            set => WorkspacePolicyJson = System.Text.Json.JsonSerializer.Serialize(value ?? new WorkspacePolicy());
        }

        public List<AgentConnectorBinding> ConnectorBindings
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ConnectorBindingsJson))
                {
                    return new List<AgentConnectorBinding>();
                }

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<AgentConnectorBinding>>(ConnectorBindingsJson) ?? new();
                }
                catch (System.Text.Json.JsonException)
                {
                    return new List<AgentConnectorBinding>();
                }
            }
            set => ConnectorBindingsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new());
        }

        ///Legacy comment translated to English.
        public bool IsHuman { get; set; }

        ///Legacy comment translated to English.
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
        /// <summary>plugin、function or workflow。</summary>
        public string Kind { get; set; }

        ///Legacy comment translated to English.
        public string Key { get; set; }

        ///Legacy comment translated to English.
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
