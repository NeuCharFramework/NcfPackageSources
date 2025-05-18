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
        private string BuildSample(BuildXncf_BuildRequest request, AppServiceLogger logger)
        {
            var oldOutputEncoding = Console.OutputEncoding;
            var oldInputEncoding = Console.InputEncoding;
            var newOutputEncoding = Encoding.UTF8;
            var newInputEncoding = Encoding.UTF8;

            Console.OutputEncoding = newOutputEncoding;
            Console.InputEncoding = newInputEncoding;

            string getLibVersionParam(string dllName, string paramName)
            {
                var dllPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                var xncfBaseVersionPath = Path.Combine(dllPath, dllName);
                var libVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.LoadFrom(xncfBaseVersionPath).Location).ProductVersion;
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

            string output;
            try
            {
                #region 检查并安装模板

                var pListTemplate = StartNewProcess();
                logger.Append("dotnet new -l :");
                pListTemplate.StandardInput.WriteLine("dotnet new -l");
                //pListTemplate.StandardInput.WriteLine("exit");
                var templateOutput = pListTemplate.StandardOutput.ReadToEnd();
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
                        var slnDir = Directory.GetParent(request.SlnFilePath).FullName;
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
                    pInstallTemplate.StandardInput.WriteLine(installPackageCmd);
                    pInstallTemplate.StandardInput.WriteLine("exit");
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
                    "--force",
                    "--IntegrationToNcf",
                    "--TargetFramework", frameworkVersion,
                    "--OrgName",request.OrgName,
                    "--XncfName",request.XncfName,
                    "--Guid", $"{Guid.NewGuid().ToString().ToUpper()}",
                    "--Icon",$"\"{request.Icon}\"",
                    "--Description",$"\"{request.Description}\"",
                    "--Version",$"\"{request.Version}\"",
                    "--MenuName",$"\"{request.MenuName}\"",
                    "--XncfBaseVersion",xncfBaseVersion,
                    "--NcfAreaBaseVersion",ncfAreaBaseVersion
                };

                if (isUseSample) args.Add("--Sample");
                if (useFunction) args.Add("--Function");
                if (isUseWeb) args.Add("--Web");
                if (isUseDatabase) args.Add("--Database");
                if (useWebApi) args.Add("--UseWebApi");

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
                var outputStr = BuildSample(request, logger); //执行模板生成
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

        #region MCP AI 接入（由于官方组件 bug，暂时使用平铺参数方式接入

        [McpServerTool,Description("生成 XNCF 模块")]
        [FunctionRender("生成 XNCF", "根据配置条件生成 XNCF", typeof(Register))]
        public async Task<StringAppResponse> Build(
            [Required,Description("解决方案文件路径")]
            string slnFilePath, 
            [Description("组织名称，默认为 Senparc")]
            string orgName, 
            [Required, Description("模块名称")]
            string xncfName, 
            [Required, Description("版本号，默认为 1.0.0")]
            string version, 
            [Required, Description("菜单显示名称")]
            string menuName, 
            [Required, Description("图标，支持 Font Awesome 图标集")]
            string icon, 
            [Description("模块说明")]
            string description    )
        {
            BuildXncf_BuildRequest request = new BuildXncf_BuildRequest() { 
              SlnFilePath = slnFilePath,
              OrgName = orgName,
              XncfName = xncfName,
              Version = version,
              MenuName = menuName,
              Icon = icon,
              Description = description,
              UseSammple =  new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("1","使用示例","使用示例",true),
              }),
              UseModule = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("database","数据库","使用数据库",true),
              }),
            //   UseWeb = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
            //     new Ncf.XncfBase.Functions.SelectionItem("1","使用Web","使用Web",true),
            //   }),
            //   UseWebApi = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
            //     new Ncf.XncfBase.Functions.SelectionItem("1","使用WebApi","使用WebApi",true),
            //   }),
              NewSlnFile = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("backup","备份 .sln 文件（推荐）","如果使用覆盖现有 .sln 文件，对当前文件进行备份",true),
              }),
              TemplatePackage = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.DropDownList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("no","已安装，不需要安装新版本","请确保已经在本地安装过版本（无论新旧），否则将自动从在线获取",true),
              }),
              FrameworkVersion = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.DropDownList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("net8.0","net8.0","使用 .NET 8.0",false),
              })
            };

            return await this.Build(request);

            /*
             *  public string SlnFilePath { get; set; }

        [Description("配置解决方案文件（.sln）||")]
        public SelectionList NewSlnFile { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("backup","备份 .sln 文件（推荐）","如果使用覆盖现有 .sln 文件，对当前文件进行备份",true),
                 new SelectionItem("new","生成新的 .sln 文件","如果不选择，将覆盖现有 .sln 文件（不会影响已有功能，但如果 sln 解决方案正在运行，可能会触发自动重启服务）,并推荐使用备份功能",false),
            });

        [MaxLength(250)]
        [Description("安装新模板||安装 XNCF 的模板，如果重新安装可能需要 30-40s，如果已安装过模板，可选择【已安装】，以节省模板获取时间。")]
        public SelectionList TemplatePackage { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("online","在线获取（从 Nuget.org 等在线环境获取最新版本，时间会略长）","从 Nuget.org 等在线环境获取最新版本，时间会略长",false),
                 new SelectionItem("local","本地安装（从 .sln 同级目录下安装 Senparc.Xncf.XncfBuilder.Template.*.nupkg 包）","从 .sln 同级目录下安装 Senparc.Xncf.XncfBuilder.Template.*.nupkg 包",false),
                 new SelectionItem("no","已安装，不需要安装新版本","请确保已经在本地安装过版本（无论新旧），否则将自动从在线获取",true),
            });

        [Description("目标框架版本||指定项目的 TFM(Target Framework Moniker)")]
        public SelectionList FrameworkVersion { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 //new SelectionItem("netstandard2.1","netstandard2.1","使用 .NET Standard 2.1（兼容 .NET Core 3.1 和 .NET 5.0-8.0）",true),
                 //new SelectionItem("netcoreapp3.1","netcoreapp3.1","使用 .NET Core 3.1",false),
                 //new SelectionItem("net6.0","net6.0","使用 .NET 6.0",false),
                 //new SelectionItem("net7.0","net7.0","使用 .NET 7.0",false),
                 new SelectionItem("net8.0","net8.0","使用 .NET 8.0",false),
            });

        [Description("自定义目标框架版本||其他目标框架版本，如果填写，将覆盖【目标框架版本】的选择")]
        public string OtherFrameworkVersion { get; set; }

        [Required]
        [MaxLength(50)]
        [Description("组织名称||用于作为模块命名空间（及名称）的前缀")]
        public string OrgName { get; set; }

        [Required]
        [MaxLength(50)]
        [Description("模块名称||同时将作为类名（请注意类名规范），支持连续英文大小写和数字，不能以数字开头，不能带有空格和.,/*等特殊符号")]
        public string XncfName { get; set; }

        //[Required]
        //[MaxLength(36)]
        //[Description("Uid||必须确保全局唯一，生成后必须固定")]
        //public string Uid { get; set; }

        [Required]
        [MaxLength(50)]
        [Description("版本号||如：1.0.0、2.0.100-beta1")]
        public string Version { get; set; }

        [Required]
        [MaxLength(50)]
        [Description("菜单名称||如“NCF 生成器”")]
        public string MenuName { get; set; }

        [Required]
        [MaxLength(50)]
        [Description("图标||支持 Font Awesome 图标集，如：fa fa-star")]
        public string Icon { get; set; }

        [Required]
        [MaxLength(400)]
        [Description("说明||模块的说明")]
        public string Description { get; set; }

            */

        }

            #endregion

            #endregion

        }
}
