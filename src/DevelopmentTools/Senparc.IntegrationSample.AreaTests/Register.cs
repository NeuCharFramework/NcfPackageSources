using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.XncfBase;

namespace Senparc.IntegrationSample.AreaTests
{
    public class Register : XncfRegisterBase,
        IXncfRegister, //注册 XNCF 基础模块接口（必须）
        IAreaRegister //注册 XNCF 页面接口（按需选用）
    {

        public override string Name => "IntegrationSample.AreaTests";

        public override string Uid => "10000001-1001-1001-1001-100000000001";

        public override string Version => "0.1.0";

        public override string MenuName => "NCF Area 集成测试";

        public override string Icon => "fa fa-university";

        public override string Description => "NCF Area 集成测试";


        public string HomeUrl => "/AreaTests";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() { };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //此处可配置页面权限
            });

            SenparcTrace.SendCustomLog("IntegrationSample.AreaTests 启动", "完成 Area:IntegrationSample.AreaTests 注册");

            return builder;

        }
    }
}
