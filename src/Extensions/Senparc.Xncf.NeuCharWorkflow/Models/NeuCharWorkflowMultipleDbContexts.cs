using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;

namespace Senparc.Xncf.NeuCharWorkflow.Models;

[MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_Sqlite : NeuCharWorkflowSenparcEntities
{
    public NeuCharWorkflowSenparcEntities_Sqlite(DbContextOptions<NeuCharWorkflowSenparcEntities_Sqlite> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_SqlServer : NeuCharWorkflowSenparcEntities
{
    public NeuCharWorkflowSenparcEntities_SqlServer(DbContextOptions<NeuCharWorkflowSenparcEntities_SqlServer> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_MySql : NeuCharWorkflowSenparcEntities
{
    public NeuCharWorkflowSenparcEntities_MySql(DbContextOptions<NeuCharWorkflowSenparcEntities_MySql> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_PostgreSQL : NeuCharWorkflowSenparcEntities
{
    public NeuCharWorkflowSenparcEntities_PostgreSQL(DbContextOptions<NeuCharWorkflowSenparcEntities_PostgreSQL> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_Oracle : NeuCharWorkflowSenparcEntities
{
    public NeuCharWorkflowSenparcEntities_Oracle(DbContextOptions<NeuCharWorkflowSenparcEntities_Oracle> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_Dm : NeuCharWorkflowSenparcEntities
{
    public NeuCharWorkflowSenparcEntities_Dm(DbContextOptions<NeuCharWorkflowSenparcEntities_Dm> options) : base(options) { }
}

// 设计时工厂确保每种受支持数据库生成独立、由本 XNCF 所有的 EF Migration。
public sealed class NeuCharWorkflowDbContextFactory_Sqlite
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_Sqlite, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
    public NeuCharWorkflowDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_SqlServer
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_SqlServer, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
    public NeuCharWorkflowDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_MySql
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_MySql, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDesignTimeDatabaseConfiguration");
    public NeuCharWorkflowDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_PostgreSQL
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_PostgreSQL, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
    public NeuCharWorkflowDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_Oracle
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_Oracle, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
    public NeuCharWorkflowDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_Dm
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_Dm, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
    public NeuCharWorkflowDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}
