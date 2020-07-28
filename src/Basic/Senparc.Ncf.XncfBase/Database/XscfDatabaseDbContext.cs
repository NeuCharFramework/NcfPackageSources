using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    /// IXncfDatabase 使用的 DbContext 基类
    /// </summary>
    public abstract class XncfDatabaseDbContext : DbContext
    {
        /// <summary>
        /// 当前 IXncfDatabase 注册类实例
        /// </summary>
        public abstract IXncfDatabase XncfDatabaseRegister { get; }

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
