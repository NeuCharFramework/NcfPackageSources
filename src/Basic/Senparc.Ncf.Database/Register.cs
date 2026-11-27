/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20201003

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.21.14.2 Improve source documentation, headers, and module metadata consistency.

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
    /// Provides ASP.NET Core application-builder extensions for selecting the
    /// <see cref="IDatabaseConfiguration"/> used by NCF database services.
    /// </summary>
    /// <remarks>
    /// The selected configuration is stored in
    /// <see cref="DatabaseConfigurationFactory.Instance"/>. This class does not
    /// register a database provider by itself; the selected configuration is
    /// consumed later when NCF creates or configures an EF Core context.
    /// </summary>
    public static class Register
    {
        #region UseNcfDatabase

        /// <summary>
        /// Indicates whether the application has selected an NCF database configuration
        /// through one of the <see cref="UseNcfDatabase(IApplicationBuilder, Type)"/>
        /// overloads.
        /// </summary>
        public static bool UseNcfDatabaseSetted = false;

        /// <summary>
        /// Selects an already-created database configuration for the current application.
        /// </summary>
        /// <param name="app">The application builder whose database configuration is being initialized.</param>
        /// <param name="databaseConfiguration">
        /// The provider-specific configuration instance. It must implement
        /// <see cref="IDatabaseConfiguration"/> and must be ready for use by the NCF
        /// database context factory.
        /// </param>
        /// <returns>The same <paramref name="app"/> instance so calls can be chained.</returns>
        /// <exception cref="NcfDatabaseException">
        /// Thrown when <paramref name="databaseConfiguration"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, IDatabaseConfiguration databaseConfiguration)
        {
            if (databaseConfiguration == null)
            {
                throw new NcfDatabaseException($"{nameof(databaseConfiguration)} 参数不能为 null", null);
            }

            DatabaseConfigurationFactory.Instance.Current = databaseConfiguration;
            return app;
        }

        /// <summary>
        /// Loads a database configuration type from an assembly and selects it for
        /// the current application.
        /// </summary>
        /// <param name="app">The application builder whose database configuration is being initialized.</param>
        /// <param name="assemblyName">The assembly name that contains the configuration type.</param>
        /// <param name="nameSpace">The namespace containing the configuration type.</param>
        /// <param name="className">The configuration class name without its namespace.</param>
        /// <returns>The same <paramref name="app"/> instance so calls can be chained.</returns>
        /// <remarks>
        /// The three type-identifying values are combined as
        /// <c>{nameSpace}.{className}</c>. The resulting object is then passed to
        /// <see cref="UseNcfDatabase(IApplicationBuilder, IDatabaseConfiguration)"/>.
        /// </remarks>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown when <paramref name="assemblyName"/> cannot be loaded.
        /// </exception>
        /// <exception cref="NcfDatabaseException">
        /// Thrown when the assembly does not expose the requested configuration
        /// object or when the loaded object is not a valid database configuration.
        /// </exception>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, string assemblyName, string nameSpace, string className)
        {
            string fullName = nameSpace + "." + className;
            var databaseConfiguration = Assembly.Load(assemblyName).CreateInstance(fullName) as IDatabaseConfiguration;
            return app.UseNcfDatabase(databaseConfiguration);
        }

        /// <summary>
        /// Creates and selects a database configuration represented by
        /// <paramref name="databaseConfigurationType"/>.
        /// </summary>
        /// <param name="app">The application builder whose database configuration is being initialized.</param>
        /// <param name="databaseConfigurationType">
        /// A concrete type implementing <see cref="IDatabaseConfiguration"/>.
        /// When the type is <see cref="BySettingDatabaseConfiguration"/>, the
        /// provider is resolved from
        /// <see cref="SiteConfig.SenparcCoreSetting"/> before the instance is created.
        /// </param>
        /// <returns>The same <paramref name="app"/> instance so calls can be chained.</returns>
        /// <exception cref="NcfDatabaseException">
        /// Thrown when the supplied type does not implement
        /// <see cref="IDatabaseConfiguration"/>, when the configured provider cannot
        /// be resolved, or when the application setting names an invalid provider.
        /// </exception>
        /// <exception cref="MissingMethodException">
        /// Thrown when the selected configuration type has no usable parameterless
        /// constructor.
        /// </exception>
        public static IApplicationBuilder UseNcfDatabase(this IApplicationBuilder app, Type databaseConfigurationType)
        {
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
        /// Creates and selects the specified database configuration type.
        /// </summary>
        /// <typeparam name="TDatabaseConfiguration">
        /// A parameterless-construction database configuration implementing
        /// <see cref="IDatabaseConfiguration"/>.
        /// </typeparam>
        /// <param name="app">The application builder whose database configuration is being initialized.</param>
        /// <returns>The same <paramref name="app"/> instance so calls can be chained.</returns>
        /// <exception cref="NcfDatabaseException">
        /// Propagated from <see cref="UseNcfDatabase(IApplicationBuilder, Type)"/>
        /// when the configuration cannot be selected.
        /// </exception>
        public static IApplicationBuilder UseNcfDatabase<TDatabaseConfiguration>(this IApplicationBuilder app)
        where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            app.UseNcfDatabase(typeof(TDatabaseConfiguration));
            return app;
        }

        #endregion
    }
}
