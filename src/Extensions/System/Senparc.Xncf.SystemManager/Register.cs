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
using Senparc.Xncf.SystemCore.Domain.Database;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.SystemManager
{
    [XncfRegister]
    [XncfOrder(5970)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.SystemManager";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SERVICE_MANAGER_UID;// "00000000-0000-0000-0000-000000000002";

        public override string Version => "1.1.1";//必须填写版本号

        public override string MenuName => "系统管理";

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => "这是系统服务核心模块，主管基础数据结构和网站核心运行数据，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            ////SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            //BasePoolEntities basePoolEntities = serviceProvider.GetService<BasePoolEntities>();

            //var senparcEntities = serviceProvider.GetService(this.TryGetXncfDatabaseDbContextType) as TenantSenparcEntities;
            //await XncfDatabaseDbContext.MigrateOnInstallAsync(senparcEntities);

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //TODO：应该提供一个 BeforeUninstall 方法，阻止卸载。

            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            return base.AddXncfModule(services, configuration, env);
        }
    }
}


