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
        /// 当前正在操作的 XNCF 数据库（仅在对特定的 IXncfDatabase 操作时有效）
        /// </summary>
        public virtual XncfDatabaseData CurrentXncfDatabaseData { get; set; }

        /// <summary>
        /// 获取 EF Code First MigrationHistory 数据库表名
        /// </summary>
        /// <returns></returns>
        public virtual string GetDatabaseMigrationHistoryTableName(XncfDatabaseData xncfDatabaseData)
        {
            if (xncfDatabaseData != null && !xncfDatabaseData.DatabaseUniquePrefix.IsNullOrWhiteSpace())
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

        Action<IRelationalDbContextOptionsBuilderInfrastructure> IDatabaseConfiguration.DbContextOptionsAction => b =>
        {
            //获取当前数据库配置
            var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration;
            //获取当前指定的 IXncfDatabase 对应信息（只在针对某个特定的 XNCF 数据库模块进行 add-migration 等情况下有效）
            var currentXncfDatabaseData = currentDatabaseConfiguration.CurrentXncfDatabaseData;
            //执行带 TBuilder 泛型的 DbContextOptionsAction 方法
            DbContextOptionsAction(b as TBuilder, currentDatabaseConfiguration.CurrentXncfDatabaseData);
        };


        //Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction =>
        //        (builder, xncfDatabaseData) => DbContextOptionsAction(builder as TBuilder, xncfDatabaseData);

        /// 使用数据库，如：
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsAction"></param>
        public abstract void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null, XncfDatabaseData xncfDatabaseData = null);
    }
}