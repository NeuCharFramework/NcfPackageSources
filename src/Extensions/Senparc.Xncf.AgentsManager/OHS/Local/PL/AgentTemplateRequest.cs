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

            //载入 PromptRange
            SystemMessagePromptCodeSelection.Items.Add(new SelectionItem()
            {
                Value = "0",
                Text = "手动输入 SystemMessage",
            });

            var promptRangeService = serviceProvider.GetService<PromptRangeService>();
            var promptItemService = serviceProvider.GetService<PromptItemService>();
            var allPromptRanges = await promptRangeService.GetFullListAsync(z => true, z => z.Id, OrderingType.Ascending);

            //获取柱状结构前缀
            Func<int, string> GetPrefix = currentLevel => string.Concat(Enumerable.Repeat("┣  ", currentLevel));

            //读取评分
            Func<decimal, string> GetScore = score => score < 0 ? "-" : score.ToString();

            foreach (var promptRange in allPromptRanges)
            {
                //获取树状结构

                var rangeTree = await promptItemService.GenerateTacticTreeAsync(promptRange.RangeName);

                if (rangeTree.Count == 0)
                {
                    continue;
                }

                //正在开始一个新的 PromptRange，插入这个 Prompt的整体引导
                SystemMessagePromptCodeSelection.Items.Add(new SelectionItem()
                {
                    Text = "▽ " + $"{promptRange.RangeName}（{promptRange.GetAvailableName()}）",
                    Value = promptRange.RangeName
                });

                var lastTactic = string.Empty;
                var lastAim = 0;
                var level = 0;

                foreach (var treeNote in rangeTree)
                {
                    // Traverse the rangeTree and its children
                    TraversePromptItem(treeNote);
                }


                void TraversePromptItem(TreeNode<PromptItem_GetIdAndNameResponse> treeNote)
                {
                    var versionObj = promptItemService.GetVersionObject(treeNote.Data.FullVersion);

                    var newLevel = lastTactic != versionObj.Tactic && versionObj.Tactic.Contains(lastTactic);
                    if (newLevel)
                    {
                        level++;//进入下一层，如从 T1 进入到 T1.1

                        //正在开始一个新的 Tactic，插入这个 Tactic
                        var partPromptCode = $"{versionObj.RangeName}-T{versionObj.Tactic}";
                        SystemMessagePromptCodeSelection.Items.Add(new SelectionItem()
                        {
                            Text = GetPrefix(level) + " ▽ " + $"{partPromptCode}",
                            Value = partPromptCode
                        });
                    }

                    lastTactic = versionObj.Tactic;
                    lastAim = versionObj.Aim;

                    var nickName = treeNote.NickName.IsNullOrEmpty() ? "" : $"{treeNote.NickName}，";
                    SystemMessagePromptCodeSelection.Items.Add(new SelectionItem()
                    {
                        Text = GetPrefix(level + 1) + $" {treeNote.Name}({nickName}AvgScore:{GetScore(treeNote.Data.EvalAvgScore)}，MaxScore:{GetScore(treeNote.Data.EvalMaxScore)})：{treeNote.Data.PromptContent.SubString(0, 30)}",
                        Value = treeNote.Data.FullVersion
                    });

                    foreach (var child in treeNote.Children)
                    {
                        TraversePromptItem(child);
                    }

                    if (newLevel)
                    {
                        level--;
                    }
                }
            }
        }
    }
}
