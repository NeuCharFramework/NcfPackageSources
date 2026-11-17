/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ConfigurationHelpler.cs

    File description: ConfigurationHelpler related implementation

    

    

    Created by: Senparc - 20210614

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
    ///Legacy comment translated to English.
    /// </summary>
    public class ConfigurationHelper
    {
        /// <summary>
        /// globalconfiguration
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static IConfiguration SwaggerConfiguration { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static CustsomSwaggerOptions CustsomSwaggerOptions { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static IHostEnvironment HostEnvironment { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static IWebHostEnvironment WebHostEnvironment { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string ContentRootPath
        {
            get
            {
                return HostEnvironment.ContentRootPath;
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public static readonly string SWAGGER_ATUH_COOKIE = nameof(SWAGGER_ATUH_COOKIE);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string GetValue(string key)
        {
            return Configuration.GetValue<string>(key);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T">type</typeparam>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static T GetValue<T>(string key)
        {
            return Configuration.GetValue<T>(key);
        }
    }
}
