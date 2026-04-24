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
        [Description("名称||Agent 模板名称")]
        public string Name { get; set; }

        [Required]
        [Description("Id||如果为 0 则新增")]
        public int Id { get; set; }

        [Required]
        [Description("SystemMessage||SystemMessage 的 PromptRangeCode（支持搜索、下拉和手动输入）")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(SystemMessagePromptCodeOptions), Filterable = true, AllowCreate = true)]
        public string SystemMessagePromptCode { get; set; }

        [JsonIgnore]
        public SelectionList SystemMessagePromptCodeOptions { get; set; } = new SelectionList(SelectionType.DropDownList);



        [Description("说明||对 Agent Template 进行说明，此信息不会对模型效果产生影响")]
        public string Description { get; set; }

        [Required]
        [Description("外接平台||需要对外发布消息的平台")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(HookRobotTypeOptions))]
        public string HookRobotType { get; set; }

        [JsonIgnore]
        public SelectionList HookRobotTypeOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());
        //TODO:可以选择多个通道


        [Description("外界平台参数||通常为 Key 之类的参数")]
        public string HookRobotParameter { get; set; }

        [Description("Function Calls||Function Calls 名称列表，多个用逗号分隔")]
        public string FunctionCallNames { get; set; }

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
        [Description("智能体名称||新智能体的名称")]
        public string Name { get; set; }

        // [Required]
        // [Description("PromptCode 作用范围||选择覆盖范围：靶场名称（Range级别）：Range、靶道前缀（Tactic级别）：Tactic、或完整版本号（精确定位）：PromptCode，只能严格从 Range、Tactic、PromptCode 中选择")]
        // public string ScopeSelection { get; set; } 

        [Description("手动输入 PromptCode||手动输入 PromptCode（支持靶场名称、靶道前缀或完整版本号），当选择[手动输入 SystemMessage]时必须在此处输入")]
        public string ManualPromptCode { get; set; }

        [Description("说明||对新智能体的说明（可选）")]
        public string Description { get; set; }

        [Description("Function Calls||Function Calls 名称列表，多个用逗号分隔（可选）")]
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
        [Description("搜索词||支持名称、PromptCode 或关键字，可输入多个，使用逗号、分号、换行分隔")]
        public string Query { get; set; }

        [Description("最大返回数量||每个搜索词的最大候选数，默认 5")]
        public int TopN { get; set; } = 5;
    }
}
