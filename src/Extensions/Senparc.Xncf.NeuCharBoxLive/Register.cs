/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.cs
    文件功能描述：NeuCharBox Live XNCF 模块注册（由 XNCF 模板生成后精简）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 模块版本 0.2：新增设置表，并注册 LiveSettingsService

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 移除 NeuChar 环境变量密钥：临时运行时为关闭（no_model），并注册 AiKernelChatClients

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
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

        public override string MenuName => "NeuCharBox Live";

        public override string Icon => "fa fa-bolt";

        public override string Description => "NeuCharBox live AI demo: invite-only live sessions";

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
        /// replaced Program.cs's temporary AddNcbLive call). Before the module is installed its tables don't exist, so the
        /// token meter fails closed: <c>GET health</c> answers 200 with <c>liveEnabled:false</c> and the other routes 503
        /// <c>live_off</c> (not 500). Install and enable the module in the NCF admin before opening the tunnel. Note that
        /// uninstalling or disabling it does NOT turn live off (the live API is registered for every scanned module). Until the
        /// runtime reads the module's settings (Task 5), live is off with no_model.
        /// </summary>
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddNcbLive(configuration, b => b
                .UseStores<EfLiveStore>()
                .UseRuntime(InterimRuntime()));
            services.AddHostedService<RetentionService>();
            // Not TryAddSingleton: Senparc.Ncf.Utility defines a type that clashes with its namespace here (CS0434).
            if (!services.Any(d => d.ServiceType == typeof(LiveSettingsService))) services.AddSingleton<LiveSettingsService>();
            // One client per AI Kernel model configuration, kept for the host's lifetime (the runtime resolves through it).
            if (!services.Any(d => d.ServiceType == typeof(AiKernelChatClients)))
                services.AddSingleton(sp => new AiKernelChatClients(logger: sp.GetService<ILogger<AiKernelChatClients>>()));

            // NCF registers this module's context under its per-database type (…_Sqlite, …_SqlServer); EfLiveStore asks
            // for the base type, which resolves to the one the host's DatabaseType selects.
            services.AddScoped<NeuCharBoxLiveSenparcEntities>(sp =>
                (NeuCharBoxLiveSenparcEntities)sp.GetRequiredService(MultipleDatabasePool.Instance.GetXncfDbContextType(GetType())));

            return base.AddXncfModule(services, configuration, env);
        }

        /// <summary>
        /// Interim (replaced in Task 5 by a runtime over the settings and AI Kernel models): off with no_model, on the
        /// built-in origins.
        /// </summary>
        static StaticLiveRuntime InterimRuntime() => new(LiveSnapshot.Off(LiveOff.NoModel, LiveCors.BuiltIns));
    }
}
