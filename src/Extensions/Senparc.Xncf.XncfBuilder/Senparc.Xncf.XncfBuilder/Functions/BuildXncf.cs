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
using Senparc.CO2NET.Extensions;

namespace Senparc.Xncf.XncfBuilder.Functions
{
    public class BuildXncf : FunctionBase
    {
        public BuildXncf(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public class Parameters : FunctionParameterLoadDataBase, IFunctionParameter
        {
            [Required]
            [MaxLength(250)]
            [Description("解决方案文件（.sln）路径||输入 NCF 项目的解决方案（.sln）文件完整物理路径，将在其并列位置生成模块目录，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\NCF.sln")]
            public string SlnFilePath { get; set; }

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
                 new SelectionItem("netstandard2.1","netstandard2.1","使用 .NET Standard 2.1（兼容 .NET Core 3.1 和 .NET 5）",true),
                 new SelectionItem("netcoreapp3.1","netcoreapp3.1","使用 .NET Core 3.1",false),
                 new SelectionItem("net5.0","net5.0","使用 .NET 5.0",false),
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
            [Description("版本号||如：1.0、2.0-beta1")]
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

            [Description("功能配置||")]
            public SelectionList UseModule { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("function","配置“函数”功能","是否需要使用函数模块（Function）",false),
                 new SelectionItem("database","配置“数据库”功能","是否需要使用数据库模块（Database），将配置空数据库",false),
                 new SelectionItem("web","配置“Web（Area） 页面”功能","是否需要使用 Web 页面模块（Web），如果选择，将忽略 .NET Standard 2.1配置，强制使用 .NET Core 3.1（默认），或 .NET 5（需要选中）",false),
            });

            [Description("安装 Sample||")]
            public SelectionList UseSammple { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","是","是否安装数据库示例，由于展示需要，将自动安装上述“数据库”、“Web（Area） 页面”功能",false),
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
                    //低版本没有数据库，此处需要try
                    var configService = serviceProvider.GetService<ServiceBase<Config>>();
                    var config = await configService.GetObjectAsync(z => true);
                    if (config != null)
                    {
                        configService.Mapper.Map(config, this);
                    }
                }
                catch
                {
                }

            }
        }

        public override string Name => "生成 XNCF";

        public override string Description => "根据配置条件生成 XNCF";

        public override Type FunctionParameterType => typeof(Parameters);

        private string _outPutBaseDir;


        /// <summary>
        /// 执行模板生成
        /// </summary>
        /// <returns></returns>
        private string BuildSample(Parameters typeParam, ref StringBuilder sb)
        {
            Console.WriteLine("开始创建 XNCF 项目");

            string projectName = GetProjectName(typeParam);
            _outPutBaseDir = Path.GetDirectoryName(typeParam.SlnFilePath);  //Path.Combine(Senparc.CO2NET.Config.RootDictionaryPath, ".."/*, $"{projectName}"*/);//找到sln根目录即可
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
            var orgName = $" --OrgName {typeParam.OrgName}";
            var xncfName = $" --XncfName {typeParam.XncfName}";
            var guid = $" --Guid {Guid.NewGuid().ToString().ToUpper()}";
            var icon = $" --Icon \"{typeParam.Icon}\"";
            var description = $" --Description \"{typeParam.Description}\"";
            var version = $" --Version {typeParam.Version}";
            var menuName = $" --MenuName \"{typeParam.MenuName}\"";
            string xncfBaseVersion = getLibVersionParam("Senparc.Ncf.XncfBase.dll", "XncfBaseVersion");
            string ncfAreaBaseVersion = getLibVersionParam("Senparc.Ncf.AreaBase.dll", "NcfAreaBaseVersion");

            //配置功能
            var isUseSample = typeParam.UseSammple.SelectedValues.Contains("1");
            var useSample = getBoolParam(isUseSample, "UseSample");
            var useFunction = getBoolParam(typeParam.UseModule.SelectedValues.Contains("function"), "UseFunction");
            var isUseWeb = isUseSample || typeParam.UseModule.SelectedValues.Contains("web");
            var useWeb = getBoolParam(isUseWeb, "UseWeb");
            var useDatabase = getBoolParam(isUseSample, "UseDatabase");

            //获取当前配置的 FrameworkVersion
            var frameworkVersion = typeParam.OtherFrameworkVersion.IsNullOrEmpty()
                                        ? typeParam.FrameworkVersion.SelectedValues.First()
                                        : typeParam.OtherFrameworkVersion;
            if (isUseWeb && frameworkVersion == "netstandard2.1")
            {
                //需要使用网页，强制修正为支持 Host 的目标框架
                frameworkVersion = "netcoreapp3.1";//TODO:后续将支持 .NET 5.0
            }

            var targetFramework = $" --TargetFramework {frameworkVersion}";

            var commandTexts = new List<string> {
                $"cd {_outPutBaseDir}",
                $"dotnet new xncf -n {projectName} --force --IntegrationToNcf {targetFramework}{useSample}{useFunction}{useWeb}{useDatabase} {orgName}{xncfName}{guid}{icon}{description}{version}{menuName}{xncfBaseVersion}{ncfAreaBaseVersion}",
                $"dotnet add ./Senparc.Web/Senparc.Web.csproj reference ./{projectName}/{projectName}.csproj",
                $"dotnet sln {typeParam.SlnFilePath} add ./{projectName}/{projectName}.csproj --solution-folder XncfModules"
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

            Process p;
            string strOutput;
            try
            {
                #region 检查并安装模板

                p = GetNewProcess();
                Console.WriteLine("dotnet new - l ：");
                p.StandardInput.WriteLine($"dir");
                p.StandardInput.WriteLine($"dotnet new -l");
                p.StandardInput.WriteLine("exit");//需要执行exit后才能读取 StandardOutput
                var output = p.StandardOutput.ReadToEnd();
                CloseProcess(p);

                p = GetNewProcess();

                Console.WriteLine("\t" + output);
                var unInstallTemplatePackage = !output.Contains("Custom XNCF Module Template");
                var installPackageCmd = string.Empty;
                switch (typeParam.TemplatePackage.SelectedValues.First())
                {
                    case "online":
                        Console.WriteLine("online");
                        base.RecordLog(sb, "配置在线安装 XNCF 模板");
                        installPackageCmd = $"dotnet new -i Senparc.Xncf.XncfBuilder.Template";
                        break;
                    case "local":
                        Console.WriteLine("local");

                        var slnDir = Directory.GetParent(typeParam.SlnFilePath).FullName;
                        var packageFile = Directory.GetFiles(slnDir, "Senparc.Xncf.XncfBuilder.Template.*.nupkg").LastOrDefault();

                        if (packageFile.IsNullOrEmpty())
                        {
                            base.RecordLog(sb, "本地未找到文件：Senparc.Xncf.XncfBuilder.Template.*.nupkg，转为在线安装");
                            goto case "online";
                        }
                        base.RecordLog(sb, $"配置本地安装 XNCF 模板：{packageFile}");
                        installPackageCmd = $"dotnet new -i {packageFile}";
                        break;
                    case "no":
                        Console.WriteLine("no");

                        base.RecordLog(sb, $"未要求安装 XNCF 模板");
                        if (unInstallTemplatePackage)
                        {
                            base.RecordLog(sb, $"未发现已安装模板，转到在线安装");
                            goto case "online";
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!installPackageCmd.IsNullOrEmpty())
                {
                    base.RecordLog(sb, $"执行 XNCF 模板安装命令：" + installPackageCmd);
                    p.StandardInput.WriteLine(installPackageCmd);
                }

                #endregion

                p = GetNewProcess();
                foreach (string item in commandTexts)
                {
                    Console.WriteLine("run:" + item);
                    p.StandardInput.WriteLine(item);
                }
                p.StandardInput.WriteLine("exit");
                strOutput = p.StandardOutput.ReadToEnd();
                base.RecordLog(sb, strOutput);

                //strOutput = Encoding.UTF8.GetString(Encoding.Default.GetBytes(strOutput));
                CloseProcess(p);
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
        /// <param name="typeParam"></param>
        /// <returns></returns>
        private static string GetProjectName(Parameters typeParam)
        {
            return $"{typeParam.OrgName}.Xncf.{typeParam.XncfName}";
        }

        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<Parameters>(param, (typeParam, sb, result) =>
            {
                var outputStr = BuildSample(typeParam, ref sb); //执行模板生成
                var projectFilePath = $"{typeParam.OrgName}.Xncf.{typeParam.XncfName}\\{typeParam.OrgName}.Xncf.{typeParam.XncfName}.csproj";

                #region 生成 .sln

                var relativeFilePath = $"{typeParam.OrgName}.Xncf.{typeParam.XncfName}.csproj";

                //生成 .sln
                if (!typeParam.SlnFilePath.ToUpper().EndsWith(".SLN"))
                {
                    result.Success = false;
                    result.Message = $"解决方案文件未找到，请手动引用项目 {projectFilePath}";
                    sb.AppendLine($"操作未全部完成：{result.Message}");
                }
                else if (File.Exists(typeParam.SlnFilePath))
                {
                    //是否创建新的 .sln 文件
                    var useNewSlnFile = typeParam.NewSlnFile.SelectedValues.Contains("new");

                    var slnFileName = Path.GetFileName(typeParam.SlnFilePath);
                    string newSlnFileName = slnFileName;
                    string newSlnFilePath = typeParam.SlnFilePath;
                    if (useNewSlnFile)
                    {
                        newSlnFileName = $"{slnFileName}-new-{SystemTime.Now.DateTime.ToString("yyyyMMdd_HHmmss")}.sln";
                        newSlnFilePath = Path.Combine(Path.GetDirectoryName(typeParam.SlnFilePath), newSlnFileName);
                        File.Copy(typeParam.SlnFilePath, newSlnFilePath);
                        sb.AppendLine($"完成 {newSlnFilePath} 文件创建");
                    }
                    else
                    {
                        var backupSln = typeParam.NewSlnFile.SelectedValues.Contains("backup");
                        var backupFileName = $"{slnFileName}-backup-{SystemTime.Now.DateTime.ToString("yyyyMMdd_HHmmss")}.sln";
                        var backupFilePath = Path.Combine(Path.GetDirectoryName(typeParam.SlnFilePath), backupFileName);
                        File.Copy(typeParam.SlnFilePath, backupFilePath);
                        sb.AppendLine($"完成 {newSlnFilePath} 文件备份");
                    }


                    result.Message = $"项目生成成功！请打开  {newSlnFilePath} 解决方案文件查看已附加的项目！<br />注意：如果您操作的项目此刻正在运行中，可能会引发重新编译，导致您看到的这个页面可能已失效。";
                }
                else
                {
                    result.Success = false;
                    result.Message = $"解决方案文件未找到，请手动引用项目 {relativeFilePath}";
                    sb.AppendLine($"操作未全部完成：{result.Message}");
                }


                Console.WriteLine(outputStr);
                Console.WriteLine(sb.ToString());

                #endregion

                #region 将当前设置保存到数据库

                var configService = base.ServiceProvider.GetService<ServiceBase<Config>>();
                var config = configService.GetObject(z => true);
                if (config == null)
                {
                    config = new Config(typeParam.SlnFilePath, typeParam.OrgName, typeParam.XncfName, typeParam.Version, typeParam.MenuName, typeParam.Icon);
                }
                else
                {
                    configService.Mapper.Map(typeParam, config);
                }
                configService.SaveObject(config);

                //result.Message += "\r\n\r\n" + outputStr;

                #endregion
            });
        }
    }
}
