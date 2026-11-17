/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.cs

    File description: Register related implementation

    

    

    Created by: Senparc - 20201003

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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
    ///Legacy comment translated to English.
    /// </summary>
    public static class Register
    {
        ///// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="databaseConfigurationType"></param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, Type databaseConfigurationType)
        //{
        //Legacy comment translated to English.
        //    if (typeof(IDatabaseConfiguration).IsAssignableFrom(databaseConfigurationType))
        //    {
        //Legacy comment translated to English.
        //    }

        //    var databaseConfiguration = Activator.CreateInstance(databaseConfigurationType, true) as IDatabaseConfiguration;

        //    DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
        //    return services;
        //}

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="databaseConfiguration"></param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, IDatabaseConfiguration databaseConfiguration)
        //{
        //Legacy comment translated to English.
        //    if (databaseConfiguration == null)
        //    {
        //Legacy comment translated to English.
        //    }

        //    DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
        //    return services;
        //}

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <param name="services"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, string assemblyName, string nameSpace, string className)
        //{
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //    return services.AddDatabase(databaseConfiguration);
        //}


        #region UseNcfDatabase

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static bool UseNcfDatabaseSetted = false;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="app"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, IDatabaseConfiguration databaseConfiguration)
        {
            //Legacy comment translated to English.
            if (databaseConfiguration == null)
            {
                throw new NcfDatabaseException($"{nameof(databaseConfiguration)} 参数不能为 null", null);
            }

            DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
            return app;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="app"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, string assemblyName, string nameSpace, string className)
        {
            //Legacy comment translated to English.
            string fullName = nameSpace + "." + className;//Legacy comment translated to English.
            var databaseConfiguration = Assembly.Load(assemblyName).CreateInstance(fullName) as IDatabaseConfiguration;//Legacy comment translated to English.
            return app.UseNcfDatabase(databaseConfiguration);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="databaseConfigurationType"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, Type databaseConfigurationType)
        {
            //Legacy comment translated to English.
            if (!typeof(IDatabaseConfiguration).IsAssignableFrom(databaseConfigurationType))
            {
                throw new NcfDatabaseException($"类型{databaseConfigurationType.Name} 必须实现接口：IDatabaseConfiguration", databaseConfigurationType);
            }

            //checkwhetherfordefaultdatabaseconfiguration（use appsettings.json file）
            if (databaseConfigurationType == typeof(BySettingDatabaseConfiguration))
            {
                var dbType = SiteConfig.SenparcCoreSetting.DatabaseType;//Legacy comment translated to English.

                if (dbType == null)
                {
                    throw new NcfDatabaseException($"当程序指定了 {databaseConfigurationType.Name} 后，请在 appsettings.json 中的 {nameof(SiteConfig.SenparcCoreSetting.DatabaseType)} 指定数据库类型！", databaseConfigurationType);
                }

                var dbTypeStr = dbType.ToString();
                try
                {
                    var typeAssemblyName = $"Senparc.Ncf.Database.{dbTypeStr}";
                    var fullTypeName = $"{typeAssemblyName}.{dbType}DatabaseConfiguration, {typeAssemblyName}";
                    databaseConfigurationType = Type.GetType(fullTypeName);

                    if (databaseConfigurationType == null)
                    {
                        throw new NcfDatabaseException($"找不到 {dbTypeStr} 配置对应的数据库配置类：{fullTypeName}", null);
                    }
                    //app.UseNcfDatabase(newDbConfigrationType);
                }
                catch (Exception ex)
                {
                    throw new NcfDatabaseException($"appsettings.json 中的 {nameof(SiteConfig.SenparcCoreSetting.DatabaseType)} 指定数据库类型错误：{SiteConfig.SenparcCoreSetting.DatabaseType}。内部错误信息：{ex.Message}", databaseConfigurationType, inner: ex);
                }
            }
            //else if (databaseConfigurationType == typeof(DatabaseConfiguration))
            //{
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //}

            var databaseConfiguration = (IDatabaseConfiguration)Activator.CreateInstance(databaseConfigurationType);

            DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;

            UseNcfDatabaseSetted = true;

            return app;
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase<TDatabaseConfiguration>(this IApplicationBuilder app)
        where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            //Legacy comment translated to English.
            app.UseNcfDatabase(typeof(TDatabaseConfiguration));
            return app;
        }

        #endregion
    }
}
