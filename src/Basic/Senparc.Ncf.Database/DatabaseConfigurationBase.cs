using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
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

                 //DbContext的程序集名称（或强制指定生成 add-migration 的程序集名称
                 var dbContextAssemblyName = xncfDatabaseData.AssemblyName ?? xncfDatabaseData.XncfDatabaseRegister.XncfDatabaseDbContextType.Assembly.FullName;
                 //Migration History 的表名
                 var databaseMigrationHistoryTableName = NcfDatabaseHelper.GetDatabaseMigrationHistoryTableName(xncfDatabaseData.XncfDatabaseRegister);

                 typedBuilder.MigrationsAssembly(dbContextAssemblyName)
                        .MigrationsHistoryTable(databaseMigrationHistoryTableName);
             }
             else
             {
                 // 程序执行时 DatabaseConfigurationFactory.CurrentXncfDatabaseData 允许为 null
             }

             //执行扩展代码
             DbContextOptionsActionExtension?.Invoke(builder, xncfDatabaseData);
         };

        public abstract Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        public abstract Action<IRelationalDbContextOptionsBuilderInfrastructure, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        /// 使用数据库，如：
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="xncfDatabaseData"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsAction">额外需要配置的内容</param>
        public void UseDatabase(IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder, string connectionString,
                XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            //执行基础代码和扩展代码
            DbContextOptionsActionBase(optionsBuilder, xncfDatabaseData);


            //执行 UseSQLite、UseSQLServer 等操作
            SetUseDatabase(optionsBuilder, connectionString, xncfDatabaseData, b => DbContextOptionsActionBase(b, xncfDatabaseData));
        }
    }
}