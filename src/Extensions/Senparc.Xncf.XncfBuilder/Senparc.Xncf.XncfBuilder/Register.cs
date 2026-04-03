using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Database;
using Senparc.Xncf.XncfBuilder.Domain.Models.Services;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.XncfBuilder.Domain.Services;
using Senparc.AI.Kernel;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using OllamaSharp.Models.Chat;
using ModelContextProtocol.Protocol;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Microsoft.AspNetCore.Routing;
using Senparc.Xncf.XncfBuilder.OHS.Local;

namespace Senparc.Xncf.XncfBuilder
{
    [XncfRegister]
    [XncfOrder(5896)]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.XncfBuilder";

        public override string Uid => "C2E1F87F-2DCE-4921-87CE-36923ED0D6EA";//Must ensure global uniqueness and must be fixed after generation

        public override string Version => "0.10.1";//Version number is required

        public override string MenuName => "XNCF 模块生成器";

        public override string Icon => "fa fa-plus";

        public override string Description => "快速生成 XNCF 模块基础程序代码，或 Sample 演示，可基于基础代码扩展自己的应用";

        public override bool EnableMcpServer => true;

        //public override IList<Type> Functions => new Type[] {
        //    typeof(BuildXncf),
        //    typeof(AddMigration),
        //};

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //Update database
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            XncfBuilderSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as XncfBuilderSenparcEntities;
            var xncfDbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());

            //Specify the data entity to be deleted
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(xncfDbContextType).Keys.ToArray();
            //Delete database table
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            await base.UninstallAsync(serviceProvider, unsinstallFunc).ConfigureAwait(false);
        }

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //services.AddScoped<PromptRange.Domain.Services.PromptService>();
            //services.AddScoped<AI.Interfaces.IAiHandler>(s => new SemanticAiHandler());

            services.AddScoped<ConfigService>();
            services.AddScoped<PromptBuilderService>();

            // Senparc.Xncf.AIKernel module
            services.AddScoped<AIModelService>();
            services.AddScoped<AIModelAppService>();

            //Console.WriteLine(BuildXncfAppService.BackendTemplate);
            //Console.WriteLine("//////" + SystemTime.Now);
            //Console.WriteLine(BuildXncfAppService.FrontendTemplate);

            return base.AddXncfModule(services, configuration, env);
        }

        #endregion

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            return base.UseXncfModule(app, registerService);
        }
    }
}
