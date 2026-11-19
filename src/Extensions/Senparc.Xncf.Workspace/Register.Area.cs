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
    public string HomeUrl => "/Admin/Workspace/Index";

    public List<AreaPageMenuItem> AreaPageMenuItems =>
    [
        new AreaPageMenuItem(GetAreaHomeUrl(), WorkspaceResource.Get("Area.Menu.Admin", "Workspace and remote editing"), "fa fa-code")
    ];

    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        return builder.AddRazorPagesOptions(_ => { });
    }

    public string LibraryPath => Path.GetFullPath(
        Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Workspace"));
}
