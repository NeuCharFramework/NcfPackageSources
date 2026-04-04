using log4net.Core;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
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
        [Description("SystemMessage||SystemMessage 的 PromptRangeCode（支持自选模式）")]
        public SelectionList SystemMessagePromptCodeSelection { get; set; } = new SelectionList(SelectionType.DropDownList);

        [Description("手动输入 SystemMessage||SystemMessage 的 PromptRangeCode（支持自选模式），当 SystemMessage 选择“手动输入 SystemMessage”时必须在此处输入")]
        public string SystemMessagePromptCode { get; set; }



        [Description("说明||对 Agent Template 进行说明，此信息不会对模型效果产生影响")]
        public string Description { get; set; }

        [Required]
        [Description("外接平台||需要对外发布消息的平台")]
        public SelectionList HookRobotType { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());
        //TODO:可以选择多个通道


        [Description("外界平台参数||通常为 Key 之类的参数")]
        public string HookRobotParameter { get; set; }

        [Description("Function Calls||Function Calls 名称列表，多个用逗号分隔")]
        public string FunctionCallNames { get; set; }

        public string GetySystemMessagePromptCode()
        {
            var selectionValue = SystemMessagePromptCodeSelection.SelectedValues.FirstOrDefault();
            if (selectionValue != "0")
            {
                return selectionValue;
            }
            else
            {
                return SystemMessagePromptCode;
            }

        }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            await base.LoadData(serviceProvider);

            //HootRobotType 枚举
            var hookRobotTypeItems = Enum.GetValues<HookRobotType>();
            foreach (var item in hookRobotTypeItems)
            {
                HookRobotType.Items.Add(new SelectionItem(((int)item).ToString(), item.ToString(), item.ToString(), false));
            }

            await PromptRangeItemHelper.LoadPromptRangeItemSelection(serviceProvider, SystemMessagePromptCodeSelection);
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

        [Required]
        [Description("PromptCode 作用范围||选择覆盖范围：靶场名称（Range级别）、靶道前缀（Tactic级别）或完整版本号（精确定位）")]
        public SelectionList ScopeSelection { get; set; } = new SelectionList(SelectionType.DropDownList);

        [Description("手动输入 PromptCode||手动输入 PromptCode（支持靶场名称、靶道前缀或完整版本号），当选择[手动输入 SystemMessage]时必须在此处输入")]
        public string ManualPromptCode { get; set; }

        [Description("说明||对新智能体的说明（可选）")]
        public string Description { get; set; }

        [Description("Function Calls||Function Calls 名称列表，多个用逗号分隔（可选）")]
        public string FunctionCallNames { get; set; }

        public string GetPromptCode()
        {
            var selectionValue = ScopeSelection.SelectedValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(selectionValue) && selectionValue != "0")
            {
                return selectionValue;
            }
            return ManualPromptCode;
        }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            await base.LoadData(serviceProvider);

            await PromptRangeItemHelper.LoadPromptRangeItemSelection(serviceProvider, ScopeSelection);
        }
    }
}
