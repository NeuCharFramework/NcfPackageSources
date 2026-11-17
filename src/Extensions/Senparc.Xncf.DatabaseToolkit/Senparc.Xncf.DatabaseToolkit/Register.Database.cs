/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
            //after implementing the [XncfAutoConfigurationMapping] attribute, this runs automatically and no manual registration is needed
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
