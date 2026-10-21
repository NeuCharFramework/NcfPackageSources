using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.Workspace.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Workspace.Domain.Models.MultipleDatabase;

[MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
public sealed class WorkspaceSenparcEntities_SqlServer
    : WorkspaceSenparcEntities
{
    public WorkspaceSenparcEntities_SqlServer(
        DbContextOptions<WorkspaceSenparcEntities_SqlServer> options)
        : base(options)
    {
    }
}

[MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
public sealed class WorkspaceSenparcEntities_MySql
    : WorkspaceSenparcEntities
{
    public WorkspaceSenparcEntities_MySql(
        DbContextOptions<WorkspaceSenparcEntities_MySql> options)
        : base(options)
    {
    }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
public sealed class WorkspaceSenparcEntities_Oracle
    : WorkspaceSenparcEntities
{
    public WorkspaceSenparcEntities_Oracle(
        DbContextOptions<WorkspaceSenparcEntities_Oracle> options)
        : base(options)
    {
    }
}

[MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
public sealed class WorkspaceSenparcEntities_PostgreSQL
    : WorkspaceSenparcEntities
{
    public WorkspaceSenparcEntities_PostgreSQL(
        DbContextOptions<WorkspaceSenparcEntities_PostgreSQL> options)
        : base(options)
    {
    }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
public sealed class WorkspaceSenparcEntities_Sqlite
    : WorkspaceSenparcEntities
{
    public WorkspaceSenparcEntities_Sqlite(
        DbContextOptions<WorkspaceSenparcEntities_Sqlite> options)
        : base(options)
    {
    }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
public sealed class WorkspaceSenparcEntities_Dm
    : WorkspaceSenparcEntities
{
    public WorkspaceSenparcEntities_Dm(
        DbContextOptions<WorkspaceSenparcEntities_Dm> options)
        : base(options)
    {
    }
}
