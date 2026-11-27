/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MenuSenparcEntities.cs
    File description: MenuSenparcEntities related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.14.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.14.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.Menu.Models
{
    /// <summary>
    /// EF Core context for the system menu module's menu and button entities.
    /// </summary>
    public class MenuSenparcEntities : XncfDatabaseDbContext
    {
        /// <summary>
        /// Initializes the menu module database context.
        /// </summary>
        /// <param name="dbContextOptions">The provider-specific EF Core options.</param>
        public MenuSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }


        /// <summary>
        /// Gets or sets the persisted system menu set.
        /// </summary>
        public DbSet<SysMenu> SysMenus { get; set; }

        /// <summary>
        /// Gets or sets the persisted system button set.
        /// </summary>
        public DbSet<SysButton> SysButtons { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
