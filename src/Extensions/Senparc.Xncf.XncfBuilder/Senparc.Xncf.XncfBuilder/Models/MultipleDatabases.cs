using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Models
{
    /// <summary>
    /// 用于生成 MySQL 数据库 Migration 信息的类，请勿修改
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class XncfBuilderEntities_MySql : XncfBuilderEntities, IMultipleMigrationDbContext
    {
        public XncfBuilderEntities_MySql(DbContextOptions dbContextOptions) : base(dbContextOptions) { }
    }

    /// <summary>
    /// 用于生成 SQLServer 数据库 Migration 信息的类，请勿修改
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
    public class XncfBuilderEntities_SqlServer : XncfBuilderEntities, IMultipleMigrationDbContext
    {
        public XncfBuilderEntities_SqlServer(DbContextOptions dbContextOptions) : base(dbContextOptions) { }
    }

}
