/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：XNCF 注册本地化显示元数据
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 统一 XNCF Register 显示元数据与多语言资源键

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.Connectors.Domain.Models.DatabaseModel;
using Senparc.Xncf.Connectors.Domain.Services;
using Senparc.Xncf.Connectors.OHS.Local;

namespace Senparc.Xncf.Connectors;

[XncfRegister]
[XncfOrder(5868)]
public sealed partial class Register : XncfRegisterBase, IXncfRegister
{
    private static readonly XncfRegisterText RegisterText =
        XncfRegisterText.Create<ConnectorsResource>(
            "Module.Connectors",
            "连接器",
            "把外部工具配置成可选择的连接器。配置账号后，可在 Chat、Workflow 和 Agent 模板中按权限使用。");

    public override string Name => "Senparc.Xncf.Connectors";

    public override string Uid => "C4E8A1D2-7B63-4F05-9A18-2D6E0B5C7F41";

    public override string Version => "0.1.0";

    public override string MenuName => RegisterText.MenuName;

    public override string Icon => "fa fa-plug";

    public override string Description => RegisterText.Description;

    public override IServiceCollection AddXncfModule(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddScoped<ConnectorsSenparcEntities>(serviceProvider =>
            (ConnectorsSenparcEntities)serviceProvider.GetRequiredService(TryGetXncfDatabaseDbContextType));
        services.AddScoped<ConnectorSecretProtector>();
        services.AddScoped<ConnectorGateway>();
        services.AddScoped<IConnectorGateway>(serviceProvider => serviceProvider.GetRequiredService<ConnectorGateway>());
        services.AddScoped<ConnectorAppService>();
        return base.AddXncfModule(services, configuration, env);
    }

    public override Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        => XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
}
