using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Sandbox.Models;

public class SandboxSenparcEntities : XncfDatabaseDbContext
{
    public SandboxSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<SandboxSession> SandboxSessions { get; set; } = null!;

    //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
}
