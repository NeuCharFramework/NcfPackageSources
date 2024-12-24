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

            //services.AddScoped<XncfBuilderSenparcEntities_SqlServer>();//注意：此处不能直接这样自动配置数据库实体，基类中已经统一配置 implementationFactory

            //AutoMap映射
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
            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
