using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Xncf.SystemCore.Domain.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.SystemCore
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "SYSTEM_CORE_"; //NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//System table,

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            #region 历史解决方案参考信息
            /* Reference information
             *      error message:
             * Chinese: EnableRetryOnFailure solves temporary database connection failure
             * English: Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
             *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
             * Problem solution description: https://www.colabug.com/2329124.html
             */

            /* Reference information
             *      error message:
             * Chinese: EnableRetryOnFailure solves temporary database connection failure
             * English: Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
             *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
             * Problem solution description: https://www.colabug.com/2329124.html
             */
            #endregion

            //var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.Current;

            /* 
             * Very important! !
             * SenparcEntities factory configuration
             * 
             * SYSTEM is a specific tag and will be located directly to __EFMigrationsHistory 
            */
            var xncfDatabaseData = new XncfDatabaseData(this, NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX);

            #region 不属于任何模块


            Func<IServiceProvider, SenparcEntities> senparcEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;

                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as SenparcEntities;
            };

            services.AddScoped<SenparcEntitiesDbContextBase>(senparcEntitiesImplementationFactory);// Inherited from DbContext
            services.AddScoped<ISenparcEntitiesDbContext>(senparcEntitiesImplementationFactory);
            services.AddScoped<SenparcEntitiesBase>(senparcEntitiesImplementationFactory);//Inherited from SenparcEntitiesMultiTenantBase
            services.AddScoped<SenparcEntities>(senparcEntitiesImplementationFactory);

            #endregion

            //BasePoolEntities factory configuration (the upper-layer application will not actually use it, it is needed when building NcfClientDbData, and NcfClientDbData does not need to be used in the official system)
            Func<IServiceProvider, BasePoolEntities> basePoolEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as BasePoolEntities;
            };
            services.AddScoped<BasePoolEntities>(basePoolEntitiesImplementationFactory);

            services.AddScoped<INcfClientDbData, NcfClientDbData>();
            services.AddScoped<INcfDbData, NcfClientDbData>();

            //Preload EntitySetKey
            EntitySetKeys.TryLoadSetInfo(typeof(BasePoolEntities));
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
            var currentInstallState = SiteConfig.IsInstalling;
            try
            {
                SiteConfig.IsInstalling = true;

                //Update database
                var pendingMigs = await basePoolEntities.Database.GetPendingMigrationsAsync();
                if (pendingMigs.Count() > 0)
                {
                    basePoolEntities.ResetMigrate();//Reset merge status

                    try
                    {
                        var script = basePoolEntities.Database.GenerateCreateScript();
                        SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);

                        basePoolEntities.Migrate();//merge

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
                SiteConfig.IsInstalling = currentInstallState;
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
                //Temporarily turn off multi-tenancy status
                SiteConfig.SenparcCoreSetting.EnableMultiTenant = false;

                var result = await GenerateCreateScript(serviceProvider);//Try to perform an update
                success = result.success;
                msg = result.msg;

                SiteConfig.SenparcCoreSetting.EnableMultiTenant = oldMultiTenant;
            }

            return (success: success, msg: msg);
        }

        #endregion
    }
}
