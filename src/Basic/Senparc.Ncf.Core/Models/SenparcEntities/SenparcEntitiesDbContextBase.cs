/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SenparcEntitiesDbContextBase.cs

    File description: SenparcEntitiesDbContextBase related implementation

    

    

    Created by: Senparc - 20210221

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public abstract class SenparcEntitiesDbContextBase : DbContext, ISenparcEntitiesDbContext
    {
        private static readonly bool[] _migrated = { true };
        private IServiceProvider _serviceProvider;
        //private IServiceScope _serviceScope;
        private RequestTenantInfo _tenantInfo;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        protected virtual IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    throw new Senparc.Ncf.Core.Exceptions.NcfDatabaseException("_serviceProvider 不可以为 null", null);

                    //base.Database.GetDbConnection().Site.GetRequiredService<>

                    //_serviceScope = SenparcDI.GlobalServiceCollection.BuildServiceProvider().CreateScope();
                    //_serviceProvider = _serviceScope.ServiceProvider;// ((IInfrastructure<IServiceProvider>)this).Instance;
                }
                return _serviceProvider;
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public RequestTenantInfo TenantInfo
        {
            get
            {
                if (this.EnableMultiTenant && _tenantInfo == null)
                {
                    _tenantInfo = MultiTenantHelper.TryGetAndCheckRequestTenantInfo(ServiceProvider, $"SenparcEntitiesDbContextBase.TenantInfo", this);
                }
                return _tenantInfo;
            }
            set
            {
                _tenantInfo = value;
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private void AddTenandId()
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
                        ///Legacy comment translated to English.
                        //requestTenantInfo = requestTenantInfo ?? MultiTenantHelper.TryGetAndCheckRequestTenantInfo(ServiceProvider, "SenparcEntitiesDbContextBase.AddTenandId()", this);
                        multiTenantEntity.TenantId = requestTenantInfo.Id;
                    }
                }
            }
        }

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




        public SenparcEntitiesDbContextBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
        {
            _serviceProvider = serviceProvider;
        }

        //~SenparcEntitiesBase()
        //{
        //    _serviceScope?.Dispose();
        //}

        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public virtual void Migrate()
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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual void ResetMigrate()
        {
            _migrated[0] = false;
        }

        #endregion

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var dt1 = SystemTime.Now;
            var types = modelBuilder.Model.GetEntityTypes().Where(e => typeof(EntityBase).IsAssignableFrom(e.ClrType)).ToList();

            Console.WriteLine($"============ SenparcEntitiesDbContextBase OnModelCreating：{this.GetType().Name} ({types.Count()} types) ============");

            //Console.WriteLine("\t\t types:" + types.Select(z => z.Name).ToJson());

            var maxLength = types.Max(z => z.Name.Length);
            Console.WriteLine($" No.  |         Entity Type");
            Console.WriteLine($"------|{new string('-', maxLength)}");
            for (int i = 0; i < types.Count; i++)
            {
                //Legacy comment translated to English.

                var entityType = types[i];
                Console.WriteLine($"  {(i + 1).ToString().PadRight(4)}| {entityType.Name}");
                SetGlobalQueryMethodInfo
                        .MakeGenericMethod(entityType.ClrType)
                        .Invoke(this, new object[] { modelBuilder });
            }


            base.OnModelCreating(modelBuilder);

            Console.WriteLine($"============ SenparcEntitiesDbContextBase OnModelCreating End({SystemTime.DiffTotalMS(dt1)}ms) ============");
            Console.WriteLine();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static readonly MethodInfo SetGlobalQueryMethodInfo = typeof(SenparcEntitiesBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQuery) /*"SetGlobalQuery"*/);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        public virtual void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase
        {
            var entityBuilder = builder.Entity<T>();//.HasQueryFilter(z => !z.Flag);

            //multi-tenancy
            //Console.WriteLine($"\t DbContext:{this.GetHashCode()} \tSetGlobalQuery<{typeof(T).Name}> this.EnableMultiTenant:" + this.EnableMultiTenant + $" / SiteConfig.SenparcCoreSetting.EnableMultiTenant:{SiteConfig.SenparcCoreSetting.EnableMultiTenant}");
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
            //Console.WriteLine($"\t {this.GetHashCode()}\tset EnableMultiTenant to:" + enable);
            EnableMultiTenant = enable;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void ResetMultiTenantEnable()
        {
            //Console.WriteLine($"\t {this.GetHashCode()}\tResetMultiTenantEnable()");
            SetMultiTenantEnable(SiteConfig.SenparcCoreSetting.EnableMultiTenant);
        }


        public override int SaveChanges()
        {
            //handlingmulti-tenancy
            AddTenandId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            //handlingmulti-tenancy
            AddTenandId();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }


        //public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //{
        //Legacy comment translated to English.
        //}
    }
}
