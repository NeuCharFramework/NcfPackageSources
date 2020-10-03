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
        public virtual Action<TBuilder> TypedDbContextOptionsAction => DbContextOptionsAction;

        public virtual Action<IRelationalDbContextOptionsBuilderInfrastructure> DbContextOptionsAction => b =>
          {
              //获取当前数据库工厂
              var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
              //获取当前指定的 IXncfDatabase 对应信息（只在针对某个特定的 XNCF 数据库模块进行 add-migration 等情况下有效）
              var currentXncfDatabaseData = databaseConfigurationFactory.CurrentXncfDatabaseData;
              if (currentXncfDatabaseData!=null)
              {
                  var typedBuilder = b as TBuilder;

                  //DbContext的程序集名称（或强制指定生成 add-migration 的程序集名称
                  var dbContextAssemblyName = currentXncfDatabaseData.AssemblyName ?? currentXncfDatabaseData.XncfDatabaseRegister.XncfDatabaseDbContextType.Assembly.FullName;
                  //Migration History 的表名
                  var databaseMigrationHistoryTableName = NcfDatabaseHelper.GetDatabaseMigrationHistoryTableName(currentXncfDatabaseData.XncfDatabaseRegister);

                  typedBuilder.MigrationsAssembly(dbContextAssemblyName)
                         .MigrationsHistoryTable(databaseMigrationHistoryTableName);
              }
              else
              {
                  // 程序执行时 DatabaseConfigurationFactory.CurrentXncfDatabaseData 允许为 null
              }
          };

        /// 使用数据库，如：
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsAction"></param>
        public abstract void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null, XncfDatabaseData xncfDatabaseData = null);
    }
}