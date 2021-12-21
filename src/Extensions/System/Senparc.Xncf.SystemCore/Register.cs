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
using Senparc.Ncf.Database;

namespace Senparc.Xncf.SystemCore
{
    [XncfRegister]
    [XncfOrder(5980)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.SystemCore";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID;// "00000000-0000-0000-0001-000000000001";

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

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddSenparcGlobalServices(configuration);//注册 CO2NET 基础引擎所需服务

            //解决中文进行编码问题
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
            //使用内存缓存
            services.AddMemoryCache();


            //注册 Lazy<T>
            services.AddTransient(typeof(Lazy<>));

            services.Configure<SenparcCoreSetting>(configuration.GetSection("SenparcCoreSetting"));

            //自动依赖注入扫描
            services.ScanAssamblesForAutoDI();
            //已经添加完所有程序集自动扫描的委托，立即执行扫描（必须）
            AssembleScanHelper.RunScan();
            //services.AddSingleton<Core.Cache.RedisProvider.IRedisProvider, Core.Cache.RedisProvider.StackExchangeRedisProvider>();

            services.AddHttpContextAccessor();
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

            //Repository & Service
            services.AddScoped<ISysButtonRespository, SysButtonRespository>();

            services.AddScoped<IAdminWorkContextProvider, AdminWorkContextProvider>();

            //忽略某些 API
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_Work.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_MiniProgram.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_Open.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_OfficialAccount.ToString());

            #region 从数据库移植

            //TODO：移植到独立的数据库模块

            #region 历史解决方案参考信息
            /* 参考信息
             *      错误信息：
             *          中文：EnableRetryOnFailure 解决短暂的数据库连接失败
             *          英文：Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
             *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
             *      问题解决方案说明：https://www.colabug.com/2329124.html
             */

            /* 参考信息
             *      错误信息：
             *          中文：EnableRetryOnFailure 解决短暂的数据库连接失败
             *          英文：Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
             *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
             *      问题解决方案说明：https://www.colabug.com/2329124.html
             */
            #endregion

            var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.Current;

            /* 
             *     非常重要！！
             * SenparcEntities 工厂配置
             * 
             * SYSTEM 为特定标记，将直接定位到 __EFMigrationsHistory 
            */
            //var xncfDatabaseData = new XncfDatabaseData(this, NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX);

            #region 不属于任何模块

            //这个配置面相基类，不属于任何模块
            Func<IServiceProvider, SenparcEntitiesMultiTenant> multiTenantImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetDbContext<SenparcEntitiesMultiTenant>(serviceProvider: s);
            };
            services.AddScoped<SenparcEntitiesMultiTenant>(multiTenantImplementationFactory);//继承自 SenparcEntitiesMultiTenantBase

            Func<IServiceProvider, SenparcEntities> senparcEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;

                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as SenparcEntities;
            };

            services.AddScoped<SenparcEntitiesDbContextBase>(senparcEntitiesImplementationFactory);// 继承自 DbContext
            services.AddScoped<ISenparcEntitiesDbContext>(senparcEntitiesImplementationFactory);
            services.AddScoped<SenparcEntitiesBase>(senparcEntitiesImplementationFactory);//继承自 SenparcEntitiesMultiTenantBase
            services.AddScoped<SenparcEntities>(senparcEntitiesImplementationFactory);

            #endregion

            //BasePoolEntities 工厂配置（实际不会用到）
            Func<IServiceProvider, BasePoolEntities> basePoolEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as BasePoolEntities;
            };
            services.AddScoped<BasePoolEntities>(basePoolEntitiesImplementationFactory);

            services.AddScoped(typeof(INcfClientDbData), typeof(NcfClientDbData));
            services.AddScoped(typeof(INcfDbData), typeof(NcfClientDbData));

            //预加载 EntitySetKey
            //EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntities));
            EntitySetKeys.TryLoadSetInfo(typeof(BasePoolEntities));

            #endregion


            return base.AddXncfModule(services, configuration, env);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            #region .NET Core默认不支持GB2312

            //http://www.mamicode.com/info-detail-2225481.html
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            #endregion


            #region Senparc.Core 设置

            //用于解决HttpContext.Connection.RemoteIpAddress为null的问题
            //https://stackoverflow.com/questions/35441521/remoteipaddress-is-always-null
            app.UseHttpMethodOverride(new HttpMethodOverrideOptions
            {
                //FormFieldName = "X-Http-Method-Override"//此为默认值
            });

            #endregion

            #region 异步线程

            {
                ////APM Ending 数据统计
                //var utility = new APMNeuralDataThreadUtility();
                //Thread thread = new Thread(utility.Run) { Name = "APMNeuralDataThread" };
                //SiteConfig.AsynThread.Add(thread.Name, thread);
            }

            SiteConfig.AsynThread.Values.ToList().ForEach(z =>
            {
                z.IsBackground = true;
                z.Start();
            }); //全部运行 

            //更多 XNCF 模块线程已经集成到 Senparc.Ncf.XncfBase.Register.ThreadCollection 中

            #endregion

            return base.UseXncfModule(app, registerService);
        }



        #region 扩展

        public async Task<(bool success, string msg)> GenerateCreateScript(IServiceProvider serviceProvider)
        {
            var success = true;
            string msg = null;

            //XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;

            BasePoolEntities basePoolEntities = serviceProvider.GetService<BasePoolEntities>();

            try
            {
                SiteConfig.IsInstalling = true;

                //更新数据库
                var pendingMigs = await basePoolEntities.Database.GetPendingMigrationsAsync();
                if (pendingMigs.Count() > 0)
                {
                    basePoolEntities.ResetMigrate();//重置合并状态

                    try
                    {
                        var script = basePoolEntities.Database.GenerateCreateScript();
                        SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);

                        basePoolEntities.Migrate();//进行合并

                        msg = "已成功合并";
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        msg = ex.Message + "\r\n" + ex.StackTrace;
                        var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                        SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), basePoolEntities.GetType(), ex));
                    }
                }
            }
            finally
            {
                SiteConfig.IsInstalling = false;
            }

            return (success, msg);
        }

        public async Task<(bool success, string msg)> InitDatabase(IServiceProvider serviceProvider/*, TenantInfoService tenantInfoService*/
            /*HttpContext httpContext,*/)
        {
            var success = false;
            string msg = null;

            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            using (var scope = serviceProvider.CreateScope())
            {
                var oldMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
                //暂时关闭多租户状态
                SiteConfig.SenparcCoreSetting.EnableMultiTenant = false;

                var result = await GenerateCreateScript(serviceProvider);//尝试执行更新
                success = result.success;
                msg = result.msg;

                SiteConfig.SenparcCoreSetting.EnableMultiTenant = oldMultiTenant;
            }

            return (success: success, msg: msg);
        }

        #endregion
    }
}
