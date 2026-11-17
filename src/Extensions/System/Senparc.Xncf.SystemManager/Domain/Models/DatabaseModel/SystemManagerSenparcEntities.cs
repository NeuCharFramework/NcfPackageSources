/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SystemManagerSenparcEntities.cs

    File description: SystemManagerSenparcEntities related implementation

    

    

    Created by: Senparc - 20211128

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SystemManagerSenparcEntities : XncfDatabaseDbContext
    {
        public SystemManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        public DbSet<FeedBack> FeedBacks { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
