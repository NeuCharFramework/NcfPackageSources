using ModelContextProtocol.Server;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET.Extensions;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    /// <summary>
    /// MCP Server Tools
    /// </summary>
    public partial class BuildXncfAppService
    {
        #region MCP AI 接入（由于官方组件 bug，暂时使用平铺参数方式接入）

        [McpServerTool, Description("生成 XNCF 模块")]
        //[FunctionRender("生成 XNCF", "根据配置条件生成 XNCF", typeof(Register))]
        public async Task<StringAppResponse> Build(
            // [Required,Description("解决方案文件路径")]
            // string slnFilePath, 
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
            string description)
        {
            Console.WriteLine("XNCF Builder: Receive MCP Call");

            BuildXncf_BuildRequest request = new BuildXncf_BuildRequest()
            {
                //   SlnFilePath = slnFilePath,
                OrgName = orgName,
                XncfName = xncfName,
                Version = version,
                MenuName = menuName,
                Icon = icon,
                Description = description,
                UseSammple = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("1","使用示例","使用示例",true),
              }),
                UseModule = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("database","数据库","使用数据库",true),
              }),
                //   UseWeb = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","使用Web","使用Web",true),
                //   }),
                //   UseWebApi = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","使用WebApi","使用WebApi",true),
                //   }),
                NewSlnFile = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("backup","备份 .sln 文件（推荐）","如果使用覆盖现有 .sln 文件，对当前文件进行备份",true),
              }),
                TemplatePackage = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.DropDownList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("no","已安装，不需要安装新版本","请确保已经在本地安装过版本（无论新旧），否则将自动从在线获取",true),
              }),
                FrameworkVersion = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.DropDownList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("net8.0","net8.0","使用 .NET 8.0",false),
              })
            };

            request.SlnFilePath = request.GetSlnFilePath();
            request.UseSammple.SelectedValues = new[] { "1" };
            request.UseModule.SelectedValues = new[] { "database" };
            request.NewSlnFile.SelectedValues = new[] { "backup" };
            request.TemplatePackage.SelectedValues = new[] { "no" };
            request.FrameworkVersion.SelectedValues = new[] { "net8.0" };

            Console.WriteLine("XNCF Builder parameters:" + request.ToJson(true));

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

        [McpServerTool]
        public async Task<string> GetBackendCodeTemplate()
        {
            var template = @"
## Database EntityFramework DbContext class sample
File Name: Template_XncfNameSenparcEntities.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
Code:
```csharp
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel;

namespace Template_OrgName.Xncf.Template_XncfName.Models
{
    public class Template_XncfNameSenparcEntities : XncfDatabaseDbContext
    {
        public Template_XncfNameSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
```


## Database Entity class sample
File Name: Color.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
Code:
```csharp
using Senparc.Ncf.Core.Models;
using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_OrgName.Xncf.Template_XncfName
{
    /// <summary>
    /// Color 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Color))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class Color : EntityBase<int>
    {
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; private set; }

        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; private set; }

        /// <summary>
        /// 附加列，测试多次数据库 Migrate
        /// </summary>
        public string AdditionNote { get; private set; }

        private Color() { }

        public Color(int red, int green, int blue)
        {
            if (red < 0 || green < 0 || blue < 0)
            {
                Random();//随机
            }
            else
            {
                Red = red;
                Green = green;
                Blue = blue;
            }
        }

        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        public void Random()
        {
            //随机产生颜色代码
            var radom = new Random();
            Func<int> getRadomColorCode = () => radom.Next(0, 255);
            Red = getRadomColorCode();
            Green = getRadomColorCode();
            Blue = getRadomColorCode();
        }
    }
}
```

## Database Entity DTO class sample
File Name: ColorDto.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel/Dto
Code:
```csharp
using Senparc.Ncf.Core.Models;

namespace Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel.Dto
{
    public class ColorDto : DtoBase
    {
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; set; }

        /// <summary>
        /// 附加列，测试多次数据库 Migrate
        /// </summary>
        public string AdditionNote { get; set; }

        public ColorDto() { }
    }
}
```

## Service class sample
File Name: Template_XncfNameService.cs
File Path: <ModuleRootPath>/Domain/Services
Code:
```csharp
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Services
{
    public class ColorService : ServiceBase<Color>
    {
        public ColorService(IRepositoryBase<Color> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public async Task<ColorDto> CreateNewColor()
        {
            Color color = new Color(-1, -1, -1);
            await base.SaveObjectAsync(color).ConfigureAwait(false);
            ColorDto colorDto = base.Mapper.Map<ColorDto>(color);
            return colorDto;
        }

        public async Task<ColorDto> GetOrInitColor()
        {
            var color = await base.GetObjectAsync(z => true);
            if (color == null)//如果是纯第一次安装，理论上不会有残留数据
            {
                //创建默认颜色
                ColorDto colorDto = await this.CreateNewColor().ConfigureAwait(false);
                return colorDto;
            }

            return base.Mapper.Map<ColorDto>(color);
        }

        public async Task<ColorDto> Random()
        {
            var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
            obj.Random();
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            return base.Mapper.Map<ColorDto>(obj);
        }

        public async Task<ColorDto> Delete(int id)
        {
             var obj = await this.GetObjectAsync(z => true, z => z.Id, OrderingType.Descending);
             await base.DeleteObject(obj);
        }
    }
}
```



";

            return template;
        }
    }
}
