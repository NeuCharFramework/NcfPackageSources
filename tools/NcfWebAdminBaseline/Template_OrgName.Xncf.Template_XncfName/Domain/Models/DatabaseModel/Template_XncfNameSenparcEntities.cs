/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Template_XncfNameSenparcEntities.cs
    File description: Template_XncfNameSenparcEntities related implementation

    Created by: Senparc - 20201226

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;

namespace Template_OrgName.Xncf.Template_XncfName.Models
{
    public class Template_XncfNameSenparcEntities : XncfDatabaseDbContext
    {
        public Template_XncfNameSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Color> Colors { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
