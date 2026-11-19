/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileManagerSenparcEntities.cs
    File description: FileManagerSenparcEntities related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Existing FileManager records predate the resource boundary. Keep
            // them private KnowledgeBase sources during migration so no historic
            // upload is accidentally exposed through the public asset endpoint.
            modelBuilder.Entity<NcfFile>(entity =>
            {
                entity.Property(file => file.ResourceScope)
                    .HasDefaultValue(NcfFileResourceScope.KnowledgeBase);
                entity.Property(file => file.AccessLevel)
                    .HasDefaultValue(NcfFileAccessLevel.Private);
                entity.HasIndex(file => new { file.ResourceScope, file.FolderId });
            });

            modelBuilder.Entity<NcfFolder>(entity =>
            {
                entity.Property(folder => folder.ResourceScope)
                    .HasDefaultValue(NcfFileResourceScope.KnowledgeBase);
                entity.HasIndex(folder => new { folder.ResourceScope, folder.ParentId });
            });
        }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
