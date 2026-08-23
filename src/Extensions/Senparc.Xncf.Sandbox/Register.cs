/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.cs
    文件功能描述：Senparc.Xncf.Sandbox 模块注册

    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱工作负载

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 支持沙箱会话 TTL 与永久保持能力

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

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
    public override string Name => "Senparc.Xncf.Sandbox";

    public override string Uid => "BDF12490-AA0B-41B4-ADB3-63155ED95A93";

    public override string Version => "0.1.0-preview1";

    public override string MenuName => SandboxResource.Get("Module.Sandbox.MenuName", "沙箱环境");

    public override string Icon => "fa fa-cube";

    public override string Description => SandboxResource.Get(
        "Module.Sandbox.Description",
        "NCF 独立沙箱编排：Docker/Wasm 快速创建与销毁实验环境");

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

        // JupyterLab 交互需要 WebSocket；鉴权与 token 注入在中间件内完成。
        app.UseWebSockets();
        app.UseMiddleware<SandboxJupyterProxyMiddleware>();
        app.UseMiddleware<SandboxNcfPreviewProxyMiddleware>();

        return base.UseXncfModule(app, registerService);
    }
}
