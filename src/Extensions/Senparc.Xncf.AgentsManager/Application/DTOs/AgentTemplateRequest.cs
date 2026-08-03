/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentTemplateRequest.cs
    文件功能描述：AgentTemplateRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.12.0-preview6 为 AgentsManager 模块接入统一资源本地化并优化功能文案

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

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
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
        //TODO:可以选择多个通道


        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.PlatformParameter")]
        public string HookRobotParameter { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Template.FunctionCalls")]
        public string FunctionCallNames { get; set; }

        public int? KnowledgeBaseId { get; set; }

        public string GetSystemMessagePromptCode()
        {
            return SystemMessagePromptCode?.Trim();
        }

        public string GetySystemMessagePromptCode()
        {
            return GetSystemMessagePromptCode();
        }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            await base.LoadData(serviceProvider);

            //HootRobotType 枚举
            var hookRobotTypeItems = Enum.GetValues<HookRobotType>();
            foreach (var item in hookRobotTypeItems)
            {
                HookRobotTypeOptions.Items.Add(new SelectionItem(((int)item).ToString(), item.ToString(), item.ToString(), item == Models.DatabaseModel.HookRobotType.None));
            }

            await PromptRangeItemHelper.LoadPromptRangeItemSelection(serviceProvider, SystemMessagePromptCodeOptions);
        }


    }

    /// <summary>
    /// 从 PromptCode 快速创建智能体的请求
    /// </summary>
    public class AgentTemplate_CreateFromPromptCodeRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Create.Name")]
        public string Name { get; set; }

        // [Required]
        // [Description("PromptCode 作用范围||选择覆盖范围：靶场名称（Range级别）：Range、靶道前缀（Tactic级别）：Tactic、或完整版本号（精确定位）：PromptCode，只能严格从 Range、Tactic、PromptCode 中选择")]
        // public string ScopeSelection { get; set; } 

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Create.ManualPromptCode")]
        public string ManualPromptCode { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Create.Description")]
        public string Description { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Create.FunctionCalls")]
        public string FunctionCallNames { get; set; }

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

    /// <summary>
    /// 搜索 AgentTemplate 并返回可用 ID 的请求
    /// </summary>
    public class AgentTemplate_FindByNameRequest : FunctionAppRequestBase
    {
        [Required]
        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Search.Query")]
        public string Query { get; set; }

        [LocalizedDescription(typeof(AgentsManagerResource), "Parameter.Agents.Search.TopN")]
        public int TopN { get; set; } = 5;
    }
}
