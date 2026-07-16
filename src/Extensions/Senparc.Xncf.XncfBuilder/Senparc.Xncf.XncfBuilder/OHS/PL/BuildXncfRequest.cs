/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BuildXncfRequest.cs
    文件功能描述：BuildXncfRequest 相关实现
    
    
    创建标识：Senparc - 20211016
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class BuildXncf_BuildRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(250)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.SolutionPath")]
        public string SlnFilePath { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.SolutionOptions")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(NewSlnFileOptions))]
        public string[] NewSlnFile { get; set; }

        [JsonIgnore]
        public SelectionList NewSlnFileOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("backup", XncfBuilderResource.Get("XncfBuilder.Option.Solution.Backup"), XncfBuilderResource.Get("XncfBuilder.Option.Solution.Backup.Help"), true),
                 new SelectionItem("new", XncfBuilderResource.Get("XncfBuilder.Option.Solution.New"), XncfBuilderResource.Get("XncfBuilder.Option.Solution.New.Help"), false),
            });

        [MaxLength(250)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.TemplatePackage")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(TemplatePackageOptions))]
        public string TemplatePackage { get; set; }

        [JsonIgnore]
        public SelectionList TemplatePackageOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("online", XncfBuilderResource.Get("XncfBuilder.Option.Template.Online"), XncfBuilderResource.Get("XncfBuilder.Option.Template.Online.Help"), false),
                 new SelectionItem("local", XncfBuilderResource.Get("XncfBuilder.Option.Template.Local"), XncfBuilderResource.Get("XncfBuilder.Option.Template.Local.Help"), false),
                 new SelectionItem("no", XncfBuilderResource.Get("XncfBuilder.Option.Template.Installed"), XncfBuilderResource.Get("XncfBuilder.Option.Template.Installed.Help"), true),
            });

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Framework")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(FrameworkVersionOptions))]
        public string FrameworkVersion { get; set; }

        [JsonIgnore]
        public SelectionList FrameworkVersionOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 //new SelectionItem("netstandard2.1","netstandard2.1","使用 .NET Standard 2.1（兼容 .NET Core 3.1 和 .NET 5.0-8.0）",true),
                 //new SelectionItem("netcoreapp3.1","netcoreapp3.1","使用 .NET Core 3.1",false),
                 //new SelectionItem("net6.0","net6.0","使用 .NET 6.0",false),
                 //new SelectionItem("net7.0","net7.0","使用 .NET 7.0",false),
                 new SelectionItem("net8.0", "net8.0", XncfBuilderResource.Get("XncfBuilder.Option.Framework.Net8"), false),
            });

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.OtherFramework")]
        public string OtherFrameworkVersion { get; set; }

        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Organization")]
        public string OrgName { get; set; }

        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.ModuleName")]
        public string XncfName { get; set; }

        //[Required]
        //[MaxLength(36)]
        //[Description("Uid||必须确保全局唯一，生成后必须固定")]
        //public string Uid { get; set; }

        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Version")]
        public string Version { get; set; }

        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.MenuName")]
        public string MenuName { get; set; }

        [Required]
        [MaxLength(50)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Icon")]
        public string Icon { get; set; }

        [Required]
        [MaxLength(400)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Description")]
        public string Description { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Features")]
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(UseModuleOptions))]
           public string[] UseModule { get; set; }

           [JsonIgnore]
           public SelectionList UseModuleOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("function", XncfBuilderResource.Get("XncfBuilder.Option.Feature.Function"), XncfBuilderResource.Get("XncfBuilder.Option.Feature.Function.Help"), false),
                 new SelectionItem("database", XncfBuilderResource.Get("XncfBuilder.Option.Feature.Database"), XncfBuilderResource.Get("XncfBuilder.Option.Feature.Database.Help"), false),
                 new SelectionItem("webapi", XncfBuilderResource.Get("XncfBuilder.Option.Feature.WebApi"), XncfBuilderResource.Get("XncfBuilder.Option.Feature.WebApi.Help"), false),
                 new SelectionItem("web", XncfBuilderResource.Get("XncfBuilder.Option.Feature.Web"), XncfBuilderResource.Get("XncfBuilder.Option.Feature.Web.Help"), false),
            });

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.InstallSample")]
           [FunctionParameterUi(ParameterType.CheckBoxList, nameof(UseSammpleOptions))]
           public bool UseSammple { get; set; }

           [JsonIgnore]
           public SelectionList UseSammpleOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1", XncfBuilderResource.Get("Common.Yes"), XncfBuilderResource.Get("XncfBuilder.Option.InstallSample.Help"), false),
            });

        /// <summary>
        /// 预载入数据
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            Config config = null;
            try
            {
                //低版本没有数据库，此处需要try
                var configService = serviceProvider.GetService<ServiceBase<Config>>();
                config = await configService.GetObjectAsync(z => true);
                if (config != null)
                {
                    #region 自动载入上次配置

                    //SenparcTrace.SendCustomLog("Xncf Builder Config", config.ToJson(true));

                    //configService.Mapper.Map(config, this);

                    //SenparcTrace.SendCustomLog("Xncf Builder Config - 2", this.ToJson(true));

                    SlnFilePath = config.SlnFilePath;
                    XncfName = config.XncfName;
                    MenuName = config.MenuName;
                    Description = config.Description;
                    OrgName = config.OrgName;
                    Version = config.Version;
                    Icon = config.Icon;

                    #endregion
                }
                else
                {
                    #region 自动查找当前项目的解决方案路径

                    SlnFilePath = this.GetSlnFilePath();

                    #endregion
                }
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                SenparcTrace.SendCustomLog("Xncf Builder Config - Ex", ex.Message + "///" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 判断当前路径下是否包含 .sln 文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsSlnDir(string path)
        {
            return Directory.GetFiles(path, "*.sln").Length > 0;
        }

        /// <summary>
        /// 获取当前解决方案文件路径
        /// </summary>
        /// <returns></returns>
        public string GetSlnFilePath()
        {
            var slnFilePath = string.Empty;
            //当前程序目录
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;

            //向上查找，直到找到
            while (!IsSlnDir(currentDir) && currentDir != null)
            {
                currentDir = Directory.GetParent(currentDir).FullName;
            }

            if (currentDir != null)
            {
                var fileNames = Directory.GetFiles(currentDir, "*.sln").OrderBy(z => z.Length).ThenBy(z => z);
                slnFilePath = Path.Combine(currentDir, fileNames.FirstOrDefault());
            }
            return slnFilePath;
        }

    }

}
