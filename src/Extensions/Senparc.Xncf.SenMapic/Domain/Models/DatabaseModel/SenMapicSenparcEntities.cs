using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.SenMapic.Models;

public class SenMapicSenparcEntities : XncfDatabaseDbContext
{
    public SenMapicSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<Color> Colors { get; set; }
    public DbSet<SenMapicTask> Tasks { get; set; }
} 