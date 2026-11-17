/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DatabaseMigrationRequest.cs

    File description: DatabaseMigrationRequest related implementation

    

    

    Created by: Senparc - 20211016

    

    Modified by: Senparc - 20260704

    Change description: v0.36.2-preview1 Updated module behavior and related support.



    Modified by: Senparc - 20260717

    Change description: v0.37.0-preview5 Updated module behavior and related support.



----------------------------------------------------------------*/
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.XncfBuilder.Domain.Models.Services;
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
    public class DatabaseMigrations_MigrationRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(250)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Migration.DatabasePlantPath")]
        public string DatabasePlantPath { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Migration.ProjectPath")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(ProjectPathOptions))]
        public string ProjectPath { get; set; }

        [JsonIgnore]
        public SelectionList ProjectPathOptions { get; set; } = new SelectionList(SelectionType.DropDownList);

        [MaxLength(250)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Migration.CustomProjectPath")]
        public string CustomProjectPath { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Migration.DatabaseTypes")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(DatabaseTypeOptions))]
        public string[] DatabaseTypes { get; set; }

        [JsonIgnore]
        public SelectionList DatabaseTypeOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem(MultipleDatabaseType.Sqlite.ToString(),MultipleDatabaseType.Sqlite.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.SqlServer.ToString(),MultipleDatabaseType.SqlServer.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.MySql.ToString(),MultipleDatabaseType.MySql.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.PostgreSQL.ToString(),MultipleDatabaseType.PostgreSQL.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.Oracle.ToString(),MultipleDatabaseType.Oracle.ToString(),"",true),
                  new SelectionItem(MultipleDatabaseType.Dm.ToString(),MultipleDatabaseType.Dm.ToString(),"",true),
            });

        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Migration.DbContext")]
        public string DbContextName { get; set; } = "[Default]";

        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Migration.Name")]
        public string MigrationName { get; set; }


        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Migration.UpdateVersion")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(UpdateVersionOptions))]
        public string UpdateVersion { get; set; }

        [JsonIgnore]
        public SelectionList UpdateVersionOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("0", XncfBuilderResource.Get("XncfBuilder.Option.Version.None"), "", true),
                 new SelectionItem("1", XncfBuilderResource.Get("XncfBuilder.Option.Version.Major"), "", false),
                 new SelectionItem("2", XncfBuilderResource.Get("XncfBuilder.Option.Version.Minor"), "", false),
                 new SelectionItem("3", XncfBuilderResource.Get("XncfBuilder.Option.Version.Patch"), "", false)
            });


        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Migration.Verbose")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(OutputVerboseOptions))]
        public bool OutputVerbose { get; set; }

        [JsonIgnore]
        public SelectionList OutputVerboseOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("true", XncfBuilderResource.Get("Common.Use"), "", false)
            });

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            try
            {
                //Legacy comment translated to English.

                this.ProjectPathOptions.Items.Add(new SelectionItem("N/A", XncfBuilderResource.Get("XncfBuilder.Option.CustomPath"), "", true));

                //Legacy comment translated to English.
                var configService = serviceProvider.GetService<ConfigService>();
                var config = await configService.GetObjectAsync(z => true);
                if (config != null)
                {
                    if (!config.SlnFilePath.IsNullOrEmpty())
                    {
                        this.DatabasePlantPath = Path.Combine(Path.GetDirectoryName(config.SlnFilePath), "Senparc.Web.DatabasePlant");
                    }

                    //Legacy comment translated to English.
                    var projectList = FunctionHelper.LoadXncfProjects(false, null, "Senparc.Areas.Admin");
                    projectList.OrderBy(z=>z.Value).ToList().ForEach(z => ProjectPathOptions.Items.Add(z));

                    //Legacy comment translated to English.
                    var sourceRootDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "..", "..", "src");
                    Console.WriteLine("查找 Source 项目源文件根目录：" + sourceRootDir);
                    var sourceProjectList = FunctionHelper.LoadXncfProjects(false, sourceRootDir, "Senparc.Areas.Admin");
                    sourceProjectList.OrderBy(z => z.Value).ToList().ForEach(z => ProjectPathOptions.Items.Add(z));
                }
            }
            catch
            {
            }
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public string GetProjectPath(DatabaseMigrations_MigrationRequest request)
        {
            var projectPath = request.ProjectPath;
            if (projectPath == "N/A")
            {
                projectPath = request.CustomProjectPath;
                if (projectPath.IsNullOrEmpty())
                {
                    throw new NcfExceptionBase(XncfBuilderResource.Get("XncfBuilder.Validation.CustomProjectPathRequired"));
                }
            }

            return projectPath;
        }

    }
}
