using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Core.Config;

namespace Senparc.Xncf.SystemPermission
{
    [XncfRegister]
    [XncfOrder(5980)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.SystemPermission";
        
        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_PERMISSION_UID;// "00000000-0000-0000-0000-000000000003";

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "权限管理";

        public override string Icon => "fa fa-bars";

        public override string Description => "系统权限管理";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            return base.AddXncfModule(services, configuration);
        }
    }
}
