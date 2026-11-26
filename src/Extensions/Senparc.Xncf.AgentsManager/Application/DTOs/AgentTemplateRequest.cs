/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentTemplateRequest.cs
    File description: AgentTemplateRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.12.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.14.0-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20261009
    Change description: v0.18.3 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.19.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 enhance Agent Studio, group configuration, and task management interactions

    Modified by: Senparc - 20261111
    Change description: v0.19.3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using log4net.Core;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Extensions;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.AgentsManager.Application.Dtos
{
    /// <summary>Requests conversion of plain prompt text into an Agent prompt item.</summary>
    public class AgentTemplate_ConvertPlainPromptRequest
    {
        [Required]
        public int AgentTemplateId { get; set; }

        [Required]
        [MaxLength(50000)]
        public string PromptContent { get; set; }

        [Required]
        [MaxLength(5000)]
        public string TestInput { get; set; }

        [Required]
        [MaxLength(5000)]
        public string ExpectedResult { get; set; }

        [Required]
        public int AiModelId { get; set; }
    }

    /// <summary>Reports the generated prompt item and evaluation result.</summary>
    public class AgentTemplate_ConvertPlainPromptResponse
    {
        /// <summary>Gets or sets the generated PromptRange code.</summary>
        public string PromptCode { get; set; }
        /// <summary>Gets or sets the generated prompt-item version.</summary>
        public string PromptItemVersion { get; set; }
        /// <summary>Gets or sets the generated prompt-result identifier.</summary>
        public int PromptResultId { get; set; }
        /// <summary>Gets or sets the evaluation score.</summary>
        public decimal Score { get; set; }
        /// <summary>Gets or sets the generated result text.</summary>
        public string ResultString { get; set; }
    }

    /// <summary>Provides Agent-template management inputs and FunctionRender selection data.</summary>
    public class AgentTemplate_ManageRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(30)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.Id")]
        public int Id { get; set; }

        [Required]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.SystemMessage")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(SystemMessagePromptCodeOptions), Filterable = true, AllowCreate = true)]
        public string SystemMessagePromptCode { get; set; }

        [JsonIgnore]
        public SelectionList SystemMessagePromptCodeOptions { get; set; } = new SelectionList(SelectionType.DropDownList);



        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.Description")]
        public string Description { get; set; }

        [Required]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.Platform")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(HookRobotTypeOptions))]
        public string HookRobotType { get; set; }

        [JsonIgnore]
        public SelectionList HookRobotTypeOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());
        // Bindings are optional; an empty collection keeps the template's explicit
        // tool and workspace policy unchanged.


        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.PlatformParameter")]
        public string HookRobotParameter { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.FunctionCalls")]
        public string FunctionCallNames { get; set; }

        public int? KnowledgeBaseId { get; set; }

        /// <summary>Gets or sets skills explicitly bound to the Agent.</summary>
        public List<AgentSkillBinding> SkillBindings { get; set; } = new();

        /// <summary>Gets or sets workspace access policy for the Agent.</summary>
        public WorkspacePolicy WorkspacePolicy { get; set; } = new();

        /// <summary>Gets or sets connector bindings available to the Agent.</summary>
        public List<AgentConnectorBinding> ConnectorBindings { get; set; }

        /// <summary>Returns the trimmed PromptRange code selected for the system message.</summary>
        public string GetSystemMessagePromptCode()
        {
            return SystemMessagePromptCode?.Trim();
        }

        /// <summary>Compatibility alias for <see cref="GetSystemMessagePromptCode"/>.</summary>
        public string GetySystemMessagePromptCode()
        {
            return GetSystemMessagePromptCode();
        }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            await base.LoadData(serviceProvider);

            // Populate both the hook-platform and PromptRange selectors before
            // the Function request is rendered.
            var hookRobotTypeItems = Enum.GetValues<HookRobotType>();
            foreach (var item in hookRobotTypeItems)
            {
                HookRobotTypeOptions.Items.Add(new SelectionItem(((int)item).ToString(), item.ToString(), item.ToString(), item == Models.DatabaseModel.HookRobotType.None));
            }

            await PromptRangeItemHelper.LoadPromptRangeItemSelection(serviceProvider, SystemMessagePromptCodeOptions);
        }


    }

    /// <summary>Requests creation of an Agent template from a PromptRange code.</summary>
    public class AgentTemplate_CreateFromPromptCodeRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Create.Name")]
        public string Name { get; set; }

        // [Required]
        //Legacy comment translated to English.
        // public string ScopeSelection { get; set; } 

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Create.ManualPromptCode")]
        public string ManualPromptCode { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Create.Description")]
        public string Description { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Create.FunctionCalls")]
        public string FunctionCallNames { get; set; }

        /// <summary>Returns the manually supplied PromptRange code.</summary>
        public string GetPromptCode()
        {
            // if (!string.IsNullOrEmpty(ScopeSelection))
            // {
            //     return ScopeSelection;
            // }
            return ManualPromptCode;
        }

        // public override async Task LoadData(IServiceProvider serviceProvider)
        // {
        //     await base.LoadData(serviceProvider);

        //     await PromptRangeItemHelper.LoadPromptRangeItemSelection(serviceProvider, ScopeSelection);
        // }
    }

    /// <summary>Requests a bounded Agent-template name search.</summary>
    public class AgentTemplate_FindByNameRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(500)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Search.Query")]
        public string Query { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Search.TopN")]
        public int TopN { get; set; } = 5;
    }
}
