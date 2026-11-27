/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BuildXncfRequest.cs
    File description: BuildXncfRequest related implementation

    Created by: Senparc - 20211016

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

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
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;

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
                 //new SelectionItem("netstandard2.1","netstandard2.1","use .NET Standard 2.1（compatible .NET Core 3.1 and .NET 5.0-8.0）",true),
                 //new SelectionItem("netcoreapp3.1","netcoreapp3.1","use .NET Core 3.1",false),
                 //new SelectionItem("net6.0","net6.0","use .NET 6.0",false),
                 //new SelectionItem("net7.0","net7.0","use .NET 7.0",false),
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
        //Legacy comment translated to English.
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

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.StartAfterBuild")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(StartPreviewOptions))]
        public bool StartPreview { get; set; }

        [JsonIgnore]
        public SelectionList StartPreviewOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1", XncfBuilderResource.Get("XncfBuilder.Option.Preview.StartAfterBuild"), XncfBuilderResource.Get("XncfBuilder.Option.Preview.StartAfterBuild.Help"), false),
            });

        [Range(0, 65535)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.Port")]
        public int PreviewPort { get; set; }

        [Range(10, 600)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.Timeout")]
        public int PreviewStartupTimeoutSeconds { get; set; } = 120;

        [MaxLength(50)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Preview.Environment")]
        public string PreviewEnvironmentName { get; set; } = XncfPreviewService.DefaultEnvironmentName;

        /// <summary>
        /// Loads persisted builder configuration into the request before rendering options.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve builder configuration services.</param>
        /// <returns>A task that completes after configuration loading.</returns>
        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            Config config = null;
            try
            {
                //Legacy comment translated to English.
                var configService = serviceProvider.GetService<ServiceBase<Config>>();
                config = await configService.GetObjectAsync(z => true);
                if (config != null)
                {
                    #region Legacy section

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
                    #region Legacy section

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
        /// Determines whether a directory contains at least one solution file.
        /// </summary>
        /// <param name="path">The directory to inspect.</param>
        /// <returns><see langword="true"/> when a solution file exists.</returns>
        private bool IsSlnDir(string path)
        {
            return Directory.GetFiles(path, "*.sln").Length > 0;
        }

        /// <summary>
        /// Locates the nearest solution file by walking upward from the application base directory.
        /// </summary>
        /// <returns>The selected solution path, or an empty string when none is found.</returns>
        public string GetSlnFilePath()
        {
            var slnFilePath = string.Empty;
            //currentprogramdirectory
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;

            //Legacy comment translated to English.
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
