/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Database.cs
    文件功能描述：Register.Database 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
            //AutoMap映射
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
            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
