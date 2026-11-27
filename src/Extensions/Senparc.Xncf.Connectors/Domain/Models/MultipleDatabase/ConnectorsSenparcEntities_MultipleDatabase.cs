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
    /// <inheritdoc />
    public ConnectorsSenparcEntities_SqlServer(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
public sealed class ConnectorsSenparcEntities_MySql : ConnectorsSenparcEntities
{
    /// <inheritdoc />
    public ConnectorsSenparcEntities_MySql(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
public sealed class ConnectorsSenparcEntities_Oracle : ConnectorsSenparcEntities
{
    /// <inheritdoc />
    public ConnectorsSenparcEntities_Oracle(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
public sealed class ConnectorsSenparcEntities_PostgreSQL : ConnectorsSenparcEntities
{
    /// <inheritdoc />
    public ConnectorsSenparcEntities_PostgreSQL(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
public sealed class ConnectorsSenparcEntities_Sqlite : ConnectorsSenparcEntities
{
    /// <inheritdoc />
    public ConnectorsSenparcEntities_Sqlite(DbContextOptions options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
public sealed class ConnectorsSenparcEntities_Dm : ConnectorsSenparcEntities
{
    /// <inheritdoc />
    public ConnectorsSenparcEntities_Dm(DbContextOptions options) : base(options) { }
}

public sealed class ConnectorsDbContextFactory_Sqlite
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_Sqlite, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");

    /// <summary>Initializes the SQLite design-time connector context factory.</summary>
    public ConnectorsDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_SqlServer
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_SqlServer, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");

    /// <summary>Initializes the SQL Server design-time connector context factory.</summary>
    public ConnectorsDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_MySql
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_MySql, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDesignTimeDatabaseConfiguration");

    /// <summary>Initializes the MySQL design-time connector context factory.</summary>
    public ConnectorsDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_PostgreSQL
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_PostgreSQL, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");

    /// <summary>Initializes the PostgreSQL design-time connector context factory.</summary>
    public ConnectorsDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_Oracle
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_Oracle, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");

    /// <summary>Initializes the Oracle design-time connector context factory.</summary>
    public ConnectorsDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class ConnectorsDbContextFactory_Dm
    : SenparcDesignTimeDbContextFactoryBase<ConnectorsSenparcEntities_Dm, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");

    /// <summary>Initializes the Dm design-time connector context factory.</summary>
    public ConnectorsDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}
