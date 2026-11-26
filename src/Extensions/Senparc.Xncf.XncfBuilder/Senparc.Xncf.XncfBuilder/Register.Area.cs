/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Related implementation.

    Created by: Senparc - 20260802

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;
using System.IO;

namespace Senparc.Xncf.XncfBuilder
{
    public partial class Register : IAreaRegister, IXncfRazorRuntimeCompilation
    {
        public string HomeUrl => "/Admin/XncfBuilder/PreviewMonitor";

        public List<AreaPageMenuItem> AreaPageMenuItems => new()
        {
            new AreaPageMenuItem(
                GetAreaHomeUrl(),
                XncfBuilderResource.Get("XncfBuilder.Preview.Monitor", "XNCF 预览监控"),
                "fa fa-dashboard")
        };

        /// <summary>
        /// Configures Razor Pages for the XNCF Builder area. The current module
        /// does not add additional page conventions and returns the supplied builder
        /// with the area's page services registered.
        /// </summary>
        /// <param name="builder">The MVC builder being configured.</param>
        /// <param name="env">The host environment used by the area contract.</param>
        /// <returns>The MVC builder with XNCF Builder Razor Pages enabled.</returns>
        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            return builder.AddRazorPagesOptions(_ => { });
        }

        public string LibraryPath => Path.GetFullPath(
            Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.XncfBuilder"));
    }
}
