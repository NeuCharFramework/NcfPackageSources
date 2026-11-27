/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: module registration and initialization logic

    Created by: Senparc - 20200818

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260717
    Change description: v0.14.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261002
    Change description: v0.16.3 Updated module behavior and related support.

    Modified by: Senparc - 20261003
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261009
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add PromptRange selector service, UI components, and tests

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.17.2 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.AgentKernel.Usage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Senparc.AI.AgentKernel;
using Senparc.CO2NET.RegisterServices;
using System.Reflection;
using Senparc.AI.Interfaces;
using Senparc.Xncf.AIKernel.AutoMapperProfiles;
using Senparc.Xncf.AIKernel.Domain.Models.ModelPreparation;
using Senparc.Xncf.AIKernel.Domain.Services;
using Microsoft.AspNetCore.Routing;

namespace Senparc.Xncf.AIKernel
{
    [XncfRegister]
    //[XncfOrder(5899)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<AIKernelResource>(
            "Module.AIKernel",
            "AI 核心模块",
            "基于 Senparc.AI，为所有 AI 项目提供基础能力");

        #region Legacy section

        public override string Name => "Senparc.Xncf.AIKernel";

        public override string Uid => "796D12D8-580B-40F3-A6E8-A5D9D2EABB69";//Legacy comment translated to English.

        public override string Version => "0.17.4.2";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa  fa-magic";

        public override string Description => RegisterText.Description;

        /// <inheritdoc />
    /// <inheritdoc/>
    public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //update the database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //execute logic according to the installation or update condition
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //new installation
                    #region Legacy section
                    //var colorService = serviceProvider.GetService<ColorAppService>();
                    //var colorResult = await colorService.GetOrInitColorAsync();

                    //Legacy comment translated to English.
                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //update
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
    /// <inheritdoc/>
    public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AIKernelSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AIKernelSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        /// <inheritdoc />
    /// <inheritdoc/>
    public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddSenparcAI(configuration);
            AgentKernelUsageRegistry.DefaultObserver ??= new Domain.Services.AgentKernelTokenUsageObserver();
            //services.AddScoped<ISenparcAiSetting, SenparcAiSetting>();
            //Console.WriteLine("======================services.AddScoped<ISenparcAiSetting, SenparcAiSetting>();================");
            services.AddScoped<AgentAiHandler>();

            //Legacy comment translated to English.
            services.AddSingleton<Senparc.Xncf.AIKernel.Domain.Services.AITokenMonitorService>();
            services.AddScoped<Senparc.Xncf.AIKernel.Domain.Services.AITokenUsageService>();
            services.AddSingleton<Senparc.Xncf.AIKernel.Domain.Services.AITokenUsageRecorder>();
            services.AddSingleton<Senparc.Xncf.AIKernel.Domain.Services.IAITokenUsageRecorder>(
                serviceProvider => serviceProvider.GetRequiredService<Senparc.Xncf.AIKernel.Domain.Services.AITokenUsageRecorder>());
            services.AddSingleton<IHostedService>(
                serviceProvider => serviceProvider.GetRequiredService<Senparc.Xncf.AIKernel.Domain.Services.AITokenUsageRecorder>());

            services.Configure<Domain.Services.FineTuningWorkerOptions>(
                configuration.GetSection(Domain.Services.FineTuningWorkerOptions.SectionName));
            services.AddSingleton<Domain.Services.FineTuningSecretStore>();
            services.AddSingleton<Domain.Services.FineTuningRuntimeSettings>();
            services.AddSingleton<Domain.Services.FineTuningWorkerSupervisor>();
            services.AddSingleton<IHostedService>(serviceProvider =>
                serviceProvider.GetRequiredService<Domain.Services.FineTuningWorkerSupervisor>());
            services.Configure<ModelPreparationOptions>(
                configuration.GetSection(ModelPreparationOptions.SectionName));
            services.AddScoped<Domain.Services.AIFineTuningWorkerService>();
            services.AddScoped<Domain.Services.FineTuningMigrationService>();
            services.AddScoped<Domain.Services.IFineTuningFileStore,
                Domain.Services.FineTuningFileStore>();
            services.AddScoped<Senparc.Xncf.FileManager.Abstractions.IFileManagerReferenceProvider,
                Domain.Services.FineTuningFileReferenceProvider>();
            services.AddSingleton<IHostedService, Domain.Services.FineTuningControlPlaneSyncService>();
            services.AddScoped<Domain.Services.IFineTuningControlPlaneService,
                Domain.Services.FineTuningControlPlaneService>();
            services.AddScoped<Domain.Services.IFineTuningWorkerConfigurationProvider,
                Domain.Services.FineTuningWorkerConfigurationProvider>();
            services.AddHttpContextAccessor();
            services.AddHttpClient<Domain.Services.FineTuningWorkerClient>(client =>
            {
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            }).ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });
            services.AddHttpClient<IModelPreparationService, ModelPreparationService>(client =>
            {
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Senparc.Xncf.AIKernel/ModelPreparation");
            }).ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });

            services.AddAutoMapper(config =>
            {
                config.AddProfile<AIKernelAutoMapperProfile>();
            });

            return base.AddXncfModule(services, configuration, env);
        }

        /// <inheritdoc />
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            registerService.UseSenparcAI();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });
            if (app is IEndpointRouteBuilder endpoints)
            {
                var group = endpoints.MapGroup("/api/aikernel/model-preparation")
                    .RequireAuthorization(NcfAuthorizationPolicyNames.AdminOnly);
                group.MapPost("/search", async (ModelSearchRequest request,
                    IModelPreparationService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.SearchAsync(request, cancellationToken)));
                group.MapPost("/status", async (ModelPreparationRequest request,
                    IModelPreparationService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetStatusAsync(request, cancellationToken)));
                group.MapPost("/download", async (ModelPreparationRequest request,
                    IModelPreparationService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.DownloadAsync(request, cancellationToken)));
            }
            return base.UseXncfModule(app, registerService);
        }
    }
}
