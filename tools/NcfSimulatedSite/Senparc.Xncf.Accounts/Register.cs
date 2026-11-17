/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.cs

    File description: module registration and initialization logic

    

    

    Created by: Senparc - 20200818

    

    Modified by: Senparc - 20260724

    Change description: v0.3.0 Updated module behavior and related support.



    Modified by: Senparc - 20261111

    Change description: v0.3.12 unify XNCF Register display metadata and localization resource keys



----------------------------------------------------------------*/
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Resources;

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
        private static readonly XncfRegisterText RegisterText =
            XncfRegisterText.Create<AccountsResource>(
                "Accounts.Register",
                "用户管理",
                "注册用户管理");

        #region Legacy section

        public override string Name => "Senparc.Xncf.Accounts";

        public override string Uid => Senparc.Ncf.Core.Config.SiteConfig.SYSTEM_XNCF_MODULE_ACCOUNTS_UID;//Legacy comment translated to English.

        public override string Version => "0.1";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-users";

        public override string Description => RegisterText.Description;

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //update the database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //execute logic according to the installation or update condition
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //new installation
                    #region Legacy section

                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //update
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AccountSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AccountSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //Legacy comment translated to English.
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
