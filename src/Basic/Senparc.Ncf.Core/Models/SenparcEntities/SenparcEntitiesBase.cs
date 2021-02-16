using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Config;
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
    public abstract class SenparcEntitiesBase : DbContext, ISenparcEntities
    {
        private static readonly bool[] _migrated = { true };
        private IServiceProvider _serviceProvider;
        private IServiceProvider SserviceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    _serviceProvider = SenparcDI.GetServiceProvider();
                }
                return _serviceProvider;
            }
        }

        public SenparcEntitiesBase(DbContextOptions options/*, IServiceProvider serviceProvider*/) : base(options)
        {
            _serviceProvider = SenparcDI.GetServiceProvider();
        }

        #region 系统表（无特殊情况不要修改）

        /// <summary>
        /// 系统设置
        /// </summary>
        public DbSet<SystemConfig> SystemConfigs { get; set; }


        /// <summary>
        /// 用户信息
        /// </summary>
        public virtual DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// 菜单
        /// </summary>
        public DbSet<SysMenu> SysMenus { get; set; }

        /// <summary>
        /// 菜单下面的按钮
        /// </summary>
        public DbSet<SysButton> SysButtons { get; set; }

        /// <summary>
        /// 系统角色
        /// </summary>
        public DbSet<SysRole> SysRoles { get; set; }

        /// <summary>
        /// 角色菜单表
        /// </summary>
        public DbSet<SysPermission> SysPermission { get; set; }

        /// <summary>
        /// 角色人员表
        /// </summary>
        public DbSet<SysRoleAdminUserInfo> SysRoleAdminUserInfos { get; set; }

        /// <summary>
        /// 用户积分日志
        /// </summary>
        public virtual DbSet<PointsLog> PointsLogs { get; set; }

        /// <summary>
        /// 用户支付日志
        /// </summary>

        public virtual DbSet<AccountPayLog> AccountPayLogs { get; set; }

        /// <summary>
        /// 扩展模块
        /// </summary>
        public DbSet<XncfModule> XncfModules { get; set; }


        /// <summary>
        /// 多租户信息
        /// </summary>
        public DbSet<TenantInfo> TenantInfos { get; set; }

        #endregion

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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region 不可修改系统表
            modelBuilder.ApplyConfiguration(new XncfModuleAccountConfigurationMapping());
            modelBuilder.ApplyConfiguration(new AccountConfigurationMapping());
            modelBuilder.ApplyConfiguration(new AccountPayLogConfigurationMapping());
            modelBuilder.ApplyConfiguration(new PointsLogConfigurationMapping());

            #endregion

            var types = modelBuilder.Model.GetEntityTypes().Where(e => typeof(EntityBase).IsAssignableFrom(e.ClrType));
            foreach (var entityType in types)
            {
                SetGlobalQueryMethodInfo
                        .MakeGenericMethod(entityType.ClrType)
                        .Invoke(this, new object[] { modelBuilder });
            }
        }

        /// <summary>
        /// 设置全局查询
        /// </summary>
        private static readonly MethodInfo SetGlobalQueryMethodInfo = typeof(SenparcEntitiesBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(t => t.IsGenericMethod && t.Name == nameof(SetGlobalQuery) /*"SetGlobalQuery"*/);

        /// <summary>
        /// 全局查询，附带软删除状态
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        public virtual void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase
        {
            //软删除
            Console.WriteLine("进入软删除 SetGlobalQuery");
            var entityBuilder = builder.Entity<T>().HasQueryFilter(z => !z.Flag);

            Console.WriteLine($"{typeof(T).Name} IsAssignableFrom IMultiTenancy:{typeof(T).IsAssignableFrom(typeof(IMultiTenancy))} / EnableMultiTenant:{SiteConfig.SenparcCoreSetting.EnableMultiTenant}");
            //多租户
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant && typeof(T).IsAssignableFrom(typeof(IMultiTenancy)))
            {
                Console.WriteLine("进入多租户 SetGlobalQuery");
                var requestTenantInfo = SserviceProvider.GetRequiredService<RequestTenantInfo>();
                entityBuilder.HasQueryFilter(z => z.TenantId == requestTenantInfo.Id);
            }

            //if (typeof(T).IsAssignableFrom(typeof(IMultipleTenant)))
            //{
            //    builder.Entity<T>().HasQueryFilter(z => !z.Flag);
            //}
        }

        /// <summary>
        /// 自动添加多租户Id
        /// </summary>
        private void AddTenandId()
        {
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                Console.WriteLine("进入多租户 AddTenandId");

                ChangeTracker.DetectChanges(); // 
                var addedEntities = this.ChangeTracker
                                            .Entries()
                                            .Where(z => z.State == EntityState.Added)
                                            .Select(z => z.Entity)
                                            .ToList();

                RequestTenantInfo requestTenantInfo = null;
                foreach (var entity in addedEntities)
                {
                    var multiTenantEntity = entity as IMultiTenancy;
                    if (multiTenantEntity != null)
                    {
                        requestTenantInfo = requestTenantInfo ?? SserviceProvider.GetRequiredService<RequestTenantInfo>();
                        multiTenantEntity.TenantId = requestTenantInfo.Id;
                    }
                }
            }
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

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);//底层引用的就是  SaveChangesAsync，所以不用处理
        }
    }
}
