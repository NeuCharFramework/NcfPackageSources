/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseSenparcEntities.cs
    File description: KnowledgeBaseSenparcEntities related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
