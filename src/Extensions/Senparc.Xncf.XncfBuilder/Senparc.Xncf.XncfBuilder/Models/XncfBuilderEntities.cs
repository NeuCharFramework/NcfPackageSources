using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder
{
    public class XncfBuilderEntities : XncfDatabaseDbContext, IMultipleMigrationDbContext
    {
        public override IXncfDatabase XncfDatabaseRegister => new Register();
        public XncfBuilderEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        /// <summary>
        /// 默认数据库
        /// </summary>
        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.SQLite;

        public DbSet<Config> Configs { get; set; }
    }
}
