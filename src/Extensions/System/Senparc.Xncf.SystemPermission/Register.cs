/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.15.12 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.SystemPermission
{
    [XncfRegister]
    [XncfOrder(5960)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<SystemPermissionResource>(
            "Module.SystemPermission",
            "权限管理",
            "管理系统角色与权限");

        #region Legacy section

        public override string Name => "Senparc.Xncf.SystemPermission";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_PERMISSION_UID;// "00000000-0000-0000-0000-000000000003";

        public override string Version => "0.2.0";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-bars";

        public override string Description => RegisterText.Description;

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //install or upgrade the database
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            return base.AddXncfModule(services, configuration, env);
        }
    }
}

