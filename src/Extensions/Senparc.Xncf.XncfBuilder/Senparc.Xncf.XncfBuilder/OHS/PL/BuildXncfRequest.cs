using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class BuildXncf_BuildRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(250)]
        [Description("解决方案文件（.sln）路径||输入 NCF 项目的解决方案（.sln）文件完整物理路径，将在其并列位置生成模块目录，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\back-end\\NCF.sln")]
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
                 //new SelectionItem("netcoreapp3.1","netcoreapp3.1","使用 .NET Core 3.1",false),
                 new SelectionItem("net6.0","net6.0","使用 .NET 6.0",false),
                 new SelectionItem("net7.0","net7.0","使用 .NET 7.0",false),
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

        [Description("功能配置||")]
        public SelectionList UseModule { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("function","配置“函数”功能","是否需要使用函数模块（Function）",false),
                 new SelectionItem("database","配置“数据库”功能","是否需要使用数据库模块（Database），将配置空数据库",false),
                 new SelectionItem("webapi","配置“WebApi”功能","是否需要使用WebApi模块（WebApi）",false),
                 new SelectionItem("web","配置“Web（Area） 页面”功能","是否需要使用 Web 页面模块（Web），如果选择，将忽略 .NET Standard 2.1配置，强制使用 .NET 6.0，或 .NET Core 3.1（需要选中）",false),
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
            Config config = null;
            try
            {
                //低版本没有数据库，此处需要try
                var configService = serviceProvider.GetService<ServiceBase<Config>>();
                config = await configService.GetObjectAsync(z => true);
                if (config != null)
                {
                    #region 自动载入上次配置

                    //SenparcTrace.SendCustomLog("Xncf Builder Config", config.ToJson(true));

                    //configService.Mapper.Map(config, this);

                    //SenparcTrace.SendCustomLog("Xncf Builder Config - 2", this.ToJson(true));

                    SlnFilePath = config.SlnFilePath;
                    XncfName = config.XncfName;
                    MenuName = config.MenuName;
                    Description = config.Description;
                    OrgName = config.OrgName;
                    Version = config.Version;
                    Icon = config.Icon;

                    #endregion
                }
                else
                {
                    #region 自动查找当前项目的解决方案路径

                    //当前程序目录
                    var currentDir = AppDomain.CurrentDomain.BaseDirectory;

                    //向上查找，直到找到
                    while (!IsSlnDir(currentDir) && currentDir != null)
                    {
                        currentDir = Directory.GetParent(currentDir).FullName;
                    }

                    if (currentDir != null)
                    {
                        SlnFilePath = Path.Combine(currentDir, Directory.GetFiles(currentDir, "*.sln").FirstOrDefault());
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                SenparcTrace.SendCustomLog("Xncf Builder Config - Ex", ex.Message + "///" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 判断当前路径下是否包含 .sln 文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsSlnDir(string path)
        {
            return Directory.GetFiles(path, "*.sln").Length > 0;
        }


    }

    public class BuildXncf_CreateDatabaseEntityRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(250)]
        [Description("生成数据库实体要求||请输入尽量完整的需求，也可以指定所需要的属性及类型")]
        public string Requirement { get; set; }

        [Description("领域||指定需要生成到的领域")]
        public SelectionList InjectDomain { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        [Description("后续操作||指定生成数据库实体后的后续操作")]
        public SelectionList MoreActions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("BuildDto","创建 DTO","创建 DTO 对象（已强制生成）",true),
                 new SelectionItem("BuildMigration","直接生成数据库迁移信息","使用 EF Core Migration 生成迁移信息（建议查看后进行）",false),
                 new SelectionItem("CreateRepository","创建 Repository","创建和实体匹配的 Repository",false),
                 new SelectionItem("CreateService","创建 Service","创建和实体匹配的 Service",false),
                 new SelectionItem("CreateAppService","创建 AppService","创建和实体匹配的 Service",false)
            });

        public override Task LoadData(IServiceProvider serviceProvider)
        {
            //扫描当前解决方案包含的所有领域项目
            var newItems = FunctionHelper.LoadXncfProjects(true);
            newItems.ForEach(z => InjectDomain.Items.Add(z));

            return base.LoadData(serviceProvider);
        }
    }

}