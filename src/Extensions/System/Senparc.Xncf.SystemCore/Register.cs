/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.14.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.14.12 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.14.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.14.14.2 Improve source documentation, headers, and module metadata consistency.

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
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.AssembleScan;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Senparc.Respository;
using Senparc.Ncf.Core.WorkContext.Provider;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using System.Text;
using Microsoft.Extensions.Hosting;
using log4net;
using log4net.Config;
using System.IO;

namespace Senparc.Xncf.SystemCore
{
    [XncfRegister]
    [XncfOrder(5980)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<SystemCoreResource>(
            "Module.SystemCore",
            "系统核心模块",
            "负责基础数据结构和网站核心运行数据的系统核心模块，请勿删除；操作前请备份数据。");

        #region Legacy section

        public override string Name => "Senparc.Xncf.SystemCore";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID;// "00000000-0000-0000-0001-000000000001";

        public override string Version => "0.14.14.2";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => RegisterText.Description;

    /// <inheritdoc/>
    public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //Legacy comment translated to English.
            //await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            /*

            //updatedatabase

            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;

            BasePoolEntities basePoolEntities = serviceProvider.GetService<BasePoolEntities>();

            var pendingMigs = await basePoolEntities.Database.GetPendingMigrationsAsync();

            if (pendingMigs.Count() > 0)

            {

                Legacy comment translated to English.



                try

                {

                    var script = basePoolEntities.Database.GenerateCreateScript();

                    SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);



                    Legacy comment translated to English.

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
            //readLogconfigurationfile
            try
            {
                var repository = LogManager.CreateRepository("NETCoreRepository");
                XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
            }

            services.AddSenparcGlobalServices(configuration);//Legacy comment translated to English.

            //Legacy comment translated to English.
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
            //Legacy comment translated to English.
            services.AddMemoryCache();


            //Legacy comment translated to English.
            services.AddTransient(typeof(Lazy<>));

            services.Configure<SenparcCoreSetting>(configuration.GetSection("SenparcCoreSetting"));

            //Legacy comment translated to English.
            //services.ScanAssamblesForAutoDI();
            //Legacy comment translated to English.
            //AssembleScanHelper.RunScan();
            ////services.AddSingleton<Core.Cache.RedisProvider.IRedisProvider, Core.Cache.RedisProvider.StackExchangeRedisProvider>();

            services.AddHttpContextAccessor();
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

            //Repository & Service
            services.AddScoped<ISysButtonRespository, SysButtonRespository>();
            services.AddScoped<ISysRolePermissionRepository, SysRolePermissionRepository>();
            services.AddScoped<Ncf.Core.Authorization.ICheckPermission, Ncf.Service.SysRolePermissionService>();

            services.AddScoped<IAdminWorkContextProvider, AdminWorkContextProvider>();

            //Legacy comment translated to English.
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_Work.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_MiniProgram.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_Open.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_OfficialAccount.ToString());

            return base.AddXncfModule(services, configuration, env);
        }

    /// <inheritdoc/>
    public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            #region Legacy section

            //http://www.mamicode.com/info-detail-2225481.html
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            #endregion

            #region Legacy section

            //Legacy comment translated to English.
            //https://stackoverflow.com/questions/35441521/remoteipaddress-is-always-null
            app.UseHttpMethodOverride(new HttpMethodOverrideOptions
            {
                //Legacy comment translated to English.
            });

            #endregion

            #region Legacy section

            {
                ////APM Ending datastatistics
                ///var utility = new APMNeuralDataThreadUtility();
                ///Thread thread = new Thread(utility.Run) { Name = "APMNeuralDataThread" };
                ///SiteConfig.AsynThread.Add(thread.Name, thread);
            }

            SiteConfig.AsynThread.Values.ToList().ForEach(z =>
            {
                z.IsBackground = true;
                z.Start();
            }); //Legacy comment translated to English.

            //Legacy comment translated to English.

            #endregion

            return base.UseXncfModule(app, registerService);
        }
    }
}
