using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.CO2NET.Extensions;
using System;

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
        public virtual string GetDatabaseMigrationHistoryTableName(XncfDatabaseData xncfDatabaseData)
        {
            if (!xncfDatabaseData.DatabaseUniquePrefix.IsNullOrWhiteSpace())
            {
                return "__" + xncfDatabaseData.DatabaseUniquePrefix + "_EFMigrationsHistory";
            }
            return null;
        }

        /// <summary>
        /// 对 DbContextOptionsBuilder 的配置操作
        /// <para>参数1：TBuilder</para>
        /// <para>参数2：IXncfDatabase（仅在针对 XNCF 进行数据库迁移时有效）</para>
        /// <para>参数3：强制指定 migration 的程序集名称（仅在针对 XNCF 进行数据库迁移时有效）</para>
        /// </summary>
        public virtual Action<TBuilder, XncfDatabaseData> DbContextOptionsAction => (builder, xncfDatabaseData) =>
            {
                //DbContext的程序集名称（或强制指定生成 add-migration 的程序集名称
                var dbContextAssemblyName = xncfDatabaseData.AssemblyName ?? xncfDatabaseData.XncfDatabaseDbContextType.Assembly.FullName;
                //Migration History 的表名
                var databaseMigrationHistoryTableName = GetDatabaseMigrationHistoryTableName(xncfDatabaseData);

                builder.MigrationsAssembly(dbContextAssemblyName)
                       .MigrationsHistoryTable(databaseMigrationHistoryTableName);
            };

        /// 使用数据库，如：
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsAction"></param>
        public abstract void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<TBuilder> dbContextOptionsAction = null);
    }
}