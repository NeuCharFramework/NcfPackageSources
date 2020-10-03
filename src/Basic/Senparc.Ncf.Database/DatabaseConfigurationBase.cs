using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// 数据库配置基类
    /// <para>官方推荐的数据库提供程序：https://docs.microsoft.com/zh-cn/ef/core/providers/?tabs=dotnet-core-cli </para>
    /// </summary>
    public abstract class DatabaseConfigurationBase<TBuilder, TExtension> : IDatabaseConfiguration<TBuilder, TExtension>
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension : RelationalOptionsExtension, new()
    {

        /// <summary>
        /// 获取 EF Code First MigrationHistory 数据库表名
        /// </summary>
        /// <returns></returns>
        public virtual string GetDatabaseMigrationHistoryTableName(string databaseUniquePrefix)
        {
            if (!databaseUniquePrefix.IsNullOrWhiteSpace())
            {
                return "__" + databaseUniquePrefix + "_EFMigrationsHistory";
            }
            return null;
        }

        /// <summary>
        /// 对 DbContextOptionsBuilder 的配置操作
        /// <para>参数1：TBuilder</para>
        /// <para>参数2：dbContext 的 AssemblyName（仅在针对 XNCF 进行数据库迁移时有效）</para>
        /// <para>参数3：MigrationHistoryTableName（仅在针对 XNCF 进行数据库迁移时有效）</para>
        /// <para>参数3：DatabaseUniquePrefix（仅在针对 XNCF 进行数据库迁移时有效）</para>
        /// </summary>
        public virtual Action<TBuilder, string, string, string> DbContextOptionsAction => (builder, assemblyName, migrationHistoryTableName, databaseUniquePrefix) =>
           {

           };

        public abstract void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<TBuilder> dbContextOptionsAction = null);
    }
}