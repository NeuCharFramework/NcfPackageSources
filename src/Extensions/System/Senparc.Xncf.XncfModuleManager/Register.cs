using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.XncfModuleManager.Domain.Services;

namespace Senparc.Xncf.XncfModuleManager
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.XncfModuleManager";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID;// "00000000-0000-0000-0000-000000000004";

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "XNCF 模块管理";

        public override string Icon => "fa fa-user-secret";//fa fa-cog

        public override string Description => "XNCF 模块管理";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //TODO：DI注入注册时候，根据指定数据库进行绑定

            XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;

            //更新数据库（目前不使用 BasePoolEntities 存放数据库模型）
            //await base.MigrateDatabaseAsync<BasePoolEntities>(serviceProvider);

            var systemModule = xncfModuleServiceExtension.GetObject(z => z.Uid == this.Uid);
            if (systemModule == null)
            {
                //只在未安装的情况下进行安装，InstallModuleAsync会访问到此方法，不做判断可能会引发死循环。
                //常规模块中请勿在此方法中自动安装模块！
                await xncfModuleServiceExtension.InstallModuleAsync(this.Uid).ConfigureAwait(false);
            }

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<XncfModuleServiceExtension>();

            return base.AddXncfModule(services, configuration);
        }

    }
}
