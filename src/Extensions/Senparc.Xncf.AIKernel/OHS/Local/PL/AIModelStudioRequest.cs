using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    public class AIModelStudioRequest_RunModelAsync : FunctionAppRequestBase
    {
        [Required]
        [Description("选择模型||")]//下拉列表
        public SelectionList Model { get; set; } = new SelectionList(SelectionType.CheckBoxList, new List<SelectionItem>());

        //TODO: 更多 AI 参数

        [Required]
        [MaxLength(1000)]
        [Description("提示词||")]
        public string Prompt { get; set; }

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            List<ModelItem> settings =
            [
                //从 appsettings.json 读取
                new("Default", Senparc.AI.Config.SenparcAiSetting, "appsettings.json"),
            ];

            if (Senparc.AI.Config.SenparcAiSetting is SenparcAiSetting aiSetting)
            {
                foreach (var item in aiSetting.Items ?? new System.Collections.Concurrent.ConcurrentDictionary<string, SenparcAiSetting>())
                {
                    settings.Add(new(item.Key, item.Value, "appsettings.json"));
                }
            }

            //从数据库中获取模型
            var aiModelService = serviceProvider.GetService<AIModelService>();
            var aiModels = await aiModelService.GetFullListAsync(z => z.Show, z => z.Alias, Ncf.Core.Enums.OrderingType.Ascending);
            foreach (var aiModel in aiModels)
            {
                var setting = aiModelService.BuildSenparcAiSetting(aiModelService.Mapper.Map<AIModelDto>(aiModel));
                settings.Add(new(aiModel.Alias, setting, "AIKernel 模块数据库"));
            }

            //集成模型参数
            settings.ForEach(z =>
            {
                var value = z.ModelAlias;
                var text = z.DisplayText;
                Model.Items.Add(new SelectionItem(value, text, $"来自：{z.From}", false) { BindData = z.SenparcAiSetting });
            });

            await base.LoadData(serviceProvider);
        }

        class ModelItem
        {

            public string ModelAlias { get; set; }
            public string From { get; set; }
            public string DisplayText => $"{ModelAlias} - {SenparcAiSetting.AiPlatform} ({SenparcAiSetting.Endpoint})";
            public ISenparcAiSetting SenparcAiSetting { get; set; }

            public ModelItem(string modelAlias, ISenparcAiSetting senparcAiSetting, string from)
            {
                ModelAlias = modelAlias;
                SenparcAiSetting = senparcAiSetting;
                From = from;
            }


        }
    }
}
