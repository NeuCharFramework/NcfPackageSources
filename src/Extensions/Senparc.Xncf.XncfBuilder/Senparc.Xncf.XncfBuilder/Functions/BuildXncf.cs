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
                var projectName = $"{typeParam.OrgName}.Xncf.{typeParam.XncfName}";
                _outPutBaseDir = Path.Combine(Senparc.CO2NET.Config.RootDictionaryPath, "..", $"{projectName}");
                _outPutBaseDir = Path.GetFullPath(_outPutBaseDir);
                if (!Directory.Exists(_outPutBaseDir))
                {
                    Directory.CreateDirectory(_outPutBaseDir);
                }

                //定义 Register 主文件
                Senparc.Xncf.XncfBuilder.Templates.Register registerPage = new Senparc.Xncf.XncfBuilder.Templates.Register()
                {
                    OrgName = typeParam.OrgName,
                    XncfName = typeParam.XncfName,
                    Uid = Guid.NewGuid().ToString().ToUpper(),
                    Version = typeParam.Version,
                    MenuName = typeParam.MenuName,
                    Icon = typeParam.Icon,
                    Description = typeParam.Description,
                };

                var useSample = typeParam.UseSammple.SelectedValues.Contains("1");

                #region 使用函数

                //判断是否使用函数（方法）
                var useFunction = typeParam.UseModule.SelectedValues.Contains("function");
                var functionTypes = "";
                if (useFunction)
                {
                    functionTypes = "typeof(MyFunction)";

                    //添加文件夹
                    var dir = AddDir("Functions");
                    Senparc.Xncf.XncfBuilder.Templates.Functions.MyFunction functionPage = new XncfBuilder.Templates.Functions.MyFunction()
                    {
                        OrgName = typeParam.OrgName,
                        XncfName = typeParam.XncfName
                    };
                    WriteContent(functionPage, sb);
                }
                registerPage.FunctionTypes = functionTypes;
                registerPage.UseFunction = useFunction;

                #endregion

                #region 判断 Web - Area
                var useWeb = useSample || typeParam.UseModule.SelectedValues.Contains("web");
                //判断 Area 
                if (useWeb)
                {
                    //生成目录
                    var areaDirs = new List<string> {
                        "Areas",
                        "Areas/Admin",
                        "Areas/Admin/Pages/",
                        $"Areas/Admin/Pages/{typeParam.XncfName}",
                        "Areas/Admin/Pages/Shared",
                    };
                    areaDirs.ForEach(z => AddDir(z));

                    //载入Page
                    var areaPages = new List<IXncfTemplatePage> {
                        new ViewStart(typeParam.OrgName,typeParam.XncfName),
                        new ViewImports(typeParam.OrgName,typeParam.XncfName),
                        new Senparc.Xncf.XncfBuilder.Templates.Areas.Admin.Pages.MyApps.Index(typeParam.OrgName,typeParam.XncfName,typeParam.MenuName),
                        new Index_cs(typeParam.OrgName,typeParam.XncfName),
                    };
                    areaPages.ForEach(z => WriteContent(z, sb));

                    //生成Register.Area
                    var registerArea = new RegisterArea(typeParam.OrgName, typeParam.XncfName, useSample);
                    WriteContent(registerArea, sb);
                }

                #endregion

                #region 判断 数据库

                var useDatabase = useSample || typeParam.UseModule.SelectedValues.Contains("database");
                registerPage.UseDatabase = useDatabase;
                if (useDatabase)
                {
                    //生成目录
                    var dbDirs = new List<string> {
                        "App_Data",
                        "App_Data/Database",
                        "Models",
                        "Models/DatabaseModel",
                        "Models/MultipleDatabase",
                        "Migrations",
                        "Migrations/Migrations.SQLite",
                        "Migrations/Migrations.SqlServer",
                        "Migrations/Migrations.MySql",
                    };
                    dbDirs.ForEach(z => AddDir(z));

                    var initMigrationTime = Templates.Migrations.Migrations.SqlServer.Init.GetFileNamePrefix();

                    //载入Page
                    var dbFiles = new List<IXncfTemplatePage> {
                        new RegisterDatabase(typeParam.OrgName, typeParam.XncfName),
                        new XncfBuilder.Templates.App_Data.Database.SenparcConfig(typeParam.OrgName, typeParam.XncfName),
                        //重复多数据库 - SQLite
                        new MySenparcEntities(typeParam.OrgName, typeParam.XncfName,useSample),
                        new XncfBuilder.Templates.Models.DatabaseModel.SenparcDbContextFactory(typeParam.OrgName, typeParam.XncfName),
                        //重复多数据库 - SqlServer
                        new Templates.Models.MultipleDatabase.SenparcEntities_SqlServer(typeParam.OrgName, typeParam.XncfName),
                          //重复多数据库 - MySql
                        new Templates.Models.MultipleDatabase.SenparcEntities_MySql(typeParam.OrgName, typeParam.XncfName),

                        //重复多数据库 - SQLite
                        new Templates.Migrations.Migrations.SQLite.Init(typeParam.OrgName, typeParam.XncfName, initMigrationTime),
                        new Templates.Migrations.Migrations.SQLite.InitDesigner(typeParam.OrgName, typeParam.XncfName, initMigrationTime),
                        //重复多数据库 - SqlServer
                        new Templates.Migrations.Migrations.SqlServer.Init(typeParam.OrgName, typeParam.XncfName, initMigrationTime),
                        new Templates.Migrations.Migrations.SqlServer.InitDesigner(typeParam.OrgName, typeParam.XncfName, initMigrationTime),
                         //重复多数据库 - MySql
                        new Templates.Migrations.Migrations.MySql.Init(typeParam.OrgName, typeParam.XncfName, initMigrationTime),
                        new Templates.Migrations.Migrations.MySql.InitDesigner(typeParam.OrgName, typeParam.XncfName, initMigrationTime),
                    };
                    dbFiles.ForEach(z => WriteContent(z, sb));
                }

                #endregion

                #region 安装 Sample
                if (useSample)
                {
                    var sampleDirs = new List<string> {
                        "Models/DatabaseModel/Dto",
                        "Models/DatabaseModel/Mapping",
                        "Services",
                    };
                    sampleDirs.ForEach(z => AddDir(z));

                    var sampleMigrationTime = Templates.Migrations.Migrations.SqlServer.Init.GetFileNamePrefix(SystemTime.Now.DateTime.AddSeconds(1));

                    //载入Page

                    var sampleFiles = new List<IXncfTemplatePage> {
                        //重复多数据库 - SQLite
                        new Templates.Migrations.Migrations.SQLite.AddSample(typeParam.OrgName, typeParam.XncfName,sampleMigrationTime),
                        new Templates.Migrations.Migrations.SQLite.AddSampleDesigner(typeParam.OrgName, typeParam.XncfName,sampleMigrationTime),
                        //重复多数据库 - SQLServer
                        new Templates.Migrations.Migrations.SqlServer.AddSample(typeParam.OrgName, typeParam.XncfName,sampleMigrationTime),
                        new Templates.Migrations.Migrations.SqlServer.AddSampleDesigner(typeParam.OrgName, typeParam.XncfName,sampleMigrationTime),
                        //重复多数据库 - MySql
                        new Templates.Migrations.Migrations.MySql.AddSample(typeParam.OrgName, typeParam.XncfName,sampleMigrationTime),
                        new Templates.Migrations.Migrations.MySql.AddSampleDesigner(typeParam.OrgName, typeParam.XncfName,sampleMigrationTime),


                        new ColorDto(typeParam.OrgName, typeParam.XncfName),
                        new Sample_ColorConfigurationMapping(typeParam.OrgName, typeParam.XncfName),

                        new Color(typeParam.OrgName, typeParam.XncfName),
                        new ColorService(typeParam.OrgName, typeParam.XncfName),

                        new DatabaseSample(typeParam.OrgName, typeParam.XncfName,typeParam.MenuName),
                        new DatabaseSample_cs(typeParam.OrgName, typeParam.XncfName,typeParam.MenuName),

                        new _SideMenu(typeParam.OrgName, typeParam.XncfName)
                    };
                    sampleFiles.ForEach(z => WriteContent(z, sb));

                    //Sample快照
                    //重复多数据库 - SQLite
                    var addSampleSnapshot = new Templates.Migrations.Migrations.SQLite.SenparcEntitiesModelSnapshotForAddSample(typeParam.OrgName, typeParam.XncfName);
                    WriteContent(addSampleSnapshot, sb);

                    //重复多数据库 - SQL Server
                    var addSampleSnapshot_SqlServer = new Templates.Migrations.Migrations.SqlServer.SenparcEntitiesModelSnapshotForAddSample(typeParam.OrgName, typeParam.XncfName);
                    WriteContent(addSampleSnapshot_SqlServer, sb);

                    //重复多数据库 - MySQL
                    var addSampleSnapshot_MySql = new Templates.Migrations.Migrations.MySql.SenparcEntitiesModelSnapshotForAddSample(typeParam.OrgName, typeParam.XncfName);
                    WriteContent(addSampleSnapshot_MySql, sb);

                }
                else if (useDatabase)
                {
                    //默认 Init 快照
                    //重复多数据库 - SQLite
                    var initSnapshot = new Templates.Migrations.Migrations.SQLite.SenparcEntitiesModelSnapshotForInit(typeParam.OrgName, typeParam.XncfName);
                    WriteContent(initSnapshot, sb);

                    //重复多数据库 - SQL Server
                    var initSnapshot_SqlServer = new Templates.Migrations.Migrations.SqlServer.SenparcEntitiesModelSnapshotForInit(typeParam.OrgName, typeParam.XncfName);
                    WriteContent(initSnapshot_SqlServer, sb);

                    //重复多数据库 - MySQL
                    var initSnapshot_MySql = new Templates.Migrations.Migrations.MySql.SenparcEntitiesModelSnapshotForInit(typeParam.OrgName, typeParam.XncfName);
                    WriteContent(initSnapshot_MySql, sb);
                }

                #endregion

                #region 生成 Register 主文件

                registerPage.UseSample = useSample;
                WriteContent(registerPage, sb);

                #endregion

                #region 生成 .csproj

                string areaBaseVersion = "";
                try
                {
                    //areaBaseVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetAssembly(Type.GetType("Senparc.Ncf.AreaBase.Admin.AdminPageModelBase,Senparc.Ncf.AreaBase")).Location).ProductVersion;
                    var dllPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                    var areaBaseDllPath = Path.Combine(dllPath, "Senparc.Ncf.AreaBase.dll");
                    areaBaseVersion =
                    FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.LoadFrom(areaBaseDllPath).Location).ProductVersion;
                }
                catch
                {
                }


                //生成 .csproj
                Senparc.Xncf.XncfBuilder.Templates.csproj csprojPage = new csproj()
                {
                    OrgName = typeParam.OrgName,
                    XncfName = typeParam.XncfName,
                    Version = typeParam.Version,
                    MenuName = typeParam.MenuName,
                    Description = typeParam.Description,
                    UseWeb = useWeb,
                    UseDatabase = useDatabase,
                    AreaBaseVersion = areaBaseVersion,
                    XncfBaseVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetAssembly(typeof(Senparc.Ncf.XncfBase.Register)).Location).ProductVersion,
                };

                //获取当前配置的 FrameworkVersion
                var frameworkVersion = typeParam.OtherFrameworkVersion.IsNullOrEmpty()
                                            ? typeParam.FrameworkVersion.SelectedValues.First()
                                            : typeParam.OtherFrameworkVersion;
                if (useWeb && frameworkVersion == "netstandard2.1")
                {
                    //需要使用网页，强制修正为支持 Host 的目标框架
                    frameworkVersion = "netcoreapp3.1";
                }
                csprojPage.FrameworkVersion = frameworkVersion;

                WriteContent(csprojPage, sb);

                #endregion

                #region 自动附加项目

                var webProjFilePath = Path.GetFullPath(Path.Combine(Senparc.CO2NET.Config.RootDictionaryPath, "Senparc.Web.csproj"));
                if (File.Exists(webProjFilePath))
                {
                    XDocument webCsproj = XDocument.Load(webProjFilePath);
                    if (!webCsproj.ToString().Contains(csprojPage.ProjectFilePath))
                    {
                        var referenceNode = new XElement("ProjectReference");
                        referenceNode.Add(new XAttribute("Include", $"..\\{csprojPage.ProjectFilePath}"));
                        var newNode = new XElement("ItemGroup", referenceNode);
                        webCsproj.Root.Add(newNode);
                        webCsproj.Save(webProjFilePath);
                    }
                }

                #endregion

                #region 生成 .sln

                //生成 .sln
                if (!typeParam.SlnFilePath.ToUpper().EndsWith(".SLN"))
                {
                    result.Success = false;
                    result.Message = $"解决方案文件未找到，请手动引用项目 {csprojPage.RelativeFilePath}";
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

                    //修改 new Sln
                    string slnContent = null;
                    using (FileStream fs = new FileStream(newSlnFilePath, FileMode.Open))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            slnContent = sr.ReadToEnd();
                            sr.Close();
                        }
                    }

                    var projGuid = Guid.NewGuid().ToString("B").ToUpper();//ex. {76FC86E0-991D-47B7-8AAB-42B27DAB10C1}
                    slnContent = slnContent.Replace(@"Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Senparc.Core"", ""Senparc.Core\Senparc.Core.csproj"", ""{D0EF2816-B99A-4554-964A-6EA6814B3A36}""
EndProject", @$"Project(""{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}"") = ""Senparc.Core"", ""Senparc.Core\Senparc.Core.csproj"", ""{{D0EF2816-B99A-4554-964A-6EA6814B3A36}}""
EndProject
Project(""{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}"") = ""{projectName}"", ""{projectName}\{csprojPage.RelativeFilePath}"", ""{projGuid}""
EndProject
").Replace(@"		{D0EF2816-B99A-4554-964A-6EA6814B3A36}.Test|Any CPU.Build.0 = Release|Any CPU
", @$"		{{D0EF2816-B99A-4554-964A-6EA6814B3A36}}.Test|Any CPU.Build.0 = Release|Any CPU
		{projGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{projGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{projGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{projGuid}.Release|Any CPU.Build.0 = Release|Any CPU
		{projGuid}.Test|Any CPU.ActiveCfg = Release|Any CPU
		{projGuid}.Test|Any CPU.Build.0 = Release|Any CPU
").Replace(@"GlobalSection(NestedProjects) = preSolution", @$"GlobalSection(NestedProjects) = preSolution
		{projGuid} = {{76FC86E0-991D-47B7-8AAB-42B27DAB10C1}}");
                    System.IO.File.WriteAllText(newSlnFilePath, slnContent, Encoding.UTF8);
                    sb.AppendLine($"已创建新的解决方案文件：{newSlnFilePath}");
                    result.Message = $"项目生成成功！请打开  {newSlnFilePath} 解决方案文件查看已附加的项目！";
                }
                else
                {
                    result.Success = false;
                    result.Message = $"解决方案文件未找到，请手动引用项目 {csprojPage.RelativeFilePath}";
                    sb.AppendLine($"操作未全部完成：{result.Message}");
                }

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

                #endregion
            });
        }
    }
}
