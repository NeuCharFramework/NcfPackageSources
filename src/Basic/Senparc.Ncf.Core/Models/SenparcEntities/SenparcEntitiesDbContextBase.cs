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
            Console.WriteLine("\t SenparcEntitiesBase OnModelCreating");

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
        }
    }
}
