using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.XncfBuilder.Functions;
using System;

namespace Senparc.Xncf.XncfBuilder
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "XncfBuilder";
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type XncfDatabaseDbContextType => typeof(XncfBuilderEntities);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            services.AddScoped<Config>();
            services.AddScoped<BuildXncf.Parameters>();

            //AutoMap映射
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<Config, ConfigDto>();
                profile.CreateMap<ConfigDto, Config>();
                profile.CreateMap<BuildXncf.Parameters, Config>();
                profile.CreateMap<Config,BuildXncf.Parameters>();
            });
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }

        void IXncfDatabase.DbContextOptionsAction(IRelationalDbContextOptionsBuilderInfrastructure dbContextOptionsAction, string assemblyName)
        {
            
        }
    }
}
