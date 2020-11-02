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
    /// 数据库配置基类
    /// <para>官方推荐的数据库提供程序：https://docs.microsoft.com/zh-cn/ef/core/providers/?tabs=dotnet-core-cli </para>
    /// </summary>
    public abstract class DatabaseConfigurationBase<TBuilder, TExtension> : IDatabaseConfiguration<TBuilder, TExtension>
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension : RelationalOptionsExtension, new()
    {
        public abstract MultipleDatabaseType MultipleDatabaseType { get; }

        /// <summary>
        /// 具有 <typeparamref name="TBuilder"/> 类型的 DbContextOptionsAction
        /// </summary>
        public virtual Action<TBuilder, XncfDatabaseData> TypedDbContextOptionsActionBase => DbContextOptionsActionBase;

        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase => (builder, xncfDatabaseData) =>
         {

             //获取当前数据库工厂
             var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
             //获取当前指定的 IXncfDatabase 对应信息（只在针对某个特定的 XNCF 数据库模块进行 add-migration 等情况下有效）
             if (xncfDatabaseData != null)
             {
                 var typedBuilder = builder as TBuilder;

                 //获取指定 IXncfDatabase 对应于当前数据库类型的 DbContext 的类型
                 var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(xncfDatabaseData.XncfDatabaseRegister.GetType());

                 //DbContext的程序集名称（或强制指定生成 add-migration 的程序集名称
                 var dbContextAssemblyName = xncfDatabaseData.AssemblyName ?? dbContextType.Assembly.FullName;
                 //Migration History 的表名
                 var databaseMigrationHistoryTableName = NcfDatabaseHelper.GetDatabaseMigrationHistoryTableName(xncfDatabaseData.XncfDatabaseRegister);

                 typedBuilder.MigrationsAssembly(dbContextAssemblyName)
                        .MigrationsHistoryTable(databaseMigrationHistoryTableName);
             }
             else
             {
                 // 程序执行时 DatabaseConfigurationFactory.CurrentXncfDatabaseData 允许为 null
             }
         };

        public abstract Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        public abstract Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        /// 备份数据库方法
        /// <para>如果返回null，则在方法内部完成备份程序</para>
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="backupFilePath"></param>
        /// <returns></returns>
        public abstract string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath);
        /// <summary>
        /// 删除指定表Sql
        /// <para>如果返回null，则在方法内部完成删除操作</para>
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract string GetDropTableSql(DbContext dbContext, string tableName);

        /// <summary>
        /// 使用数据库，如：
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="xncfDatabaseData"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsAction">额外需要配置的内容</param>
        public void UseDatabase(DbContextOptionsBuilder builder,/*IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder,*/ string connectionString,
                XncfDatabaseData xncfDatabaseData = null, 
                Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            //执行 UseSQLite、UseSQLServer 等操作
            SetUseDatabase(builder, connectionString, xncfDatabaseData, b =>
                {
                    //执行基础代码
                    DbContextOptionsActionBase(b, xncfDatabaseData);
                    //执行扩展代码
                    DbContextOptionsActionExtension?.Invoke(b, xncfDatabaseData);
                    //执行外部传入的其他方法
                    dbContextOptionsAction?.Invoke(b, xncfDatabaseData);
                }
            );
        }

    }
}