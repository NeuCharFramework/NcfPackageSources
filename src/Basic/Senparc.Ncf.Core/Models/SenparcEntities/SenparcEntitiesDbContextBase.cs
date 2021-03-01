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
    /// SenparcEntities 的基类
    /// </summary>
    public abstract class SenparcEntitiesDbContextBase : DbContext, ISenparcEntitiesDbContext
    {
        private static readonly bool[] _migrated = { true };
        private IServiceProvider _serviceProvider;
        //private IServiceScope _serviceScope;

        /// <summary>
        /// SenparcDI 储存的 GlobalServiceCollection 生成的 ServiceProvider
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
        /// 自动添加多租户Id
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

                RequestTenantInfo requestTenantInfo = null;
                foreach (var entity in addedEntities)
                {
                    if (!(entity is IIgnoreMulitTenant) && (entity is IMultiTenancy multiTenantEntity))
                    {
                        //如果未设置，则进行设定
                        requestTenantInfo = requestTenantInfo ?? MultiTenantHelper.TryGetAndCheckRequestTenantInfo(ServiceProvider, "SenparcEntitiesDbContextBase.AddTenandId()", this);
                        multiTenantEntity.TenantId = requestTenantInfo.Id;
                    }
                }
            }
        }

        public bool? _enableMultiTenant;

        /// <summary>
        /// 是否启用多租户，默认读取 SiteConfig.SenparcCoreSetting.EnableMultiTenant
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

        #region Migration 迁移相关方法

        /// <summary>
        /// 执行 EF Core 的合并操作（等价于 update-database）
        /// <para>出于安全考虑，每次执行 Migrate() 方法之前，必须先执行 ResetMigrate() 开启允许 Migrate 执行的状态。</para>
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
        /// 重置合并状态
        /// </summary>
        public virtual void ResetMigrate()
        {
            _migrated[0] = false;
        }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("\t SenparcEntitiesDbContextBase OnModelCreating");

            var types = modelBuilder.Model.GetEntityTypes().Where(e => typeof(EntityBase).IsAssignableFrom(e.ClrType));
            Console.WriteLine("\t\t types:" + types.Select(z=>z.Name).ToJson());
            foreach (var entityType in types)
            {
                Console.WriteLine("\t\t\t type:" + entityType.Name);

                SetGlobalQueryMethodInfo
                        .MakeGenericMethod(entityType.ClrType)
                        .Invoke(this, new object[] { modelBuilder });
            }

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// 设置全局查询
        /// </summary>
        private static readonly MethodInfo SetGlobalQueryMethodInfo = typeof(SenparcEntitiesBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQuery) /*"SetGlobalQuery"*/);

        /// <summary>
        /// 全局查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        public virtual void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase
        {
            //软删除
            var entityBuilder = builder.Entity<T>().HasQueryFilter(z => !z.Flag);

            //多租户
            //Console.WriteLine($"\t DbContext:{this.GetHashCode()} \tSetGlobalQuery<{typeof(T).Name}> this.EnableMultiTenant:" + this.EnableMultiTenant + $" / SiteConfig.SenparcCoreSetting.EnableMultiTenant:{SiteConfig.SenparcCoreSetting.EnableMultiTenant}");
            if (this.EnableMultiTenant && typeof(IMultiTenancy).IsAssignableFrom(typeof(T)) && !(typeof(IIgnoreMulitTenant).IsAssignableFrom(typeof(T))))
            {
                RequestTenantInfo requestTenantInfo = MultiTenantHelper.TryGetAndCheckRequestTenantInfo(ServiceProvider, $"SenparcEntitiesDbContextBase.SetGlobalQuery<{typeof(T).Name}>(ModelBuilder builder)", this);
                entityBuilder.HasQueryFilter(z => z.TenantId == requestTenantInfo.Id);
            }
        }



        /// <summary>
        /// 设置当前 DbContext 是否启用上下文
        /// </summary>
        /// <param name="enable"></param>
        public void SetMultiTenantEnable(bool enable)
        {
            //Console.WriteLine($"\t {this.GetHashCode()}\tset EnableMultiTenant to:" + enable);
            EnableMultiTenant = enable;
        }

        /// <summary>
        /// 多租户状态重置为 SiteConfig.SenparcCoreSetting.EnableMultiTenant
        /// </summary>
        public void ResetMultiTenantEnable()
        {
            //Console.WriteLine($"\t {this.GetHashCode()}\tResetMultiTenantEnable()");
            SetMultiTenantEnable(SiteConfig.SenparcCoreSetting.EnableMultiTenant);
        }


        public override int SaveChanges()
        {
            //处理多租户
            AddTenandId();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            //处理多租户
            AddTenandId();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }


        //public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //{
        //    return base.SaveChangesAsync(cancellationToken);//底层引用的就是  SaveChangesAsync，所以不用处理
        //}
    }
}
