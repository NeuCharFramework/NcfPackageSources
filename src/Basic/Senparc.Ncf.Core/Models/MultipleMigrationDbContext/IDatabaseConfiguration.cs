using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Data.Common;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 数据库配置接口
    /// <para>官方推荐的数据库提供程序：https://docs.microsoft.com/zh-cn/ef/core/providers/?tabs=dotnet-core-cli </para>
    /// <typeparamref name="TBuilder">DbContextOptionsBuilder 的类型，如：SQL Server 使用 SqlServerDbContextOptionsBuilder</typeparamref>
    /// </summary>
    public interface IDatabaseConfiguration<TBuilder, TExtension> : IDatabaseConfiguration
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension : RelationalOptionsExtension, new()
    {

    }

    /// <summary>
    /// 数据库配置接口
    /// </summary>
    public interface IDatabaseConfiguration
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        MultipleDatabaseType MultipleDatabaseType { get; }

        /// <summary>
        /// 操作 DbContextOptions 的基础方法
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase { get; }

        /// <summary>
        /// 操作 DbContextOptions 的扩展方法
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        /// <summary>
        /// 设定 UseSLite、UseSQLServer 等方法
        /// </summary>
        Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        /// 使用数据库，如：
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="builder">DbContextOptionsBuilder</param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dbContextOptionsAction">额外配置操作</param>
        /// <param name="xncfDatabaseData">IXncfDatabase 信息（仅在针对 XNCF 进行数据库迁移时有效）</param>
        void UseDatabase(DbContextOptionsBuilder builder, /*IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder,*/ string connectionString,
                XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null);

        /// <summary>
        /// 备份数据库方法
        /// <para>如果返回null，则在方法内部完成备份程序</para>
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="backupFilePath"></param>
        string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath);

        /// <summary>
        /// 删除指定表Sql
        /// <para>如果返回null，则在方法内部完成删除操作</para>
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        string GetDropTableSql(DbContext dbContext, string tableName);
    }
}