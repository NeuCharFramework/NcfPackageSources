using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase.VersionManager;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    public class DatabaseMigrationsAppService : AppServiceBase
    {
        /// <summary>
        /// 获取迁移文件生成目录
        /// </summary>
        /// <param name="request"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        private string GetMigrationDir(DatabaseMigrations_MigrationRequest request, string dbType)
        {
            string projectPath = request.GetProjectPath(request);

            var migrationPath = Path.Combine(projectPath, "Domain", "Migrations", $"{dbType}");
            //Console.WriteLine("1220== migrationPath: " + migrationPath);
            return migrationPath;
        }

        public DatabaseMigrationsAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        [FunctionRender("Add-Migration 命令", "可视化完成多数据库的 add-migration 命令，使用 Code First 更新数据库。注意：根据计算机配置和数据库情况，执行过程可能在30-60秒不等，请耐心等待。", typeof(Register))]
        public async Task<StringAppResponse> AddMigration(DatabaseMigrations_MigrationRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request.DatabaseTypes.SelectedValues.Count() == 0)
                {
                    response.Success = false;
                    response.Data = "至少选择 1 个数据库！";
                    return null;
                }

                var commandTexts = new List<string>();

                //添加停机坪引用（直接引用会有问题）
                //var slnFilePath = Path.Combine(request.DatabasePlantPath, "..\\");
                //commandTexts.Add($"dotnet sln {slnFilePath} add {request.ProjectPath}");
                //commandTexts.Add($"dotnet add {request.DatabasePlantPath} reference {request.ProjectPath}");

                //进入项目目录
                var projectPath = request.GetProjectPath(request);
                commandTexts.Add(@$"cd ""{projectPath}""");

                //执行迁移
                foreach (var dbType in request.DatabaseTypes.SelectedValues)
                {
                    string migrationDir = GetMigrationDir(request, dbType);
                    var outputVerbose = request.OutputVerbose.SelectedValues.Contains("1") ? " -v" : "";

                    //数据库上下文实体名称
                    var dbContextName = request.DbContextName;
                    if (dbContextName == "[Default]")
                    {
                        //会自动拼接数据类型
                        dbContextName = FunctionHelper.GetSenparcEntitiesFilePath(projectPath, dbType);
                    }
                    else
                    {
                        var dbTypeSuffix = $"_{dbType}";
                        dbContextName += dbTypeSuffix;
                    }

                    Func<string, string> removeFileName = path =>
                    {
                        if (path.EndsWith(".csproj"))
                        {
                            return Path.GetDirectoryName(path);
                        }
                        else
                        {
                            return path;
                        }
                    };

                    //把 request.DatabasePlantPath 中独立存在的 \ 替换为 \\
                    var databasePlantPath = removeFileName(request.DatabasePlantPath.Replace("\\", "\\\\"));
                    var migrationDirFinal = removeFileName(migrationDir.Replace("\\", "\\\\"));

                    var migrationsCmd = $"dotnet ef migrations add {request.MigrationName} -c {dbContextName} -s \"{databasePlantPath}\" -o \"{migrationDirFinal}\"{outputVerbose}";

                    await Console.Out.WriteLineAsync(migrationsCmd);

                    commandTexts.Add(migrationsCmd);
                    // --framework netcoreapp3.1
                    // 如需指定框架，可以追加上述参数，也可以支持更多参数，如net5.0
                }

                ////移除停机坪引用（直接引用会有问题）
                //commandTexts.Add($"dotnet remove {request.DatabasePlantPath} reference {request.ProjectPath}");
                //commandTexts.Add($"dotnet sln {slnFilePath} remove {request.ProjectPath}");

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
                    logger.Append(strOutput);

                    //strOutput = Encoding.UTF8.GetString(Encoding.Default.GetBytes(strOutput));
                    p.WaitForExit();
                    p.Close();
                }
                catch (Exception e)
                {
                    strOutput = e.Message;
                }

                ////Pomelo-MySQL 命名有不统一的情况，需要处理
                //if (request.DatabaseTypes.SelectedValues.Contains(MultipleDatabaseType.MySql.ToString()))
                //{
                //    string migrationDir = GetMigrationDir(request, MultipleDatabaseType.MySql.ToString());
                //    var defaultFileName = $"{request.DbContextName}ModelSnapshot.cs";
                //    var pomeloFileName = $"{request.DbContextName}_MySqlModelSnapshot.cs";
                //    if (File.Exists(defaultFileName) && File.Exists(pomeloFileName))
                //    {
                //        File.Delete(defaultFileName);
                //        base.RecordLog(sb, $"扫描到不兼容常规格式的 Pomelo.EntityFrameworkCore.MySql 的快照文件：{pomeloFileName}，已将默认文件删除（{defaultFileName}）！");
                //    }
                //}

                response.Data = "执行完毕，请查看日志！";

                if (strOutput.Contains("Build FAILED", StringComparison.InvariantCultureIgnoreCase))
                {
                    response.Data += "重要提示：可能出现错误，请检查日志！";
                }
                else
                {
                    //更新版本号
                    try
                    {
                        logger.Append("");
                        logger.Append("==== 版本号更新 ====");

                        var updateVesionType = request.UpdateVersion.SelectedValues.FirstOrDefault();
                        if (updateVesionType != "0")
                        {
                            var registerFile = Path.Combine(projectPath, "Register.cs");
                            if (File.Exists(registerFile))
                            {
                                logger.Append("Register.cs 文件存在，开始更新版本号");

                                //获取 Register.cs 文件内容
                                var fileContent = File.ReadAllText(registerFile);
                                //获取版本号
                                var result = VersionHelper.ParseFromCode(fileContent);
                                var oldVersion = result.VersionInfo;
                                logger.Append($"当前版本号：{oldVersion.ToString()}");

                                var newVersion = new VersionInfo();
                                switch (updateVesionType)
                                {
                                    case "1":
                                        newVersion = oldVersion with { Major = oldVersion.Major + 1 };
                                        break;
                                    case "2":
                                        newVersion = oldVersion with { Minor = oldVersion.Minor + 1 };
                                        break;
                                    case "3":
                                        newVersion = oldVersion with { Patch = oldVersion.Patch + 1 };
                                        break;
                                    default:
                                        throw new NcfExceptionBase("无法识别的版本更新类型");
                                }


                                //更新代码
                                var newCode = VersionHelper.ReplaceVersionInCode(fileContent, result.RawVersionString, newVersion);
                                //保存代码
                                using (var fw = new FileStream(registerFile, FileMode.Create))
                                {
                                    using (var sw = new StreamWriter(fw))
                                    {
                                        await sw.WriteLineAsync(newCode);
                                        await sw.FlushAsync();
                                    }
                                }
                                logger.Append($"已替换为新版本号：{newVersion.ToString()}");
                            }
                            else
                            {
                                logger.Append("Register.cs 文件不存在，跳过");
                            }
                        }
                        else
                        {
                            logger.Append("不要求自动更新版本号，跳过");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Append("更新版本出错：" + ex.Message);
                        new NcfExceptionBase(ex.Message, ex);
                    }
                }

                return null;
            });
        }
    }
}
