using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;


namespace Senparc.Xncf.Menu
{
    [XncfRegister]
    [XncfOrder(5960)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.Menu";

        public override string Uid => "E9226280-39A4-44DD-9DC9-98429042E495";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "菜单管理";

        public override string Icon => "fa fa-bars";

        public override string Description => "系统菜单管理";

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
