/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConfigurationHelpler.cs
    File description: ConfigurationHelpler related implementation

    Created by: Senparc - 20210614

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.Swagger.Models;
using System;

namespace Senparc.Xncf.Swagger.Utils
{
    /// <summary>
    /// Holds the configuration and hosting state consumed by Swagger registration helpers.
    /// </summary>
    public class ConfigurationHelper
    {
        /// <summary>
        /// Gets or sets the application configuration.
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the Swagger-specific configuration section.
        /// </summary>
        public static IConfiguration SwaggerConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the current Swagger UI and API options.
        /// </summary>
        public static CustsomSwaggerOptions CustsomSwaggerOptions { get; set; }

        /// <summary>
        /// Gets or sets the current hosting environment.
        /// </summary>
        public static IHostEnvironment HostEnvironment { get; set; }

        /// <summary>
        /// Gets or sets the current web-host environment.
        /// </summary>
        public static IWebHostEnvironment WebHostEnvironment { get; set; }

        /// <summary>
        /// Gets the application content-root path.
        /// </summary>
        public static string ContentRootPath
        {
            get
            {
                return HostEnvironment.ContentRootPath;
            }
        }

        /// <summary>
        /// Gets the web-root path, falling back to a conventional <c>wwwroot</c> directory.
        /// </summary>
        public static string WebRootPath
        {
            get
            {
                if (!string.IsNullOrEmpty(WebHostEnvironment.WebRootPath))
                    return WebHostEnvironment.WebRootPath;
                else
                    return System.IO.Path.Combine(ContentRootPath, "wwwroot");
            }
        }

        /// <summary>
        /// Gets the authentication-scheme name used by the Swagger cookie.
        /// </summary>
        public static readonly string SWAGGER_ATUH_COOKIE = nameof(SWAGGER_ATUH_COOKIE);

        /// <summary>
        /// Reads an untyped configuration value.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configured string value, or the provider default when absent.</returns>
        public static string GetValue(string key)
        {
            return Configuration.GetValue<string>(key);
        }

        /// <summary>
        /// Reads a typed value from the configured Swagger configuration.
        /// </summary>
        /// <typeparam name="T">The requested configuration value type.</typeparam>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configured value, or the provider's default when absent.</returns>
        public static T GetValue<T>(string key)
        {
            return Configuration.GetValue<T>(key);
        }
    }
}
