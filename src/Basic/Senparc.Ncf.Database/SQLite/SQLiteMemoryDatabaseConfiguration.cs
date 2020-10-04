using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure.Internal;
using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database.SQLite
{
    /// <summary>
    /// SQLite 内存数据库配置（NCF 对 IDatabaseConfiguration 的一个默认实现，也可以自定义配置）
    /// </summary>
    public class SQLiteMemoryDatabaseConfiguration : DatabaseConfigurationBase<SqliteDbContextOptionsBuilder, SqliteOptionsExtension>
    {
        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase => (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
        {
            //强制使用内存数据库
            connectionString = "Filename=:memory:";
            //其他更多配置

            //执行 UseSqlite（必须）
            optionsBuilder.UseSqlite(connectionString, actionBase);
        };

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (builder, xncfDatabaseData) =>
        {
            //更多数据库操作独立配置（非必须）
        };
    }
}
