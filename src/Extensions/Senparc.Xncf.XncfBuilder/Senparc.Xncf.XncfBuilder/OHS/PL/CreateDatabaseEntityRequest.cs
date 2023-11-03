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
            var newItems = FunctionHelper.LoadXncfProjects(true);
            newItems.ForEach(z => InjectDomain.Items.Add(z));

            return base.LoadData(serviceProvider);
        }
    }
}
