using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// 数据库注册类
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// 使用指定数据库
        /// </summary>
        /// <typeparam name="TDatabaseConfiguration"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection UseDatabase<TDatabaseConfiguration>(this IServiceCollection services)
                where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration = new TDatabaseConfiguration();
            return services;
        }
    }
}
