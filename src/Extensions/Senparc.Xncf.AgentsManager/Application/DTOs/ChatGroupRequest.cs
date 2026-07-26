/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupRequest.cs
    文件功能描述：ChatGroupRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.12.0-preview6 为 AgentsManager 模块接入统一资源本地化并优化功能文案

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

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class ChatGroup_ManageChatGroupRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.SelectManage")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(ChatGroupOptions))]
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

            MembersOptions.Items = agentsTemplates.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();
            AdminOptions.Items = agentsTemplates.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();
            EnterAgentOptions.Items = agentsTemplates.Select(z => new SelectionItem(z.Id.ToString(), z.Name, z.Description)).ToList();

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
            //new SelectionItem("Default","系统默认","通过系统默认配置的固定 AI 模型信息",true)
        });

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Chat.Individuation")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(IndividuationOptions))]
        public bool Individuation { get; set; } = true;

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

            //载入 AI 模型
            await BuildXncfRequestHelper.LoadAiModelData(serviceProvider, AIModelOptions);

            await base.LoadData(serviceProvider);
        }
    }

    public class ChatGroup_RunGroupRequest 
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ChatGroup ID
        /// </summary>
        public int ChatGroupId { get; set; }

        /// <summary>
        /// 如果是 0 ，则使用系统默认配置
        /// </summary>
        public int AiModelId { get; set; }

        /// <summary>
        /// 发起对话的要求
        /// </summary>
        public string PromptCommand { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 使用个性化智能体
        /// </summary>
        public bool Personality { get; set; }

        /// <summary>
        /// 消息平台
        /// </summary>
        public HookPlatform HookPlatform { get; set; }
        /// <summary>
        /// 消息平台参数
        /// </summary>
        public string HookParameter { get; set; }

        /// <summary>
        /// 最大对话轮数
        /// </summary>
        public int ChatMaxRound { get; set; } = ChatGroupService.ChatMaxRound;

        /// <summary>
        /// 可选：业务关联 ID（例如 Prompt 优化的 RequestId），用于在执行上下文中关联工具调用
        /// </summary>
        public string CorrelationId { get; set; }
    }

    /// <summary>
    /// 删除对话请求
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
            // 加载所有可用的 ChatGroup
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
