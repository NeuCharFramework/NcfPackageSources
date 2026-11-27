/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20250325

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261004
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.5.10 use compatible icon for MCP module

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.5.14.2 Expand MCP endpoint gateway, credentials, and tool invocation support.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.IO;

namespace Senparc.Xncf.MCP
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

        /// <summary>Gets the MCP administration home URL.</summary>
        public string HomeUrl => "/Admin/MCP/Index";

        /// <summary>Gets the MCP administration menu entries.</summary>
        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(), McpResource.Get("Area.Home", "首页"),"fa fa-plug")
                     };

        /// <summary>
        /// Configures MCP Razor Pages and records module startup diagnostics.
        /// </summary>
        /// <param name="builder">The MVC builder being configured.</param>
        /// <param name="env">The current host environment.</param>
        /// <returns>The configured MVC builder.</returns>
        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("MCP 启动", "完成 Area:Senparc.Xncf.MCP 注册");

            return builder;
        }

        #endregion

        #region Legacy section
        /// <summary>Gets the absolute MCP module path used for runtime compilation.</summary>
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.MCP"));
        #endregion
    }
}
