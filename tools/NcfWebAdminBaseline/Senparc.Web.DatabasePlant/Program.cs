/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Program.cs
    File description: Senparc.Web.DatabasePlant related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.0.22 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Web.DatabasePlant
{
    public class Program
    {
        /// <summary>
        /// Builds and runs the database-plant host using the supplied process arguments.
        /// </summary>
        /// <param name="args">The process arguments.</param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates the default host builder configured to use the database-plant startup class.
        /// </summary>
        /// <param name="args">The process arguments.</param>
        /// <returns>The configured host builder.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
