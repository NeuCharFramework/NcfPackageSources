using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.Swagger.Models;
using System;

namespace Senparc.Xncf.Swagger.Utils
{
    /// <summary>
    ///Configuration helper class
    /// </summary>
    public class ConfigurationHelper
    {
        /// <summary>
        ///global configuration
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        ///Configuration of Swagger module
        /// </summary>
        public static IConfiguration SwaggerConfiguration { get; set; }

        /// <summary>
        /// Custom configuration information for Swagger module
        /// </summary>
        public static CustsomSwaggerOptions CustsomSwaggerOptions { get; set; }

        /// <summary>
        /// Hosting environment information
        /// </summary>
        public static IHostEnvironment HostEnvironment { get; set; }

        /// <summary>
        ///Web hosting environment information
        /// </summary>
        public static IWebHostEnvironment WebHostEnvironment { get; set; }

        /// <summary>
        ///Internal access (project root path)
        /// </summary>
        public static string ContentRootPath
        {
            get
            {
                return HostEnvironment.ContentRootPath;
            }
        }

        /// <summary>
        ///web external access (wwwroot)
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
        ///Cookie authentication name
        /// </summary>
        public static readonly string SWAGGER_ATUH_COOKIE = nameof(SWAGGER_ATUH_COOKIE);

        /// <summary>
        /// Get the value of AppsettingsJson
        /// </summary>
        /// <param name="key">Key path, such as: ConnectionStrings:SQLServerConn</param>
        /// <returns></returns>
        public static string GetValue(string key)
        {
            return Configuration.GetValue<string>(key);
        }

        /// <summary>
        /// Get the value of AppsettingsJson
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="key">Key path</param>
        /// <returns></returns>
        public static T GetValue<T>(string key)
        {
            return Configuration.GetValue<T>(key);
        }
    }
}
