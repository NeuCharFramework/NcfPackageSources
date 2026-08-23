/*-----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseMigrationsAppService.cs
    文件功能描述：DatabaseMigrationsAppService 相关实现
    
    
    创建标识：Senparc - 20211016
    
    修改标识：Senparc - 20260704
    修改描述：v0.36.2-preview1 优化数据库迁移命令日志清洗与请求模型能力

    修改标识：Senparc - 20260717
    修改描述：v0.37.0-preview5 增强 XNCF 构建、数据库迁移与 AI 生成流程的本地化支持

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

    修改标识：Senparc - 20260822
    修改描述：v0.41.0 优化 XncfBuilder 预览任务与工作区服务

----------------------------------------------------------------*/
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    public class DatabaseMigrationsAppService : AppServiceBase
    {
        // ANSI CSI 控制序列（例如 \x1B[39;49m），用于清理日志中的颜色转义字符。
        private static readonly Regex AnsiEscapeRegex = new(@"\x1B\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled);

        private class CommandExecutionResult
        {
            public bool Started { get; set; }
            public int ExitCode { get; set; }
            public string StandardOutput { get; set; }
            public string StandardError { get; set; }
            public Exception StartException { get; set; }
        }

        private static async Task<CommandExecutionResult> ExecuteDotNetEfCommandAsync(string workingDirectory, IEnumerable<string> args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = new Process { StartInfo = startInfo };
            try
            {
                if (!process.Start())
                {
                    return new CommandExecutionResult
                    {
                        Started = false,
                        ExitCode = -1,
                        StandardOutput = string.Empty,
                        StandardError = "进程未能启动。"
                    };
                }
            }
            catch (Exception ex)
            {
                return new CommandExecutionResult
                {
                    Started = false,
                    ExitCode = -1,
                    StandardOutput = string.Empty,
                    StandardError = ex.Message,
                    StartException = ex
                };
            }

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return new CommandExecutionResult
            {
                Started = true,
                ExitCode = process.ExitCode,
                StandardOutput = await stdOutTask,
                StandardError = await stdErrTask
            };
        }

        private static string BuildCommandPreview(IEnumerable<string> args)
        {
            return "dotnet " + string.Join(" ", args.Select(arg =>
            {
                if (arg == null)
                {
                    return "\"\"";
                }

                return arg.IndexOf(' ') >= 0 ? $"\"{arg}\"" : arg;
            }));
        }

        private static void AppendCommandOutput(Senparc.Ncf.Core.AppServices.AppServiceLogger logger, CommandExecutionResult result)
        {
            var stdOut = StripAnsiEscapeSequences(result.StandardOutput);
            var stdErr = StripAnsiEscapeSequences(result.StandardError);

            if (!string.IsNullOrWhiteSpace(stdOut))
            {
                logger.Append(stdOut.TrimEnd('\r', '\n'));
            }

            if (!string.IsNullOrWhiteSpace(stdErr))
            {
                logger.Append(stdErr.TrimEnd('\r', '\n'));
            }

            logger.Append($"命令退出码：{result.ExitCode}");
        }

        private static string StripAnsiEscapeSequences(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            return AnsiEscapeRegex.Replace(content, string.Empty);
        }

        /// <summary>
        /// 获取迁移文件生成目录
        /// </summary>
        /// <param name="request"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        private static string GetMigrationDir(string projectPath, string dbType)
        {
            return MigrationFileLayoutHelper.GetMigrationDirectory(projectPath, dbType);
        }

        private static void AppendSnapshotAlignment(
            Senparc.Ncf.Core.AppServices.AppServiceLogger logger,
            MigrationSnapshotAlignmentResult alignment,
            string dbContextName)
        {
            if (!alignment.SnapshotFound)
            {
                logger.Append($"未发现 {dbContextName} 的已有 snapshot，将由 EF Core 首次创建。");
                return;
            }

            if (alignment.Moved)
            {
                logger.Append($"已将 snapshot 归位：{alignment.OriginalPath} -> {alignment.SnapshotPath}");
            }

            if (alignment.NamespaceChanged)
            {
                logger.Append($"已统一 snapshot 命名空间：{alignment.SnapshotPath}");
            }

            foreach (var duplicateFile in alignment.RemovedDuplicateFiles)
            {
                logger.Append($"已移除重复 snapshot：{duplicateFile}");
            }
        }

        public DatabaseMigrationsAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        // Migration generation writes project files and migration sources directly. It remains an
        // administrator UI operation, not an automatically imported AI chat function.
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.AddMigration.Name", "Function.XncfBuilder.AddMigration.Description", typeof(Register), AllowAiInvocation = false)]
        public async Task<StringAppResponse> AddMigration(DatabaseMigrations_MigrationRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var databaseTypes = request.DatabaseTypes ?? Array.Empty<string>();
                if (databaseTypes.Length == 0)
                {
                    response.Success = false;
                    response.Data = "至少选择 1 个数据库！";
                    return null;
                }

                //添加停机坪引用（直接引用会有问题）
                //var slnFilePath = Path.Combine(request.DatabasePlantPath, "..\\");
                //commandTexts.Add($"dotnet sln {slnFilePath} add {request.ProjectPath}");
                //commandTexts.Add($"dotnet add {request.DatabasePlantPath} reference {request.ProjectPath}");

                //进入项目目录
                string projectPath;
                string databasePlantPath;
                try
                {
                    projectPath = MigrationFileLayoutHelper.GetProjectDirectory(
                        request.GetProjectPath(request),
                        "目标项目路径");
                    databasePlantPath = MigrationFileLayoutHelper.GetProjectDirectory(
                        request.DatabasePlantPath,
                        "数据库停机坪路径");
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.Data = $"迁移路径无效：{ex.Message}";
                    logger.Append(response.Data);
                    return null;
                }

                logger.Append($"工作目录：{projectPath}");

                var allMigrationsSucceeded = true;

                //执行迁移
                foreach (var dbType in databaseTypes)
                {
                    string migrationDir = GetMigrationDir(projectPath, dbType);

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

                    string migrationOutputDirectory;
                    string expectedNamespace;
                    IReadOnlyCollection<string> migrationFilesBefore;
                    try
                    {
                        expectedNamespace = MigrationFileLayoutHelper.GetExpectedNamespace(projectPath, dbType);
                        migrationOutputDirectory = MigrationFileLayoutHelper.GetOutputDirectoryArgument(projectPath, migrationDir);

                        var alignment = MigrationFileLayoutHelper.AlignSnapshot(
                            projectPath,
                            migrationDir,
                            dbContextName,
                            expectedNamespace);
                        AppendSnapshotAlignment(logger, alignment, dbContextName);
                        migrationFilesBefore = MigrationFileLayoutHelper.CaptureMigrationFiles(migrationDir);
                    }
                    catch (Exception ex)
                    {
                        allMigrationsSucceeded = false;
                        response.Success = false;
                        response.Data = $"迁移文件布局检查失败（{dbType}）：{ex.Message}";
                        logger.Append(response.Data);
                        break;
                    }

                    var commandArgs = new List<string>
                    {
                        "ef",
                        "migrations",
                        "add",
                        request.MigrationName,
                        "-c",
                        dbContextName,
                        "-p",
                        projectPath,
                        "-s",
                        databasePlantPath,
                        "-o",
                        migrationOutputDirectory
                    };

                    if (request.OutputVerbose)
                    {
                        commandArgs.Add("-v");
                    }

                    logger.Append("");
                    logger.Append($"==== 执行迁移（{dbType}）====");
                    logger.Append(BuildCommandPreview(commandArgs));

                    var executeResult = await ExecuteDotNetEfCommandAsync(projectPath, commandArgs);
                    AppendCommandOutput(logger, executeResult);

                    if (!executeResult.Started || executeResult.ExitCode != 0)
                    {
                        allMigrationsSucceeded = false;
                        response.Success = false;
                        response.Data = $"迁移命令执行失败（{dbType}），请查看日志。";
                        break;
                    }

                    try
                    {
                        var generatedFiles = MigrationFileLayoutHelper.VerifyGeneratedMigrationFiles(
                            migrationDir,
                            migrationFilesBefore);
                        var alignment = MigrationFileLayoutHelper.AlignSnapshot(
                            projectPath,
                            migrationDir,
                            dbContextName,
                            expectedNamespace);
                        if (!alignment.SnapshotFound)
                        {
                            throw new InvalidOperationException($"EF Core 未生成 {dbContextName} 的 snapshot。");
                        }

                        AppendSnapshotAlignment(logger, alignment, dbContextName);
                        logger.Append($"已生成 migration：{generatedFiles.MigrationFile}");
                        logger.Append($"已生成 Designer：{generatedFiles.DesignerFile}");
                        logger.Append($"迁移文件目录验证通过：{migrationDir}");
                    }
                    catch (Exception ex)
                    {
                        allMigrationsSucceeded = false;
                        response.Success = false;
                        response.Data = $"snapshot 归位失败（{dbType}）：{ex.Message}";
                        logger.Append(response.Data);
                        break;
                    }

                    // --framework netcoreapp3.1
                    // 如需指定框架，可以追加上述参数，也可以支持更多参数，如net5.0
                }

                ////移除停机坪引用（直接引用会有问题）
                //commandTexts.Add($"dotnet remove {request.DatabasePlantPath} reference {request.ProjectPath}");
                //commandTexts.Add($"dotnet sln {slnFilePath} remove {request.ProjectPath}");

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

                if (!allMigrationsSucceeded)
                {
                    string errMsg = "迁移命令存在失败，已跳过版本号更新。";
                    response.Data += errMsg;
                    logger.Append(errMsg);
                }
                else
                {
                    //更新版本号
                    try
                    {
                        logger.Append("");
                        logger.Append("==== 版本号更新 ====");

                        var updateVesionType = request.UpdateVersion;
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
