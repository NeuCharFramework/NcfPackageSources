using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Exceptions;
using System;

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
        public static IServiceCollection AddDatabase<TDatabaseConfiguration>(this IServiceCollection services)
                where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration = new TDatabaseConfiguration();
            return services;
        }

        /// <summary>
        /// 使用指定数据库
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDatabase(this IServiceCollection services, Type databaseConfigurationType)
        {
            

            if (databaseConfigurationType.GetInterface("IDatabaseConfiguration") == null)
            {
                throw new NcfDatabaseException($"类型{databaseConfigurationType.Name} 必须实现接口：IDatabaseConfiguration", databaseConfigurationType);
            }

            var databaseConfiguration = Activator.CreateInstance(databaseConfigurationType, true) as IDatabaseConfiguration;

            DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration = databaseConfiguration;
            return services;
        }
    }
}
