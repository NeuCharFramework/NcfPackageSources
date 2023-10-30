using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text;
using Senparc.Ncf.XncfBase.Functions;
using System.Threading.Tasks;
using System.IO;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class CreateDatabaseEntityRequest : FunctionAppRequestBase
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

            var currentDir = System.IO.Directory.GetCurrentDirectory();

            while (currentDir != null)
            {
                var slnFile = Directory.GetFiles(currentDir, "*.sln");
                if (slnFile.Length > 0)
                {
                    break;
                }
                currentDir = Directory.GetParent(currentDir).FullName;
            }

            if (currentDir != null)
            {
                //找到了 SLN 文件，开始展开地毯式搜索

                //第一步：查找 XNCF

                var projectFolders = Directory.GetDirectories(currentDir, "*.XNCF.*", SearchOption.AllDirectories);

                foreach (var projectFolder in projectFolders)
                {
                    //第二步：查看 Register 文件是否存在
                    var registerFilePath = Path.Combine(projectFolder, "Register.cs");
                    if (!File.Exists(registerFilePath))
                    {
                        continue;//不存在则跳过
                    }

                    //第三步：检查 Register 文件是否合格

                    var registerContent = File.ReadAllText(registerFilePath);
                    if (registerContent.Contains("[XncfRegister]") &&
                        registerContent.Contains("IXncfRegister") &&
                        registerContent.Contains("Uid"))
                    {
                        InjectDomain.Items.Add(
                            new SelectionItem(
                                projectFolder,
                             Path.GetFileName(projectFolder),
                                Path.GetDirectoryName(projectFolder)));
                    }
                }
            }


            if (currentDir == null || InjectDomain.Items.Count == 0)
            {
                InjectDomain.Items.Add(
                             new SelectionItem(
                                 "N/A",
                                 "没有发现任何可用的 XNCF 项目，请确保你正在一个标准的 NCF 开发环境中！"));
            }

            return base.LoadData(serviceProvider);
        }
    }
}
