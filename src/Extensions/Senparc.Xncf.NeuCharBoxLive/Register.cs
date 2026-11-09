/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.cs
    文件功能描述：NeuCharBox Live 模块注册：服务、数据库、/live/api 分支与安装卸载


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 模块版本 0.2：新增设置表，并注册 LiveSettingsService

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 移除 NeuChar 环境变量密钥：临时运行时为关闭（no_model），并注册 AiKernelChatClients

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 改用 ModuleLiveRuntime：模块已安装且开放、未启用多租户、设置已启用且选了模型时才开启 Live

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 UseXncfModule 接入模块自己的 /live/api 分支（LiveApiPipeline），宿主 Program.cs 无需改动

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 MenuName 与 Description 改走资源文件（NeuCharBoxLiveResource），英文与简体中文

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 AddXncfModule 先调用 base（DbContext 注册不会被跳过）；运行时只在 NCF 就绪后读数据库；保留清理读模块运行时；新增 LiveApiHookCheck

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Models;

namespace Senparc.Xncf.NeuCharBoxLive
{
    /// <summary>
    /// The NeuCharBox live server as an XNCF module: the live API (Senparc.Xncf.NeuCharBoxLive.Api), its stores in the NCF database and the
    /// admin pages (Applications, Codes, Runs).
    /// </summary>
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.NeuCharBoxLive";

        /// <summary>Fixed for good: NCF keys the installed module by it.</summary>
        public override string Uid => "8D3E6A52-4B1F-4C7E-9A3D-2F6B1C0E7A91";

        public override string Version => "0.2";

        public override string MenuName => NeuCharBoxLiveResource.Get("Module.NeuCharBoxLive.MenuName", "NeuCharBox Live");

        public override string Icon => "fa fa-bolt";

        public override string Description => NeuCharBoxLiveResource.Get("Module.NeuCharBoxLive.Description", "NeuCharBox live AI demo: invite-only live sessions");

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            // Install and update both apply this module's pending migrations (Domain/Migrations/<database>).
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        /// <summary>
        /// Uninstalling keeps the tables: the run log is the handover cost record, and codes Leo sent out should
        /// work again after a reinstall. Drop them by hand (DatabaseToolkit or SQL) if they really must go.
        /// </summary>
        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion

        /// <summary>
        /// NCF calls this for every scanned module, installed or not, so the live API is registered from here (it
        /// replaced Program.cs's temporary AddNcbLive call). The live API answers whatever the module's state, but live is on
        /// only while <see cref="ModuleLiveRuntime"/> finds this module installed and open, multi-tenancy off, and the
        /// settings enabled with a usable model: until then <c>GET health</c> answers 200 with <c>liveEnabled:false</c> and the
        /// other routes 503 <c>live_off</c> (not 500). No restart is needed: uninstalling or closing the module turns live off
        /// within <see cref="ModuleLiveRuntime.Period"/>, and a settings save takes effect right away.
        /// <para>
        /// The DbContexts first: NCF swallows an exception from this method, and a module whose contexts are missing sends the
        /// host's database check, and so the whole site, into maintenance mode. Nothing after that call can undo it, and
        /// AddNcbLive never throws for a bad NcbLive value (live turns off with <c>config</c> instead).
        /// </para>
        /// </summary>
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            // NCF's per-database contexts, then AddXncfDatabaseModule (the base type the stores ask for).
            base.AddXncfModule(services, configuration, env);

            services.AddNcbLive(configuration, b => b
                .UseStores<EfLiveStore>()
                .UseRuntime(sp => new ModuleLiveRuntime(sp.GetRequiredService<LiveSettingsService>(),
                    sp.GetRequiredService<AiKernelChatClients>(), sp.GetRequiredService<IModuleState>(),
                    sp.GetRequiredService<IAiModelSource>(), env, () => SiteConfig.SenparcCoreSetting.EnableMultiTenant,
                    () => SiteConfig.NcfCoreState.AllThreadXncfLoaded,
                    sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILogger<ModuleLiveRuntime>>())));
            // The concrete runtime is resolved only by the module's own wiring: its refresh loop here, RetentionService (it
            // needs the real state, not the config wrapper's) and the Settings page after a save. Visitors' routes read ILiveRuntime.
            services.AddHostedService(sp => sp.GetRequiredService<ModuleLiveRuntime>());
            services.AddHostedService(sp => new RetentionService(sp.GetRequiredService<IRunStore>(), sp.GetRequiredService<ModuleLiveRuntime>(),
                sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILogger<RetentionService>>()));
            // One error once the host has started if UseXncfModule never ran (spec 2).
            services.AddHostedService(sp => new LiveApiHookCheck(sp.GetService<IHostApplicationLifetime>(),
                sp.GetRequiredService<ILogger<LiveApiHookCheck>>()));
            // Not TryAddSingleton: Senparc.Ncf.Utility defines a type that clashes with its namespace here (CS0434).
            if (!services.Any(d => d.ServiceType == typeof(LiveSettingsService))) services.AddSingleton<LiveSettingsService>();
            // One client per AI Kernel model configuration, kept for the host's lifetime (the runtime resolves through it).
            if (!services.Any(d => d.ServiceType == typeof(AiKernelChatClients)))
                services.AddSingleton(sp => new AiKernelChatClients(logger: sp.GetService<ILogger<AiKernelChatClients>>()));
            // What the runtime reads from NCF: this module's row in the module table, and AI Kernel's models.
            if (!services.Any(d => d.ServiceType == typeof(IModuleState)))
                services.AddSingleton<IModuleState>(sp => new XncfModuleState(sp.GetRequiredService<IServiceScopeFactory>(), Uid));
            if (!services.Any(d => d.ServiceType == typeof(IAiModelSource)))
                services.AddSingleton<IAiModelSource>(sp => new AiKernelModelSource(sp.GetRequiredService<IServiceScopeFactory>()));

            return services;
        }

        /// <summary>
        /// NCF calls this for every scanned module, installed or not, before the host's UseRouting: the module adds its own
        /// /live/api branch here (<see cref="LiveApiPipeline"/>: CORS on the snapshot's origins, the contract's JSON 404 and
        /// 405), so the host's Program.cs needs no edit. Nothing outside /live/api goes through it.
        /// </summary>
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            LiveApiPipeline.Use(app);
            return base.UseXncfModule(app, registerService);
        }
    }
}
