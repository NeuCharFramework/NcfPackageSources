using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
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

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class DatabaseMigrations_MigrationRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(250)]
        [Description("Senparc.Web.DatabasePlant 项目物理路径||用于使用 net8.0 等目标框架启动迁移操作，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\back-end\\Senparc.Web.DatabasePlant\\。请确保需要操作的 XNCF 项目已被此项目引用！")]
        public string DatabasePlantPath { get; set; }

        [Description("XNCF 项目路径||选择 XNCF 项目根目录的完整物理路径")]
        public SelectionList ProjectPath { get; set; } = new SelectionList(SelectionType.DropDownList);

        [MaxLength(250)]
        [Description("自定义 XNCF 项目路径||仅当“XNCF 项目路径”选择“自定义路径”时有效。输入 XNCF 项目根目录的完整物理路径，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\MyDemo.Xncf.NewApp\\")]
        public string CustomProjectPath { get; set; }

        [Description("生成数据库类型||更多类型陆续添加中")]
        public SelectionList DatabaseTypes { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem(MultipleDatabaseType.Sqlite.ToString(),MultipleDatabaseType.Sqlite.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.SqlServer.ToString(),MultipleDatabaseType.SqlServer.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.MySql.ToString(),MultipleDatabaseType.MySql.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.PostgreSQL.ToString(),MultipleDatabaseType.PostgreSQL.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.Oracle.ToString(),MultipleDatabaseType.Oracle.ToString(),"",true),
                  new SelectionItem(MultipleDatabaseType.Dm.ToString(),MultipleDatabaseType.Dm.ToString(),"",true),
            });

        [Required]
        [MaxLength(100)]
        [Description("自定义 DbContext 名称||如：MyDemoSenparcEntities（注意：不需要加数据库类型后缀）。输入[Default]可自动获取由 XncfBuilder 自动生成的模块的 SenparcEntities。")]
        public string DbContextName { get; set; } = "[Default]";

        [Required]
        [MaxLength(100)]
        [Description("更新名称||可使用英文、数字、下划线，不可以有空格，如：Add_Config")]
        public string MigrationName { get; set; }


        [Description("自动更新版本号||自动更新 Register.cs 中的版本号")]
        public SelectionList UpdateVersion { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("0","不更新","",true),
                 new SelectionItem("1","主版本号（Major） + 1","",false),
                 new SelectionItem("2","次版本号（Minor） + 1","",false),
                 new SelectionItem("3","修订版本号（Patch） + 1","",false)
            });


        [Description("输出详细日志||使用 add-migration 的 -v 参数")]
        public SelectionList OutputVerbose { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","",false)
            });

        /// <summary>
        /// 预载入数据
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            try
            {
                //TODO:单独生成一个表来记录

                this.ProjectPath.Items.Add(new SelectionItem("N/A", "自定义路径", "", true));

                //添加“停机坪”路径
                var configService = serviceProvider.GetService<ConfigService>();
                var config = await configService.GetObjectAsync(z => true);
                if (config != null)
                {
                    if (!config.SlnFilePath.IsNullOrEmpty())
                    {
                        this.DatabasePlantPath = Path.Combine(Path.GetDirectoryName(config.SlnFilePath), "Senparc.Web.DatabasePlant");
                    }

                    //添加当前解决方案的项目选项
                    var projectList = FunctionHelper.LoadXncfProjects(false,"Senparc.Areas.Admin");
                    projectList.ForEach(z => ProjectPath.Items.Add(z));
                }
            }
            catch
            {
            }
        }


        /// <summary>
        /// 获取项目路径
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public string GetProjectPath(DatabaseMigrations_MigrationRequest request)
        {
            var projectPath = request.ProjectPath.SelectedValues.FirstOrDefault();
            if (projectPath == "N/A")
            {
                projectPath = request.CustomProjectPath;
                if (projectPath.IsNullOrEmpty())
                {
                    throw new NcfExceptionBase("请填写“自定义 XNCF 项目路径”！");
                }
            }

            return projectPath;
        }

    }
}
