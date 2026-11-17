/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BuildXncfRequest.AI.cs
    File description: BuildXncfRequest.AI related implementation

    Created by: Senparc - 20240514

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.AI.Exceptions;
using Senparc.Xncf.AIKernel.Models;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public static class BuildXncfRequestHelper
    {
        public static async Task LoadAiModelData(IServiceProvider serviceProvider, SelectionList aiModel)
        {
            var defaultSetting = Senparc.AI.Config.SenparcAiSetting;
            try
            {
                aiModel.Items.Add(new SelectionItem(
                    "Default",
                    XncfBuilderResource.Format("XncfBuilder.AI.DefaultModel", "系统默认（AiPlatform：{0}，Endpoint：{1}）", defaultSetting.AiPlatform, defaultSetting.Endpoint),
                    XncfBuilderResource.Get("XncfBuilder.AI.DefaultModel.Help"),
                    true));
            }
            catch (SenparcAiException)
            {
                //Legacy comment translated to English.

                aiModel.Items.Add(new SelectionItem(
                    "Default",
                    XncfBuilderResource.Format("XncfBuilder.AI.DefaultModel.Unconfigured", "系统默认（AiPlatform：{0}，未检测到 Endpoint；请先在 appsettings.json 中配置模型）", defaultSetting.AiPlatform),
                    XncfBuilderResource.Get("XncfBuilder.AI.DefaultModel.Help"),
                    true));
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
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Entity.Requirement")]
        public string Requirement { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Entity.Domain")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(InjectDomainOptions))]
        public string InjectDomain { get; set; }

        [JsonIgnore]
        public SelectionList InjectDomainOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Entity.Actions")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(MoreActionsOptions))]
        public string[] MoreActions { get; set; }

        [JsonIgnore]
        public SelectionList MoreActionsOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("BuildDto", XncfBuilderResource.Get("XncfBuilder.Option.Action.Dto"), XncfBuilderResource.Get("XncfBuilder.Option.Action.Dto.Help"), true),
                 new SelectionItem("BuildMigration", XncfBuilderResource.Get("XncfBuilder.Option.Action.Migration"), XncfBuilderResource.Get("XncfBuilder.Option.Action.Migration.Help"), true),
                 new SelectionItem("CreateRepository", XncfBuilderResource.Get("XncfBuilder.Option.Action.Repository"), XncfBuilderResource.Get("XncfBuilder.Option.Action.Repository.Help"), false),
                 new SelectionItem("CreateService", XncfBuilderResource.Get("XncfBuilder.Option.Action.Service"), XncfBuilderResource.Get("XncfBuilder.Option.Action.Service.Help"), false),
                 new SelectionItem("CreateAppService", XncfBuilderResource.Get("XncfBuilder.Option.Action.AppService"), XncfBuilderResource.Get("XncfBuilder.Option.Action.AppService.Help"), false)
            });

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Entity.UsePromptRange")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(UseDatabasePromptOptions))]
        public bool UseDatabasePrompt { get; set; } = true;

        [JsonIgnore]
        public SelectionList UseDatabasePromptOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1", XncfBuilderResource.Get("Common.Yes"), "", true)
        });

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Entity.AIModel")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(AIModelOptions))]
        public string AIModel { get; set; }

        [JsonIgnore]
        public SelectionList AIModelOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>
        {
            //Legacy comment translated to English.
        });

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //Legacy comment translated to English.
            var newItems = FunctionHelper.LoadXncfProjects(true, null,"Senparc.Areas.Admin");
            newItems.ForEach(z => InjectDomainOptions.Items.Add(z));

            //Legacy comment translated to English.
            await BuildXncfRequestHelper.LoadAiModelData(serviceProvider, AIModelOptions);

            await base.LoadData(serviceProvider);
        }
    }

    public class BuildXncf_InitPromptRequest : FunctionAppRequestBase
    {
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Prompt.Override")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(OverrideOptions))]
        public bool Override { get; set; }

        [JsonIgnore]
        public SelectionList OverrideOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1", XncfBuilderResource.Get("Common.Yes"), "", false)
                });

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Prompt.AIModel")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(AIModelOptions))]
        public string AIModel { get; set; }

        [JsonIgnore]
        public SelectionList AIModelOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>
        {
            //Legacy comment translated to English.
        });

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            //Legacy comment translated to English.
            await BuildXncfRequestHelper.LoadAiModelData(serviceProvider, AIModelOptions);

            await base.LoadData(serviceProvider);
        }
    }

    //public class BuildXncf_CreateAppServiceRequest : FunctionAppRequestBase
    //{
    //Legacy comment translated to English.
    //    public SelectionList InjectDomain { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

    //    [Required]
    //    [MaxLength(250)]
    //Legacy comment translated to English.
    //    public string Requirement { get; set; }

    //    public override async Task LoadData(IServiceProvider serviceProvider)
    //    {
    //Legacy comment translated to English.
    //        var newItems = FunctionHelper.LoadXncfProjects(true, "Senparc.Areas.Admin");
    //        newItems.ForEach(z => InjectDomain.Items.Add(z));

    //        await base.LoadData(serviceProvider);
    //    }
    //}

}
