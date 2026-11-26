/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: module registration and initialization logic

    Created by: Senparc - 20200818

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260717
    Change description: v0.17.0-preview5 integrate shared resource localization and improve feature text for the PromptRange module

    Modified by: Senparc - 20261021
    Change description: v0.20.0 register Skill catalog, execution, and testing services

    Modified by: Senparc - 20261103
    Change description: v0.20.8 implement AdminChat memory storage and management system

    Modified by: Senparc - 20261105
    Change description: v0.20.14 register Prompt reference and memory prompt services

    Modified by: Senparc - 20261111
    Change description: v0.20.14 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261124
    Change description: v0.20.17 Improved PromptRange localization consistency and prompt tooling behavior.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.AI.AgentKernel;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Xncf.PromptRange
{
    [XncfRegister]
    [XncfOrder(5897)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<PromptRangeResource>(
            "Module.PromptRange",
            "提示词靶场",
            "用于训练和评估提示词（Prompt）");

        #region Legacy section

        public override string Name => "Senparc.Xncf.PromptRange";

        public override string Uid => "C6175B8E-9F79-4053-9523-F8E4AC0C3E18"; //Legacy comment translated to English.

        public override string Version => "0.15.4"; //the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-dot-circle-o";

        public override string Description => RegisterText.Description;

        /// <inheritdoc />
        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider,
            InstallOrUpdate installOrUpdate)
        {
            //update the database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //execute logic according to the installation or update condition
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //new installation

                    #region Legacy section

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
        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            PromptRangeSenparcEntities mySenparcEntities =
                serviceProvider.GetService(mySenparcEntitiesType) as PromptRangeSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion

            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion


        /// <inheritdoc />
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });
            return base.UseXncfModule(app, registerService);
        }

        /// <inheritdoc />
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment env)
        {
            //services.AddScoped<Color>();
            //services.AddScoped<ColorDto>();
            //services.AddScoped<ColorService>();
            services.AddScoped<PromptService>();
            services.AddScoped<PromptRangeService>();
            services.AddScoped<PromptItemService>();
            services.AddScoped<Senparc.Xncf.PromptRange.Abstractions.References.IPromptReferenceService, PromptReferenceService>();
            services.AddScoped<Senparc.Xncf.PromptRange.Abstractions.Memory.IMemoryPromptProvider, MemoryPromptProvider>();
            services.AddScoped<Senparc.Xncf.PromptRange.Abstractions.SystemMessages.IAdminChatSystemMessageProvider, AdminChatSystemMessagePromptProvider>();
            services.AddScoped<Senparc.Xncf.PromptRange.Abstractions.Shotting.IPromptShottingService, PromptShottingService>();
            services.AddScoped<PromptResultService>();
            services.AddScoped<PromptResultChatService>();
            services.AddScoped<LlModelService>();
            services.AddScoped<PromptRangeSkillService>();
            services.AddScoped<ISkillTestRunner, PromptRangeSkillTestService>();
            services.AddScoped<ISkillCatalog>(serviceProvider => serviceProvider.GetRequiredService<PromptRangeSkillService>());
            services.AddScoped<ISkillExecutor>(serviceProvider => serviceProvider.GetRequiredService<PromptRangeSkillService>());
            services.AddScoped<ISkillPackageService>(serviceProvider => serviceProvider.GetRequiredService<PromptRangeSkillService>());
            services.AddSingleton<PromptResultStreamHub>();

            services.AddAutoMapper(z =>
            {
                z.CreateMap<Domain.Models.DatabaseModel.PromptRange, PromptRangeDto>().ReverseMap();
                z.CreateMap<PromptItem, PromptItemDto>().ReverseMap();
                z.CreateMap<PromptResult, PromptResultDto>().ReverseMap();
                z.CreateMap<PromptResultChat, PromptResultChatDto>().ReverseMap();
                // z.CreateMap<LlModel, LlModelDto>().ReverseMap();
                //
                // z.CreateMap<LlModel, LlmModel_GetPageItemResponse>();

                //TODO:morek
            });

            return base.AddXncfModule(services, configuration, env);
        }
    }
}




