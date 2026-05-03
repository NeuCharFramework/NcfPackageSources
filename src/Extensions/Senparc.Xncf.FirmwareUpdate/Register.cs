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
    public override string Name => "Senparc.Xncf.FirmwareUpdate";

    public override string Uid => "E3A61F92-8C54-4A01-B9D7-2F6E8C11A90B";

    public override string Version => "0.1.0";

    public override string MenuName => "NCF 安装包镜像";

    public override string Icon => "fa fa-cloud-download";

    public override string Description => "从 GitHub 同步 NCF 桌面端安装包到本机 ~/wwwroot/NcfPackages，保留最近 3 个版本，并生成 latest-release.json 供 ncf.pub 与桌面端备用下载使用。";

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
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Senparc.Xncf.FirmwareUpdate/0.1 (+https://github.com/NeuCharFramework/NCF)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        });
        services.AddScoped<NcfPackageMirrorService>();
        services.AddScoped<FirmwareUpdateAppService>();
        return base.AddXncfModule(services, configuration, env);
    }
}
