using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Database.SQLite
{
    /// <summary>
    /// SQLite 内存数据库配置（NCF 对 IDatabaseConfiguration 的一个默认实现，也可以自定义配置）
    /// </summary>
    public class SQLiteMemoryDatabaseConfiguration : IDatabaseConfiguration
    {
        public Action<IRelationalDbContextOptionsBuilderInfrastructure> DbContextOptionsAction => b => { };

        public Type DbContextOptionsBuilderType => typeof(SqliteDbContextOptionsBuilder);

        public void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null)
        {
            connectionString = "Filename=:memory:";//强制使用内存数据库
            optionsBuilder.UseSqlite(connectionString, dbContextOptionsAction);//beta6
        }
    }
}
