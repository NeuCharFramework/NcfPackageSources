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

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.15.14.2 Improve source documentation, headers, and module metadata consistency.

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
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;

namespace Senparc.Xncf.XncfModuleManager
{
    [XncfRegister]
    [XncfOrder(5950)]
    /// <summary>
    /// Registers the core services used to install, update, and manage XNCF modules.
    /// </summary>
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<XncfModuleManagerResource>(
            "Module.XncfModuleManager",
            "XNCF 模块管理核心",
            "安装、更新和管理 XNCF 模块的核心组件");

        #region Module metadata

        /// <inheritdoc />
        public override string Name => "Senparc.Xncf.XncfModuleManager";

        /// <inheritdoc />
        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID;// "00000000-0000-0000-0000-000000000004";

        /// <inheritdoc />
        public override string Version => "0.15.14.2";//the version number is required

        /// <inheritdoc />
        public override string MenuName => RegisterText.MenuName;

        /// <inheritdoc />
        public override string Icon => "fa fa-user-secret";//fa fa-cog

        /// <inheritdoc />
        public override string Description => RegisterText.Description;

        /// <inheritdoc />
        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            try
            {
                Console.WriteLine($"执行 Xncf.XncfModuleManager.InstallOrUpdateAsync");
                await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

                Console.WriteLine($"执行 Xncf.XncfModuleManager.InstallOrUpdateAsync 完毕");
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("Xncf.XncfModuleManager.InstallOrUpdateAsync 发生异常", $"{ex.Message}");
                SenparcTrace.BaseExceptionLog(ex);

                throw;
            }
          

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        /// <inheritdoc />
        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        /// <inheritdoc />
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddScoped<XncfModuleServiceExtension>();

            return base.AddXncfModule(services, configuration, env);
        }

    }
}
