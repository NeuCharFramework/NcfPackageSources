using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.XncfBuilder.Templates;
using Senparc.Xncf.XncfBuilder.Templates.Areas.Admin.Pages;
using Senparc.Xncf.XncfBuilder.Templates.Areas.Admin.Pages.MyApps;
using Senparc.Xncf.XncfBuilder.Templates.Areas.Admin.Pages.Shared;
using Senparc.Xncf.XncfBuilder.Templates.Migrations;
using Senparc.Xncf.XncfBuilder.Templates.Models.DatabaseModel;
using Senparc.Xncf.XncfBuilder.Templates.Models.DatabaseModel.Dto;
using Senparc.Xncf.XncfBuilder.Templates.Models.DatabaseModel.Mapping;
using Senparc.Xncf.XncfBuilder.Templates.Services;
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
            [Description("XNCF 项目路径||输入 XNCF 项目根目录的完整物理路径，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\MyDemo.Xncf.NewApp\\")]
            public string ProjectPath { get; set; }

            [Description("生成数据库类型||更多类型陆续添加中")]
            public SelectionList DatabaseTypes { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem(MultipleDatabaseType.SQLite.ToString(),MultipleDatabaseType.SQLite.ToString(),"",true),
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

        private string _outPutBaseDir;
        /// <summary>
        /// 输出内容
        /// </summary>
        /// <param name="page"></param>
        private void WriteContent(IXncfTemplatePage page, StringBuilder sb)
        {
            String pageContent = page.TransformText();
            System.IO.File.WriteAllText(Path.Combine(_outPutBaseDir, page.RelativeFilePath), pageContent, Encoding.UTF8);
            sb.AppendLine($"已添加文件：{page.RelativeFilePath}");
        }

        /// <summary>
        /// 添加文件夹
        /// </summary>
        /// <param name="dirName"></param>
        private string AddDir(string dirName)
        {
            var path = Path.Combine(_outPutBaseDir, dirName);
            Directory.CreateDirectory(path);
            return path;
        }

        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<Parameters>(param, (typeParam, sb, result) =>
            {
                if (!typeParam.DatabaseTypes.SelectedValues.Contains(MultipleDatabaseType.SQLite.ToString()))
                {
                    result.Message = $"{MultipleDatabaseType.SQLite} 暂时为默认数据库，必选";
                    return;
                }


                var commandTexts = new List<string> {
                @$"cd {typeParam.ProjectPath}",
                //@"dir",
                //@"dotnet --version",
                //@"dotnet ef",
                //@"dotnet ef migrations add Int2 --context XncfBuilderEntities_SqlServer --output-dir Migrations/Test",// 本行为示例，将自动根据条件执行
            };

                foreach (var dbType in typeParam.DatabaseTypes.SelectedValues)
                {
                    var migrationDir = Path.Combine(typeParam.ProjectPath, "Migrations", $"Migrations.{dbType}");
                    var outputVerbose = typeParam.OutputVerbose.SelectedValues.Contains("1") ? " -v" : "";
                    var dbTypeSuffix = Enum.TryParse(dbType, out MultipleDatabaseType dbTypeEnum) && dbTypeEnum == MultipleDatabaseType.Default
                                            ? "" : $"_{dbType}";//如果是SQL Lite
                    commandTexts.Add($"dotnet ef migrations add {typeParam.MigrationName} -c {typeParam.DbContextName}{dbTypeSuffix} -o {migrationDir}{outputVerbose}");
                }

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
                result.Message = "执行完毕，请查看日志！";

                if (strOutput.Contains("Build FAILED", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Message += "重要提示：可能出现错误，请检查日志！";
                }
            });
        }
    }
}
