using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// XNCF 模块数据库配置
    /// </summary>
    public interface IXncfDatabase
    {
        /// <summary>
        /// 数据库表全局唯一的前缀，务必避免和其他模块重复
        /// </summary>
        string DatabaseUniquePrefix { get; }
        /// <summary>
        /// 创建数据库模型
        /// </summary>
        void OnModelCreating(ModelBuilder modelBuilder);

        ///// <summary>
        ///// 设置数据库，主要提供给使用
        ///// </summary>
        ///// <param name="dbContextOptionsAction"></param>
        ///// <param name="assemblyName">MigrationsAssembly 的程序集名称，如果为 null，为默认使用当前 XncfDatabaseDbContextType 所在的程序集</param>
        //void DbContextOptionsAction(IRelationalDbContextOptionsBuilderInfrastructure dbContextOptionsAction,
        //                            string assemblyName = null);

        /// <summary>
        /// XncfDatabaseDbContext 类型
        /// </summary>
        Type XncfDatabaseDbContextType { get; }

        /// <summary>
        /// 添加数据库模块
        /// </summary>
        /// <param name="services"></param>
        void AddXncfDatabaseModule(IServiceCollection services);
    }
}
