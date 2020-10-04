using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.DatabaseToolkit.Functions;
using System;

namespace Senparc.Xncf.DatabaseToolkit
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "DatabaseToolkit";
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type XncfDatabaseDbContextType => typeof(DatabaseToolkitEntities);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //SenparcTrace.SendCustomLog("执行调试", "DatabaseToolkit.AddXncfDatabaseModule");
            services.AddScoped<DbConfig>();
            services.AddScoped<SetConfig>();
            services.AddScoped<SetConfig.SetConfig_Parameters>();

            //AutoMap映射
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<SetConfig.SetConfig_Parameters, SetConfig>();
                profile.CreateMap<SetConfig.SetConfig_Parameters, DbConfig>();
            });
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
