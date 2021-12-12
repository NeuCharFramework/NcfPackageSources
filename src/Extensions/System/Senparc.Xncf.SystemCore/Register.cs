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

namespace Senparc.Xncf.SystemCore
{
    [XncfRegister]
    [XncfOrder(5980)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.SystemCore";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID;// "00000000-0000-0000-0000-000000000001";

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "系统核心模块";

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => "这是系统服务核心模块，主管基础数据结构和网站核心运行数据，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //SenparcEntities 不进行任何数据库实体的构建，只作为容器
            //await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            /*
            //更新数据库
            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            BasePoolEntities basePoolEntities = serviceProvider.GetService<BasePoolEntities>();
            var pendingMigs = await basePoolEntities.Database.GetPendingMigrationsAsync();
            if (pendingMigs.Count() > 0)
            {
                basePoolEntities.ResetMigrate();//重置合并状态

                try
                {
                    var script = basePoolEntities.Database.GenerateCreateScript();
                    SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);

                    basePoolEntities.Migrate();//进行合并
                }
                catch (Exception ex)
                {
                    var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                    SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), basePoolEntities.GetType(), ex));
                }
            }
            */

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
