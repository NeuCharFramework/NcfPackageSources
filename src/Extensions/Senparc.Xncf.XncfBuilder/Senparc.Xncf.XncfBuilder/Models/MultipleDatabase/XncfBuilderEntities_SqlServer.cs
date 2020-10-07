using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Models.MultipleDatabase
{
    /// <summary>
    /// 用于生成 SQLServer 数据库 Migration 信息的类，请勿修改
    /// </summary>
    public class XncfBuilderEntities_SqlServer : XncfBuilderEntities, IMultipleMigrationDbContext
    {
        public XncfBuilderEntities_SqlServer(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.SqlServer;
    }
}
