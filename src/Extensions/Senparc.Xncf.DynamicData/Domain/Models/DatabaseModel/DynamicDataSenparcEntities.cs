/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DynamicDataSenparcEntities.cs
    File description: DynamicDataSenparcEntities related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.DynamicData.Models.DatabaseModel;

namespace Senparc.Xncf.DynamicData.Models
{
    public class DynamicDataSenparcEntities : XncfDatabaseDbContext
    {
        public DynamicDataSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }

        public DbSet<TableMetadata> TableMetadatas { get; set; }
        public DbSet<ColumnMetadata> ColumnMetadatas { get; set; }
        public DbSet<TableData> TableDatas { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
