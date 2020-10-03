using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace Senparc.Ncf.Database
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
        ///// <summary>
        ///// 对 DbContextOptionsBuilder 的配置操作
        ///// <para>参数1：TBuilder</para>
        ///// </summary>
        //new Action<TBuilder> DbContextOptionsAction { get; }
    }

    /// <summary>
    /// 数据库配置接口
    /// </summary>
    public interface IDatabaseConfiguration
    {
        /// <summary>
        /// 注：实现类中，此方法应该返回值应该和基类的 DbContextOptionsAction 属性一致（如果有）
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure> DbContextOptionsAction { get; }

        ///// <summary>
        ///// 对 DbContextOptionsBuilder 的配置操作
        ///// </summary>
        //Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction { get; }

        /// <summary>
        /// 使用数据库，如：
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsAction"></param>
        /// <param name="xncfDatabaseData">IXncfDatabase 信息（仅在针对 XNCF 进行数据库迁移时有效）</param>
        void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null, XncfDatabaseData xncfDatabaseData = null);

    }
}