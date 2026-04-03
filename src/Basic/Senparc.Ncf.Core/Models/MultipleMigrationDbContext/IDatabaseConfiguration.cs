using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Data.Common;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Database configuration interface
    /// <para>Officially recommended database provider: https://docs.microsoft.com/zh-cn/ef/core/providers/?tabs=dotnet-core-cli </para>
    /// <typeparamref name="TBuilder">The type of DbContextOptionsBuilder, such as: SQL Server uses SqlServerDbContextOptionsBuilder</typeparamref>
    /// </summary>
    public interface IDatabaseConfiguration<TBuilder, TExtension> : IDatabaseConfiguration
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension : RelationalOptionsExtension, new()
    {

    }

    /// <summary>
    ///Database configuration interface
    /// </summary>
    public interface IDatabaseConfiguration
    {
        /// <summary>
        /// database type
        /// </summary>
        MultipleDatabaseType MultipleDatabaseType { get; }

        /// <summary>
        /// Basic methods for operating DbContextOptions
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase { get; }

        /// <summary>
        /// Extension method to operate DbContextOptions
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        /// <summary>
        /// Set methods such as UseSLite and UseSQLServer
        /// </summary>
        Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        /// Use a database, such as:
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="builder">DbContextOptionsBuilder</param>
        /// <param name="connectionString">Connection string</param>
        /// <param name="dbContextOptionsAction">Additional configuration operations</param>
        /// <param name="xncfDatabaseData">IXncfDatabase information (valid only when doing database migration for XNCF)</param>
        void UseDatabase(DbContextOptionsBuilder builder, /*IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder,*/ string connectionString,
                XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null);

        /// <summary>
        /// Backup database method
        /// <para>If null is returned, the backup procedure is completed inside the method</para>
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="backupFilePath"></param>
        string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath);

        /// <summary>
        /// Delete the specified table Sql
        /// <para>If null is returned, the deletion operation is completed inside the method</para>
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        string GetDropTableSql(DbContext dbContext, string tableName);
    }
}