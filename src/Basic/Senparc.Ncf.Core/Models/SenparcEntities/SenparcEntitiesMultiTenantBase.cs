using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Models
{
    /* 说明：
     * 把 SenparcEntitiesMultiTenantBase 独立出来做一层的意义在于剥离必须需要支持多租户的对象（如系统表），
     * 这样可以单独初始化一个只有 TenantInfo，而没有任何其他系统表的 DbContext。
     * 这么做可以避免一种死循环的情况：
     *      1、在缓存第一次录入租户信息（TenantInfos）的过程中，需要初始化所有表，并执行 OnModelCreating()，
     *          而 OnModelCreating() 在整个系统服务生命周期中只执行一次，而此时马上要确定依赖多租户对象（如系统表）的租户信息，
     *      2、但多租户信息此时尚未生成，如果尝试获取，就进入到 1 的循环中。
     */

    /// <summary>
    /// 多租户 EF Core DbContext
    /// </summary>
    public class SenparcEntitiesMultiTenantBase : SenparcEntitiesDbContextBase, ISenparcEntitiesDbContext
    {
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
                        requestTenantInfo = requestTenantInfo ?? MultiTenantHelper.TryGetAndCheckRequestTenantInfo(ServiceProvider, "SenparcEntitiesMultiTenantBase.AddTenandId()", this);
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



        /// <summary>
        /// 多租户信息
        /// </summary>
        public DbSet<TenantInfo> TenantInfos { get; set; }


        public SenparcEntitiesMultiTenantBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }


        /// <summary>
        /// 设置当前 DbContext 是否启用上下文
        /// </summary>
        /// <param name="enable"></param>
        public void SetMultiTenantEnable(bool enable)
        {
            Console.WriteLine($"\t {this.GetHashCode()}\tset EnableMultiTenant to:" + enable);
            EnableMultiTenant = enable;
        }

        /// <summary>
        /// 多租户状态重置为 SiteConfig.SenparcCoreSetting.EnableMultiTenant
        /// </summary>
        public void ResetMultiTenantEnable()
        {
            Console.WriteLine($"\t {this.GetHashCode()}\tResetMultiTenantEnable()");
            SetMultiTenantEnable(SiteConfig.SenparcCoreSetting.EnableMultiTenant);
        }


        /// <summary>
        /// 全局查询，附带软租户查询状态
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        public override void SetGlobalQuery<T>(ModelBuilder builder)
        {
            //多租户
            Console.WriteLine($"\t DbContext:{this.GetHashCode()} \tSetGlobalQuery<{typeof(T).Name}> this.EnableMultiTenant:" + this.EnableMultiTenant + $" / SiteConfig.SenparcCoreSetting.EnableMultiTenant:{SiteConfig.SenparcCoreSetting.EnableMultiTenant}");
            if (this.EnableMultiTenant && typeof(IMultiTenancy).IsAssignableFrom(typeof(T)) && !(typeof(IIgnoreMulitTenant).IsAssignableFrom(typeof(T))))
            {
                RequestTenantInfo requestTenantInfo = MultiTenantHelper.TryGetAndCheckRequestTenantInfo(ServiceProvider, $"SenparcEntitiesMultiTenantBase.SetGlobalQuery<{typeof(T).Name}>(ModelBuilder builder)", this);
                var entityBuilder = builder.Entity<T>();
                entityBuilder.HasQueryFilter(z => z.TenantId == requestTenantInfo.Id);
            }

            base.SetGlobalQuery<T>(builder);
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
