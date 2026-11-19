/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseRuntimeState.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.35.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

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
    public static DatabaseRuntimeState Uninitialized(string message, Exception exception = null) =>
        new(DatabaseRuntimeStatus.Uninitialized, message, Array.Empty<DatabaseModuleMigration>(), exception);

    public static DatabaseRuntimeState Ready() =>
        new(DatabaseRuntimeStatus.Ready, "数据库架构已就绪。", Array.Empty<DatabaseModuleMigration>());

    public static DatabaseRuntimeState UpgradeRequired(
        string message,
        IReadOnlyList<DatabaseModuleMigration> modules,
        Exception exception = null) =>
        new(DatabaseRuntimeStatus.UpgradeRequired, message, modules, exception);

    public static DatabaseRuntimeState Unavailable(string message, Exception exception) =>
        new(DatabaseRuntimeStatus.Unavailable, message, Array.Empty<DatabaseModuleMigration>(), exception);
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class DatabaseRuntimeStateStore
{
    private DatabaseRuntimeState _current = DatabaseRuntimeState.Uninitialized("数据库状态尚未检查。");

    public DatabaseRuntimeState Current => Volatile.Read(ref _current);

    public void Set(DatabaseRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Volatile.Write(ref _current, state);
    }
}
