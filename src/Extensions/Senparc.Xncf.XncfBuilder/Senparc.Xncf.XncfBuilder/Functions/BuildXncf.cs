using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Functions
{

    public class BuildXncf : FunctionBase
    {
        public BuildXncf(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public class Parameters : IFunctionParameter
        {
            [Required]
            [MaxLength(50)]
            [Description("组织名称||用于作为模块命名空间（及名称）的前缀")]
            public string OrgName { get; set; }

            [Required]
            [MaxLength(50)]
            [Description("模块名称||同时将作为类名，支持英文大小写和数字，不能以数字开头，不能带有空格和.,/*等特殊符号")]
            public string Name { get; set; }

            [Required]
            [MaxLength(36)]
            [Description("Uid||必须确保全局唯一，生成后必须固定")]
            public string Uid { get; set; }

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
            [Description("图标||支持 Font Awesome 图标集，如：fa fa-ofbuilding")]
            public string Icon { get; set; }

            [Required]
            [MaxLength(400)]
            [Description("说明||模块的说明")]
            public string Description { get; set; }

            [Description("使用函数||")]
            public SelectionList UseFunction { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","是否需要使用函数模块（Function）",false),
            });

            [Description("使用数据库||")]
            public SelectionList UseDatabase { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","是否需要使用数据库模块（Database）",false),
            });

            [Description("使用 Web 页面||")]
            public SelectionList UseWeb { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","是否需要使用 Web 页面模块（Database）",false),
            });

            [Description("使用中间件||")]
            public SelectionList UseMiddleware { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","是否需要使用中间件模块（Middleware）",false),
            });

            [Required]
            [MaxLength(250)]
            [Description("解决方案文件||输入解决方案文件物理路径，将在其并列位置生成模块目录，如：E:\\Senparc\\Ncf\\ncf.sln")]
            public string SlnFilePath { get; set; }
        }


        public override string Name => "生成 XNCF";

        public override string Description => "根据配置条件生成 XNCF";

        public override Type FunctionParameterType => typeof(Parameters);

        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<Parameters>(param, (typeParam, sb, result) =>
            {
                Senparc.Xncf.XncfBuidler.Templates.Register page = new Senparc.Xncf.XncfBuidler.Templates.Register() { 
                 OrgName = "SenparcTest"
                };
                String pageContent = page.TransformText();
                System.IO.File.WriteAllText("../Senparc.Test.Ncf/Register.cs", pageContent);
            });
        }
    }
}
