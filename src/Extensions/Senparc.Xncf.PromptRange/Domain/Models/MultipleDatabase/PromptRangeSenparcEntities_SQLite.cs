using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.PromptRange.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class PromptRangeSenparcEntities_Sqlite : PromptRangeSenparcEntities
    {
        public PromptRangeSenparcEntities_Sqlite(DbContextOptions<PromptRangeSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                modelBuilder.Entity<PromptItem>(b =>
                {
                    b.Property(e => e.EvalAvgScore).HasConversion<double>();
                    b.Property(e => e.EvalMaxScore).HasConversion<double>();
                });
            }

            //if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            //{
            //    foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            //    {
            //        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableProperty property in entityType.GetProperties())
            //        {
            //            Type propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            //            if (propertyType == typeof(Decimal))
            //            {
            //                property.SetProviderClrType(typeof(Double));//Decimals are treated as doubles
            //            }
            //        }
            //    }
            //}

            base.OnModelCreating(modelBuilder);
        }

    }


    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c PromptRangeSenparcEntities_Sqlite -o Domain/Migrations/Migrations.Sqlite </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<PromptRangeSenparcEntities_Sqlite, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
        }
    }
}