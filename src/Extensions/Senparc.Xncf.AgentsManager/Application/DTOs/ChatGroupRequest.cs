/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupRequest.cs
    文件功能描述：ChatGroupRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        [Description("选择组||选择需要操作的组，或新增")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(ChatGroupOptions))]
        public string ChatGroup { get; set; }

        [JsonIgnore]
        public SelectionList ChatGroupOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem> {
             new SelectionItem("New","新建组","新建",true)
            });

        [Required]
        [MaxLength(30)]
        [Description("群名称||群名称")]
        public string Name { get; set; }

        [Description("群成员||群成员（可不选，改用“群成员（手动输入）”）")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(MembersOptions))]
        public string[] Members { get; set; }

        [JsonIgnore]
        public SelectionList MembersOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [Description("群成员（手动输入）||可选。支持名称、ID、PromptCode，多个值可用逗号、分号、换行分隔")]
        public string MemberNamesOrIds { get; set; }

        [Description("群主||优先自动使用评分最高的“主持人”Agent；手动输入仅作兼容")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(AdminOptions))]
        public string Admin { get; set; }

        [JsonIgnore]
        public SelectionList AdminOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [Description("群主（手动输入）||可选。支持名称、ID 或 PromptCode")]
        public string AdminNameOrId { get; set; }

        [Description("对接人||优先自动使用评分最高的“主持人”Agent；手动输入仅作兼容")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(EnterAgentOptions))]
        public string EnterAgent { get; set; }

        [JsonIgnore]
        public SelectionList EnterAgentOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [Description("对接人（手动输入）||可选。支持名称、ID 或 PromptCode")]
        public string EnterAgentNameOrId { get; set; }


        [MaxLength(200)]
        [Description("说明||说明")]
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
        [Description("选择组||选择需要运行的组")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(ChatGroupsOptions))]
        public string[] ChatGroups { get; set; }

        [JsonIgnore]
        public SelectionList ChatGroupsOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [Description("AI 模型||请选择运行此程序的外围 AI 模型")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(AIModelOptions))]
        public string AIModel { get; set; }

        [JsonIgnore]
        public SelectionList AIModelOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>
        {
            //new SelectionItem("Default","系统默认","通过系统默认配置的固定 AI 模型信息",true)
        });

        [Description("个性化智能体||")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(IndividuationOptions))]
        public bool Individuation { get; set; } = true;

        [JsonIgnore]
        public SelectionList IndividuationOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>
        {
            new SelectionItem("1","是","采用个性化 AI 参数运行 Agent",true)
        });

        [Required]
        [MaxLength(500)]
        [Description("我能帮你做什么||说明需要 Agents 协助你完成的工作内容")]
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
        [Description("选择要删除的对话||选择需要删除的对话（包括所有消息和任务）")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(ChatGroupsOptions))]
        public string[] ChatGroups { get; set; }

        [JsonIgnore]
        public SelectionList ChatGroupsOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        [Description("确认删除||勾选此项以确认删除此对话及其所有数据")]
        public bool ConfirmDelete { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            // 加载所有可用的 ChatGroup
            var chatGroupService = serviceProvider.GetService<ChatGroupService>();
            var chatGroups = await chatGroupService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

            ChatGroupsOptions.Items = chatGroups.Select(z => new SelectionItem(
                z.Id.ToString(),
                z.Name,
                $"{z.Description} (创建时间: {z.AddTime:yyyy-MM-dd HH:mm})"
            )).ToList();

            await base.LoadData(serviceProvider);
        }
    }
}
