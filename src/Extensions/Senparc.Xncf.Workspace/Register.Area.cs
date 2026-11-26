/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Workspace admin page registration

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.2 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.Workspace;

public sealed partial class Register : IAreaRegister, IXncfRazorRuntimeCompilation
{
    /// <summary>
    /// Gets the Workspace administration home URL.
    /// </summary>
    public string HomeUrl => "/Admin/Workspace/Index";

    /// <summary>
    /// Gets the Workspace administration menu entries.
    /// </summary>
    public List<AreaPageMenuItem> AreaPageMenuItems =>
    [
        new AreaPageMenuItem(GetAreaHomeUrl(), WorkspaceResource.Get("Area.Menu.Admin", "Workspace and remote editing"), "fa fa-code")
    ];

    /// <summary>
    /// Registers the Workspace Razor Pages and returns the configured MVC builder.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <param name="env">The current host environment.</param>
    /// <returns>The configured MVC builder.</returns>
    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        return builder.AddRazorPagesOptions(_ => { });
    }

    /// <summary>
    /// Gets the absolute Workspace module path used for runtime compilation.
    /// </summary>
    public string LibraryPath => Path.GetFullPath(
        Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Workspace"));
}
