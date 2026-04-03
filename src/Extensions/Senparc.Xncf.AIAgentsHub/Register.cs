using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.AIAgentsHub.Models;
using Senparc.Xncf.AIAgentsHub.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.AIAgentsHub.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.AIAgentsHub
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.AIAgentsHub";

        public override string Uid => "49EFBD38-B093-46B1-9297-31FF8E6E8B29";//It must be globally unique and must be fixed after generation. It has been automatically generated and can also be modified by yourself.

        public override string Version => "0.1.0";//Version number is required

        public override string MenuName => "AI Agent Hub";

        public override string Icon => "fa fa-robot";

        public override string Description => "Artificial Intelligence Agent Hub Module";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //Update database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //Execute logic based on different conditions for installation or update
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //New installation
            #region Initialize database data
                    var colorService= serviceProvider.GetService<ColorAppService>();
                    var colorResult = await colorService.GetOrInitColorAsync();
            #endregion
                    break;
                case InstallOrUpdate.Update:
                    //renew
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region Delete database (demo)

            var mySenparcEntitiesType= this.TryGetXncfDatabaseDbContextType;
            AIAgentsHubSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AIAgentsHubSenparcEntities;

            //Specify the data entity to be deleted

            //Note: As a demonstration, all tables created by this module are deleted when uninstalling the module. During actual operation, please operate with caution and sort the entities in the order of deletion!
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddScoped<ColorAppService>();

            services.AddAutoMapper(z =>
            {
                z.CreateMap<Color, ColorDto>().ReverseMap();
            });
            return base.AddXncfModule(services, configuration, env);
        }
    }
}
