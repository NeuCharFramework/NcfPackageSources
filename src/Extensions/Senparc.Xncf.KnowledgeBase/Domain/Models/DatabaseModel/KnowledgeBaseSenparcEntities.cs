using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;

namespace Senparc.Xncf.KnowledgeBase.Models
{
    public class KnowledgeBaseSenparcEntities : XncfDatabaseDbContext
    {
        public KnowledgeBaseSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }
        public DbSet<DatabaseModel.KnowledgeBase> KnowledgeBases { get; set; }
        public DbSet<KnowledgeBaseItem> KnowledgeBasesItems { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //If there is no special need, the OnModelCreating method does not need to be written. Registration is already required in Register.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
