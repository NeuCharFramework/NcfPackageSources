/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicSenparcEntities.cs
    File description: SenMapicSenparcEntities related implementation

    Created by: Senparc - 20250114

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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