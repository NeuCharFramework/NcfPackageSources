using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Connectors.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Connectors.Domain.Models.MultipleDatabase;

[MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
public sealed class ConnectorsSenparcEntities_SqlServer : ConnectorsSenparcEntities
{
    public ConnectorsSenparcEntities_SqlServer(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
public sealed class ConnectorsSenparcEntities_MySql : ConnectorsSenparcEntities
{
    public ConnectorsSenparcEntities_MySql(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
public sealed class ConnectorsSenparcEntities_Oracle : ConnectorsSenparcEntities
{
    public ConnectorsSenparcEntities_Oracle(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
public sealed class ConnectorsSenparcEntities_PostgreSQL : ConnectorsSenparcEntities
{
    public ConnectorsSenparcEntities_PostgreSQL(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
public sealed class ConnectorsSenparcEntities_Sqlite : ConnectorsSenparcEntities
{
    public ConnectorsSenparcEntities_Sqlite(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
public sealed class ConnectorsSenparcEntities_Dm : ConnectorsSenparcEntities
{
    public ConnectorsSenparcEntities_Dm(DbContextOptions options) : base(options) { }
}

public sealed class ConnectorsDbContextFactory_Sqlite
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_Sqlite, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");

    public ConnectorsDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_SqlServer
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_SqlServer, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");

    public ConnectorsDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_MySql
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_MySql, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDesignTimeDatabaseConfiguration");

    public ConnectorsDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_PostgreSQL
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_PostgreSQL, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");

    public ConnectorsDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_Oracle
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_Oracle, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");

    public ConnectorsDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_Dm
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_Dm, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");

    public ConnectorsDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}
