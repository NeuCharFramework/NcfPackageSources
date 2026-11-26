/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.8.2 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

    Modified by: Senparc - 20261125
    Change description: v0.8.4 Registered and localized KnowledgeBase chat-tool services.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
//using Microsoft.KernelMemory;
//using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Senparc.Xncf.KnowledgeBase.Models;
using Senparc.Xncf.KnowledgeBase.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.Domain.Services;

namespace Senparc.Xncf.KnowledgeBase
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<KnowledgeBaseResource>(
            "Module.KnowledgeBase",
            "AI 知识库",
            "创建、管理和检索 AI 知识库");

        #region Legacy section

        public override string Name => "Senparc.Xncf.KnowledgeBase";

        public override string Uid => "CEAFC442-EE03-42EA-AD54-E607AD0C03A9";//Legacy comment translated to English.

        public override string Version => "0.1.10";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-lightbulb-o";

        public override string Description => RegisterText.Description;

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
                    var colorService = serviceProvider.GetService<ColorAppService>();
                    var colorResult = await colorService.GetOrInitColorAsync();
                    #endregion
                    break;
                case InstallOrUpdate.Update:
                    //update
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    /// <inheritdoc/>
    public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            KnowledgeBaseSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as KnowledgeBaseSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

    /// <inheritdoc/>
    public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddScoped<ColorAppService>();
            services.AddScoped<Domain.Services.KnowledgeBaseService>();
            AddChatToolServices(services);
            services.AddScoped<Senparc.Xncf.FileManager.Domain.Services.INcfFileDeletionGuard, Domain.Services.KnowledgeBaseFileDeletionGuard>();
            services.AddAutoMapper(z =>
            {
                z.CreateMap<Color, ColorDto>().ReverseMap();
            });
            return base.AddXncfModule(services, configuration, env);
        }
    }
}










