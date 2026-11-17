/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: localized display metadata for XNCF registration

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 unify XNCF Register display metadata and localization resource keys

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
            "Connectors",
            "Configure external tools as selectable connectors. After an account is configured, Chat, Workflow, and Agent templates can use them according to permissions.");

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
