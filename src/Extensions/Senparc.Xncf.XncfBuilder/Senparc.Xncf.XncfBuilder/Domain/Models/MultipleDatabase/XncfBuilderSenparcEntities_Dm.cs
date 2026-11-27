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
    /// Dameng-specific XNCF Builder database context used by runtime registration and migrations.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class XncfBuilderSenparcEntities_Dm : XncfBuilderSenparcEntities, IMultipleMigrationDbContext
    {
        public XncfBuilderSenparcEntities_Dm(DbContextOptions<XncfBuilderSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Legacy comment translated to English.
            modelBuilder.Entity<XncfPreviewTask>().Property(task => task.ErrorMessage).HasColumnType("CLOB");
            modelBuilder.Entity<XncfPreviewTask>().Property(task => task.RecentOutput).HasColumnType("CLOB");
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -C XncfBuilderSenparcEntities_Dm -o Domain/Migrations/Migrations.Dm </para>
    /// </summary>
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<XncfBuilderSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Dm()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
