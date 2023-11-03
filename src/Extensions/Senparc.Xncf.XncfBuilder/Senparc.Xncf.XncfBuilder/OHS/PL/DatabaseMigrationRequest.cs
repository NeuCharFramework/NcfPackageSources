using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
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
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class DatabaseMigrations_MigrationRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(250)]
        [Description("Senparc.Web.DatabasePlant 项目物理路径||用于使用 net6.0 等目标框架启动迁移操作，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\back-end\\Senparc.Web.DatabasePlant\\")]
        public string DatabasePlantPath { get; set; }

        [Required]
        [MaxLength(250)]
        [Description("XNCF 项目路径||输入 XNCF 项目根目录的完整物理路径，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\MyDemo.Xncf.NewApp\\")]
        public SelectionList ProjectPath { get; set; } = new SelectionList(SelectionType.DropDownList);

        [Description("生成数据库类型||更多类型陆续添加中")]
        public SelectionList DatabaseTypes { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem(MultipleDatabaseType.Sqlite.ToString(),MultipleDatabaseType.Sqlite.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.SqlServer.ToString(),MultipleDatabaseType.SqlServer.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.MySql.ToString(),MultipleDatabaseType.MySql.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.PostgreSQL.ToString(),MultipleDatabaseType.PostgreSQL.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.Oracle.ToString(),MultipleDatabaseType.Oracle.ToString(),"",true),
            });

        [Required]
        [MaxLength(100)]
        [Description("自定义 DbContext 名称||如：MyDemoSenparcEntities（注意：不需要加数据库类型后缀）")]
        public string DbContextName { get; set; }

        [Required]
        [MaxLength(100)]
        [Description("更新名称||可使用英文、数字、下划线，不可以有空格，如：Add_Config")]
        public string MigrationName { get; set; }

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

                this.ProjectPath.Items.Add(new SelectionItem("custom", "自定义路径", "", true));

                //添加“停机坪”路径
                var configService = serviceProvider.GetService<ConfigService>();
                var config = await configService.GetObjectAsync(z => true);
                if (config != null)
                {
                    if (!config.SlnFilePath.IsNullOrEmpty())
                    {
                        this.DatabasePlantPath = Path.Combine(Path.GetFullPath(config.SlnFilePath), "Senparc.Web.DatabasePlant");
                    }


                    //添加当前项目选项
                    var projectList = FunctionHelper.LoadXncfProjects(false);
                }


            }
            catch
            {
            }

        }
    }
}
