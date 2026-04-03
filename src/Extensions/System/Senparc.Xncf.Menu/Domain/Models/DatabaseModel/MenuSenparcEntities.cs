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
        /// menu
        /// </summary>
        public DbSet<SysMenu> SysMenus { get; set; }

        /// <summary>
        /// button below the menu
        /// </summary>
        public DbSet<SysButton> SysButtons { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //If there is no special need, the OnModelCreating method does not need to be written. Registration is already required in Register.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
