using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.XncfBuilder.Models.MultipleDatabase;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;

namespace Senparc.Xncf.XncfBuilder
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "XncfBuilder";

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            services.AddScoped<BuildXncf_BuildRequest>();

            //services.AddScoped<XncfBuilderSenparcEntities_SqlServer>();//Note: The database entity cannot be automatically configured directly here. The implementationFactory has been configured uniformly in the base class.

            //AutoMap mapping
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<Config, ConfigDto>();
                profile.CreateMap<ConfigDto, Config>();
                profile.CreateMap<BuildXncf_BuildRequest, Config>();
                profile.CreateMap<Config, BuildXncf_BuildRequest>();
            });
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //After implementing the [XncfAutoConfigurationMapping] feature, it can be executed automatically without adding it manually.
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
