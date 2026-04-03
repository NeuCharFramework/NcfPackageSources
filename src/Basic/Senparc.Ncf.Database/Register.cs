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
    /// Database registration class
    /// </summary>
    public static class Register
    {
        ///// <summary>
        ///// Use the specified database
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
        ///// Use the specified database
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="databaseConfigurationType"></param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, Type databaseConfigurationType)
        //{
        //    //The type that must implement the IDatabaseConfiguration interface can be used for the next step of configuration.
        //    if (typeof(IDatabaseConfiguration).IsAssignableFrom(databaseConfigurationType))
        //    {
        //        throw new NcfDatabaseException($"Type {databaseConfigurationType.Name} must implement the interface: IDatabaseConfiguration", databaseConfigurationType);
        //    }

        //    var databaseConfiguration = Activator.CreateInstance(databaseConfigurationType, true) as IDatabaseConfiguration;

        //    DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
        //    return services;
        //}

        ///// <summary>
        ///// Use the specified database
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="databaseConfiguration"></param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, IDatabaseConfiguration databaseConfiguration)
        //{
        //    //The type that must implement the IDatabaseConfiguration interface can be used for the next step of configuration.
        //    if (databaseConfiguration == null)
        //    {
        //        throw new NcfDatabaseException($"{nameof(databaseConfiguration)} parameter cannot be null", null);
        //    }

        //    DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
        //    return services;
        //}

        ///// <summary>
        ///// Use the specified database
        ///// </summary>
        ///// <param name="services"></param>
        ///// <param name="assemblyName">DatabaseConfiguration assembly name</param>
        ///// <param name="nameSpace">DatabaseConfiguration namespace</param>
        ///// <param name="className">DatabaseConfiguration class name</param>
        ///// <returns></returns>
        //public static IServiceCollection AddDatabase(this IServiceCollection services, string assemblyName, string nameSpace, string className)
        //{
        //    //TODO: Integrated into CO2NET
        //    string fullName = nameSpace + "." + className;//Namespace.Type name
        //    var databaseConfiguration = Assembly.Load(assemblyName).CreateInstance(fullName) as IDatabaseConfiguration;//Load the assembly and create the namespace.type name instance in the assembly
        //    return services.AddDatabase(databaseConfiguration);
        //}


        #region UseNcfDatabase

        /// <summary>
        /// Whether the UseNcfDatabase() method has been executed
        /// </summary>
        public static bool UseNcfDatabaseSetted = false;

        /// <summary>
        /// Use the specified database
        /// </summary>
        /// <param name="app"></param>
        /// <param name="databaseConfiguration">Available DatabaseConfiguration, not <see cref="BySettingDatabaseConfiguration"/></param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, IDatabaseConfiguration databaseConfiguration)
        {
            //The type that must implement the IDatabaseConfiguration interface can be used for the next step of configuration.
            if (databaseConfiguration == null)
            {
                throw new NcfDatabaseException($"{nameof(databaseConfiguration)} 参数不能为 null", null);
            }

            DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
            return app;
        }

        /// <summary>
        /// Use the specified database
        /// </summary>
        /// <param name="app"></param>
        /// <param name="assemblyName">DatabaseConfiguration assembly name</param>
        /// <param name="nameSpace">DatabaseConfiguration namespace</param>
        /// <param name="className">DatabaseConfiguration class name</param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, string assemblyName, string nameSpace, string className)
        {
            //TODO: Integrate into CO2NET
            string fullName = nameSpace + "." + className;//namespace.typename
            var databaseConfiguration = Assembly.Load(assemblyName).CreateInstance(fullName) as IDatabaseConfiguration;//Load the assembly and create the namespace.typename instance in the assembly
            return app.UseNcfDatabase(databaseConfiguration);
        }

        /// <summary>
        /// Use the specified database (must be executed after the UseXncfModule() method)
        /// </summary>
        /// <param name="app"></param>
        /// <param name="databaseConfigurationType"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, Type databaseConfigurationType)
        {
            //The type that must implement the IDatabaseConfiguration interface can be used for the next step of configuration.
            if (!typeof(IDatabaseConfiguration).IsAssignableFrom(databaseConfigurationType))
            {
                throw new NcfDatabaseException($"类型{databaseConfigurationType.Name} 必须实现接口：IDatabaseConfiguration", databaseConfigurationType);
            }

            //Determine whether it is the default database configuration (using the appsettings.json file)
            if (databaseConfigurationType == typeof(BySettingDatabaseConfiguration))
            {
                var dbType = SiteConfig.SenparcCoreSetting.DatabaseType;//The setting has not been completed yet

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
            //    //for unit testing
            //    Console.WriteLine("Enter the unit test environment");
            //}

            var databaseConfiguration = (IDatabaseConfiguration)Activator.CreateInstance(databaseConfigurationType);

            DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;

            UseNcfDatabaseSetted = true;

            return app;
        }


        /// <summary>
        /// Use the specified database
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseNcfDatabase<TDatabaseConfiguration>(this IApplicationBuilder app)
        where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            //Add database
            app.UseNcfDatabase(typeof(TDatabaseConfiguration));
            return app;
        }

        #endregion
    }
}
