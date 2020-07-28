using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using static Senparc.Xncf.ChangeNamespace.Functions.ChangeNamespace;

namespace Senparc.Xncf.ChangeNamespace.Functions
{

    /// <summary>
    /// 还原命名空间
    /// </summary>
    public class RestoreNameSpace : FunctionBase
    {
        public class RestoreNameSpace_Parameters : IFunctionParameter
        {
            [Required]
            [MaxLength(300)]
            [Description("路径||本地物理路径，如：E:\\Senparc\\Ncf\\")]
            public string Path { get; set; }
            [Required]
            [MaxLength(100)]
            [Description("当前自定义的命名空间||命名空间根，一般以.结尾，如：[My.Namespace.]，最终将替换为例如[Senparc.Ncf.]或[Senparc.]")]
            public string MyNamespace { get; set; }
        }


        //注意：Name 必须在单个 Xncf 模块中唯一！
        public override string Name => "还原命名空间";

        public override string Description => "还原所有源码在 .cs, .cshtml 中的命名空间为 NCF 默认（建议在断崖式更新之前进行此操作）";

        public override Type FunctionParameterType => typeof(RestoreNameSpace_Parameters);

        public RestoreNameSpace(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<RestoreNameSpace_Parameters>(param, (typeParam, sb, result) =>
            {
                var changeNamespaceParam = new ChangeNamespace_Parameters()
                {
                    NewNamespace = "Senparc.",
                    Path = typeParam.Path
                };

                ChangeNamespace changeNamespaceFunction = new ChangeNamespace(base.ServiceProvider);
                changeNamespaceFunction.OldNamespaceKeyword = typeParam.MyNamespace;
                var newesult = changeNamespaceFunction.Run(changeNamespaceParam);
                if (result.Success)
                {
                    result.Message = "还原命名空间成功！";
                }
                return newesult;
            });
        }
    }
}
