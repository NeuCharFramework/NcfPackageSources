using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Database
{
    /// <summary>
    /// 数据库配置接口
    /// <para>官方推荐的数据库提供程序：https://docs.microsoft.com/zh-cn/ef/core/providers/?tabs=dotnet-core-cli </para>
    /// </summary>
    public interface IDatabaseConfiguration
    {
        /// <summary>
        /// DbContextOptionsBuilder 的类型，如：SQL Server 使用 SqlServerDbContextOptionsBuilder
        /// </summary>
        Type DbContextOptionsBuilderType { get; }

        /// <summary>
        /// 对 DbContextOptionsBuilder 的配置操作
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure> DbContextOptionsAction { get; }

        /// <summary>
        /// 使用数据库，如：
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="dbContextOptionsAction"></param>
        void UseDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString, Action<IRelationalDbContextOptionsBuilderInfrastructure> dbContextOptionsAction = null);
    }


    ///// <summary>
    ///// 数据库配置接口
    ///// </summary>
    //public interface IDatabaseConfiguration<IRelationalDbContextOptionsBuilder> : IDatabaseConfiguration
    //{

    //}

    //public interface IDatabaseConfiguration {
    
    //}
}