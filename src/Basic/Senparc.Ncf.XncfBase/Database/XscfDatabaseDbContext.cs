using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel.Resolution;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    /// IXncfDatabase 使用的 DbContext 基类
    /// </summary>
    public abstract class XncfDatabaseDbContext : DbContext, IMultipleMigrationDbContext
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

        public XncfDatabaseDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (XncfDatabaseRegister == null)
            {
                throw new ArgumentNullException(nameof(XncfDatabaseRegister));
            }

            XncfDatabaseRegister.OnModelCreating(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }
    }
}
