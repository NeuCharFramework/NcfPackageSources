using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// XSCF 模块数据库配置
    /// </summary>
    public interface IXscfDatabase
    {
        /// <summary>
        /// 全局唯一的前缀，务必避免和其他模块重复
        /// </summary>
        string DatabaseUniquePrefix { get; }
        /// <summary>
        /// 创建数据库模型
        /// </summary>
        void OnModelCreating(ModelBuilder modelBuilder);
        /// <summary>
        /// 设置数据库，主要提供给使用
        /// </summary>
        /// <param name="dbContextOptionsAction"></param>
        /// <param name="assemblyName">MigrationsAssembly 的程序集名称，如果为 null，为默认使用当前 XscfDatabaseDbContextType 所在的程序集</param>
        void DbContextOptionsAction(IRelationalDbContextOptionsBuilderInfrastructure dbContextOptionsAction,
                                    string assemblyName = null);

        /// <summary>
        /// XscfDatabaseDbContext 类型
        /// </summary>
        Type XscfDatabaseDbContextType { get; }

        /// <summary>
        /// 添加数据库模块
        /// </summary>
        /// <param name="services"></param>
        void AddXscfDatabaseModule(IServiceCollection services);
    }
}
