using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Diagnostics;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.XncfBuilder.Functions
{

    public class AddMigration : FunctionBase
    {
        public AddMigration(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public class Parameters : FunctionParameterLoadDataBase, IFunctionParameter
        {
            [Required]
            [MaxLength(250)]
            [Description("Senparc.Web.DatabasePlant 项目物理路径||用于使用 netcoreapp3.1 等目标框架启动迁移操作，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\Senparc.Web.DatabasePlant\\")]
            public string DatabasePlantPath { get; set; }

            [Required]
            [MaxLength(250)]
            [Description("XNCF 项目路径||输入 XNCF 项目根目录的完整物理路径，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\MyDemo.Xncf.NewApp\\")]
            public string ProjectPath { get; set; }

            [Description("生成数据库类型||更多类型陆续添加中")]
            public SelectionList DatabaseTypes { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem(MultipleDatabaseType.Sqlite.ToString(),MultipleDatabaseType.Sqlite.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.SqlServer.ToString(),MultipleDatabaseType.SqlServer.ToString(),"",true),
                 new SelectionItem(MultipleDatabaseType.MySql.ToString(),MultipleDatabaseType.MySql.ToString(),"",true),
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
                    ////低版本没有数据库，此处需要try
                    //var configService = serviceProvider.GetService<ServiceBase<Config>>();
                    //var config = await configService.GetObjectAsync(z => true);
                    //if (config != null)
                    //{
                    //    configService.Mapper.Map(config, this);
                    //}
                }
                catch
                {
                }

            }
        }

        public override string Name => "Add-Migration 命令";

        public override string Description => "可视化完成多数据库的 add-migration 命令，使用 Code First 更新数据库。注意：根据计算机配置和数据库情况，执行过程可能在30-60秒不等，请耐心等待。";

        public override Type FunctionParameterType => typeof(Parameters);

        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<Parameters>(param, (typeParam, sb, result) =>
            {
                if (typeParam.DatabaseTypes.SelectedValues.Count() == 0)
                {
                    result.Message = "至少选择 1 个数据库！";
                    return;
                }

                var commandTexts = new List<string>();

                //添加停机坪引用
                commandTexts.Add($"dotnet add {typeParam.DatabasePlantPath} reference {typeParam.ProjectPath}");

                //进入项目目录
                commandTexts.Add(@$"cd {typeParam.ProjectPath}");

                //执行迁移
                foreach (var dbType in typeParam.DatabaseTypes.SelectedValues)
                {
                    string migrationDir = GetMigrationDir(typeParam, dbType);
                    var outputVerbose = typeParam.OutputVerbose.SelectedValues.Contains("1") ? " -v" : "";
                    var dbTypeSuffix = $"_{dbType}";
                    commandTexts.Add($"dotnet ef migrations add {typeParam.MigrationName} -c {typeParam.DbContextName}{dbTypeSuffix} -s {typeParam.DatabasePlantPath} -o {migrationDir}{outputVerbose}");
                    // --framework netcoreapp3.1
                    // 如需指定框架，可以追加上述参数，也可以支持更多参数，如net5.0
                }

                //移除停机坪引用
                commandTexts.Add($"dotnet remove {typeParam.DatabasePlantPath} reference {typeParam.ProjectPath}");

                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                string strOutput = null;
                try
                {
                    p.Start();
                    foreach (string item in commandTexts)
                    {
                        p.StandardInput.WriteLine(item);
                    }
                    p.StandardInput.WriteLine("exit");
                    strOutput = p.StandardOutput.ReadToEnd();
                    base.RecordLog(sb, strOutput);

                    //strOutput = Encoding.UTF8.GetString(Encoding.Default.GetBytes(strOutput));
                    p.WaitForExit();
                    p.Close();
                }
                catch (Exception e)
                {
                    strOutput = e.Message;
                }

                ////Pomelo-MySQL 命名有不统一的情况，需要处理
                //if (typeParam.DatabaseTypes.SelectedValues.Contains(MultipleDatabaseType.MySql.ToString()))
                //{
                //    string migrationDir = GetMigrationDir(typeParam, MultipleDatabaseType.MySql.ToString());
                //    var defaultFileName = $"{typeParam.DbContextName}ModelSnapshot.cs";
                //    var pomeloFileName = $"{typeParam.DbContextName}_MySqlModelSnapshot.cs";
                //    if (File.Exists(defaultFileName) && File.Exists(pomeloFileName))
                //    {
                //        File.Delete(defaultFileName);
                //        base.RecordLog(sb, $"扫描到不兼容常规格式的 Pomelo.EntityFrameworkCore.MySql 的快照文件：{pomeloFileName}，已将默认文件删除（{defaultFileName}）！");
                //    }
                //}

                result.Message = "执行完毕，请查看日志！";

                if (strOutput.Contains("Build FAILED", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Message += "重要提示：可能出现错误，请检查日志！";
                }
            });
        }

        /// <summary>
        /// 获取迁移文件生成目录
        /// </summary>
        /// <param name="typeParam"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        private string GetMigrationDir(Parameters typeParam, string dbType)
        {
            return Path.Combine(typeParam.ProjectPath, "Migrations", $"Migrations.{dbType}");
        }
    }
}
