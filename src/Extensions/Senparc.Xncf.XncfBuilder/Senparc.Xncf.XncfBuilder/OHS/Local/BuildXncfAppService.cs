using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Service;
using Senparc.Xncf.XncfBuilder.Domain.Models.Services;
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
    public class BuildXncfAppService : AppServiceBase
    {
        private string _outPutBaseDir;

        /// <summary>
        /// 执行模板生成
        /// </summary>
        /// <returns></returns>
        private string BuildSample(BuildXncf_BuildRequest request, AppServiceLogger logger)
        {
            Console.WriteLine("开始创建 XNCF 项目");

            string projectName = GetProjectName(request);
            _outPutBaseDir = Path.GetDirectoryName(request.SlnFilePath);  //Path.Combine(Senparc.CO2NET.Config.RootDictionaryPath, ".."/*, $"{projectName}"*/);//找到sln根目录即可
            //_outPutBaseDir = Path.GetFullPath(_outPutBaseDir);
            if (!Directory.Exists(_outPutBaseDir))
            {
                Directory.CreateDirectory(_outPutBaseDir);
            }

            //TODO:参数合法性校验

            //获取类库当前项目引用版本信息
            Func<string, string, string> getLibVersionParam = (dllName, paramName) =>
            {
                var dllPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                var xncfBaseVersionPath = Path.Combine(dllPath, dllName);
                var libVBersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.LoadFrom(xncfBaseVersionPath).Location).ProductVersion;
                return $" --{paramName} {libVBersion}";
            };

            //配置 true 或 false 的参数
            Func<bool, string, string> getBoolParam = (condition, paramName) => $" --{paramName} {(condition ? "true" : "false")}";

            //基础信息  注意： -- 前面统一加 1 个空格
            var orgName = $" --OrgName {request.OrgName}";
            var xncfName = $" --XncfName {request.XncfName}";
            var guid = $" --Guid {Guid.NewGuid().ToString().ToUpper()}";
            var icon = $" --Icon \"{request.Icon}\"";
            var description = $" --Description \"{request.Description}\"";
            var version = $" --Version {request.Version}";
            var menuName = $" --MenuName \"{request.MenuName}\"";
            string xncfBaseVersion = getLibVersionParam("Senparc.Ncf.XncfBase.dll", "XncfBaseVersion");
            string ncfAreaBaseVersion = getLibVersionParam("Senparc.Ncf.AreaBase.dll", "NcfAreaBaseVersion");

            //配置功能
            var isUseSample = request.UseSammple.SelectedValues.Contains("1");
            var isUseDatabase = isUseSample || request.UseModule.SelectedValues.Contains("database");
            var useSample = getBoolParam(isUseSample, "Sample");
            var useFunction = getBoolParam(request.UseModule.SelectedValues.Contains("function"), "Function");
            var isUseWeb = isUseSample || request.UseModule.SelectedValues.Contains("web");
            var useWeb = getBoolParam(isUseWeb, "Web");
            var useDatabase = getBoolParam(isUseDatabase, "Database");
            var useWebApi = getBoolParam(request.UseModule.SelectedValues.Contains("webapi"), "UseWebApi");

            //获取当前配置的 FrameworkVersion
            var frameworkVersion = request.OtherFrameworkVersion.IsNullOrEmpty()
                                        ? request.FrameworkVersion.SelectedValues.First()
                                        : request.OtherFrameworkVersion;
            if (isUseWeb && frameworkVersion == "netstandard2.1")
            {
                //需要使用网页，强制修正为支持 Host 的目标框架
                frameworkVersion = "net6.0";
            }

            var targetFramework = $" --TargetFramework {frameworkVersion}";

            var commandTexts = new List<string> {
                $"cd {_outPutBaseDir}",
                //"echo %DATE:~0,4%-%DATE:~5,2%-%DATE:~8,2% %TIME:~0,2%:%TIME:~3,2%:%TIME:~6,2%",
                //下一句如果上方执行了dotnet new的命令，执行大约需要1分钟
                $"dotnet new XNCF -n {projectName} --force --IntegrationToNcf {targetFramework}{useSample}{useFunction}{useWeb}{useDatabase}{useWebApi} {orgName}{xncfName}{guid}{icon}{description}{version}{menuName}{xncfBaseVersion}{ncfAreaBaseVersion}",
                $"dotnet add ./Senparc.Web/Senparc.Web.csproj reference ./{projectName}/{projectName}.csproj",
                $"dotnet sln {request.SlnFilePath} add ./{projectName}/{projectName}.csproj --solution-folder XncfModules"
            };

            Func<Process> GetNewProcess = () =>
            {
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                return p;
            };

            Action<Process> CloseProcess = p =>
            {
                p.WaitForExit();
                p.Close();
            };

            string strOutput;
            try
            {
                #region 检查并安装模板

                Process pListTemplate = GetNewProcess();
                Console.WriteLine("dotnet new - l ：");
                pListTemplate.StandardInput.WriteLine($"dotnet new -l");
                pListTemplate.StandardInput.WriteLine("exit");//需要执行exit后才能读取 StandardOutput
                var output = pListTemplate.StandardOutput.ReadToEnd();
                CloseProcess(pListTemplate);


                Console.WriteLine("\t" + output);
                var unInstallTemplatePackage = !output.Contains("Custom XNCF Module Template");
                var installPackageCmd = string.Empty;
                switch (request.TemplatePackage.SelectedValues.First())
                {
                    case "online":
                        Console.WriteLine("online");
                        logger.Append("配置在线安装 XNCF 模板");
                        installPackageCmd = $"dotnet new -i Senparc.Xncf.XncfBuilder.Template";
                        break;
                    case "local":
                        Console.WriteLine("local");

                        var slnDir = Directory.GetParent(request.SlnFilePath).FullName;
                        var packageFile = Directory.GetFiles(slnDir, "Senparc.Xncf.XncfBuilder.Template.*.nupkg").LastOrDefault();

                        if (packageFile.IsNullOrEmpty())
                        {
                            logger.Append("本地未找到文件：Senparc.Xncf.XncfBuilder.Template.*.nupkg，转为在线安装");
                            goto case "online";
                        }
                        logger.Append($"配置本地安装 XNCF 模板：{packageFile}");
                        installPackageCmd = $"dotnet new -i {packageFile}";
                        break;
                    case "no":
                        Console.WriteLine("no");

                        logger.Append($"未要求安装 XNCF 模板");
                        if (unInstallTemplatePackage)
                        {
                            logger.Append($"未发现已安装模板，转到在线安装");
                            goto case "online";
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!installPackageCmd.IsNullOrEmpty())
                {
                    var pInstallTemplate = GetNewProcess();
                    logger.Append($"执行 XNCF 模板安装命令：" + installPackageCmd);
                    pInstallTemplate.StandardInput.WriteLine(installPackageCmd);
                    pInstallTemplate.StandardInput.WriteLine("exit");//需要执行exit后才能读取 StandardOutput
                    CloseProcess(pInstallTemplate);
                }
                Console.WriteLine($"[{SystemTime.Now}] finish install template");

                #endregion

                var pCreate = GetNewProcess();

                Console.WriteLine($"[{SystemTime.Now}] start to create xncf");

                foreach (string item in commandTexts)
                {
                    Console.WriteLine("run:" + item);
                    pCreate.StandardInput.WriteLine(item);
                    Console.WriteLine($"[{SystemTime.Now}] run：{item}");
                }
                pCreate.StandardInput.WriteLine("exit");
                Console.WriteLine($"[{SystemTime.Now}] run：exit");

                strOutput = pCreate.StandardOutput.ReadToEnd();
                Console.WriteLine($"[{SystemTime.Now}] ReadToEnd()");

                logger.Append(strOutput);

                //strOutput = Encoding.UTF8.GetString(Encoding.Default.GetBytes(strOutput));
                CloseProcess(pCreate);
            }
            catch (Exception e)
            {
                strOutput = e.Message;
            }


            return strOutput;
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

        public BuildXncfAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        [FunctionRender("生成 XNCF", "根据配置条件生成 XNCF", typeof(Register))]
        public async Task<StringAppResponse> Build(BuildXncf_BuildRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
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
    }
}
