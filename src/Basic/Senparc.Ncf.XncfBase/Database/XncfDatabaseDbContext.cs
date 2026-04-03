using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    ///DbContext base class used by IXncfDatabase
    /// </summary>
    public abstract class XncfDatabaseDbContext : DbContext, ISenparcEntitiesDbContext, IMultipleMigrationDbContext
    {
        MultipleMigrationDbContextAttribute _multipleMigrationDbContext;
        /// <summary>
        /// MultipleMigrationDbContext
        /// </summary>
        public MultipleMigrationDbContextAttribute MultipleMigrationDbContext
        {
            get
            {
                if (_multipleMigrationDbContext == null)
                {
                    _multipleMigrationDbContext = this.GetType().GetCustomAttribute(typeof(MultipleMigrationDbContextAttribute)) as MultipleMigrationDbContextAttribute;

                    if (_multipleMigrationDbContext == null)
                    {
                        throw new NcfDatabaseException($"{this.GetType().Name} 未标记 {nameof(MultipleMigrationDbContextAttribute)} 特性！", null, this.GetType());
                    }
                }
                return _multipleMigrationDbContext;
            }
        }

        /// <summary>
        /// Current IXncfDatabase registration class instance
        /// </summary>
        public IXncfDatabase XncfDatabaseRegister => MultipleMigrationDbContext.XncfDatabaseRegister;


        protected XncfDatabaseDbContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (XncfDatabaseRegister == null)
            {
                throw new ArgumentNullException(nameof(XncfDatabaseRegister));
            }

            XncfDatabaseRegister.OnModelCreating(modelBuilder);

            var types = modelBuilder.Model.GetEntityTypes().Where(e => typeof(EntityBase).IsAssignableFrom(e.ClrType));
            foreach (var entityType in types)
            {
                SetGlobalQueryMethodInfo(entityType, modelBuilder);
            }

            base.OnModelCreating(modelBuilder);
        }

        #region ISenparcEntities 接口


        public bool? _enableMultiTenant;

        /// <summary>
        /// Whether to enable multi-tenancy, the default is to read SiteConfig.SenparcCoreSetting.EnableMultiTenant
        /// </summary>
        public bool EnableMultiTenant
        {
            get
            {
                if (!_enableMultiTenant.HasValue)
                {
                    _enableMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
                }
                return _enableMultiTenant.Value;
            }
            private set
            {
                _enableMultiTenant = value;
            }
        }

        private RequestTenantInfo _tenantInfo;

        /// <summary>
        /// Tenant information in the current context <![CDATA[If multi-tenancy is not enabled, the default value is NULL]]>
        /// </summary>
        public RequestTenantInfo TenantInfo
        {
            get
            {
                if (this.EnableMultiTenant && _tenantInfo == null)
                {
                    var serviceProvicer = Senparc.CO2NET.SenparcDI.GetServiceProvider();
                    _tenantInfo = MultiTenantHelper.TryGetAndCheckRequestTenantInfo(serviceProvicer, $"SenparcEntitiesDbContextBase.TenantInfo", this);
                }
                return _tenantInfo;
            }
            set
            {
                _tenantInfo = value;
            }
        }

        private static readonly bool[] _migrated = { true };

        /// <summary>
        /// 
        /// </summary>
        private void SetGlobalQueryMethodInfo(IMutableEntityType entityType, ModelBuilder modelBuilder)
        {
            var dbContextType = this.GetType();
            dbContextType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(t => t.IsGenericMethod && t.Name == "SetGlobalQuery")
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, new object[] { modelBuilder });
        }


        public void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase
        {
            builder.Entity<T>().HasQueryFilter(z => !z.Flag);
        }

        public void ResetMigrate()
        {
            _migrated[0] = false;
        }

        public void Migrate()
        {
            if (!_migrated[0])
            {
                lock (_migrated)
                {
                    if (!_migrated[0])
                    {
                        Database.Migrate(); // apply all migrations
                        _migrated[0] = true;
                    }
                }
            }
        }


        #endregion

        /// <summary>
        /// Migrate series of operations used during installation
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="databaseRegister">Register that implements the database interface, databaseRegister.TryGetXncfDatabaseDbContextType must return a subclass of XncfDatabaseDbContext</param>
        /// <param name="checkdbContextType">Whether the dbContextType type needs to be checked</param>
        /// <returns></returns>
        public static async Task MigrateOnInstallAsync(IServiceProvider serviceProvider, IXncfDatabase databaseRegister, bool checkdbContextType = true)
        {
            var dbContextType = databaseRegister.TryGetXncfDatabaseDbContextType;
            if (checkdbContextType && !dbContextType.IsSubclassOf(typeof(XncfDatabaseDbContext)))
            {
                throw new NcfDatabaseException($"dbContextType 参数必须继承自 XncfDatabaseDbContext，当前类型：{dbContextType.FullName}", null, dbContextType);
            }

            var xncfDatabaseDbContext = serviceProvider.GetService(dbContextType) as XncfDatabaseDbContext;
            await MigrateOnInstallAsync(xncfDatabaseDbContext);
        }


        /// <summary>
        /// Migrate series of operations used during installation
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task MigrateOnInstallAsync(XncfDatabaseDbContext dbContext)
        {
            if (dbContext is null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }
            //Update database
            var pendingMigs = await dbContext.Database.GetPendingMigrationsAsync();

            SenparcTrace.SendCustomLog("MigrateOnInstallAsync", $"dbContext:{dbContext.GetType().FullName}");

            if (pendingMigs.Count() > 0)
            {
                dbContext.ResetMigrate();//Reset merge status

                SenparcTrace.SendCustomLog("MigrateOnInstallAsync", $"dbContext.ResetMigrate() 运行完毕");

                try
                {
                    var script = dbContext.Database.GenerateCreateScript();
                    SenparcTrace.SendCustomLog($"MigrateOnInstallAsync", $"{dbContext.GetType().Name}.Database.GenerateCreateScript:\r\n{script}");

                    dbContext.Migrate();//merge
                    SenparcTrace.SendCustomLog("MigrateOnInstallAsync", $"dbContext.Migrate() 运行完毕");
                }
                catch (Exception ex)
                {
                    var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                    SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), dbContext.GetType(), ex));
                }
            }
        }
    }
}
