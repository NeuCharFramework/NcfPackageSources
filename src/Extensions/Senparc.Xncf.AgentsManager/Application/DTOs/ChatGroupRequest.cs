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
using System.Web.Mvc;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.AgentsManager.Application.Dtos
{
    public class ChatGroup_FindByNameRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Search.Query")]
        public string Query { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Search.TopN")]
        public int TopN { get; set; } = 5;
    }

    public class ChatGroup_ManageChatGroupRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.SelectManage")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(ChatGroupOptions), AllowCreate = true)]
        public string ChatGroup { get; set; }

        [JsonIgnore]
        public SelectionList ChatGroupOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem> {
             new SelectionItem("New", AgentsManagerResource.Get("Agents.Chat.NewGroup"), AgentsManagerResource.Get("Agents.Chat.NewGroup.Help"), true)
            });

        [Required]
        [MaxLength(30)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Name")]
        public string Name { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Members")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(MembersOptions))]
        public string[] Members { get; set; }

        [JsonIgnore]
        public SelectionList MembersOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.MembersManual")]
        public string MemberNamesOrIds { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Admin")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(AdminOptions))]
        public string Admin { get; set; }

        [JsonIgnore]
        public SelectionList AdminOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.AdminManual")]
        public string AdminNameOrId { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Contact")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(EnterAgentOptions))]
        public string EnterAgent { get; set; }

        [JsonIgnore]
        public SelectionList EnterAgentOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.ContactManual")]
        public string EnterAgentNameOrId { get; set; }


        [MaxLength(200)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Description")]
        public string Description { get; set; }

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

    public class ChatGroup_RunChatGroupRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.SelectRun")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(ChatGroupsOptions))]
        public string[] ChatGroups { get; set; }

        [JsonIgnore]
        public SelectionList ChatGroupsOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.AIModel")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(AIModelOptions))]
        public string AIModel { get; set; }

        [JsonIgnore]
        public SelectionList AIModelOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>
        {
            //Legacy comment translated to English.
        });

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Individuation")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(IndividuationOptions))]
        public bool Individuation { get; set; } = true;

        /// <summary>
        /// whether tool calls require human approval first。
        /// </summary>
        public bool RequireHumanApproval { get; set; }

        ///Legacy comment translated to English.
        public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; } = HumanInTheLoopLevel.Automatic;

        public ToolPermissionMode PluginToolPermission { get; set; } = ToolPermissionMode.Inherit;

        public ToolPermissionMode McpToolPermission { get; set; } = ToolPermissionMode.Inherit;

        public bool IncludeHumanParticipant { get; set; }

        [JsonIgnore]
        public SelectionList IndividuationOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>
        {
            new SelectionItem("1", AgentsManagerResource.Get("Common.Yes"), AgentsManagerResource.Get("Agents.Chat.Individuation.Help"), true)
        });

        [Required]
        [MaxLength(500)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Command")]
        public string Command { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //ChatGroup
            var chatGroupService = serviceProvider.GetService<ServiceBase<ChatGroup>>();
            var chatGroups = await chatGroupService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);

            ChatGroupsOptions.Items = chatGroups.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();

            //Legacy comment translated to English.
            await BuildXncfRequestHelper.LoadAiModelData(serviceProvider, AIModelOptions);

            await base.LoadData(serviceProvider);
        }
    }

    public class ChatGroup_RunGroupRequest 
    {
        /// <summary>
        /// task name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ChatGroup ID
        /// </summary>
        public int ChatGroupId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int AiModelId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string PromptCommand { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Personality { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public HookPlatform HookPlatform { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string HookParameter { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int ChatMaxRound { get; set; } = ChatGroupService.DefaultChatMaxRound;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public bool RequireHumanApproval { get; set; }

        ///Legacy comment translated to English.
        public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; } = HumanInTheLoopLevel.Automatic;

        ///Legacy comment translated to English.
        public ToolPermissionMode PluginToolPermission { get; set; } = ToolPermissionMode.Inherit;

        ///Legacy comment translated to English.
        public ToolPermissionMode McpToolPermission { get; set; } = ToolPermissionMode.Inherit;

        ///Legacy comment translated to English.
        public bool IncludeHumanParticipant { get; set; }

        ///Legacy comment translated to English.
        public bool LimitConnectors { get; set; }

        ///Legacy comment translated to English.
        public List<string> ConnectorInstanceIds { get; set; }

        ///Legacy comment translated to English.
        [JsonIgnore]
        public string HumanRecipientUserId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [JsonIgnore]
        public System.Threading.CancellationToken CancellationToken { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class ChatGroup_DeleteChatGroupRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.SelectDelete")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(ChatGroupsOptions))]
        public string[] ChatGroups { get; set; }

        [JsonIgnore]
        public SelectionList ChatGroupsOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.ConfirmDelete")]
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
