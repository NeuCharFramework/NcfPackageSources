/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseRuntimeState.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.35.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.39.10 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Web.Infrastructure.Database;

public enum DatabaseRuntimeStatus
{
    Uninitialized,
    UpgradeRequired,
    Ready,
    Unavailable
}

public sealed record DatabaseModuleMigration(
    string Uid,
    string Name,
    string Version,
    Type DbContextType,
    IReadOnlyList<string> PendingMigrations);

public sealed record DatabaseRuntimeState(
    DatabaseRuntimeStatus Status,
    string Message,
    IReadOnlyList<DatabaseModuleMigration> Modules,
    Exception Exception = null)
{
    /// <summary>Creates a state indicating that database inspection has not run.</summary>
    /// <param name="message">The diagnostic state message.</param>
    /// <param name="exception">An optional inspection exception.</param>
    public static DatabaseRuntimeState Uninitialized(string message, Exception exception = null) =>
        new(DatabaseRuntimeStatus.Uninitialized, message, Array.Empty<DatabaseModuleMigration>(), exception);

    /// <summary>Creates a state indicating that all database schemas are ready.</summary>
    public static DatabaseRuntimeState Ready() =>
        new(DatabaseRuntimeStatus.Ready, "数据库架构已就绪。", Array.Empty<DatabaseModuleMigration>());

    /// <summary>Creates a state indicating that one or more modules require migration.</summary>
    /// <param name="message">The diagnostic state message.</param>
    /// <param name="modules">The modules with pending migrations.</param>
    /// <param name="exception">An optional inspection exception.</param>
    public static DatabaseRuntimeState UpgradeRequired(
        string message,
        IReadOnlyList<DatabaseModuleMigration> modules,
        Exception exception = null) =>
        new(DatabaseRuntimeStatus.UpgradeRequired, message, modules, exception);

    /// <summary>Creates a state indicating that database inspection is unavailable.</summary>
    /// <param name="message">The diagnostic state message.</param>
    /// <param name="exception">The exception that prevented inspection.</param>
    public static DatabaseRuntimeState Unavailable(string message, Exception exception) =>
        new(DatabaseRuntimeStatus.Unavailable, message, Array.Empty<DatabaseModuleMigration>(), exception);
}

/// <summary>
/// Stores the latest database runtime state for process-wide diagnostics.
/// </summary>
public sealed class DatabaseRuntimeStateStore
{
    private DatabaseRuntimeState _current = DatabaseRuntimeState.Uninitialized("数据库状态尚未检查。");

    /// <summary>Gets the most recently published database runtime state.</summary>
    public DatabaseRuntimeState Current => Volatile.Read(ref _current);

    /// <summary>Publishes a non-null database runtime state.</summary>
    /// <param name="state">The state to publish.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is null.</exception>
    public void Set(DatabaseRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Volatile.Write(ref _current, state);
    }
}
