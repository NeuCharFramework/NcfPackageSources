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
            [MaxLength(300)]
            [Description("模块名称||不能带有空格和.")]
            public string Name { get; set; }
        }


        public override string Name => "生成 XNCF";

        public override string Description => "根据配置条件生成 XNCF";

        public override Type FunctionParameterType => typeof(Parameters);

        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<Parameters>(param, (typeParam, sb, result) =>
            {
                //生成
            });
        }
    }
}
