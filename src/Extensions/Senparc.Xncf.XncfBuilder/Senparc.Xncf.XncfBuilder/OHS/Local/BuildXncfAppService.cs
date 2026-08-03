/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BuildXncfAppService.cs
    文件功能描述：BuildXncfAppService 相关实现
    
    
    创建标识：Senparc - 20211016
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260705
    修改描述：v0.36.3-preview2 重构系统配置初始化与更新流程并统一模型处理

    修改标识：Senparc - 20260705
    修改描述：v0.36.4-preview3 重构系统配置初始化与更新流程并统一模型处理

    修改标识：Senparc - 20260717
    修改描述：v0.37.0-preview5 增强 XNCF 构建、数据库迁移与 AI 生成流程的本地化支持

    修改标识：Senparc - 20260725
    修改描述：移除 cmd.exe 依赖并按 dotnet 命令退出码返回真实生成结果

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.VersionManager;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.Xncf.XncfBuilder.Domain.Models.Services;
using Senparc.Xncf.XncfBuilder.Domain.Services;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Senparc.CO2NET.Helpers;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    [McpServerToolType]
    public partial class BuildXncfAppService : AppServiceBase
    {

        public BuildXncfAppService(IServiceProvider serviceProvider, AIModelService aIModelService, XncfModuleService xncfModuleService, XncfModuleServiceExtension xncfModuleServiceExtension) : base(serviceProvider)
        {
            this._aIModelService = aIModelService;
            this._xncfModuleService = xncfModuleService;
            this._xncfModuleServiceExtension = xncfModuleServiceExtension;
        }

        #region 生成 XNCF 项目

        private readonly AIModelService _aIModelService;
        private readonly XncfModuleService _xncfModuleService;
        private readonly XncfModuleServiceExtension _xncfModuleServiceExtension;

        internal sealed class DotNetCommandResult
        {
            public bool Started { get; init; }
            public int ExitCode { get; init; }
            public string StandardOutput { get; init; } = string.Empty;
            public string StandardError { get; init; } = string.Empty;
        }

        internal static async Task<DotNetCommandResult> ExecuteDotNetCommandAsync(string workingDirectory, IEnumerable<string> args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
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
                    return new DotNetCommandResult
                    {
                        Started = false,
                        ExitCode = -1,
                        StandardError = "进程未能启动。"
                    };
                }
            }
            catch (Exception ex)
            {
                return new DotNetCommandResult
                {
                    Started = false,
                    ExitCode = -1,
                    StandardError = ex.Message
                };
            }

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync().ConfigureAwait(false);

            return new DotNetCommandResult
            {
                Started = true,
                ExitCode = process.ExitCode,
                StandardOutput = await standardOutputTask.ConfigureAwait(false),
                StandardError = await standardErrorTask.ConfigureAwait(false)
            };
        }

        private static string BuildCommandPreview(IEnumerable<string> args)
        {
            return "dotnet " + string.Join(" ", args.Select(arg =>
                string.IsNullOrEmpty(arg) || arg.Any(char.IsWhiteSpace)
                    ? $"\"{arg?.Replace("\"", "\\\"")}\""
                    : arg));
        }

        private static void AppendCommandResult(AppServiceLogger logger, DotNetCommandResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                logger.Append(result.StandardOutput.TrimEnd('\r', '\n'));
            }

            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                logger.Append("Error:");
                logger.Append(result.StandardError.TrimEnd('\r', '\n'));
            }

            logger.Append($"命令退出码：{result.ExitCode}");
        }

        private static async Task<DotNetCommandResult> RunDotNetCommandAsync(
            string operation,
            string workingDirectory,
            IEnumerable<string> args,
            AppServiceLogger logger,
            bool throwOnFailure = true)
        {
            var argumentList = args.ToArray();
            logger.Append($"[{SystemTime.Now}] {operation}");
            logger.Append($"[{SystemTime.Now}] 执行命令：{BuildCommandPreview(argumentList)}");

            var result = await ExecuteDotNetCommandAsync(workingDirectory, argumentList).ConfigureAwait(false);
            AppendCommandResult(logger, result);

            if (throwOnFailure && (!result.Started || result.ExitCode != 0))
            {
                var detail = !string.IsNullOrWhiteSpace(result.StandardError)
                    ? result.StandardError.Trim()
                    : result.StandardOutput.Trim();
                if (detail.Length > 1000)
                {
                    detail = detail[..1000] + "...";
                }

                throw new InvalidOperationException(
                    $"{operation}失败（退出码：{result.ExitCode}）。{(string.IsNullOrEmpty(detail) ? "请查看执行日志。" : $" {detail}")}");
            }

            return result;
        }

        private static string PrepareSolutionFile(BuildXncf_BuildRequest request, AppServiceLogger logger)
        {
            var solutionDirectory = Path.GetDirectoryName(request.SlnFilePath)
                ?? throw new InvalidOperationException("无法获取解决方案文件所在目录。");
            var options = request.NewSlnFile ?? Array.Empty<string>();
            var timestamp = SystemTime.Now.DateTime.ToString("yyyyMMdd_HHmmss");
            var solutionName = Path.GetFileNameWithoutExtension(request.SlnFilePath);
            var solutionExtension = Path.GetExtension(request.SlnFilePath);

            if (options.Contains("new"))
            {
                var newSolutionFilePath = Path.Combine(
                    solutionDirectory,
                    $"{solutionName}-new-{timestamp}{solutionExtension}");
                File.Copy(request.SlnFilePath, newSolutionFilePath);
                logger.Append($"完成 {newSolutionFilePath} 文件创建");
                return newSolutionFilePath;
            }

            if (options.Contains("backup"))
            {
                var backupFilePath = Path.Combine(
                    solutionDirectory,
                    $"{solutionName}-backup-{timestamp}{solutionExtension}");
                File.Copy(request.SlnFilePath, backupFilePath);
                logger.Append($"完成 {backupFilePath} 文件备份");
            }

            return request.SlnFilePath;
        }

        /// <summary>
        /// 执行模板生成
        /// </summary>
        /// <returns></returns>
        private async Task<XncfBuildResult> BuildSampleAsync(BuildXncf_BuildRequest request, AppServiceLogger logger)
        {
            string getLibVersionParam(string dllName, string paramName)
            {
                var dllPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location).LocalPath);
                var xncfBaseVersionPath = Path.Combine(dllPath, dllName);
                var libVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.LoadFrom(xncfBaseVersionPath).Location).ProductVersion;//.ToString();//.ProductVersion;
                return $"{libVersion}";
            }

            string projectName = GetProjectName(request);
            var outputBaseDir = Path.GetDirectoryName(request.SlnFilePath)
                ?? throw new InvalidOperationException("无法获取解决方案文件所在目录。");

            var webProjectFilePath = Path.Combine(outputBaseDir, "Senparc.Web", "Senparc.Web.csproj");
            if (!File.Exists(webProjectFilePath))
            {
                throw new FileNotFoundException("未找到需要添加 XNCF 引用的 Senparc.Web.csproj。", webProjectFilePath);
            }

            #region 检查并安装模板

            var templateListResult = await RunDotNetCommandAsync(
                "检查 XNCF 模板",
                outputBaseDir,
                new[] { "new", "list", "XNCF" },
                logger,
                throwOnFailure: false).ConfigureAwait(false);
            var templateInstalled = templateListResult.Started && templateListResult.ExitCode == 0;

            string packageToInstall = null;
            switch (request.TemplatePackage)
            {
                case "online":
                    logger.Append("配置在线安装 XNCF 模板");
                    packageToInstall = "Senparc.Xncf.XncfBuilder.Template";
                    break;
                case "local":
                    var packageFile = Directory.GetFiles(outputBaseDir, "Senparc.Xncf.XncfBuilder.Template.*.nupkg")
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .FirstOrDefault();
                    if (string.IsNullOrEmpty(packageFile))
                    {
                        logger.Append("本地未找到文件：Senparc.Xncf.XncfBuilder.Template.*.nupkg，转为在线安装");
                        packageToInstall = "Senparc.Xncf.XncfBuilder.Template";
                    }
                    else
                    {
                        logger.Append($"配置本地安装 XNCF 模板：{packageFile}");
                        packageToInstall = packageFile;
                    }
                    break;
                case "no":
                    logger.Append("未要求安装 XNCF 模板");
                    if (!templateInstalled)
                    {
                        logger.Append("未发现已安装模板，转到在线安装");
                        packageToInstall = "Senparc.Xncf.XncfBuilder.Template";
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.TemplatePackage), request.TemplatePackage, "无法识别模板安装选项。");
            }

            if (!string.IsNullOrEmpty(packageToInstall))
            {
                await RunDotNetCommandAsync(
                    "安装 XNCF 模板",
                    outputBaseDir,
                    new[] { "new", "install", packageToInstall },
                    logger).ConfigureAwait(false);

                await RunDotNetCommandAsync(
                    "验证 XNCF 模板",
                    outputBaseDir,
                    new[] { "new", "list", "XNCF" },
                    logger).ConfigureAwait(false);
            }

            #endregion

            #region 从模板安装 XNCF 项目

            var frameworkVersion = string.IsNullOrEmpty(request.OtherFrameworkVersion)
                ? request.FrameworkVersion
                : request.OtherFrameworkVersion;

            string xncfBaseVersion = getLibVersionParam("Senparc.Ncf.XncfBase.dll", "XncfBaseVersion");
            string ncfAreaBaseVersion = getLibVersionParam("Senparc.Ncf.AreaBase.dll", "NcfAreaBaseVersion");

            var useModules = request.UseModule ?? Array.Empty<string>();
            var isUseSample = request.UseSammple;
            var isUseDatabase = isUseSample || useModules.Contains("database");
            var useFunction = useModules.Contains("function");
            var isUseWeb = isUseSample || useModules.Contains("web");
            var useWebApi = useModules.Contains("webapi");
            var projectDirectory = Path.Combine(outputBaseDir, projectName);
            var projectFilePath = Path.Combine(projectDirectory, $"{projectName}.csproj");

            var args = new List<string>
            {
                "new", "XNCF",
                "-n", projectName,
                "-o", projectDirectory,
                "--force", "true",
                "--IntegrationToNcf", "true",
                "--TargetFramework", frameworkVersion,
                "--OrgName", request.OrgName,
                "--XncfName", request.XncfName,
                "--Guid", Guid.NewGuid().ToString().ToUpperInvariant(),
                "--Icon", request.Icon,
                "--Description", request.Description,
                "--Version", request.Version,
                "--MenuName", request.MenuName,
                "--XncfBaseVersion", xncfBaseVersion,
                "--NcfAreaBaseVersion", ncfAreaBaseVersion
            };

            if (isUseSample)
            {
                args.AddRange(new[] { "--Sample", "true" });
            }
            if (useFunction)
            {
                args.AddRange(new[] { "--Function", "true" });
            }
            if (isUseWeb)
            {
                args.AddRange(new[] { "--Web", "true" });
            }
            if (isUseDatabase)
            {
                args.AddRange(new[] { "--Database", "true" });
            }
            if (useWebApi)
            {
                args.AddRange(new[] { "--UseWebApi", "true" });
            }

            await RunDotNetCommandAsync("创建 XNCF 项目", outputBaseDir, args, logger).ConfigureAwait(false);

            if (!File.Exists(projectFilePath))
            {
                throw new FileNotFoundException("dotnet new 已结束，但未找到生成的 XNCF 项目文件。", projectFilePath);
            }

            #endregion

            #region 修改项目文件引用等

            await RunDotNetCommandAsync(
                "添加 Senparc.Web 项目引用",
                outputBaseDir,
                new[] { "add", webProjectFilePath, "reference", projectFilePath },
                logger).ConfigureAwait(false);

            var solutionFilePath = PrepareSolutionFile(request, logger);

            await RunDotNetCommandAsync(
                "将 XNCF 项目添加到解决方案",
                outputBaseDir,
                new[] { "sln", solutionFilePath, "add", projectFilePath, "--solution-folder", "XncfModules" },
                logger).ConfigureAwait(false);

            #endregion

            return new XncfBuildResult(solutionFilePath, projectName);
        }


        /// <summary>
        /// 项目名称
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static string GetProjectName(BuildXncf_BuildRequest request)
        {
            return $"{request.OrgName}.Xncf.{request.XncfName}";
        }

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Generate.Name", "Function.XncfBuilder.Generate.Description", typeof(Register))]
        public async Task<StringAppResponse> Build(BuildXncf_BuildRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Data = "项目生成失败：未提供生成参数。";
                    return null;
                }

                if (!string.Equals(Path.GetExtension(request.SlnFilePath), ".sln", StringComparison.OrdinalIgnoreCase)
                    || !File.Exists(request.SlnFilePath))
                {
                    response.Success = false;
                    response.Data = $"项目生成失败：解决方案文件未找到：{request.SlnFilePath}";
                    logger.Append(response.Data);
                    return null;
                }

                try
                {
                    var buildResult = await BuildSampleAsync(request, logger).ConfigureAwait(false);

                    var configService = base.ServiceProvider.GetService<ConfigService>();
                    configService?.UpdateConfig(request);

                    if (request.StartPreview)
                    {
                        try
                        {
                            var previewService = base.ServiceProvider.GetRequiredService<IXncfPreviewService>();
                            var previewSession = await previewService.StartAsync(
                                new XncfPreviewStartOptions
                                {
                                    SolutionFilePath = buildResult.SolutionFilePath,
                                    ModuleProjectName = buildResult.ProjectName,
                                    Port = request.PreviewPort,
                                    StartupTimeoutSeconds = request.PreviewStartupTimeoutSeconds,
                                    EnvironmentName = request.PreviewEnvironmentName
                                },
                                message => logger.Append(message),
                                base.CancellationToken).ConfigureAwait(false);

                            response.Data = $"项目生成成功！请打开 {buildResult.SolutionFilePath} 解决方案文件查看已附加的项目！<br />" +
                                BuildPreviewStartedHtml(
                                    previewSession,
                                    previewService.GetPersistenceStatus());
                            response.Success = true;
                        }
                        catch (Exception previewException)
                        {
                            response.Success = false;
                            response.StateCode = 101;
                            response.ErrorMessage = previewException.Message;
                            response.Data = $"项目已经生成并添加到 {buildResult.SolutionFilePath}，但独立预览启动失败：{previewException.Message}";
                            logger.Append($"Preview Exception: {previewException.Message}");
                        }
                    }
                    else
                    {
                        response.Data = $"项目生成成功！请打开 {buildResult.SolutionFilePath} 解决方案文件查看已附加的项目！<br />当前运行中的 Senparc.Web 不会自动加载新模块；可使用“启动 / 更新 XNCF 独立预览”进行无重启测试。";
                        response.Success = true;
                    }
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.StateCode = 100;
                    response.ErrorMessage = ex.Message;
                    response.Data = $"项目生成失败：{ex.Message}";
                    logger.Append("Exception:");
                    logger.Append(ex.Message);
                }

                return null;
            });
        }



        #endregion

        private sealed record XncfBuildResult(string SolutionFilePath, string ProjectName);

    }
}
