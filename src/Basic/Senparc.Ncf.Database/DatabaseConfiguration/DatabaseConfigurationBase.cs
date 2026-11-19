/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseConfigurationBase.cs
    File description: DatabaseConfigurationBase related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public abstract class DatabaseConfigurationBase<TBuilder, TExtension> : IDatabaseConfiguration<TBuilder, TExtension>
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension :  RelationalOptionsExtension, new()
    {
        public abstract MultipleDatabaseType MultipleDatabaseType { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual Action<TBuilder, XncfDatabaseData> TypedDbContextOptionsActionBase => DbContextOptionsActionBase;

        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase => (builder, xncfDatabaseData) =>
         {

             //Legacy comment translated to English.
             var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
             //Legacy comment translated to English.
             if (xncfDatabaseData != null)
             {
                 var typedBuilder = builder as TBuilder;

                 //Legacy comment translated to English.
                 var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(xncfDatabaseData.XncfDatabaseRegister.GetType());

                 //Legacy comment translated to English.
                 var dbContextAssemblyName = xncfDatabaseData.AssemblyName ?? dbContextType.Assembly.FullName;
                 //Legacy comment translated to English.
                 var databaseMigrationHistoryTableName = NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(xncfDatabaseData.XncfDatabaseRegister);

                 typedBuilder.MigrationsAssembly(dbContextAssemblyName)
                        .MigrationsHistoryTable(databaseMigrationHistoryTableName);
             }
             else
             {
                 //Legacy comment translated to English.
             }
         };

        public abstract Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        public abstract Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="backupFilePath"></param>
        /// <returns></returns>
        public abstract string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath);
        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string GetDropTableSql(DbContext dbContext, string tableName);

        /// <summary>
        ///Legacy comment translated to English.
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="xncfDatabaseData"></param>
        /// <param name="connectionString"></param>
        ///Legacy comment translated to English.
        public void UseDatabase(DbContextOptionsBuilder builder,/*IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder,*/ string connectionString,
                XncfDatabaseData xncfDatabaseData = null, 
                Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            //Legacy comment translated to English.
            SetUseDatabase(builder, connectionString, xncfDatabaseData, b =>
                {
                    //executebasecode
                    DbContextOptionsActionBase(b, xncfDatabaseData);
                    //executeextensioncode
                    DbContextOptionsActionExtension?.Invoke(b, xncfDatabaseData);
                    //Legacy comment translated to English.
                    dbContextOptionsAction?.Invoke(b, xncfDatabaseData);
                }
            );
        }

    }
}