using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Database
{
    /// <summary>
    /// 数据库配置接口
    /// </summary>
    /// <typeparam name="TDbContextOptionsBuilder"></typeparam>
    /// <typeparam name="TOptionExtension"></typeparam>
    public interface IDatabaseConfiguration
    {
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