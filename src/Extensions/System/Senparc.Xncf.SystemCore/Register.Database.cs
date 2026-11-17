/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.Database.cs

    File description: Register.Database related implementation

    

    

    Created by: Senparc - 20211128

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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
        public const string DATABASE_PREFIX = "SYSTEM_CORE_"; //NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//system tables，

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            #region historical solution reference information
            /* Legacy comment translated to English.

             *      errorinformation：

             *          Legacy comment translated to English.

             *          Legacy comment translated to English.

             *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.

             *      Legacy comment translated to English.

             */

            /* Legacy comment translated to English.

             *      errorinformation：

             *          Legacy comment translated to English.

             *          Legacy comment translated to English.

             *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.

             *      Legacy comment translated to English.

             */
            #endregion

            //var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.Current;

            /* 

             *     Legacy comment translated to English.

             * Legacy comment translated to English.

             * 

             * Legacy comment translated to English. 

            */
            var xncfDatabaseData = new XncfDatabaseData(this, NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX);

            #region Legacy section


            Func<IServiceProvider, SenparcEntities> senparcEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;

                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as SenparcEntities;
            };

            services.AddScoped<SenparcEntitiesDbContextBase>(senparcEntitiesImplementationFactory);//Legacy comment translated to English.
            services.AddScoped<ISenparcEntitiesDbContext>(senparcEntitiesImplementationFactory);
            services.AddScoped<SenparcEntitiesBase>(senparcEntitiesImplementationFactory);//Legacy comment translated to English.
            services.AddScoped<SenparcEntities>(senparcEntitiesImplementationFactory);

            #endregion

            //Legacy comment translated to English.
            Func<IServiceProvider, BasePoolEntities> basePoolEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as BasePoolEntities;
            };
            services.AddScoped<BasePoolEntities>(basePoolEntitiesImplementationFactory);

            services.AddScoped<INcfClientDbData, NcfClientDbData>();
            services.AddScoped<INcfDbData, NcfClientDbData>();

            //Legacy comment translated to English.
            EntitySetKeys.TryLoadSetInfo(typeof(BasePoolEntities));
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        #region Legacy section

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

                //updatedatabase
                var pendingMigs = await basePoolEntities.Database.GetPendingMigrationsAsync();
                if (pendingMigs.Count() > 0)
                {
                    basePoolEntities.ResetMigrate();//Legacy comment translated to English.

                    try
                    {
                        var script = basePoolEntities.Database.GenerateCreateScript();
                        SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);

                        basePoolEntities.Migrate();//Legacy comment translated to English.

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
                //temporarilydisablemulti-tenancystatus
                SiteConfig.SenparcCoreSetting.EnableMultiTenant = false;

                var result = await GenerateCreateScript(serviceProvider);//Legacy comment translated to English.
                success = result.success;
                msg = result.msg;

                SiteConfig.SenparcCoreSetting.EnableMultiTenant = oldMultiTenant;
            }

            return (success: success, msg: msg);
        }

        #endregion
    }
}
