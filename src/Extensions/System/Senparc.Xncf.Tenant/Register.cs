using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Core.Config;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Senparc.Xncf.SystemManager.Domain.DatabaseModel;

namespace Senparc.Xncf.Tenant
{
    [XncfRegister]
    [XncfOrder(6000)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.Tenant";

        public override string Uid => SiteConfig.SYSTEM_XNCF_TANENT_UID;// "00000000-0000-0000-0000-000000000006";

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "多租户";

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => "多租户模块，这是系统服务核心模块，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            TenantSenparcEntities tenantEntities = serviceProvider.GetService<TenantSenparcEntities>();

            //更新数据库
            var pendingMigs = await tenantEntities.Database.GetPendingMigrationsAsync();
            if (pendingMigs.Count() > 0)
            {
                tenantEntities.ResetMigrate();//重置合并状态

                try
                {
                    var script = tenantEntities.Database.GenerateCreateScript();
                    SenparcTrace.SendCustomLog("tenantEntities.Database.GenerateCreateScript", script);

                    tenantEntities.Migrate();//进行合并
                }
                catch (Exception ex)
                {
                    var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                    SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), tenantEntities.GetType(), ex));
                }
            }

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //TODO：应该提供一个 BeforeUninstall 方法，阻止卸载。

            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            return base.AddXncfModule(services, configuration);
        }
    }
}
