/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20260504

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.4.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.4.9 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.4.11 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.FirmwareUpdate.Domain.Services;
using Senparc.Xncf.FirmwareUpdate.Models;
using Senparc.Xncf.FirmwareUpdate.OHS.Local.AppService;

namespace Senparc.Xncf.FirmwareUpdate;

[XncfRegister]
public partial class Register : XncfRegisterBase, IXncfRegister
{
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<FirmwareUpdateResource>(
            "Module.FirmwareUpdate",
            "NCF 安装包镜像",
            "从 GitHub 分别同步 NCF Host 运行包与 NcfDesktop 桌面应用到 host/desktop 目录，并生成各自的下载清单。");

    public override string Name => "Senparc.Xncf.FirmwareUpdate";

    public override string Uid => "E3A61F92-8C54-4A01-B9D7-2F6E8C11A90B";

    public override string Version => "0.1.0";

    public override string MenuName => RegisterText.MenuName;

    public override string Icon => "fa fa-cloud-download";

    public override string Description => RegisterText.Description;

    public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
    {
        await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this).ConfigureAwait(false);

        var configService = serviceProvider.GetRequiredService<ServiceBase<FirmwareUpdateConfig>>();
        var existing = await configService.GetObjectAsync(_ => true).ConfigureAwait(false);
        if (existing == null)
        {
            var row = new FirmwareUpdateConfig
            {
                AutoMirrorEnabled = false,
                UpdateIntervalHours = 24,
                TenantId = 0,
                Flag = false,
                LastUpdateTime = SystemTime.Now.DateTime
            };
            await configService.SaveObjectAsync(row).ConfigureAwait(false);
        }
    }

    public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
    {
        var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
        var mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as FirmwareUpdateSenparcEntities;
        var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
        await base.DropTablesAsync(serviceProvider, mySenparcEntities!, dropTableKeys).ConfigureAwait(false);
        await unsinstallFunc().ConfigureAwait(false);
    }

    public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        services.AddHttpClient("Senparc.Xncf.FirmwareUpdate.GitHub", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Senparc.Xncf.FirmwareUpdate/0.1 (+https://github.com/NeuCharFramework)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        });
        services.AddScoped<NcfPackageMirrorService>();
        services.AddScoped<FirmwareUpdateAppService>();
        return base.AddXncfModule(services, configuration, env);
    }
}
