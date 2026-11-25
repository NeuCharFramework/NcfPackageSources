/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 register the connector admin menu

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.Connectors;

public sealed partial class Register : IAreaRegister
{
    public string HomeUrl => "/Admin/Connectors/Index";

    public List<AreaPageMenuItem> AreaPageMenuItems =>
    [
        new AreaPageMenuItem(GetAreaHomeUrl(), ConnectorsResource.Get("Area.Connectors.Config", "Connector setup"), "fa fa-plug")
    ];

    /// <summary>
    /// Registers connector Razor Pages options for the application MVC builder.
    /// </summary>
    /// <param name="builder">The MVC builder to configure.</param>
    /// <param name="env">The host environment used by area registration.</param>
    /// <returns>The configured MVC builder.</returns>
    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        => builder.AddRazorPagesOptions(_ => { });

    public string LibraryPath => Path.GetFullPath(
        Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Connectors"));
}
