/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileManagerSenparcEntities.cs
    文件功能描述：FileManagerSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20250105
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
