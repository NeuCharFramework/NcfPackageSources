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
        ///user information
        /// </summary>
        public virtual DbSet<Account> Accounts { get; set; }

        /// <summary>
        ///User payment log
        /// </summary>

        public virtual DbSet<AccountPayLog> AccountPayLogs { get; set; }

        /// <summary>
        ///User Points Log
        /// </summary>
        public virtual DbSet<PointsLog> PointsLogs { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Use [XncfAutoConfigurationMapping] to automate
            //modelBuilder.ApplyConfiguration(new AccountConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new AccountPayLogConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new PointsLogConfigurationMapping());
        }
    }

    /// <summary>
    /// Design-time DbContext creation (Code-First database migration is only used during development and will not be executed in the production environment)
    /// </summary>
    public class SenparcDbContextFactoryHeler
    {
      
    }
}
