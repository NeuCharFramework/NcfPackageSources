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
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;

namespace Senparc.Xncf.XncfModuleManager
{
    [XncfRegister]
    [XncfOrder(5950)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister Interface

        public override string Name => "Senparc.Xncf.XncfModuleManager";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID;// "00000000-0000-0000-0000-000000000004";

        public override string Version => "0.1.2";//Version number is required

        public override string MenuName => "XNCF 模块管理核心";

        public override string Icon => "fa fa-user-secret";//fa fa-cog

        public override string Description => "XNCF 模块管理核心";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            try
            {
                Console.WriteLine($"执行 Xncf.XncfModuleManager.InstallOrUpdateAsync");
                //Install or upgrade database
                await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

                Console.WriteLine($"执行 Xncf.XncfModuleManager.InstallOrUpdateAsync 完毕");
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("Xncf.XncfModuleManager.InstallOrUpdateAsync 发生异常", $"{ex.Message}");
                SenparcTrace.BaseExceptionLog(ex);

                throw;
            }
          

            //await InstallModulesAndMenusAsync(serviceProvider);

            //TODO: Bind by the specified database when registering DI injection

            //XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
            ////SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;

            ////Update database (BasePoolEntities is not used for storing DB models currently)
            ////await base.MigrateDatabaseAsync<BasePoolEntities>(serviceProvider);

            //var systemModule = xncfModuleServiceExtension.GetObject(z => z.Uid == this.Uid);
            //if (systemModule == null)
            //{
            //    //Install only when not installed. InstallModuleAsync calls this method; missing this check may cause an infinite loop.
            //    //Do not auto-install modules in this method for regular modules.
            //    await xncfModuleServiceExtension.InstallModuleAsync(this.Uid).ConfigureAwait(false);
            //}

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddScoped<XncfModuleServiceExtension>();

            return base.AddXncfModule(services, configuration, env);
        }

        ///// <summary>
        ///// Install module and configure menu
        ///// </summary>
        ///// <param name="serviceProvider"></param>
        ///// <returns></returns>
        //public async Task InstallModulesAndMenusAsync(IServiceProvider serviceProvider)
        //{
        //    XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
        //    var systemModule = xncfModuleServiceExtension.GetObject(z => z.Uid == this.Uid);
        //    if (systemModule == null)
        //    {
        //        //Install only when not installed. InstallModuleAsync calls this method; missing this check may cause an infinite loop.
        //        //Do not auto-install modules in this method for regular modules.
        //        await xncfModuleServiceExtension.InstallModuleAsync(this.Uid).ConfigureAwait(false);
        //    }

        //}

    }
}
