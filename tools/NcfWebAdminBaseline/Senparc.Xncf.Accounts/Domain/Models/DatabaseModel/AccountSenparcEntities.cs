/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AccountSenparcEntities.cs
    File description: AccountSenparcEntities related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.3.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.Accounts.Models
{
    public class AccountSenparcEntities : XncfDatabaseDbContext
    {
        public AccountSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }


        /// <summary>
        /// userinformation
        /// </summary>
        public virtual DbSet<Account> Accounts { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>

        public virtual DbSet<AccountPayLog> AccountPayLogs { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual DbSet<PointsLog> PointsLogs { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Legacy comment translated to English.
            //modelBuilder.ApplyConfiguration(new AccountConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new AccountPayLogConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new PointsLogConfigurationMapping());
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// </summary>
    public class SenparcDbContextFactoryHeler
    {
      
    }
}
