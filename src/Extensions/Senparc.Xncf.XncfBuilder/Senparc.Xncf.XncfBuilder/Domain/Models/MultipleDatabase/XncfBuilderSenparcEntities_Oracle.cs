using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Models.MultipleDatabase
{
    /// <summary>
    /// Oracle-specific XNCF Builder database context used by runtime registration and migrations.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
    public class XncfBuilderSenparcEntities_Oracle : XncfBuilderSenparcEntities, IMultipleMigrationDbContext
    {
        public XncfBuilderSenparcEntities_Oracle(DbContextOptions<XncfBuilderSenparcEntities_Oracle> dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Legacy comment translated to English.
            modelBuilder.Entity<XncfPreviewTask>().Property(task => task.ErrorMessage).HasColumnType("NCLOB");
            modelBuilder.Entity<XncfPreviewTask>().Property(task => task.RecentOutput).HasColumnType("NCLOB");
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -C XncfBuilderSenparcEntities_Oracle -o Domain/Migrations/Migrations.Oracle </para>
    /// </summary>
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<XncfBuilderSenparcEntities_Oracle, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Oracle()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
