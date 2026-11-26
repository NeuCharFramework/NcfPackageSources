/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20211211

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261002
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.15.14 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.15.15 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

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
using Microsoft.EntityFrameworkCore;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.Tenant.OHS.Remote;
using Senparc.Xncf.Tenant.ACL.Repository;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Models;
using Senparc.Ncf.Core.MultiTenant;
using AutoMapper;
using Senparc.Xncf.Tenant.Domain.DatabaseModel.AutoMapper;
using Senparc.Ncf.Database;
using Senparc.Xncf.Tenant.Domain.Services;

namespace Senparc.Xncf.Tenant
{
    [XncfRegister]
    [XncfOrder(5990)]
    /// <summary>
    /// Registers the system tenant module, its tenant-aware database context,
    /// repository services, and request middleware.
    /// </summary>
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<TenantResource>(
            "Module.Tenant",
            "多租户",
            "系统多租户核心模块，请勿删除；操作前请备份数据。");

        #region Module metadata

        /// <inheritdoc />
        public override string Name => "Senparc.Xncf.Tenant";

        /// <inheritdoc />
        public override string Uid => SiteConfig.SYSTEM_XNCF_TANENT_UID;// "00000000-0000-0000-0000-000000000006";

        /// <inheritdoc />
        public override string Version => "0.1";//the version number is required

        /// <inheritdoc />
        public override string MenuName => RegisterText.MenuName;

        /// <inheritdoc />
        public override string Icon => "fa fa-university";//fa fa-cog

        /// <inheritdoc />
        public override string Description => RegisterText.Description;

        /// <inheritdoc />
    /// <inheritdoc/>
    public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //install or upgrade the database
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        /// <inheritdoc />
        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }
        #endregion

        /// <inheritdoc />
        /// <remarks>
        /// Registers tenant repositories, the background tenant provider, and the
        /// AutoMapper profile before allowing the base register to add shared XNCF services.
        /// </remarks>
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            AddMultiTenant(services);
            EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntitiesMultiTenant));
            services.AddScoped<ITenantInfoDbData, TenantInfoDbData>();
            services.AddScoped<TenantInfoRepository>();
            services.AddScoped<IClientRepositoryBase<TenantInfo>, TenantInfoRepository>();
            services.AddScoped<IBackgroundTenantProvider, BackgroundTenantProvider>();

            services.AddAutoMapper(z => z.AddProfile<TenantInfoProfile>());

            return base.AddXncfModule(services, configuration, env);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Inserts <see cref="TenantMiddleware"/> into the request pipeline before
        /// delegating to the shared module pipeline.
        /// </remarks>
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseMiddleware<TenantMiddleware>();
            return base.UseXncfModule(app, registerService);
        }

        #region Legacy section

        //public async Task<(bool success, string msg)> GenerateCreateScript(IServiceProvider serviceProvider)
        //{
        //    var success = true;
        //    string msg = null;

        //    //XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
        //    //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;

        //    BasePoolEntities basePoolEntities = serviceProvider.GetService<BasePoolEntities>();

        //    try
        //    {
        //        SiteConfig.IsInstalling = true;

        //        //updatedatabase
        //        var pendingMigs = await basePoolEntities.Database.GetPendingMigrationsAsync();
        //        if (pendingMigs.Count() > 0)
        //        {
        //Legacy comment translated to English.

        //            try
        //            {
        //                var script = basePoolEntities.Database.GenerateCreateScript();
        //                SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);

        //Legacy comment translated to English.

        //Legacy comment translated to English.
        //            }
        //            catch (Exception ex)
        //            {
        //                success = false;
        //                msg = ex.Message + "\r\n" + ex.StackTrace;
        //                var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
        //                SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), basePoolEntities.GetType(), ex));
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        SiteConfig.IsInstalling = false;
        //    }

        //    return (success, msg);
        //}

        //public async Task<(bool success, string msg)> InitDatabase(IServiceProvider serviceProvider/*, TenantInfoService tenantInfoService*/
        //    /*HttpContext httpContext,*/)
        //{
        //    var success = false;
        //    string msg = null;

        //    //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
        //    using (var scope = serviceProvider.CreateScope())
        //    {
        //        var oldMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
        //        //temporarilydisablemulti-tenancystatus
        //        SiteConfig.SenparcCoreSetting.EnableMultiTenant = false;

        //Legacy comment translated to English.
        //        success = result.success;
        //        msg = result.msg;

        //        SiteConfig.SenparcCoreSetting.EnableMultiTenant = oldMultiTenant;
        //    }

        //    return (success: success, msg: msg);
        //}

        #endregion


        /// <summary>
        /// Registers tenant-scoped services and creates the multi-tenant database
        /// context through <see cref="MultipleDatabasePool"/>.
        /// </summary>
        /// <param name="services">The service collection receiving tenant registrations.</param>
        /// <returns>The same service collection for fluent registration.</returns>
        public IServiceCollection AddMultiTenant(IServiceCollection services)
        {
            services.AddScoped<TenantInfoDto>();
            services.AddScoped<TenantInfoDbData>();

            Func<IServiceProvider, SenparcEntitiesMultiTenant> multiTenantImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetDbContext<SenparcEntitiesMultiTenant>(serviceProvider: s);
            };
            services.AddScoped<SenparcEntitiesMultiTenant>(multiTenantImplementationFactory);

            return services;
        }


    }
}
