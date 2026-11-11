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
        new AreaPageMenuItem(GetAreaHomeUrl(), ConnectorsResource.Get("Area.Connectors.Config", "连接器配置"), "fa fa-plug")
    ];

    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        => builder.AddRazorPagesOptions(_ => { });

    public string LibraryPath => Path.GetFullPath(
        Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Connectors"));
}
