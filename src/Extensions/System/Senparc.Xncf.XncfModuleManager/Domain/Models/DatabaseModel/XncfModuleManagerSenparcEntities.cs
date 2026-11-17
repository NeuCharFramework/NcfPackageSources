/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: XncfModuleManagerSenparcEntities.cs

    File description: XncfModuleManagerSenparcEntities related implementation

    

    

    Created by: Senparc - 20211128

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.XncfModuleManager.Models
{
    public class XncfModuleManagerSenparcEntities : XncfDatabaseDbContext
    {
        public XncfModuleManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {

        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<XncfModule> XncfModules { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
