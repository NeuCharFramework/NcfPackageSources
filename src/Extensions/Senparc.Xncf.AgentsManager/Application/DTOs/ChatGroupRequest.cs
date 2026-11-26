/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupRequest.cs
    File description: ChatGroupRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.12.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.15.0-preview20 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261009
    Change description: v0.18.3 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 enhance Agent Studio, group configuration, and task management interactions

    Modified by: Senparc - 20261111
    Change description: v0.19.3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.AgentsManager.Application.Dtos
{
    /// <summary>Minimal, stable request used by the AdminChat Agent-group command.</summary>
    public class ChatGroup_ChatToolRunRequest : FunctionAppRequestBase
    {
        [Required]
        public int ChatGroupId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Command { get; set; }
    }

    /// <summary>Requests a bounded search for enabled Agent groups.</summary>
    public class ChatGroup_FindByNameRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Search.Query")]
        /// <summary>Gets or sets the case-insensitive text used to search enabled groups.</summary>
        public string Query { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Search.TopN")]
        /// <summary>Gets or sets the maximum number of matching groups to return.</summary>
        public int TopN { get; set; } = 5;
    }

    /// <summary>Provides group identity, role, member, and policy inputs for group management.</summary>
    public class ChatGroup_ManageChatGroupRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.SelectManage")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(ChatGroupOptions), AllowCreate = true)]
        /// <summary>Gets or sets the existing group identifier or the create-new sentinel.</summary>
        public string ChatGroup { get; set; }

        [JsonIgnore]
        /// <summary>Gets the UI options used to select an existing group or create one.</summary>
        public SelectionList ChatGroupOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem> {
             new SelectionItem("New", AgentsManagerResource.Get("Agents.Chat.NewGroup"), AgentsManagerResource.Get("Agents.Chat.NewGroup.Help"), true)
            });

        [Required]
        [MaxLength(30)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Name")]
        /// <summary>Gets or sets the display name of the group being created or updated.</summary>
        public string Name { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Members")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(MembersOptions))]
        /// <summary>Gets or sets the selected Agent template identifiers.</summary>
        public string[] Members { get; set; }

        [JsonIgnore]
        /// <summary>Gets the selectable Agent template entries for group membership.</summary>
        public SelectionList MembersOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.MembersManual")]
        /// <summary>Gets or sets manually entered member names or identifiers.</summary>
        public string MemberNamesOrIds { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Admin")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(AdminOptions))]
        /// <summary>Gets or sets the selected administrator Agent identifier.</summary>
        public string Admin { get; set; }

        [JsonIgnore]
        /// <summary>Gets the selectable non-human Agent entries for the group administrator.</summary>
        public SelectionList AdminOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.AdminManual")]
        /// <summary>Gets or sets a manually entered administrator name or identifier.</summary>
        public string AdminNameOrId { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Contact")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(EnterAgentOptions))]
        /// <summary>Gets or sets the Agent selected as the group's entry contact.</summary>
        public string EnterAgent { get; set; }

        [JsonIgnore]
        /// <summary>Gets the selectable non-human Agent entries for the entry contact.</summary>
        public SelectionList EnterAgentOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.ContactManual")]
        /// <summary>Gets or sets a manually entered entry-contact name or identifier.</summary>
        public string EnterAgentNameOrId { get; set; }


        [MaxLength(200)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Description")]
        /// <summary>Gets or sets the administrator-facing group description.</summary>
        public string Description { get; set; }

        /// <inheritdoc />
        /// <remarks>Loads existing groups and eligible Agent templates into selection lists.</remarks>
        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //ChatGroup
            var chatGroupService = serviceProvider.GetService<ChatGroupService>();
            var chatGroups = await chatGroupService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);

            chatGroups.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description))
                .ToList().ForEach(z => ChatGroupOptions.Items.Add(z));

            //Agent
            var agentTemplateService = serviceProvider.GetService<AgentsTemplateService>();
            var agentsTemplates = await agentTemplateService.GetFullListAsync(z => z.Enable, z => z.Name, Ncf.Core.Enums.OrderingType.Ascending);
            var roleAgents = agentsTemplates.Where(z => !z.IsHuman).ToList();

            MembersOptions.Items = agentsTemplates.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();
            AdminOptions.Items = roleAgents.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();
            EnterAgentOptions.Items = roleAgents.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();

            var admin = AdminOptions.Items.FirstOrDefault(z => z.Text == "群主");
            if (admin != null)
            {
                admin.DefaultSelected = true;
            }

            await base.LoadData(serviceProvider);
        }
    }

    /// <summary>Provides selection, model, policy, and command inputs for running Agent groups.</summary>
    public class ChatGroup_RunChatGroupRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.SelectRun")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(ChatGroupsOptions))]
        /// <summary>Gets or sets the identifiers of groups to execute.</summary>
        public string[] ChatGroups { get; set; }

        [JsonIgnore]
        /// <summary>Gets the selectable enabled groups for the run operation.</summary>
        public SelectionList ChatGroupsOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.AIModel")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(AIModelOptions))]
        /// <summary>Gets or sets the AI model identifier selected for the run.</summary>
        public string AIModel { get; set; }

        [JsonIgnore]
        /// <summary>Gets the available AI model choices populated during request loading.</summary>
        public SelectionList AIModelOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>
        {
        });

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Individuation")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(IndividuationOptions))]
        /// <summary>Gets or sets whether the group uses individualized Agent behavior.</summary>
        public bool Individuation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether tool calls must wait for human approval before execution.
        /// </summary>
        public bool RequireHumanApproval { get; set; }

        /// <summary>Gets or sets the permitted level of human participation.</summary>
        public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; } = HumanInTheLoopLevel.Automatic;

        /// <summary>Gets or sets the plugin-tool permission mode for this run.</summary>
        public ToolPermissionMode PluginToolPermission { get; set; } = ToolPermissionMode.Inherit;

        /// <summary>Gets or sets the MCP-tool permission mode for this run.</summary>
        public ToolPermissionMode McpToolPermission { get; set; } = ToolPermissionMode.Inherit;

        /// <summary>Gets or sets whether a human participant is included in the run.</summary>
        public bool IncludeHumanParticipant { get; set; }

        [JsonIgnore]
        /// <summary>Gets the UI option used to enable individualized behavior.</summary>
        public SelectionList IndividuationOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>
        {
            new SelectionItem("1", AgentsManagerResource.Get("Common.Yes"), AgentsManagerResource.Get("Agents.Chat.Individuation.Help"), true)
        });

        [Required]
        [MaxLength(500)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Command")]
        /// <summary>Gets or sets the user command submitted to the selected groups.</summary>
        public string Command { get; set; }

        /// <inheritdoc />
        /// <remarks>Loads group and AI-model choices before the Function request is rendered.</remarks>
        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //ChatGroup
            var chatGroupService = serviceProvider.GetService<ServiceBase<ChatGroup>>();
            var chatGroups = await chatGroupService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);

            ChatGroupsOptions.Items = chatGroups.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();

            // Load the available AI models after the group choices have been prepared.
            await BuildXncfRequestHelper.LoadAiModelData(serviceProvider, AIModelOptions);

            await base.LoadData(serviceProvider);
        }
    }

    public class ChatGroup_RunGroupRequest 
    {
        /// <summary>
        /// Gets or sets the persisted task name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the group identifier to execute.
        /// </summary>
        public int ChatGroupId { get; set; }

        /// <summary>
        /// Gets or sets the selected AI model identifier.
        /// </summary>
        public int AiModelId { get; set; }

        /// <summary>
        /// Gets or sets the prompt or command passed to the group.
        /// </summary>
        public string PromptCommand { get; set; }

        /// <summary>
        /// Gets or sets the task description stored for later inspection.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets whether individualized responses are requested.
        /// </summary>
        public bool Personality { get; set; }

        /// <summary>
        /// Gets or sets the notification or callback platform.
        /// </summary>
        public HookPlatform HookPlatform { get; set; }
        /// <summary>
        /// Gets or sets the platform-specific callback parameter.
        /// </summary>
        public string HookParameter { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of conversation rounds.
        /// </summary>
        public int ChatMaxRound { get; set; } = ChatGroupService.DefaultChatMaxRound;

        /// <summary>
        /// Gets or sets the correlation identifier used to associate related runs.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets whether tool calls require human approval.
        /// </summary>
        public bool RequireHumanApproval { get; set; }

        /// <summary>Gets or sets the permitted level of human participation.</summary>
        public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; } = HumanInTheLoopLevel.Automatic;

        /// <summary>Gets or sets the plugin-tool permission mode.</summary>
        public ToolPermissionMode PluginToolPermission { get; set; } = ToolPermissionMode.Inherit;

        /// <summary>Gets or sets the MCP-tool permission mode.</summary>
        public ToolPermissionMode McpToolPermission { get; set; } = ToolPermissionMode.Inherit;

        /// <summary>Gets or sets whether a human participant is included.</summary>
        public bool IncludeHumanParticipant { get; set; }

        /// <summary>Gets or sets whether connector access is restricted to an allow-list.</summary>
        public bool LimitConnectors { get; set; }

        /// <summary>Gets or sets the connector instance identifiers allowed for the run.</summary>
        public List<string> ConnectorInstanceIds { get; set; }

        /// <summary>Gets or sets the internal human-recipient identifier.</summary>
        [JsonIgnore]
        public string HumanRecipientUserId { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token used by the running task.
        /// </summary>
        [JsonIgnore]
        public System.Threading.CancellationToken CancellationToken { get; set; }
    }

    /// <summary>
    /// Supplies the group selection and confirmation inputs for group deletion.
    /// </summary>
    public class ChatGroup_DeleteChatGroupRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.SelectDelete")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(ChatGroupsOptions))]
        /// <summary>Gets or sets the identifiers of groups to delete.</summary>
        public string[] ChatGroups { get; set; }

        [JsonIgnore]
        /// <summary>Gets the selectable groups available for deletion.</summary>
        public SelectionList ChatGroupsOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.ConfirmDelete")]
        /// <summary>Gets or sets whether the caller confirmed the destructive operation.</summary>
        public bool ConfirmDelete { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //Legacy comment translated to English.
            var chatGroupService = serviceProvider.GetService<ChatGroupService>();
            var chatGroups = await chatGroupService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

            ChatGroupsOptions.Items = chatGroups.Select(z => new SelectionItem(
                z.Id.ToString(),
                z.Name,
                AgentsManagerResource.Format("Agents.Chat.CreatedAt", "{0}（创建时间：{1:g}）", z.Description, z.AddTime)
            )).ToList();

            await base.LoadData(serviceProvider);
        }
    }
}
