/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: module registration and initialization logic

    Created by: Senparc - 20200816

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20260729
    Change description: v0.37.1-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.40.0-preview10 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.41.8 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.XncfBuilder.Abstractions;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Database;
using Senparc.Xncf.XncfBuilder.Domain.Models.Services;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.XncfBuilder.Domain.Services;
using Senparc.AI.AgentKernel;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using OllamaSharp.Models.Chat;
using ModelContextProtocol.Protocol;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Microsoft.AspNetCore.Routing;
using Senparc.Xncf.XncfBuilder.OHS.Local;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using Senparc.Xncf.XncfBuilder.Domain.Services.Development;

namespace Senparc.Xncf.XncfBuilder
{
    [XncfRegister]
    [XncfOrder(5896)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<XncfBuilderResource>(
            "Module.XncfBuilder",
            "XNCF 模块生成器",
            "快速生成 XNCF 模块基础代码和示例，并据此扩展应用");

        #region Legacy section

        public override string Name => "Senparc.Xncf.XncfBuilder";

        public override string Uid => "C2E1F87F-2DCE-4921-87CE-36923ED0D6EA";//Legacy comment translated to English.

        public override string Version => "0.10.3";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-plus";

        public override string Description => RegisterText.Description;

        // The builder's MCP tool attributes are intentionally disabled. Do not
        // publish an unauthenticated MCP route until an administrator-only
        // endpoint policy is applied to the re-enabled tools.
        public override bool EnableMcpServer => false;

        //public override IList<Type> Functions => new Type[] {
        //    typeof(BuildXncf),
        //    typeof(AddMigration),
        //};

        /// <inheritdoc/>
        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //updatedatabase
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        /// <inheritdoc/>
        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            XncfBuilderSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as XncfBuilderSenparcEntities;
            var xncfDbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());

            //specify the data entities to delete
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(xncfDbContextType).Keys.ToArray();
            //deletedatabasetable
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            await base.UninstallAsync(serviceProvider, unsinstallFunc).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            if (!services.Any(d => d.ServiceType == typeof(IXncfModulesInventoryRequestWaiter)))
            {
                services.AddSingleton<IXncfModulesInventoryRequestWaiter, XncfModulesInventoryRequestWaiter>();
            }

            //services.AddScoped<PromptRange.Domain.Services.PromptService>();
            //services.AddScoped<AI.Interfaces.IAiHandler>(s => new AgentAiHandler());

            services.AddScoped<ConfigService>();
            services.AddScoped<PromptBuilderService>();

            if (!services.Any(d => d.ServiceType == typeof(IXncfPreviewStateStore)))
            {
                services.AddSingleton<IXncfPreviewStateStore, XncfPreviewStateStore>();
            }

            // The workflow service owns per-job operation locks; its state store creates short
            // database scopes so no scoped repository can escape into this singleton.
            if (!services.Any(d => d.ServiceType == typeof(IXncfDevelopmentJobStateStore)))
            {
                services.AddSingleton<IXncfDevelopmentJobStateStore, XncfDevelopmentJobStateStore>();
            }
            if (!services.Any(d => d.ServiceType == typeof(IXncfDevelopmentJobService)))
            {
                services.AddSingleton<IXncfDevelopmentJobService, XncfDevelopmentJobService>();
            }

            if (!services.Any(d => d.ServiceType == typeof(XncfPreviewService)))
            {
                services.AddSingleton<XncfPreviewService>();
                services.AddSingleton<IXncfPreviewService>(serviceProvider =>
                    serviceProvider.GetRequiredService<XncfPreviewService>());
                services.AddSingleton<IHostedService>(serviceProvider =>
                    serviceProvider.GetRequiredService<XncfPreviewService>());
                services.AddSingleton<IHostedService, XncfPreviewPersistenceInitializerHostedService>();
            }

            //Legacy comment translated to English.
            services.AddScoped<AIModelService>();
            services.AddScoped<AIModelAppService>();

            //Console.WriteLine(BuildXncfAppService.BackendTemplate);
            //Console.WriteLine("//////" + SystemTime.Now);
            //Console.WriteLine(BuildXncfAppService.FrontendTemplate);

            return base.AddXncfModule(services, configuration, env);
        }

        #endregion

        /// <inheritdoc/>
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            return base.UseXncfModule(app, registerService);
        }
    }
}
