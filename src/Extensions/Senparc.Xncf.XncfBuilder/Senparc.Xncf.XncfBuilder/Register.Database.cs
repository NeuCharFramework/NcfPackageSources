/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20200921

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

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

            //Legacy comment translated to English.

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

            //after implementing the [XncfAutoConfigurationMapping] attribute, this runs automatically and no manual registration is needed
            //modelBuilder.ApplyConfiguration(new DbConfig_WeixinUserConfigurationMapping());
        }
    }
}
