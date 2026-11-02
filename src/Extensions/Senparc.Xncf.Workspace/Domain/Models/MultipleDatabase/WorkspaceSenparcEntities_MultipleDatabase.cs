using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Workspace.Domain.Models.DatabaseModel;
using System;

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

public sealed class WorkspaceDbContextFactory_Sqlite
    : SenparcDesignTimeDbContextFactoryBase<WorkspaceSenparcEntities_Sqlite, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");

    public WorkspaceDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class WorkspaceDbContextFactory_SqlServer
    : SenparcDesignTimeDbContextFactoryBase<WorkspaceSenparcEntities_SqlServer, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");

    public WorkspaceDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class WorkspaceDbContextFactory_MySql
    : SenparcDesignTimeDbContextFactoryBase<WorkspaceSenparcEntities_MySql, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDesignTimeDatabaseConfiguration");

    public WorkspaceDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class WorkspaceDbContextFactory_PostgreSQL
    : SenparcDesignTimeDbContextFactoryBase<WorkspaceSenparcEntities_PostgreSQL, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");

    public WorkspaceDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class WorkspaceDbContextFactory_Oracle
    : SenparcDesignTimeDbContextFactoryBase<WorkspaceSenparcEntities_Oracle, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");

    public WorkspaceDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class WorkspaceDbContextFactory_Dm
    : SenparcDesignTimeDbContextFactoryBase<WorkspaceSenparcEntities_Dm, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");

    public WorkspaceDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}
