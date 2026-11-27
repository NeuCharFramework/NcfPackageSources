/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowMultipleDbContexts.cs
    File description: Data models, DTOs, and mapping definitions

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 improve workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260913
    Change description: v0.4.0 map Oracle/Dm long-text columns: Chat content and replay JSON use CLOB

    Modified by: Senparc - 20260915
    Change description: v0.4.0 improve Chat trigger message persistence and recovery

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.NeuCharWorkflow.Models;

[MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_Sqlite : NeuCharWorkflowSenparcEntities
{
    /// <inheritdoc />
    public NeuCharWorkflowSenparcEntities_Sqlite(DbContextOptions<NeuCharWorkflowSenparcEntities_Sqlite> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_SqlServer : NeuCharWorkflowSenparcEntities
{
    /// <inheritdoc />
    public NeuCharWorkflowSenparcEntities_SqlServer(DbContextOptions<NeuCharWorkflowSenparcEntities_SqlServer> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_MySql : NeuCharWorkflowSenparcEntities
{
    /// <inheritdoc />
    public NeuCharWorkflowSenparcEntities_MySql(DbContextOptions<NeuCharWorkflowSenparcEntities_MySql> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_PostgreSQL : NeuCharWorkflowSenparcEntities
{
    /// <inheritdoc />
    public NeuCharWorkflowSenparcEntities_PostgreSQL(DbContextOptions<NeuCharWorkflowSenparcEntities_PostgreSQL> options) : base(options) { }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_Oracle : NeuCharWorkflowSenparcEntities
{
    /// <inheritdoc />
    public NeuCharWorkflowSenparcEntities_Oracle(DbContextOptions<NeuCharWorkflowSenparcEntities_Oracle> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Chat content can contain up to 8000 characters; Oracle's default
        // NVARCHAR2(2000) is too small, so use NCLOB.
        modelBuilder.Entity<NeuCharWorkflowChatMessage>().Property(z => z.Content).HasColumnType("NCLOB");

        // Replay JSON size is unbounded. The AddExecutionReplay migration uses
        // CLOB on Oracle, so keep the mapping consistent and avoid narrowing it.
        modelBuilder.Entity<NeuCharWorkflowExecutionLog>().Property(z => z.ReplaySnapshotJson).HasColumnType("CLOB");
        modelBuilder.Entity<NeuCharWorkflowExecutionLog>().Property(z => z.ReplayEventsJson).HasColumnType("CLOB");
    }
}

[MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
public sealed class NeuCharWorkflowSenparcEntities_Dm : NeuCharWorkflowSenparcEntities
{
    /// <inheritdoc />
    public NeuCharWorkflowSenparcEntities_Dm(DbContextOptions<NeuCharWorkflowSenparcEntities_Dm> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Replay JSON size is unbounded. The AddExecutionReplay migration uses
        // CLOB on Dm, so keep the mapping consistent and avoid narrowing it.
        modelBuilder.Entity<NeuCharWorkflowExecutionLog>().Property(z => z.ReplaySnapshotJson).HasColumnType("CLOB");
        modelBuilder.Entity<NeuCharWorkflowExecutionLog>().Property(z => z.ReplayEventsJson).HasColumnType("CLOB");
    }
}

// The design-time factory keeps EF migrations independent and owned by this
// XNCF for every supported database.
public sealed class NeuCharWorkflowDbContextFactory_Sqlite
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_Sqlite, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
    /// <summary>Initializes the SQLite design-time workflow context factory.</summary>
    public NeuCharWorkflowDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_SqlServer
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_SqlServer, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
    /// <summary>Initializes the SQL Server design-time workflow context factory.</summary>
    public NeuCharWorkflowDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_MySql
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_MySql, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDesignTimeDatabaseConfiguration");
    /// <summary>Initializes the MySQL design-time workflow context factory.</summary>
    public NeuCharWorkflowDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_PostgreSQL
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_PostgreSQL, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
    /// <summary>Initializes the PostgreSQL design-time workflow context factory.</summary>
    public NeuCharWorkflowDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_Oracle
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_Oracle, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
    /// <summary>Initializes the Oracle design-time workflow context factory.</summary>
    public NeuCharWorkflowDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}

public sealed class NeuCharWorkflowDbContextFactory_Dm
    : SenparcDesignTimeDbContextFactoryBase<NeuCharWorkflowSenparcEntities_Dm, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
        app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
    /// <summary>Initializes the Dm design-time workflow context factory.</summary>
    public NeuCharWorkflowDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath) { }
}
