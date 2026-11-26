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

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

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
    /// Base EF Core context for XNCF module databases, including soft-delete,
    /// optional tenant query filters, and guarded migration helpers.
    /// </summary>
    public abstract class XncfDatabaseDbContext : DbContext, ISenparcEntitiesDbContext, IMultipleMigrationDbContext
    {
        MultipleMigrationDbContextAttribute _multipleMigrationDbContext;
        /// <summary>
        /// Gets the migration metadata attached to the concrete database context type.
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
        /// Gets the module database registration associated with this context.
        /// </summary>
        public IXncfDatabase XncfDatabaseRegister => MultipleMigrationDbContext.XncfDatabaseRegister;


        /// <summary>
        /// Initializes the XNCF database context with provider options.
        /// </summary>
        /// <param name="dbContextOptions">The EF Core context options.</param>
        protected XncfDatabaseDbContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        { }

        /// <inheritdoc />
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
        /// Gets whether multi-tenant query filters are enabled for this context.
        /// The default value is read from global NCF configuration.
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
        /// Gets or sets the tenant context used by multi-tenant query filters and inserts.
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
        /// Invokes the generic global-query configuration method for the discovered entity type.
        /// </summary>
        /// <param name="entityType">The EF Core entity metadata.</param>
        /// <param name="modelBuilder">The model builder receiving the query filter.</param>
        private void SetGlobalQueryMethodInfo(IMutableEntityType entityType, ModelBuilder modelBuilder)
        {
            var dbContextType = this.GetType();
            dbContextType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(t => t.IsGenericMethod && t.Name == "SetGlobalQuery")
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, new object[] { modelBuilder });
        }


        /// <summary>
        /// Applies a global query filter for soft deletion and, when enabled, tenant ownership.
        /// </summary>
        /// <typeparam name="T">The entity type receiving the query filter.</typeparam>
        /// <param name="builder">The model builder used to configure the entity.</param>
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
        /// Enables or disables tenant query filtering for this context instance.
        /// </summary>
        /// <param name="enable">Whether multi-tenant filtering should be active.</param>
        public void SetMultiTenantEnable(bool enable)
        {
            EnableMultiTenant = enable;
        }

        /// <summary>
        /// Restores the context's tenant filtering flag from global NCF configuration.
        /// </summary>
        public void ResetMultiTenantEnable()
        {
            SetMultiTenantEnable(SiteConfig.SenparcCoreSetting.EnableMultiTenant);
        }

        #region Legacy section

        /// <inheritdoc />
        public override int SaveChanges()
        {
            //handlingmulti-tenancy
            AddTenantId();
            return base.SaveChanges();
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            //handlingmulti-tenancy
            AddTenantId();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// Assigns the current tenant ID to newly added multi-tenant entities unless
        /// they explicitly opt out of tenant filtering.
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

        /// <summary>
        /// Clears the process-local migration flag so the next call to <see cref="Migrate"/>
        /// invokes the database migration pipeline again.
        /// </summary>
        public void ResetMigrate()
        {
            _migrated[0] = false;
        }

        /// <summary>
        /// Applies pending EF Core migrations once per context instance/process guard.
        /// Concurrent callers are serialized by the migration guard.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the database provider cannot apply the migrations.</exception>
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
        /// Resolves the module DbContext, generates a creation script when migrations are pending,
        /// and applies migrations during module installation.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve the module context.</param>
        /// <param name="databaseRegister">The module database registration.</param>
        /// <param name="checkdbContextType">Whether to require the resolved type to derive from this context.</param>
        /// <returns>A task that completes after migration handling.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the resolved context type is invalid.</exception>
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
        /// Generates and applies pending migrations for an existing XNCF database context.
        /// </summary>
        /// <param name="dbContext">The context to migrate.</param>
        /// <returns>A task that completes after migration handling.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
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
