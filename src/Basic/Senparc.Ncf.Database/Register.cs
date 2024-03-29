using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using System;
using System.Reflection;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// 数据库注册类
    /// </summary>
    public static class Register
    {
        ///// <summary>
        ///// 使用指定数据库
        ///// </summary>
        ///// <typeparam name="TDatabaseConfiguration"></typeparam>
        ///// <param name="services"></param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase<TDatabaseConfiguration>(this IServiceCollection services)
        //        where TDatabaseConfiguration : IDatabaseConfiguration, new()
        //{
        //    DatabaseConfigurationFactory.Instance.Current = new TDatabaseConfiguration();
        //    return services;
        //}

        ///// <summary>
        ///// 使用指定数据库
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="databaseConfigurationType"></param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, Type databaseConfigurationType)
        //{
        //    //必须实现 IDatabaseConfiguration 接口的类型才能进行下一步配置
        //    if (typeof(IDatabaseConfiguration).IsAssignableFrom(databaseConfigurationType))
        //    {
        //        throw new NcfDatabaseException($"类型{databaseConfigurationType.Name} 必须实现接口：IDatabaseConfiguration", databaseConfigurationType);
        //    }

        //    var databaseConfiguration = Activator.CreateInstance(databaseConfigurationType, true) as IDatabaseConfiguration;

        //    DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
        //    return services;
        //}

        ///// <summary>
        ///// 使用指定数据库
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="databaseConfiguration"></param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, IDatabaseConfiguration databaseConfiguration)
        //{
        //    //必须实现 IDatabaseConfiguration 接口的类型才能进行下一步配置
        //    if (databaseConfiguration == null)
        //    {
        //        throw new NcfDatabaseException($"{nameof(databaseConfiguration)} 参数不能为 null", null);
        //    }

        //    DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
        //    return services;
        //}

        ///// <summary>
        ///// 使用指定数据库
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="assemblyName">DatabaseConfiguration 程序集名称</param>
        ///// <param name="nameSpace">DatabaseConfiguration 命名空间</param>
        ///// <param name="className">DatabaseConfiguration 类名</param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, string assemblyName, string nameSpace, string className)
        //{
        //    //TODO:集成到 CO2NET
        //    string fullName = nameSpace + "." + className;//命名空间.类型名
        //    var databaseConfiguration = Assembly.Load(assemblyName).CreateInstance(fullName) as IDatabaseConfiguration;//加载程序集，创建程序集里面的 命名空间.类型名 实例
        //    return services.AddDatabase(databaseConfiguration);
        //}

        #region UseNcfDatabase

        /// <summary>
        /// 使用指定数据库
        /// </summary>
        /// <param name="services"></param>
        /// <param name="databaseConfiguration"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, IDatabaseConfiguration databaseConfiguration)
        {
            //必须实现 IDatabaseConfiguration 接口的类型才能进行下一步配置
            if (databaseConfiguration == null)
            {
                throw new NcfDatabaseException($"{nameof(databaseConfiguration)} 参数不能为 null", null);
            }

            DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
            return app;
        }

        /// <summary>
        /// 使用指定数据库
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblyName">DatabaseConfiguration 程序集名称</param>
        /// <param name="nameSpace">DatabaseConfiguration 命名空间</param>
        /// <param name="className">DatabaseConfiguration 类名</param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, string assemblyName, string nameSpace, string className)
        {
            //TODO:集成到 CO2NET
            string fullName = nameSpace + "." + className;//命名空间.类型名
            var databaseConfiguration = Assembly.Load(assemblyName).CreateInstance(fullName) as IDatabaseConfiguration;//加载程序集，创建程序集里面的 命名空间.类型名 实例
            return app.UseNcfDatabase(databaseConfiguration);
        }

        /// <summary>
        /// 使用指定数据库
        /// </summary>
        /// <param name="services"></param>
        /// <param name="databaseConfigurationType"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, Type databaseConfigurationType)
        {
            //必须实现 IDatabaseConfiguration 接口的类型才能进行下一步配置
            if (!typeof(IDatabaseConfiguration).IsAssignableFrom(databaseConfigurationType))
            {
                throw new NcfDatabaseException($"类型{databaseConfigurationType.Name} 必须实现接口：IDatabaseConfiguration", databaseConfigurationType);
            }

            //判断是否为默认数据库配置（使用 appsettings.json 文件）
            if (databaseConfigurationType == typeof(BySettingDatabaseConfiguration))
            {
                Console.WriteLine(SiteConfig.SenparcCoreSetting.ToJson(true));

                var dbType = SiteConfig.SenparcCoreSetting.DatabaseType;//此时还没有设置完成

                if (dbType == null)
                {
                    throw new NcfDatabaseException($"当程序指定了 {databaseConfigurationType.Name} 后，请在 appsettings.json 中的 {nameof(SiteConfig.SenparcCoreSetting.DatabaseType)} 指定数据库类型！", databaseConfigurationType);
                }

                var dbTypeStr = dbType.ToString();
                try
                {
                    var typeAssemblyName =$"Senparc.Ncf.Database.{dbTypeStr}";
                    var fullTypeName = $"{typeAssemblyName}.{dbType}DatabaseConfiguration, {typeAssemblyName}";
                    databaseConfigurationType = Type.GetType(fullTypeName);
                    
                    if (databaseConfigurationType==null)
                    {
                        throw new NcfDatabaseException($"找不到 {dbTypeStr} 配置对应的数据库配置类：{fullTypeName}",null);
                    }
                    //app.UseNcfDatabase(newDbConfigrationType);
                }
                catch (Exception ex)
                {
                    throw new NcfDatabaseException($"appsettings.json 中的 {nameof(SiteConfig.SenparcCoreSetting.DatabaseType)} 指定数据库类型错误：{SiteConfig.SenparcCoreSetting.DatabaseType}", databaseConfigurationType, inner: ex);
                }
            }

            var databaseConfiguration = Activator.CreateInstance(databaseConfigurationType, true) as IDatabaseConfiguration;

            DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
            return app;
        }


        /// <summary>
        /// 使用指定数据库
        /// </summary>
        /// <param name="services"></param>
        /// <param name="databaseConfigurationType"></param>
        /// <returns></returns>
        //public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, Type databaseConfigurationType)
        public static IApplicationBuilder UseNcfDatabase<TDatabaseConfiguration>(this IApplicationBuilder app)
        where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            //添加数据库
            //var instance = new TDatabaseConfiguration();

            app.UseNcfDatabase(typeof(TDatabaseConfiguration));

            //var databaseConfiguration = Activator.CreateInstance(databaseConfigurationType, true) as IDatabaseConfiguration;

            //DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
            return app;
        }

        #endregion
    }
}
