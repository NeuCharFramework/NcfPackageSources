using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Xncf.SystemCore.Domain.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.SystemCore
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//系统表，

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
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
            var xncfDatabaseData = new XncfDatabaseData(this, NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX);

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
            EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntities));
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
           
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
