using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.AI.Exceptions;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public static class BuildXncfRequestHelper
    {
        public static async Task LoadAiModelData(IServiceProvider serviceProvider, SelectionList aiModel)
        {
            var defaultSetting = Senparc.AI.Config.SenparcAiSetting;
            try
            {
                aiModel.Items.Add(new SelectionItem("Default", $"系统默认（AiPlatform：{defaultSetting.AiPlatform}，Endpoint：{defaultSetting.Endpoint}）", "通过系统默认配置的固定 AI 模型信息", true));
            }
            catch (SenparcAiException ex)
            {
                //Endpoint 可能未配置

                aiModel.Items.Add(new SelectionItem("Default", $"系统默认（AiPlatform：{defaultSetting.AiPlatform}，Endpoint：未检测到，如需选择此选项，请先在 appsettings.json 中完成模型配置", "通过系统默认配置的固定 AI 模型信息", true));
            }

            var aiModelAppService = serviceProvider.GetService<AIModelAppService>();
            var aiModels = await aiModelAppService.GetListAsync(new AIKernel.OHS.Local.PL.AIModel_GetListRequest() { Show = true });

            if (aiModels.Data != null)
            {
                foreach (var item in aiModels.Data)
                {
                    aiModel.Items.Add(new SelectionItem(item.Id.ToString(), $"{item.DeploymentName}({item.ModelId}) - {item.Endpoint}", item.Note));
                }
            }
        }
    }


    public class BuildXncf_CreateDatabaseEntityRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(250)]
        [Description("生成数据库实体要求||请输入尽量完整的需求，也可以指定所需要的属性及类型")]
        public string Requirement { get; set; }

        [Description("领域||指定需要生成到的领域")]
        public SelectionList InjectDomain { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [Description("后续操作||指定生成数据库实体后的后续操作")]
        public SelectionList MoreActions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("BuildDto","创建 DTO","创建 DTO 对象（已强制生成）",true),
                 new SelectionItem("BuildMigration","直接生成数据库迁移信息","使用 EF Core Migration 生成迁移信息（建议查看后进行）",true),
                 new SelectionItem("CreateRepository","创建 Repository","创建和实体匹配的 Repository",false),
                 new SelectionItem("CreateService","创建 Service","创建和实体匹配的 Service",false),
                 new SelectionItem("CreateAppService","创建 AppService","创建和实体匹配的 Service",false)
            });

        [Description("AI 模型||请选择生成代码所使用的 AI 模型")]
        public SelectionList AIModel { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>
        {
            //new SelectionItem("Default","系统默认","通过系统默认配置的固定 AI 模型信息",true)
        });

        [Description("使用数据库 Prompt||指定 Prompt 来源。如果选中，系统将自动安装 PromptRange 模块并初始化 Prompt，全程无需任何人为干预；如不选中此选项，请在运行项目下 Domain/PromptPlugins/ 文件夹下存放 XncfBuilderPlugin 文件夹及所有文件内容。")]
        public SelectionList UseDatabasePrompt { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","是","",true)
        });


        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //扫描当前解决方案包含的所有领域项目
            var newItems = FunctionHelper.LoadXncfProjects(true, "Senparc.Areas.Admin");
            newItems.ForEach(z => InjectDomain.Items.Add(z));

            //载入 AI 模型
            await BuildXncfRequestHelper.LoadAiModelData(serviceProvider, AIModel);

            await base.LoadData(serviceProvider);
        }
    }

    public class BuildXncf_InitPromptRequest : FunctionAppRequestBase
    {
        [Description("覆盖||如果记录已存在，则删除 XncfBuilderPlugin 靶场，使用官方版本重建")]
        public SelectionList Override { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","是","",false)
                });

        [Description("AI 模型||请选择新建的靶场（Range）中所有靶道（Tactics）使用的 AI 模型")]
        public SelectionList AIModel { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>
        {
            //new SelectionItem("Default","系统默认","通过系统默认配置的固定 AI 模型信息",true)
        });

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //载入 AI 模型
            await BuildXncfRequestHelper.LoadAiModelData(serviceProvider, AIModel);

            await base.LoadData(serviceProvider);
        }
    }

}
