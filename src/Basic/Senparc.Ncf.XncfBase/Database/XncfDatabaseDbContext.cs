using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Linq;
using System.Reflection;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    /// IXncfDatabase 使用的 DbContext 基类
    /// </summary>
    public abstract class XncfDatabaseDbContext : DbContext, ISenparcEntities, IMultipleMigrationDbContext
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
        /// 当前 IXncfDatabase 注册类实例
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
    }
}
