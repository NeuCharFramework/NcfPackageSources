using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Senparc.Xncf.Accounts.Models;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.Accounts
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.Accounts";

        public override string Uid => Senparc.Ncf.Core.Config.SiteConfig.SYSTEM_XNCF_MODULE_ACCOUNTS_UID;//Cannot be modified

        public override string Version => "0.1";//Version number is required

        public override string MenuName => "用户管理";

        public override string Icon => "fa fa-users";

        public override string Description => "注册用户管理";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //Update database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //Execute logic based on different conditions for installation or update
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //New installation
                    #region 初始化数据库数据

                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //renew
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AccountSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AccountSenparcEntities;

            //Specify the data entity to be deleted

            //Note: As a demonstration, all tables created by this module are deleted when uninstalling the module. During actual operation, please operate with caution and sort the entities in the order of deletion!
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //Register User Login Policy
            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserAnonymous", policy =>
                {
                    policy.RequireClaim("UserMember");
                });
            });

            return base.AddXncfModule(services, configuration, env);
        }
    }
}
