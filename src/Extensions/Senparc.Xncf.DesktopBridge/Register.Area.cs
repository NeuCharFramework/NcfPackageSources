/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Related implementation.

    Created by: Senparc - 20260801

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.DesktopBridge;

public sealed partial class Register : IAreaRegister, IXncfRazorRuntimeCompilation
{
    public string HomeUrl => "/Admin/DesktopBridge/Index";

    public List<AreaPageMenuItem> AreaPageMenuItems =>
    [
        new AreaPageMenuItem(GetAreaHomeUrl(), "设备配对与会话", "fa fa-desktop")
    ];

    /// <summary>
    /// Registers the DesktopBridge Razor Pages area with the supplied MVC builder.
    /// </summary>
    /// <param name="builder">The MVC builder being configured.</param>
    /// <param name="env">The host environment reserved for area-specific configuration.</param>
    /// <returns>The same MVC builder after DesktopBridge area configuration.</returns>
    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        return builder.AddRazorPagesOptions(_ => { });
    }

    public string LibraryPath => Path.GetFullPath(
        Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.DesktopBridge"));
}

