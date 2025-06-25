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

        private string _outPutBaseDir;
        private readonly AIModelService _aIModelService;
        private readonly XncfModuleService _xncfModuleService;
        private readonly XncfModuleServiceExtension _xncfModuleServiceExtension;

        /// <summary>
        /// 执行模板生成
        /// </summary>
        /// <returns></returns>
        private async Task<string> BuildSampleAsync(BuildXncf_BuildRequest request, AppServiceLogger logger)
        {
            var oldOutputEncoding = Console.OutputEncoding;
            var oldInputEncoding = Console.InputEncoding;
            var newOutputEncoding = Encoding.UTF8;
            var newInputEncoding = Encoding.UTF8;

            Console.OutputEncoding = newOutputEncoding;
            Console.InputEncoding = newInputEncoding;

            string getLibVersionParam(string dllName, string paramName)
            {
                var dllPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location).LocalPath);
                var xncfBaseVersionPath = Path.Combine(dllPath, dllName);
                var libVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.LoadFrom(xncfBaseVersionPath).Location).ToString();//.ProductVersion;
                return $"{libVersion}";
            }

            Process StartNewProcess(string fileName = "cmd.exe")
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardInputEncoding = newInputEncoding,
                        StandardOutputEncoding = newOutputEncoding,
                        StandardErrorEncoding = newOutputEncoding
                    }
                };
                p.Start();
                return p;
            }

            void CloseProcess(Process p)
            {
                p.WaitForExit();
                p.Close();
            }

            async Task<string> ReadOutputAsync(Process process)
            {
                var output = new StringBuilder();
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    output.AppendLine(line);
                }
                return output.ToString();
            }

            string output;
            try
            {
                #region 检查并安装模板

                var pListTemplate = StartNewProcess();
                logger.Append("dotnet new -l :");
                await pListTemplate.StandardInput.WriteLineAsync("dotnet new -l").ConfigureAwait(false);

                await Task.Delay(2000);

                await pListTemplate.StandardInput.WriteLineAsync("exit").ConfigureAwait(false);

                // Read output asynchronously while the process is running.
                //var templateOutputTask = ReadOutputAsync(pListTemplate);

                // Await the completion of the output reading task.

                //pListTemplate.StandardInput.WriteLine("exit");
                var templateOutput = await pListTemplate.StandardOutput.ReadToEndAsync().ConfigureAwait(false);//await templateOutputTask; //pListTemplate.StandardOutput.ReadToEnd();
                CloseProcess(pListTemplate);

                logger.Append(templateOutput);
                var unInstallTemplatePackage = !templateOutput.Contains("Custom XNCF Module Template");
                var installPackageCmd = string.Empty;
                switch (request.TemplatePackage.SelectedValues.First())
                {
                    case "online":
                        logger.Append("配置在线安装 XNCF 模板");
                        installPackageCmd = $"dotnet new -i Senparc.Xncf.XncfBuilder.Template";
                        break;
                    case "local":
                        string slnDir = Directory.GetParent(request.SlnFilePath).FullName;
                        var packageFile = Directory.GetFiles(slnDir, "Senparc.Xncf.XncfBuilder.Template.*.nupkg").LastOrDefault();
                        if (string.IsNullOrEmpty(packageFile))
                        {
                            logger.Append("本地未找到文件：Senparc.Xncf.XncfBuilder.Template.*.nupkg，转为在线安装");
                            goto case "online";
                        }
                        logger.Append($"配置本地安装 XNCF 模板：{packageFile}");
                        installPackageCmd = $"dotnet new -i {packageFile}";
                        break;
                    case "no":
                        logger.Append("未要求安装 XNCF 模板");
                        if (unInstallTemplatePackage)
                        {
                            logger.Append("未发现已安装模板，转到在线安装");
                            goto case "online";
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!string.IsNullOrEmpty(installPackageCmd))
                {
                    var pInstallTemplate = StartNewProcess();
                    logger.Append($"执行 XNCF 模板安装命令：" + installPackageCmd);
                    await pInstallTemplate.StandardInput.WriteLineAsync(installPackageCmd).ConfigureAwait(false);
                    await pInstallTemplate.StandardInput.WriteLineAsync("exit").ConfigureAwait(false);
                    CloseProcess(pInstallTemplate);
                }
                #endregion

                #region 从模板安装 XNCF 项目

                Console.WriteLine("开始创建 XNCF 项目");
                string projectName = GetProjectName(request);
                _outPutBaseDir = Path.GetDirectoryName(request.SlnFilePath);

                if (!Directory.Exists(_outPutBaseDir))
                {
                    Directory.CreateDirectory(_outPutBaseDir);
                }

                var frameworkVersion = string.IsNullOrEmpty(request.OtherFrameworkVersion)
                    ? request.FrameworkVersion.SelectedValues.First()
                    : request.OtherFrameworkVersion;

                string xncfBaseVersion = getLibVersionParam("Senparc.Ncf.XncfBase.dll", "XncfBaseVersion");
                string ncfAreaBaseVersion = getLibVersionParam("Senparc.Ncf.AreaBase.dll", "NcfAreaBaseVersion");

                var isUseSample = request.UseSammple.SelectedValues.Contains("1");
                var isUseDatabase = isUseSample || request.UseModule.SelectedValues.Contains("database");
                //var useSample = getBoolParam(isUseSample, "Sample");
                var useFunction = request.UseModule.SelectedValues.Contains("function");
                var isUseWeb = isUseSample || request.UseModule.SelectedValues.Contains("web");
                //var useWeb = getBoolParam(isUseWeb, "Web");
                //var useDatabase = getBoolParam(isUseDatabase, "Database");
                var useWebApi = request.UseModule.SelectedValues.Contains("webapi");

                //采用一个独立的进程
                var args = new List<string>
                {
                    "new", "XNCF",
                    //$"-n {projectName} --force --IntegrationToNcf {targetFramework} {useSample} {useFunction} {useWeb} {useDatabase} {useWebApi} {orgName} {xncfName} {guid} {icon} {description} {version} {menuName} {xncfBaseVersion} {ncfAreaBaseVersion}"
                    "-n", projectName,
                    "-o",Path.Combine(_outPutBaseDir,projectName),
                    "--force","true",
                    "--IntegrationToNcf","true",
                    "--TargetFramework", frameworkVersion,
                    "--OrgName",request.OrgName,
                    "--XncfName",request.XncfName,
                    "--Guid", $"{Guid.NewGuid().ToString().ToUpper()}",
                    "--Icon",$"{request.Icon}",
                    "--Description",$"{request.Description}",
                    "--Version",$"{request.Version}",
                    "--MenuName",$"{request.MenuName}",
                    "--XncfBaseVersion",xncfBaseVersion,
                    "--NcfAreaBaseVersion",ncfAreaBaseVersion
                };

                if (isUseSample)
                {
                    args.Add("--Sample");
                    args.Add("1");
                }
                if (useFunction)
                {
                    args.Add("--Function");
                    args.Add("1");

                }
                if (isUseWeb)
                {
                    args.Add("--Web");
                    args.Add("1");
                }
                if (isUseDatabase)
                {
                    args.Add("--Database");
                    args.Add("1");
                }
                if (useWebApi)
                {
                    args.Add("--UseWebApi");
                    args.Add("1");
                }

                var pDotnet = StartNewProcess("dotnet");

                foreach (var arg in args)
                {
                    pDotnet.StartInfo.ArgumentList.Add(arg);
                }
                //pDotnet.StandardInput.WriteLine(command);
                pDotnet.Start();

                logger.Append($"[{SystemTime.Now}] 开始创建 XNCF 项目");
                logger.Append($"[{SystemTime.Now}] 执行命令：dotnet {string.Join(" ", args)}");
                Console.WriteLine($"[{SystemTime.Now}] 执行命令：dotnet {string.Join(" ", args)}");

                string outputDotNet = pDotnet.StandardOutput.ReadToEnd();
                logger.Append(outputDotNet);
                string errorDotNet = pDotnet.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(errorDotNet))
                {
                    logger.Append("Error:");
                    logger.Append(errorDotNet);
                }

                CloseProcess(pDotnet);

                #endregion

                #region 修改项目文件引用等

                var commandTexts = new List<string>
                {
                    "chcp 65001",
                    $"cd {_outPutBaseDir}",
                    //$"dotnet new XNCF ",//仅作为标记
                    $"dotnet add ./Senparc.Web/Senparc.Web.csproj reference ./{projectName}/{projectName}.csproj",
                    $"dotnet sln \"{request.SlnFilePath}\" add \"./{projectName}/{projectName}.csproj\" --solution-folder XncfModules"
                };

                var pCreate = StartNewProcess();
                logger.Append($"[{SystemTime.Now}] start to create xncf");
                foreach (var command in commandTexts)
                {
                    pCreate.StandardInput.WriteLine(command);
                    logger.Append($"[{SystemTime.Now}] run：{command}");
                }
                pCreate.StandardInput.WriteLine("exit");
                logger.Append($"[{SystemTime.Now}] run：exit");

                output = pCreate.StandardOutput.ReadToEnd();
                logger.Append(output);
                var error = pCreate.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error))
                {
                    logger.Append("Error:");
                    logger.Append(error);
                }
                CloseProcess(pCreate);
                #endregion
            }
            catch (Exception e)
            {
                output = e.Message;
                logger.Append("Exception:");
                logger.Append(e.Message);
            }
            finally
            {
                Console.OutputEncoding = oldOutputEncoding;
                Console.InputEncoding = oldInputEncoding;
            }

            return output;
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

        [FunctionRender("生成 XNCF", "根据配置条件生成 XNCF", typeof(Register))]
        public async Task<StringAppResponse> Build(BuildXncf_BuildRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var outputStr = await BuildSampleAsync(request, logger); //执行模板生成
                var projectFilePath = $"{request.OrgName}.Xncf.{request.XncfName}\\{request.OrgName}.Xncf.{request.XncfName}.csproj";

                #region 生成 .sln

                var relativeFilePath = $"{request.OrgName}.Xncf.{request.XncfName}.csproj";

                //生成 .sln
                if (!request.SlnFilePath.ToUpper().EndsWith(".SLN"))
                {
                    response.Success = false;
                    response.Data = $"解决方案文件未找到，请手动引用项目 {projectFilePath}";
                    logger.Append($"操作未全部完成：{response.Data}");
                }
                else if (File.Exists(request.SlnFilePath))
                {
                    //是否创建新的 .sln 文件
                    var useNewSlnFile = request.NewSlnFile.SelectedValues.Contains("new");

                    var slnFileName = Path.GetFileName(request.SlnFilePath);
                    string newSlnFileName = slnFileName;
                    string newSlnFilePath = request.SlnFilePath;
                    if (useNewSlnFile)
                    {
                        newSlnFileName = $"{slnFileName}-new-{SystemTime.Now.DateTime.ToString("yyyyMMdd_HHmmss")}.sln";
                        newSlnFilePath = Path.Combine(Path.GetDirectoryName(request.SlnFilePath), newSlnFileName);
                        File.Copy(request.SlnFilePath, newSlnFilePath);
                        logger.Append($"完成 {newSlnFilePath} 文件创建");
                    }
                    else
                    {
                        var backupSln = request.NewSlnFile.SelectedValues.Contains("backup");
                        var backupFileName = $"{slnFileName}-backup-{SystemTime.Now.DateTime.ToString("yyyyMMdd_HHmmss")}.sln";
                        var backupFilePath = Path.Combine(Path.GetDirectoryName(request.SlnFilePath), backupFileName);
                        File.Copy(request.SlnFilePath, backupFilePath);
                        logger.Append($"完成 {newSlnFilePath} 文件备份");
                    }

                    response.Data = $"项目生成成功！请打开  {newSlnFilePath} 解决方案文件查看已附加的项目！<br />注意：如果您操作的项目此刻正在运行中，可能会引发重新编译，导致您看到的这个页面可能已失效。";
                    response.Success = true;
                }
                else
                {
                    response.Success = false;
                    response.Data = $"解决方案文件未找到，请手动引用项目 {relativeFilePath}";
                    logger.Append($"操作未全部完成：{response.Data}");
                }


                Console.WriteLine(outputStr);
                Console.WriteLine(logger.ToString());

                #endregion

                #region 将当前设置保存到数据库

                var configService = base.ServiceProvider.GetService<ConfigService>();
                configService.UpdateConfig(request);

                #endregion

                return null;
            });
        }



        #endregion

    }
}
