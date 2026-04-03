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
        /// Get the migration file generation directory
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
                commandTexts.Add($"chcp 65001"); // Set code page to UTF-8

                //Add tarmac reference (direct reference will cause problems)
                //var slnFilePath = Path.Combine(request.DatabasePlantPath, "..\\");
                //commandTexts.Add($"dotnet sln {slnFilePath} add {request.ProjectPath}");
                //commandTexts.Add($"dotnet add {request.DatabasePlantPath} reference {request.ProjectPath}");

                //Enter the project directory
                var projectPath = request.GetProjectPath(request);
                commandTexts.Add(@$"cd ""{projectPath}""");

                //Execute migration
                foreach (var dbType in request.DatabaseTypes.SelectedValues)
                {
                    string migrationDir = GetMigrationDir(request, dbType);
                    var outputVerbose = request.OutputVerbose.SelectedValues.Contains("1") ? " -v" : "";

                    //Database context entity name
                    var dbContextName = request.DbContextName;
                    if (dbContextName == "[Default]")
                    {
                        //Data types will be automatically spliced
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

                    //Replace the independent \ in request.DatabasePlantPath with \\
                    var databasePlantPath = removeFileName(request.DatabasePlantPath.Replace("\\", "\\\\"));
                    var migrationDirFinal = removeFileName(migrationDir.Replace("\\", "\\\\"));

                    var migrationsCmd = $"dotnet ef migrations add {request.MigrationName} -c {dbContextName} -s \"{databasePlantPath}\" -o \"{migrationDirFinal}\"{outputVerbose}";

                    await Console.Out.WriteLineAsync(migrationsCmd);

                    commandTexts.Add(migrationsCmd);
                    // --framework netcoreapp3.1
                    // If you need to specify a framework, you can add the above parameters, and you can also support more parameters, such as net5.0
                }

                ////Remove the apron reference (direct reference will cause problems)
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

                ////Pomelo-MySQL naming is inconsistent and needs to be dealt with
                //if (request.DatabaseTypes.SelectedValues.Contains(MultipleDatabaseType.MySql.ToString()))
                //{
                //    string migrationDir = GetMigrationDir(request, MultipleDatabaseType.MySql.ToString());
                //    var defaultFileName = $"{request.DbContextName}ModelSnapshot.cs";
                //    var pomeloFileName = $"{request.DbContextName}_MySqlModelSnapshot.cs";
                //    if (File.Exists(defaultFileName) && File.Exists(pomeloFileName))
                //    {
                //        File.Delete(defaultFileName);
                //        base.RecordLog(sb, $"Scanned the snapshot file of Pomelo.EntityFrameworkCore.MySql that is incompatible with the regular format: {pomeloFileName}, the default file has been deleted ({defaultFileName})!");
                //    }
                //}

                response.Data = "执行完毕，请查看日志！";

                if (strOutput.Contains("Build FAILED", StringComparison.InvariantCultureIgnoreCase))
                {
                    response.Data += "重要提示：可能出现错误，请检查日志！";
                }
                else
                {
                    //Update version number
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

                                //Get the contents of the Register.cs file
                                var fileContent = File.ReadAllText(registerFile);
                                //Get version number
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


                                //Update code
                                var newCode = VersionHelper.ReplaceVersionInCode(fileContent, result.RawVersionString, newVersion);
                                //save code
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
