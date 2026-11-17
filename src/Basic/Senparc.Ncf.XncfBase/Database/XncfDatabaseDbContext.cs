/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfDatabaseDbContext.cs
    File description: XncfDatabaseDbContext related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261005
    Change description: v0.28.1 0.28.1 Merge remote-tracking branch 'origin/Developer-MAF-V3' into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.28.1 Updated module behavior and related support.

----------------------------------------------------------------*/

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
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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

        #region Legacy section


        public bool? _enableMultiTenant;

        /// <summary>
        /// whether enabledmulti-tenancy，defaultread SiteConfig.SenparcCoreSetting.EnableMultiTenant
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
        ///Legacy comment translated to English.
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


        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase
        {
            var entityBuilder = builder.Entity<T>();//.HasQueryFilter(z => !z.Flag);

            //multi-tenancy
            if (this.EnableMultiTenant && typeof(IMultiTenancy).IsAssignableFrom(typeof(T)) && !(typeof(IIgnoreMulitTenant).IsAssignableFrom(typeof(T))))
            {
                //Legacy comment translated to English.
                entityBuilder.HasQueryFilter(z => z.TenantId == TenantInfo.Id && !z.Flag);
            }
            else
            {
                //Legacy comment translated to English.
                entityBuilder.HasQueryFilter(z => !z.Flag);
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="enable"></param>
        public void SetMultiTenantEnable(bool enable)
        {
            EnableMultiTenant = enable;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void ResetMultiTenantEnable()
        {
            SetMultiTenantEnable(SiteConfig.SenparcCoreSetting.EnableMultiTenant);
        }

        #region Legacy section

        public override int SaveChanges()
        {
            //handlingmulti-tenancy
            AddTenantId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            //handlingmulti-tenancy
            AddTenantId();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private void AddTenantId()
        {
            if (this.EnableMultiTenant)
            {
                ChangeTracker.DetectChanges(); // 
                var addedEntities = this.ChangeTracker
                                            .Entries()
                                            .Where(z => z.State == EntityState.Added)
                                            .Select(z => z.Entity)
                                            .ToList();

                RequestTenantInfo requestTenantInfo = TenantInfo;
                foreach (var entity in addedEntities)
                {
                    if (!(entity is IIgnoreMulitTenant) && (entity is IMultiTenancy multiTenantEntity))
                    {
                        multiTenantEntity.TenantId = requestTenantInfo.Id;
                    }
                }
            }
        }

        #endregion

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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="serviceProvider"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
            //updatedatabase
            var pendingMigs = await dbContext.Database.GetPendingMigrationsAsync();

            SenparcTrace.SendCustomLog("MigrateOnInstallAsync", $"dbContext:{dbContext.GetType().FullName}");

            if (pendingMigs.Count() > 0)
            {
                dbContext.ResetMigrate();//Legacy comment translated to English.

                SenparcTrace.SendCustomLog("MigrateOnInstallAsync", $"dbContext.ResetMigrate() 运行完毕");

                try
                {
                    var script = dbContext.Database.GenerateCreateScript();
                    SenparcTrace.SendCustomLog($"MigrateOnInstallAsync", $"{dbContext.GetType().Name}.Database.GenerateCreateScript:\r\n{script}");

                    dbContext.Migrate();//Legacy comment translated to English.
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
