using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.XncfModuleManager.Models
{
    public class XncfModuleManagerSenparcEntities : XncfDatabaseDbContext
    {
        public XncfModuleManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {

        }

        /// <summary>
        /// Extended modules
        /// </summary>
        public DbSet<XncfModule> XncfModules { get; set; }

        //If there are no special requirements, OnModelCreating can be omitted because registration is already required in Register.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
