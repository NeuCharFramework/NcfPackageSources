/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: MenuSenparcEntities.cs

    File description: MenuSenparcEntities related implementation

    

    

    Created by: Senparc - 20211128

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.Menu.Models
{
    public class MenuSenparcEntities : XncfDatabaseDbContext
    {
        public MenuSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<SysMenu> SysMenus { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
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
