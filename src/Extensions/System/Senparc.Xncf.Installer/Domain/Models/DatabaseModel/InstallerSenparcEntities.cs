using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.Installer.Models.DatabaseModel
{
    public class InstallerSenparcEntities : XncfDatabaseDbContext
    {
        public InstallerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        //public DbSet<Color> Colors { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
