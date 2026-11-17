/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260815
    Change description: v0.2.0 add the NCF preview sandbox workload

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support sandbox session TTL and persistent retention

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20260918
    Change description: v0.3.3 synchronize the module version (add port mappings, Notebook creation, aliases, and quota improvements)

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261028
    Change description: register IHttpContextAccessor so the singleton orchestrator can capture request tenant context

    Modified by: Senparc - 20261103
    Change description: v0.4.0 restore request tenant context in the sandbox singleton orchestrator and iterate background reclamation by tenant

    Modified by: Senparc - 20261111
    Change description: v0.4.2 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.IsolatedRuntime;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Sandbox.Application.AppServices;
using Senparc.Xncf.Sandbox.Domain.Services;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;
using Senparc.Xncf.Sandbox.Models;
using Senparc.Xncf.Sandbox.OHS.Local.Middleware;
using Senparc.Xncf.Sandbox.Abstractions;
using System.Reflection;

namespace Senparc.Xncf.Sandbox;

[XncfRegister]
public partial class Register : XncfRegisterBase, IXncfRegister
{
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<SandboxResource>(
            "Module.Sandbox",
            "沙箱环境",
            "NCF 独立沙箱编排：Docker/Wasm 快速创建与销毁实验环境");

    public override string Name => "Senparc.Xncf.Sandbox";

    public override string Uid => "BDF12490-AA0B-41B4-ADB3-63155ED95A93";

    public override string Version => "0.4.0";

    public override string MenuName => RegisterText.MenuName;

    public override string Icon => "fa fa-cube";

    public override string Description => RegisterText.Description;

    public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
    {
        await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this).ConfigureAwait(false);
    }

    public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
    {
        var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
        var mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as SandboxSenparcEntities;
        var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
        await base.DropTablesAsync(serviceProvider, mySenparcEntities!, dropTableKeys).ConfigureAwait(false);
        await unsinstallFunc().ConfigureAwait(false);
    }

    public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        services.Configure<SandboxImageOptions>(configuration.GetSection(SandboxImageOptions.SectionName));
        services.Configure<SandboxDockerOptions>(configuration.GetSection(SandboxDockerOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddDockerIsolatedRuntime(configuration);
        services.Configure<SandboxNcfPreviewOptions>(configuration.GetSection(SandboxNcfPreviewOptions.SectionName));
        services.AddSingleton<ISandboxImageResolver, SandboxImageResolver>();
        services.AddSingleton(new SandboxQuotaPolicy());
        services.AddSingleton<DockerSandboxRuntime>();
        services.AddSingleton<WasmSandboxRuntime>();
        services.AddSingleton<ISandboxRuntime>(sp => sp.GetRequiredService<DockerSandboxRuntime>());
        services.AddSingleton<ISandboxRuntime>(sp => sp.GetRequiredService<WasmSandboxRuntime>());
        services.AddSingleton<SandboxOrchestrator>();
        services.AddSingleton<SandboxNcfPreviewWorkloadService>();
        services.AddSingleton<IXncfSandboxPreviewService>(sp => sp.GetRequiredService<SandboxNcfPreviewWorkloadService>());
        services.AddHostedService(sp => sp.GetRequiredService<SandboxOrchestrator>());

        services.AddScoped<SandboxSessionService>();
        services.AddScoped<SandboxAppService>();
        services.AddHttpClient(SandboxJupyterProxyMiddleware.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });
        services.AddHttpClient(SandboxNcfPreviewProxyMiddleware.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });
        return base.AddXncfModule(services, configuration, env);
    }

    public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
        });

        //Legacy comment translated to English.
        app.UseWebSockets();
        app.UseMiddleware<SandboxJupyterProxyMiddleware>();
        app.UseMiddleware<SandboxNcfPreviewProxyMiddleware>();

        return base.UseXncfModule(app, registerService);
    }
}
