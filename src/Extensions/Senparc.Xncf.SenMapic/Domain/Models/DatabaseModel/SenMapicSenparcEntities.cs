using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.SenMapic;
using Senparc.Xncf.SenMapic.Models;

public class SenMapicSenparcEntities : XncfDatabaseDbContext
{
    public SenMapicSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<Color> Colors { get; set; }
    public DbSet<SenMapicTask> SenMapicTasks { get; set; }
    public DbSet<SenMapicTaskItem> SenMapicTaskItems { get; set; }
} 