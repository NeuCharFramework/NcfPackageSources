/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseMigrationsAppService.cs
    File description: DatabaseMigrationsAppService related implementation

    Created by: Senparc - 20211016

    Modified by: Senparc - 20260704
    Change description: v0.36.2-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

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
        //Legacy comment translated to English.
        private static readonly Regex AnsiEscapeRegex = new(@"\x1B\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled);

        private class CommandExecutionResult
        {
            public bool Started { get; set; }
            public int ExitCode { get; set; }
            public string StandardOutput { get; set; }
            public string StandardError { get; set; }
            public Exception StartException { get; set; }
        }

        /// <summary>
        /// Executes a tokenized <c>dotnet ef</c> command without shell interpretation
        /// and captures process startup, exit code, standard output, and standard error.
        /// </summary>
        /// <param name="workingDirectory">The directory from which the command runs.</param>
        /// <param name="args">The tokenized command arguments.</param>
        /// <returns>The captured command execution result.</returns>
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

        /// <summary>
        /// Formats tokenized migration-command arguments for administrator logs.
        /// </summary>
        /// <param name="args">The command arguments to format.</param>
        /// <returns>A human-readable command preview.</returns>
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

        /// <summary>
        /// Appends sanitized command output and exit status to the application log.
        /// </summary>
        /// <param name="logger">The application logger.</param>
        /// <param name="result">The command result to record.</param>
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

        /// <summary>
        /// Removes terminal color/control escape sequences before output is persisted
        /// in the administrator log.
        /// </summary>
        /// <param name="content">The raw process output.</param>
        /// <returns>Output without ANSI escape sequences.</returns>
        private static string StripAnsiEscapeSequences(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            return AnsiEscapeRegex.Replace(content, string.Empty);
        }

        /// <summary>
        /// Resolves the provider-specific migration directory for a project.
        /// </summary>
        /// <param name="projectPath">The project root path.</param>
        /// <param name="dbType">The database provider name.</param>
        /// <returns>The migration directory path.</returns>
        private static string GetMigrationDir(string projectPath, string dbType)
        {
            return MigrationFileLayoutHelper.GetMigrationDirectory(projectPath, dbType);
        }

        /// <summary>
        /// Writes snapshot relocation, namespace-normalization, and duplicate-removal
        /// details to the migration operation log.
        /// </summary>
        /// <param name="logger">The application logger.</param>
        /// <param name="alignment">The snapshot alignment result.</param>
        /// <param name="dbContextName">The database context name.</param>
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

        /// <summary>
        /// Initializes the database migration application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public DatabaseMigrationsAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        // Migration generation writes project files and migration sources directly. It remains an
        // administrator UI operation, not an automatically imported AI chat function.
        /// <summary>
        /// Generates provider-specific EF Core migration files, aligns the matching
        /// model snapshot, and reports each command through the administrator response pipeline.
        /// </summary>
        /// <param name="request">The target project, DbContext, migration name, and providers.</param>
        /// <returns>A response containing migration-generation results and diagnostics.</returns>
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

                //Legacy comment translated to English.
                //var slnFilePath = Path.Combine(request.DatabasePlantPath, "..\\");
                //commandTexts.Add($"dotnet sln {slnFilePath} add {request.ProjectPath}");
                //commandTexts.Add($"dotnet add {request.DatabasePlantPath} reference {request.ProjectPath}");

                //Legacy comment translated to English.
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

                //Legacy comment translated to English.
                foreach (var dbType in databaseTypes)
                {
                    string migrationDir = GetMigrationDir(projectPath, dbType);

                    //Legacy comment translated to English.
                    var dbContextName = request.DbContextName;
                    if (dbContextName == "[Default]")
                    {
                        //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
                }

                // Project-reference cleanup is intentionally retained as disabled
                // compatibility code while migration generation owns the solution state.
                //commandTexts.Add($"dotnet remove {request.DatabasePlantPath} reference {request.ProjectPath}");
                //commandTexts.Add($"dotnet sln {slnFilePath} remove {request.ProjectPath}");

                // Provider-specific snapshot reconciliation remains a disabled legacy
                // path; current alignment is handled by MigrationFileLayoutHelper.
                //if (request.DatabaseTypes.SelectedValues.Contains(MultipleDatabaseType.MySql.ToString()))
                //{
                //    string migrationDir = GetMigrationDir(request, MultipleDatabaseType.MySql.ToString());
                //    var defaultFileName = $"{request.DbContextName}ModelSnapshot.cs";
                //    var pomeloFileName = $"{request.DbContextName}_MySqlModelSnapshot.cs";
                //    if (File.Exists(defaultFileName) && File.Exists(pomeloFileName))
                //    {
                //        File.Delete(defaultFileName);
                //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
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

                                //get Register.cs filecontent
                                var fileContent = File.ReadAllText(registerFile);
                                //Legacy comment translated to English.
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


                                //updatecode
                                var newCode = VersionHelper.ReplaceVersionInCode(fileContent, result.RawVersionString, newVersion);
                                //savecode
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
