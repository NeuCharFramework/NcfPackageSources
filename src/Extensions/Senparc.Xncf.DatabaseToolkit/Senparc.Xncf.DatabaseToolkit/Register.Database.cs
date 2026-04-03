using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.DatabaseToolkit.Domain.Services;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.Services;
using System;
using static Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService.DatabaseConfigAppService;

namespace Senparc.Xncf.DatabaseToolkit
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "DatabaseToolkit";
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //AutoMap mapping
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<SetConfigFunctionAppRequest, DbConfig>();
            });

            //services.AddScoped<DatabaseBackupAppService>();
            //services.AddScoped<DbConfigQueryService>();

            services.AddSingleton<DatabaseSchemaMetadataProvider>();
            services.AddScoped<DatabaseExecutor>();
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //After implementing the [XncfAutoConfigurationMapping] feature, it can be executed automatically without adding it manually.
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
