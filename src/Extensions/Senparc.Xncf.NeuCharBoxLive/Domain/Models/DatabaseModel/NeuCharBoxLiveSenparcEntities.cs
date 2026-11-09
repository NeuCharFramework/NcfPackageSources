/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharBoxLiveSenparcEntities.cs
    文件功能描述：模块的 DbContext：邀请码、申请、运行记录与设置四张表


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增设置表 DbSet

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    /// <summary>The module's tables. Per-database subclasses (…_Sqlite, …_SqlServer) carry the migrations.</summary>
    public class NeuCharBoxLiveSenparcEntities : XncfDatabaseDbContext
    {
        public NeuCharBoxLiveSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<InviteCodeEntity> InviteCodes { get; set; } = null!;
        public DbSet<ApplicationEntity> Applications { get; set; } = null!;
        public DbSet<RunEntity> Runs { get; set; } = null!;
        public DbSet<SettingsEntity> Settings { get; set; } = null!;

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }
    }
}
