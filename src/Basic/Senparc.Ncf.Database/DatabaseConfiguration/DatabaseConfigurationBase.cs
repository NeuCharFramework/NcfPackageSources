using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Data.Common;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// Database configuration base class
    /// <para>Officially recommended database provider: https://docs.microsoft.com/zh-cn/ef/core/providers/?tabs=dotnet-core-cli </para>
    /// </summary>
    public abstract class DatabaseConfigurationBase<TBuilder, TExtension> : IDatabaseConfiguration<TBuilder, TExtension>
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension :  RelationalOptionsExtension, new()
    {
        public abstract MultipleDatabaseType MultipleDatabaseType { get; }

        /// <summary>
        /// DbContextOptionsAction with type <typeparamref name="TBuider"/>
        /// </summary>
        public virtual Action<TBuilder, XncfDatabaseData> TypedDbContextOptionsActionBase => DbContextOptionsActionBase;

        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase => (builder, xncfDatabaseData) =>
         {

             //Get the current database factory
             var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
             //Get the corresponding information of the currently specified IXncfDatabase (only valid when add-migration is performed for a specific XNCF database module)
             if (xncfDatabaseData != null)
             {
                 var typedBuilder = builder as TBuilder;

                 //Gets the type of the DbContext corresponding to the current database type for the specified IXncfDatabase
                 var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(xncfDatabaseData.XncfDatabaseRegister.GetType());

                 //The assembly name of the DbContext (or force specifying the assembly name that generates add-migration
                 var dbContextAssemblyName = xncfDatabaseData.AssemblyName ?? dbContextType.Assembly.FullName;
                 //Migration History table name
                 var databaseMigrationHistoryTableName = NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(xncfDatabaseData.XncfDatabaseRegister);

                 typedBuilder.MigrationsAssembly(dbContextAssemblyName)
                        .MigrationsHistoryTable(databaseMigrationHistoryTableName);
             }
             else
             {
                 // DatabaseConfigurationFactory.CurrentXncfDatabaseData is allowed to be null when the program is executed
             }
         };

        public abstract Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        public abstract Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        /// Backup database method
        /// <para>If null is returned, the backup procedure is completed inside the method</para>
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="backupFilePath"></param>
        /// <returns></returns>
        public abstract string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath);
        /// <summary>
        /// Delete the specified table Sql
        /// <para>If null is returned, the deletion operation is completed inside the method</para>
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string GetDropTableSql(DbContext dbContext, string tableName);

        /// <summary>
        /// Use a database, such as:
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="xncfDatabaseData"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsAction">Additional content that needs to be configured</param>
        public void UseDatabase(DbContextOptionsBuilder builder,/*IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder,*/ string connectionString,
                XncfDatabaseData xncfDatabaseData = null, 
                Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            //Perform operations such as UseSQLite, UseSQLServer, etc.
            SetUseDatabase(builder, connectionString, xncfDatabaseData, b =>
                {
                    //Execute basic code
                    DbContextOptionsActionBase(b, xncfDatabaseData);
                    //Execute extension code
                    DbContextOptionsActionExtension?.Invoke(b, xncfDatabaseData);
                    //Execute other methods passed in from outside
                    dbContextOptionsAction?.Invoke(b, xncfDatabaseData);
                }
            );
        }

    }
}