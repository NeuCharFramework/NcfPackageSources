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
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<TenantResource>(
            "Module.Tenant",
            "多租户",
            "系统多租户核心模块，请勿删除；操作前请备份数据。");

        #region Legacy section

        public override string Name => "Senparc.Xncf.Tenant";

        public override string Uid => SiteConfig.SYSTEM_XNCF_TANENT_UID;// "00000000-0000-0000-0000-000000000006";

        public override string Version => "0.1";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => RegisterText.Description;

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //install or upgrade the database
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //Legacy comment translated to English.

            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            AddMultiTenant(services);//Legacy comment translated to English.
            EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntitiesMultiTenant));//Legacy comment translated to English.
            //Legacy comment translated to English.
            services.AddScoped<ITenantInfoDbData, TenantInfoDbData>();
            services.AddScoped<TenantInfoRepository>();
            services.AddScoped<IClientRepositoryBase<TenantInfo>, TenantInfoRepository>();
            services.AddScoped<IBackgroundTenantProvider, BackgroundTenantProvider>();

            //Legacy comment translated to English.
            services.AddAutoMapper(z => z.AddProfile<TenantInfoProfile>());

            return base.AddXncfModule(services, configuration, env);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            #region Legacy section

            app.UseMiddleware<TenantMiddleware>();//Legacy comment translated to English.

            #endregion
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public IServiceCollection AddMultiTenant(IServiceCollection services)
        {
            services.AddScoped<TenantInfoDto>();
            services.AddScoped<TenantInfoDbData>();

            //Legacy comment translated to English.
            Func<IServiceProvider, SenparcEntitiesMultiTenant> multiTenantImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetDbContext<SenparcEntitiesMultiTenant>(serviceProvider: s);
            };
            services.AddScoped<SenparcEntitiesMultiTenant>(multiTenantImplementationFactory);//Legacy comment translated to English.

            return services;
        }


    }
}
