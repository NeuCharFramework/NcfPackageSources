/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Database.cs
    文件功能描述：Register.Database 相关实现
    
    
    创建标识：Senparc - 20200921
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

----------------------------------------------------------------*/

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
            modelBuilder.Entity<XncfPreviewTask>()
                .HasIndex(task => task.SessionId)
                .IsUnique();
            modelBuilder.Entity<XncfPreviewTask>()
                .HasIndex(task => new { task.ModuleProjectName, task.StartedAtUtc });

            modelBuilder.Entity<XncfPreviewHost>()
                .HasIndex(host => host.SessionId)
                .IsUnique();
            modelBuilder.Entity<XncfPreviewHost>()
                .HasIndex(host => new { host.Status, host.UpdatedAtUtc });

            modelBuilder.Entity<XncfDevelopmentJob>()
                .HasIndex(job => job.JobId)
                .IsUnique();
            modelBuilder.Entity<XncfDevelopmentJob>()
                .HasIndex(job => new { job.Stage, job.UpdatedAtUtc });
            modelBuilder.Entity<XncfDevelopmentJob>()
                .HasIndex(job => new { job.ModuleProjectName, job.CreatedAtUtc });
            modelBuilder.Entity<XncfDevelopmentJob>()
                .HasIndex(job => new { job.OwnerAdminUserId, job.UpdatedAtUtc });

            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
