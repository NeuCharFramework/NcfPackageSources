/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LegacyWorkflowMigrationService.cs
    File description: enhance workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

/// <summary>
/// Migrates legacy Admin Workflow tables into the standalone NeuChar Workflow module,
/// preserving source keys and refusing destructive cleanup until all records are copied.
/// </summary>
public sealed class LegacyWorkflowMigrationService
{
    private const string LegacyWorkflowTable = "ADMIN_NeuCharWorkflow";
    private const string LegacyVersionTable = "ADMIN_NeuCharWorkflowVersion";
    private const string LegacyFunctionTable = "ADMIN_NeuCharPivotFunction";
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes the legacy Workflow migration service.
    /// </summary>
    /// <param name="scopeFactory">The scope factory used to resolve the legacy and target DbContexts.</param>
    public LegacyWorkflowMigrationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Reads legacy workflow/version/function data, copies it idempotently into the XNCF tables,
    /// upgrades Function node references, and drops legacy tables only after completeness checks.
    /// </summary>
    /// <param name="installOrUpdate">The installation operation triggering migration.</param>
    /// <exception cref="InvalidOperationException">Thrown when context resolution, orphan versions, incomplete copy, or cleanup fails.</exception>
    public async Task MigrateAsync(InstallOrUpdate installOrUpdate)
    {
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        var contextType = new Register().TryGetXncfDatabaseDbContextType;
        var dbContext = provider.GetService(contextType) as XncfDatabaseDbContext;
        if (dbContext == null)
        {
            throw new InvalidOperationException($"无法解析 Workflow 数据库上下文：{contextType.FullName}");
        }

        List<LegacyWorkflowRow> workflows;
        try
        {
            workflows = await ReadLegacyWorkflowsAsync(dbContext).ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            SenparcTrace.SendCustomLog("NeuChar Workflow 迁移", $"未发现历史 Workflow 表，跳过切换：{ex.Message}");
            return;
        }
        var legacyVersions = await ReadLegacyVersionsAsync(dbContext).ConfigureAwait(false);
        var versions = legacyVersions.Rows;
        if (workflows.Count == 0 && versions.Count > 0)
        {
            throw new InvalidOperationException(
                "历史 Workflow 版本表存在无法关联的版本记录，已保留 Admin 旧表以便人工核对后重试。");
        }
        var functionMap = await TryReadFunctionMapAsync(dbContext).ConfigureAwait(false);
        var workflowService = provider.GetRequiredService<NeuCharWorkflowService>();
        var versionService = provider.GetRequiredService<NeuCharWorkflowVersionService>();
        var migrated = new Dictionary<int, WorkflowEntity>();
        var migratedVersionSourceKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var legacy in workflows)
        {
            var sourceKey = $"admin-workflow:{legacy.Id}";
            var current = await workflowService.GetObjectAsync(z => z.LegacySourceKey == sourceKey).ConfigureAwait(false);
            if (current == null)
            {
                current = new WorkflowEntity(legacy.Name, legacy.AdminUserId);
                current.Update(
                    legacy.Name,
                    legacy.Description,
                    UpgradeGraphReferences(legacy.GraphJson, functionMap),
                    legacy.Enabled,
                    legacy.TriggerType,
                    legacy.TriggerConfigJson,
                    legacy.NextRunAt,
                    legacy.AutoSaveMinutes);
                current.RestoreFromLegacy(
                    sourceKey,
                    legacy.LastRunAt,
                    legacy.LastSucceeded,
                    legacy.LastError,
                    legacy.Revision,
                    legacy.Flag,
                    legacy.AddTime,
                    legacy.LastUpdateTime,
                    legacy.TenantId,
                    legacy.AdminRemark,
                    legacy.Remark);
                await workflowService.SaveObjectAsync(current).ConfigureAwait(false);
            }
            migrated[legacy.Id] = current;
        }

        foreach (var legacy in versions)
        {
            if (!migrated.TryGetValue(legacy.WorkflowId, out var workflow))
            {
                continue;
            }
            var sourceKey = $"admin-workflow-version:{legacy.Id}";
            var current = await versionService.GetObjectAsync(z => z.LegacySourceKey == sourceKey).ConfigureAwait(false);
            if (current != null)
            {
                migratedVersionSourceKeys.Add(sourceKey);
                continue;
            }
            current = new NeuCharWorkflowVersion(workflow, legacy.AdminUserId, legacy.SaveSource);
            current.RestoreFromLegacy(
                sourceKey,
                legacy.Name,
                legacy.Description,
                UpgradeGraphReferences(legacy.GraphJson, functionMap),
                legacy.Enabled,
                legacy.TriggerType,
                legacy.TriggerConfigJson,
                legacy.AutoSaveMinutes,
                legacy.Revision,
                legacy.Flag,
                legacy.AddTime,
                legacy.LastUpdateTime,
                legacy.TenantId,
                legacy.AdminRemark,
                legacy.Remark);
            await versionService.SaveObjectAsync(current).ConfigureAwait(false);
            migratedVersionSourceKeys.Add(sourceKey);
        }

        //Legacy comment translated to English.
        var allCopied = workflows.All(row => migrated.ContainsKey(row.Id));
        var allVersionsCopied = versions.All(row => migratedVersionSourceKeys.Contains($"admin-workflow-version:{row.Id}"));
        if (!allCopied || !allVersionsCopied)
        {
            throw new InvalidOperationException("历史 Workflow 数据或版本未完整复制，已保留 Admin 旧表以便重试。");
        }

        if (legacyVersions.TableExists)
        {
            await DropLegacyTableAsync(dbContext, LegacyVersionTable).ConfigureAwait(false);
        }
        await DropLegacyTableAsync(dbContext, LegacyWorkflowTable).ConfigureAwait(false);
        SenparcTrace.SendCustomLog(
            "NeuChar Workflow 迁移",
            $"已将 {workflows.Count} 个工作流和 {versions.Count} 个版本迁移到独立 XNCF，并删除 Admin 旧 Workflow 表。安装操作：{installOrUpdate}");
    }

    /// <summary>
    /// Reads legacy workflow rows using the current database connection.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>A task containing legacy workflow rows.</returns>
    private static async Task<List<LegacyWorkflowRow>> ReadLegacyWorkflowsAsync(DbContext context)
    {
        const string sql = "SELECT Id, Name, Description, GraphJson, AdminUserId, Enabled, TriggerType, TriggerConfigJson, NextRunAt, LastRunAt, LastSucceeded, LastError, Revision, AutoSaveMinutes, Flag, AddTime, LastUpdateTime, TenantId, AdminRemark, Remark FROM ADMIN_NeuCharWorkflow";
        return await ReadRowsAsync(context, sql, reader => new LegacyWorkflowRow(
            GetInt(reader, "Id"),
            GetString(reader, "Name") ?? "未命名工作流",
            GetString(reader, "Description"),
            GetString(reader, "GraphJson") ?? "{\"nodes\":[],\"edges\":[]}",
            GetInt(reader, "AdminUserId"),
            GetBool(reader, "Enabled"),
            GetString(reader, "TriggerType") ?? "manual",
            GetString(reader, "TriggerConfigJson") ?? "{}",
            GetDateTime(reader, "NextRunAt"),
            GetDateTime(reader, "LastRunAt"),
            GetBoolNullable(reader, "LastSucceeded"),
            GetString(reader, "LastError"),
            GetInt(reader, "Revision"),
            GetInt(reader, "AutoSaveMinutes", 3),
            GetBool(reader, "Flag"),
            GetDateTime(reader, "AddTime") ?? DateTime.UtcNow,
            GetDateTime(reader, "LastUpdateTime") ?? DateTime.UtcNow,
            GetInt(reader, "TenantId", -1),
            GetString(reader, "AdminRemark"),
            GetString(reader, "Remark"))).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads legacy workflow versions and reports whether the version table exists.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>A task containing version rows and table-existence state.</returns>
    private static async Task<LegacyVersionsReadResult> ReadLegacyVersionsAsync(DbContext context)
    {
        const string sql = "SELECT Id, WorkflowId, Revision, Name, Description, GraphJson, Enabled, TriggerType, TriggerConfigJson, AutoSaveMinutes, AdminUserId, SaveSource, Flag, AddTime, LastUpdateTime, TenantId, AdminRemark, Remark FROM ADMIN_NeuCharWorkflowVersion";
        try
        {
            var rows = await ReadRowsAsync(context, sql, reader => new LegacyWorkflowVersionRow(
                GetInt(reader, "Id"),
                GetInt(reader, "WorkflowId"),
                GetInt(reader, "Revision"),
                GetString(reader, "Name") ?? "未命名工作流",
                GetString(reader, "Description"),
                GetString(reader, "GraphJson") ?? "{\"nodes\":[],\"edges\":[]}",
                GetBool(reader, "Enabled"),
                GetString(reader, "TriggerType") ?? "manual",
                GetString(reader, "TriggerConfigJson") ?? "{}",
                GetInt(reader, "AutoSaveMinutes", 3),
                GetInt(reader, "AdminUserId"),
                GetString(reader, "SaveSource") ?? "manual",
                GetBool(reader, "Flag"),
                GetDateTime(reader, "AddTime") ?? DateTime.UtcNow,
                GetDateTime(reader, "LastUpdateTime") ?? DateTime.UtcNow,
                GetInt(reader, "TenantId", -1),
                GetString(reader, "AdminRemark"),
                GetString(reader, "Remark"))).ConfigureAwait(false);
            return new LegacyVersionsReadResult(rows, true);
        }
        catch (DbException)
        {
            //Legacy comment translated to English.
            return new LegacyVersionsReadResult(new List<LegacyWorkflowVersionRow>(), false);
        }
    }

    /// <summary>
    /// Reads legacy Pivot Function IDs and maps them to module/function references.
    /// Missing legacy tables produce an empty map.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>A task containing the legacy Function map.</returns>
    private static async Task<IReadOnlyDictionary<int, (string ModuleUid, string FunctionKey)>> TryReadFunctionMapAsync(DbContext context)
    {
        const string sql = "SELECT Id, ModuleUid, FunctionKey FROM ADMIN_NeuCharPivotFunction";
        try
        {
            var rows = await ReadRowsAsync(context, sql, reader => (
                Id: GetInt(reader, "Id"),
                ModuleUid: GetString(reader, "ModuleUid"),
                FunctionKey: GetString(reader, "FunctionKey"))).ConfigureAwait(false);
            return rows.Where(z => z.Id > 0 && !string.IsNullOrWhiteSpace(z.ModuleUid) && !string.IsNullOrWhiteSpace(z.FunctionKey))
                .ToDictionary(z => z.Id, z => (z.ModuleUid!, z.FunctionKey!));
        }
        catch (DbException)
        {
            return new Dictionary<int, (string ModuleUid, string FunctionKey)>();
        }
    }

    /// <summary>
    /// Executes a read-only SQL query and projects each data-reader row.
    /// </summary>
    /// <typeparam name="T">The projected row type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="sql">The fixed migration SQL statement.</param>
    /// <param name="projector">The row projection function.</param>
    /// <returns>A task containing projected rows.</returns>
    private static async Task<List<T>> ReadRowsAsync<T>(DbContext context, string sql, Func<DbDataReader, T> projector)
    {
        var connection = context.Database.GetDbConnection();
        var mustClose = connection.State != ConnectionState.Open;
        if (mustClose)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            var result = new List<T>();
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                result.Add(projector(reader));
            }
            return result;
        }
        finally
        {
            if (mustClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Drops one of the explicitly allowlisted legacy Workflow tables.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="tableName">The allowlisted legacy table name.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for any table name outside the allowlist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when table deletion fails after migration.</exception>
    private static async Task DropLegacyTableAsync(DbContext context, string tableName)
    {
        var sql = tableName switch
        {
            LegacyVersionTable => "DROP TABLE ADMIN_NeuCharWorkflowVersion",
            LegacyWorkflowTable => "DROP TABLE ADMIN_NeuCharWorkflow",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "不允许删除非 Workflow 历史表。")
        };
        try
        {
            await context.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            throw new InvalidOperationException($"已完成数据复制，但删除旧表 {tableName} 失败；为避免双写，需先处理该数据库错误。", ex);
        }
    }

    /// <summary>
    /// Adds current module/function references to legacy Function nodes using the old numeric ID map.
    /// Malformed graph JSON is returned unchanged for later validation.
    /// </summary>
    /// <param name="json">The legacy graph JSON.</param>
    /// <param name="functions">Legacy Function ID mapping.</param>
    /// <returns>The upgraded or original graph JSON.</returns>
    private static string UpgradeGraphReferences(string json, IReadOnlyDictionary<int, (string ModuleUid, string FunctionKey)> functions)
    {
        if (functions.Count == 0 || string.IsNullOrWhiteSpace(json))
        {
            return json ?? "{\"nodes\":[],\"edges\":[]}";
        }
        try
        {
            var graph = JsonNode.Parse(json) as JsonObject;
            if (graph?["nodes"] is not JsonArray nodes)
            {
                return json;
            }
            foreach (var node in nodes.OfType<JsonObject>())
            {
                if (!string.Equals(node["type"]?.GetValue<string>(), "function", StringComparison.OrdinalIgnoreCase) ||
                    node["config"] is not JsonObject config ||
                    !TryGetInt(config["functionId"], out var functionId) ||
                    !functions.TryGetValue(functionId, out var function))
                {
                    continue;
                }
                config["moduleUid"] ??= function.ModuleUid;
                config["functionKey"] ??= function.FunctionKey;
            }
            return graph.ToJsonString();
        }
        catch (JsonException)
        {
            return json;
        }
    }

    /// <summary>Reads an integer database column with a fallback.</summary>
    /// <param name="reader">The data reader.</param><param name="name">The column name.</param><param name="fallback">The fallback value.</param>
    private static int GetInt(DbDataReader reader, string name, int fallback = 0)
    {
        var value = GetValue(reader, name);
        return value == null ? fallback : Convert.ToInt32(value);
    }

    /// <summary>Reads a database Boolean column, treating null as false.</summary>
    /// <param name="reader">The data reader.</param><param name="name">The column name.</param>
    private static bool GetBool(DbDataReader reader, string name) =>
        GetBoolNullable(reader, name) ?? false;

    /// <summary>Reads a nullable database Boolean column.</summary>
    /// <param name="reader">The data reader.</param><param name="name">The column name.</param>
    private static bool? GetBoolNullable(DbDataReader reader, string name)
    {
        var value = GetValue(reader, name);
        if (value == null) return null;
        return value is bool boolValue ? boolValue : Convert.ToInt32(value) != 0;
    }

    /// <summary>Reads a nullable database date-time column.</summary>
    /// <param name="reader">The data reader.</param><param name="name">The column name.</param>
    private static DateTime? GetDateTime(DbDataReader reader, string name)
    {
        var value = GetValue(reader, name);
        return value == null ? null : Convert.ToDateTime(value);
    }

    /// <summary>Reads a nullable database column and converts it to text.</summary>
    /// <param name="reader">The data reader.</param><param name="name">The column name.</param>
    private static string? GetString(DbDataReader reader, string name) => GetValue(reader, name)?.ToString();

    /// <summary>Reads a database column value, returning null for SQL NULL.</summary>
    /// <param name="reader">The data reader.</param><param name="name">The column name.</param>
    private static object? GetValue(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }

    /// <summary>Attempts to read a positive integer from a JSON node.</summary>
    /// <param name="node">The JSON node.</param><param name="value">Receives the parsed integer.</param>
    /// <returns><see langword="true"/> when a positive integer is available.</returns>
    private static bool TryGetInt(JsonNode? node, out int value)
    {
        try
        {
            value = node?.GetValue<int>() ?? 0;
            return value > 0;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private sealed record LegacyWorkflowRow(
        int Id, string Name, string? Description, string GraphJson, int AdminUserId, bool Enabled,
        string TriggerType, string TriggerConfigJson, DateTime? NextRunAt, DateTime? LastRunAt,
        bool? LastSucceeded, string? LastError, int Revision, int AutoSaveMinutes, bool Flag,
        DateTime AddTime, DateTime LastUpdateTime, int TenantId, string? AdminRemark, string? Remark);

    private sealed record LegacyWorkflowVersionRow(
        int Id, int WorkflowId, int Revision, string Name, string? Description, string GraphJson, bool Enabled,
        string TriggerType, string TriggerConfigJson, int AutoSaveMinutes, int AdminUserId, string SaveSource, bool Flag,
        DateTime AddTime, DateTime LastUpdateTime, int TenantId, string? AdminRemark, string? Remark);

    private sealed record LegacyVersionsReadResult(
        List<LegacyWorkflowVersionRow> Rows,
        bool TableExists);
}
