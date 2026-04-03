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
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.SystemCore";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID;// "00000000-0000-0000-0001-000000000001";

        public override string Version => "0.1.1";//Version number is required

        public override string MenuName => "系统核心模块";

        public override string Icon => "fa fa-university";//fa fa-cog

        public override string Description => "这是系统服务核心模块，主管基础数据结构和网站核心运行数据，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //SenparcEntities does not build any database entities, only serves as a container
            //await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            /*
            //update database
            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            BasePoolEntities basePoolEntities = serviceProvider.GetService<BasePoolEntities>();
            var pendingMigs = await basePoolEntities.Database.GetPendingMigrationsAsync();
            if (pendingMigs.Count() > 0)
            {
                basePoolEntities.ResetMigrate();//Reset merge status

                try
                {
                    var script = basePoolEntities.Database.GenerateCreateScript();
                    SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);

                    basePoolEntities.Migrate();//Merge
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
            //TODO: A BeforeUninstall method should be provided to prevent uninstallation.

            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //Read Log configuration file
            try
            {
                var repository = LogManager.CreateRepository("NETCoreRepository");
                XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
            }

            services.AddSenparcGlobalServices(configuration);//Register the services required by the CO2NET basic engine

            //Solve the problem of encoding in Chinese
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
            //Use memory cache
            services.AddMemoryCache();


            //Register Lazy<T>
            services.AddTransient(typeof(Lazy<>));

            services.Configure<SenparcCoreSetting>(configuration.GetSection("SenparcCoreSetting"));

            ////Automatic dependency injection scanning
            //services.ScanAssamblesForAutoDI();
            ////The delegates for automatic scanning of all assemblies have been added and the scan will be executed immediately (required)
            //AssembleScanHelper.RunScan();
            ////services.AddSingleton<Core.Cache.RedisProvider.IRedisProvider, Core.Cache.RedisProvider.StackExchangeRedisProvider>();

            services.AddHttpContextAccessor();
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

            //Repository & Service
            services.AddScoped<ISysButtonRespository, SysButtonRespository>();
            services.AddScoped<ISysRolePermissionRepository, SysRolePermissionRepository>();
            services.AddScoped<Ncf.Core.Authorization.ICheckPermission, Ncf.Service.SysRolePermissionService>();

            services.AddScoped<IAdminWorkContextProvider, AdminWorkContextProvider>();

            //Ignore some APIs
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_Work.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_MiniProgram.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_Open.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_OfficialAccount.ToString());

            return base.AddXncfModule(services, configuration, env);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            #region .NET Core默认不支持GB2312

            //http://www.mamicode.com/info-detail-2225481.html
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            #endregion

            #region Senparc.Core 设置

            //Used to solve the problem that HttpContext.Connection.RemoteIpAddress is null
            //https://stackoverflow.com/questions/35441521/remoteipaddress-is-always-null
            app.UseHttpMethodOverride(new HttpMethodOverrideOptions
            {
                //FormFieldName = "X-Http-Method-Override"//This is the default value
            });

            #endregion

            #region 异步线程

            {
                ////APM Ending data statistics
                //var utility = new APMNeuralDataThreadUtility();
                //Thread thread = new Thread(utility.Run) { Name = "APMNeuralDataThread" };
                //SiteConfig.AsynThread.Add(thread.Name, thread);
            }

            SiteConfig.AsynThread.Values.ToList().ForEach(z =>
            {
                z.IsBackground = true;
                z.Start();
            }); //Run all 

            //More XNCF module threads have been integrated into Senparc.Ncf.XncfBase.Register.ThreadCollection

            #endregion

            return base.UseXncfModule(app, registerService);
        }
    }
}
