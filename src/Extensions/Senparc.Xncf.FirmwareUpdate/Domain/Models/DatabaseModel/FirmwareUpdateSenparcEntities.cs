/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FirmwareUpdateSenparcEntities.cs
    File description: FirmwareUpdateSenparcEntities related implementation

    Created by: Senparc - 20260504

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FirmwareUpdate.Models;

public class FirmwareUpdateSenparcEntities : XncfDatabaseDbContext
{
    public FirmwareUpdateSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<Senparc.Xncf.FirmwareUpdate.FirmwareUpdateConfig> FirmwareUpdateConfigs { get; set; } = null!;
}
