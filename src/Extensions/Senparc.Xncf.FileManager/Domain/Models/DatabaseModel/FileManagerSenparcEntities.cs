using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.FileManager.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.FileManager.Models
{
    public class FileManagerSenparcEntities : XncfDatabaseDbContext
    {
        public FileManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }

        public DbSet<NcfFile> NcfFiles { get; set; }

        public DbSet<NcfFolder> NcfFolders { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //If there is no special need, the OnModelCreating method does not need to be written. Registration is already required in Register.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
