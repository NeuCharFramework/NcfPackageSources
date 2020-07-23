using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    /// IXscfDatabase 使用的 DbContext 基类
    /// </summary>
    public abstract class XscfDatabaseDbContext : DbContext
    {
        /// <summary>
        /// 当前 IXscfDatabase 注册类实例
        /// </summary>
        public abstract IXscfDatabase XscfDatabaseRegister { get; }

        public XscfDatabaseDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (XscfDatabaseRegister == null)
            {
                throw new ArgumentNullException(nameof(XscfDatabaseRegister));
            }

            XscfDatabaseRegister.OnModelCreating(modelBuilder);


            base.OnModelCreating(modelBuilder);
        }
    }
}
