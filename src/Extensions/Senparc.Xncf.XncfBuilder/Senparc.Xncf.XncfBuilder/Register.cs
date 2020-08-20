using Senparc.Ncf.XncfBase;
using Senparc.Xncf.XncfBuilder.Functions;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.XncfBuilder
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IRegister 接口

        public override string Name => "Senparc.Xncf.XncfBuilder";

        public override string Uid => "C2E1F87F-2DCE-4921-87CE-36923ED0D6EA";//必须确保全局唯一，生成后必须固定

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "XNCF 模块生成器";

        public override string Icon => "fa fa-ofwrench";

        public override string Description => "快速生成 XNCF 模块基础程序代码，或 Sample 演示，可基于基础代码扩展自己的应用";

        public override IList<Type> Functions => new Type[] { typeof(BuildXncf) };

        #endregion
    }
}
