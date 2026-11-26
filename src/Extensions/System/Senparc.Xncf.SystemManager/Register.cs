/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview3 integrate shared resource localization and improve feature text for the SystemManager module

    Modified by: Senparc - 20260803
    Change description: v1.1.3 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.15.3-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.15.12 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

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
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<SystemManagerResource>(
            "Module.SystemManager",
            "系统管理",
            "负责系统配置和核心运行数据的管理模块，请勿删除；操作前请备份数据。");

        #region Legacy section

        public override string Name => "Senparc.Xncf.SystemManager";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SERVICE_MANAGER_UID;// "00000000-0000-0000-0000-000000000002";

        public override string Version => "1.1.3";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => RegisterText.Description;

    /// <inheritdoc/>
    public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //install or upgrade the database
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            ////SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            //BasePoolEntities basePoolEntities = serviceProvider.GetService<BasePoolEntities>();

            //var senparcEntities = serviceProvider.GetService(this.TryGetXncfDatabaseDbContextType) as TenantSenparcEntities;
            //await XncfDatabaseDbContext.MigrateOnInstallAsync(senparcEntities);

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

    /// <inheritdoc/>
    public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //Legacy comment translated to English.

            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }
        #endregion

    /// <inheritdoc/>
    public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            return base.AddXncfModule(services, configuration, env);
        }
    }
}



