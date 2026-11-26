/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IxncfMcp.cs
    File description: IxncfMcp related implementation

    Created by: Senparc - 20250620

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase
{
    internal interface IxncfMcp
    {
        /// <summary>
        /// Registers MCP services for the module.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="env">The host environment.</param>
        /// <returns>The configured service collection.</returns>
        public IServiceCollection AddMcp(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {

            return services;
        }
    }
}
