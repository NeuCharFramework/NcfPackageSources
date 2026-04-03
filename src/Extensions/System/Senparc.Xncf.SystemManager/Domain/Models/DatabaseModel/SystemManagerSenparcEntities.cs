using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    /// The current context should not be irrelevant to the tenant
    /// </summary>
    public class SystemManagerSenparcEntities : XncfDatabaseDbContext
    {
        public SystemManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        /// <summary>
        ///system settings
        /// </summary>
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        public DbSet<FeedBack> FeedBacks { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //If there is no special need, the OnModelCreating method does not need to be written. Registration is already required in Register.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
