/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowAnalyticsService.cs
    文件功能描述：Workflow 运行、资源和趋势统计

    修改标识：Senparc - 20260829
    修改描述：v0.3.0 新增工作流分析查询与管理端可视化

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

public sealed class NeuCharWorkflowAnalyticsService(
    NeuCharWorkflowService workflowService,
    NeuCharWorkflowExecutionLogService executionLogService,
    NeuCharWorkflowRunCoordinator runCoordinator)
{
    public async Task<WorkflowAnalyticsResult> GetAsync(
        int adminUserId,
        WorkflowAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflows = await workflowService.GetNameMapAsync(adminUserId, cancellationToken).ConfigureAwait(false);
        var workflowIds = ResolveWorkflowIds(workflows, query.WorkflowId);
        if (workflowIds.Count == 0)
        {
            return EmptyResult(query);
        }

        var (fromUtc, toUtc) = NormalizeDateRange(query);
        var logs = await executionLogService.GetAnalyticsLogsAsync(
                workflowIds,
                NormalizeStatus(query.Status),
                fromUtc,
                toUtc,
                cancellationToken)
            .ConfigureAwait(false);

        var activeRuns = runCoordinator.GetActiveRuns(adminUserId)
            .Where(run => workflowIds.Contains(run.WorkflowId))
            .Where(run => InDateRange(run.StartedAt.UtcDateTime, fromUtc, toUtc))
            .ToList();

        var logRuns = logs
            .Select(log => ToRun(log, workflows))
            .ToList();
        var liveRuns = activeRuns
            .Where(run => !logRuns.Any(log =>
                string.Equals(log.CorrelationId, BuildCorrelationId(run.WorkflowId, run.RunId), StringComparison.OrdinalIgnoreCase)))
            .Select(run => ToLiveRun(run, workflows))
            .ToList();
        var allRuns = logRuns.Concat(liveRuns).ToList();

        var baseRuns = allRuns
            .Where(run => MatchesStatus(run.Status, query.Status))
            .Where(run => MatchesKeyword(run, query.Keyword))
            .ToList();
        var matchingRuns = baseRuns
            .Where(run => MatchesResource(run.Resources, query))
            .OrderByDescending(run => run.StartedAt)
            .ToList();

        var resourceRows = BuildResourceRows(matchingRuns);
        var resourceOptions = BuildResourceRows(baseRuns)
            .OrderBy(row => row.TypeLabel)
            .ThenBy(row => row.Name)
            .Select(ToResourceOption)
            .ToList();
        var workflowRows = BuildWorkflowRows(matchingRuns);
        var trendRows = BuildTrendRows(matchingRuns);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var runRows = matchingRuns
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToRunSummary)
            .ToList();

        return new WorkflowAnalyticsResult(
            BuildSummary(matchingRuns),
            trendRows,
            resourceRows,
            workflowRows,
            runRows,
            resourceOptions,
            workflows
                .OrderBy(item => item.Value)
                .Select(item => new WorkflowAnalyticsOption(
                    "workflow",
                    string.Empty,
                    item.Key.ToString(CultureInfo.InvariantCulture),
                    item.Value,
                    item.Value))
                .ToList(),
            page,
            pageSize,
            matchingRuns.Count > page * pageSize);
    }

    public async Task<WorkflowAnalyticsSummary> GetSummaryAsync(
        int adminUserId,
        int? workflowId = null,
        string? from = null,
        string? to = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflows = await workflowService.GetNameMapAsync(adminUserId, cancellationToken).ConfigureAwait(false);
        var workflowIds = ResolveWorkflowIds(workflows, workflowId);
        if (workflowIds.Count == 0)
        {
            return BuildSummary(Array.Empty<AnalyticsRun>());
        }

        var query = new WorkflowAnalyticsQuery(From: from, To: to, WorkflowId: workflowId);
        var (fromUtc, toUtc) = NormalizeDateRange(query);
        var sources = await executionLogService.GetTaskSummaryAsync(
                workflowIds,
                fromUtc,
                toUtc,
                cancellationToken)
            .ConfigureAwait(false);
        var runs = sources
            .Select(source => ToSummaryRun(source, workflows))
            .ToList();
        var activeRuns = runCoordinator.GetActiveRuns(adminUserId)
            .Where(run => workflowIds.Contains(run.WorkflowId))
            .Where(run => InDateRange(run.StartedAt.UtcDateTime, fromUtc, toUtc))
            .ToList();
        var correlationIds = runs
            .Select(run => run.CorrelationId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        runs.AddRange(activeRuns
            .Where(run => !correlationIds.Contains(BuildCorrelationId(run.WorkflowId, run.RunId)))
            .Select(run => ToLiveRun(run, workflows)));

        return BuildSummary(runs);
    }

    public static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var result)
            ? DateTime.SpecifyKind(result.Date, DateTimeKind.Utc)
            : null;
    }

    public static string NormalizeStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "running" or "success" or "failed" => status.Trim().ToLowerInvariant(),
        _ => string.Empty
    };

    private static (DateTime? FromUtc, DateTime? ToUtc) NormalizeDateRange(WorkflowAnalyticsQuery query)
    {
        var from = ParseDate(query.From);
        var to = ParseDate(query.To);
        return (from, to?.AddDays(1));
    }

    private static IReadOnlyList<int> ResolveWorkflowIds(
        IReadOnlyDictionary<int, string> workflows,
        int? workflowId)
    {
        if (workflowId.HasValue && workflowId.Value > 0)
        {
            return workflows.ContainsKey(workflowId.Value)
                ? new[] { workflowId.Value }
                : Array.Empty<int>();
        }

        return workflows.Keys.ToList();
    }

    private static bool InDateRange(DateTime value, DateTime? fromUtc, DateTime? toUtc) =>
        (!fromUtc.HasValue || value >= fromUtc.Value) &&
        (!toUtc.HasValue || value < toUtc.Value);

    private static AnalyticsRun ToRun(
        NeuCharWorkflowAnalyticsLog log,
        IReadOnlyDictionary<int, string> workflows)
    {
        var resources = ParseResources(log.ReplayEventsJson);
        return new AnalyticsRun(
            log.Id,
            log.WorkflowId,
            log.CorrelationId,
            workflows.TryGetValue(log.WorkflowId, out var workflowName)
                ? workflowName
                : string.IsNullOrWhiteSpace(log.WorkflowName) ? $"工作流 #{log.WorkflowId}" : log.WorkflowName,
            ToStatus(log.FinishedAt, log.Succeeded),
            ToUtcOffset(log.StartedAt),
            ToUtcOffset(log.FinishedAt),
            log.Succeeded,
            log.ResultSummary,
            log.Error,
            log.ReplaySnapshotHash != null && log.ReplayEventsJson != null,
            GetRunId(log.WorkflowId, log.CorrelationId),
            "history",
            resources);
    }

    private static AnalyticsRun ToSummaryRun(
        NeuCharWorkflowTaskSummarySource source,
        IReadOnlyDictionary<int, string> workflows) =>
        new(
            source.Id,
            source.WorkflowId,
            source.CorrelationId,
            workflows.TryGetValue(source.WorkflowId, out var workflowName)
                ? workflowName
                : $"工作流 #{source.WorkflowId}",
            ToStatus(source.FinishedAt, source.Succeeded),
            ToUtcOffset(source.StartedAt),
            ToUtcOffset(source.FinishedAt),
            source.Succeeded,
            null,
            null,
            false,
            GetRunId(source.WorkflowId, source.CorrelationId),
            "history",
            Array.Empty<ResourceUsage>());

    private static AnalyticsRun ToLiveRun(
        NeuCharWorkflowActiveRun run,
        IReadOnlyDictionary<int, string> workflows) =>
        new(
            null,
            run.WorkflowId,
            BuildCorrelationId(run.WorkflowId, run.RunId),
            workflows.TryGetValue(run.WorkflowId, out var workflowName)
                ? workflowName
                : $"工作流 #{run.WorkflowId}",
            "running",
            run.StartedAt,
            null,
            null,
            string.IsNullOrWhiteSpace(run.LastNodeName) && string.IsNullOrWhiteSpace(run.LastMessage)
                ? "等待工作流引擎开始执行。"
                : $"{run.LastNodeName ?? "工作流"}：{run.LastMessage ?? "正在执行"}",
            null,
            false,
            run.RunId,
            run.Source,
            Array.Empty<ResourceUsage>());

    private static WorkflowAnalyticsSummary BuildSummary(IEnumerable<AnalyticsRun> runs)
    {
        var items = runs.ToList();
        var successCount = items.Count(run => run.Status == "success");
        var failedCount = items.Count(run => run.Status == "failed");
        var finished = items.Where(run => run.FinishedAt.HasValue && run.StartedAt <= run.FinishedAt.Value)
            .Select(run => (run.FinishedAt!.Value - run.StartedAt).TotalSeconds)
            .ToList();
        var resourceUsages = items.SelectMany(run => run.Resources).ToList();

        return new WorkflowAnalyticsSummary(
            items.Count,
            items.Count(run => run.Status == "running"),
            successCount,
            failedCount,
            successCount + failedCount,
            successCount + failedCount == 0 ? null : successCount * 100d / (successCount + failedCount),
            finished.Count == 0 ? null : finished.Average(),
            finished.Count == 0 ? null : finished.Max(),
            resourceUsages.Count,
            resourceUsages.Select(ResourceKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    private static IReadOnlyList<WorkflowAnalyticsResourceSummary> BuildResourceRows(
        IEnumerable<AnalyticsRun> runs)
    {
        return runs
            .SelectMany(run => run.Resources.Select(resource => (run, resource)))
            .GroupBy(item => ResourceKey(item.resource), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First().resource;
                return new WorkflowAnalyticsResourceSummary(
                    first.Type,
                    ResourceTypeLabel(first.Type),
                    first.ProviderId,
                    first.ObjectId,
                    first.Name,
                    group.Count(),
                    group.Count(item => item.resource.Success),
                    group.Count(item => !item.resource.Success),
                    group.Select(item => item.run.WorkflowId).Distinct().Count(),
                    group.Max(item => item.resource.Timestamp));
            })
            .OrderByDescending(item => item.CallCount)
            .ThenBy(item => item.TypeLabel)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private static IReadOnlyList<WorkflowAnalyticsWorkflowSummary> BuildWorkflowRows(
        IEnumerable<AnalyticsRun> runs)
    {
        return runs
            .GroupBy(run => new { run.WorkflowId, run.WorkflowName })
            .Select(group =>
            {
                var success = group.Count(run => run.Status == "success");
                var failed = group.Count(run => run.Status == "failed");
                var durations = group
                    .Where(run => run.FinishedAt.HasValue && run.StartedAt <= run.FinishedAt.Value)
                    .Select(run => (run.FinishedAt!.Value - run.StartedAt).TotalSeconds)
                    .ToList();
                return new WorkflowAnalyticsWorkflowSummary(
                    group.Key.WorkflowId,
                    group.Key.WorkflowName,
                    group.Count(),
                    group.Count(run => run.Status == "running"),
                    success,
                    failed,
                    success + failed == 0 ? null : success * 100d / (success + failed),
                    durations.Count == 0 ? null : durations.Average(),
                    group.Max(run => run.StartedAt));
            })
            .OrderByDescending(item => item.RunCount)
            .ThenBy(item => item.WorkflowName)
            .ToList();
    }

    private static IReadOnlyList<WorkflowAnalyticsTrendPoint> BuildTrendRows(
        IEnumerable<AnalyticsRun> runs)
    {
        return runs
            .GroupBy(run => run.StartedAt.UtcDateTime.Date)
            .OrderBy(group => group.Key)
            .Select(group => new WorkflowAnalyticsTrendPoint(
                group.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                group.Count(),
                group.Count(run => run.Status == "running"),
                group.Count(run => run.Status == "success"),
                group.Count(run => run.Status == "failed")))
            .ToList();
    }

    private static WorkflowAnalyticsRunSummary ToRunSummary(AnalyticsRun run) =>
        new(
            run.ExecutionLogId,
            run.WorkflowId,
            run.WorkflowName,
            run.Status,
            run.Source,
            run.StartedAt,
            run.FinishedAt,
            run.FinishedAt.HasValue ? (run.FinishedAt.Value - run.StartedAt).TotalSeconds : null,
            run.Summary,
            run.Error,
            run.RunId,
            run.ReplayAvailable,
            run.Resources
                .GroupBy(resource => ResourceKey(resource), StringComparer.OrdinalIgnoreCase)
                .Select(group => new WorkflowAnalyticsRunResource(
                    group.First().Type,
                    ResourceTypeLabel(group.First().Type),
                    group.First().ProviderId,
                    group.First().ObjectId,
                    group.First().Name))
                .ToList());

    private static WorkflowAnalyticsOption ToResourceOption(WorkflowAnalyticsResourceSummary row) =>
        new(
            row.Type,
            row.ProviderId,
            row.ObjectId,
            row.Name,
            $"{row.TypeLabel} · {row.Name}");

    private static bool MatchesStatus(string status, string? filter)
    {
        var normalized = NormalizeStatus(filter);
        return string.IsNullOrWhiteSpace(normalized) ||
               string.Equals(status, normalized, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesKeyword(AnalyticsRun run, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        var value = keyword.Trim();
        return Contains(run.WorkflowName, value) ||
               Contains(run.Status, value) ||
               Contains(run.Summary, value) ||
               Contains(run.Error, value) ||
               run.Resources.Any(resource => Contains(resource.Name, value) ||
                                             Contains(resource.ObjectId, value));
    }

    private static bool MatchesResource(
        IReadOnlyList<ResourceUsage> resources,
        WorkflowAnalyticsQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.ResourceType) &&
            string.IsNullOrWhiteSpace(query.ResourceProviderId) &&
            string.IsNullOrWhiteSpace(query.ResourceId) &&
            string.IsNullOrWhiteSpace(query.ResourceName))
        {
            return true;
        }

        return resources.Any(resource =>
            (string.IsNullOrWhiteSpace(query.ResourceType) ||
             string.Equals(resource.Type, query.ResourceType, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(query.ResourceProviderId) ||
             string.Equals(resource.ProviderId, query.ResourceProviderId, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(query.ResourceId) ||
             string.Equals(resource.ObjectId, query.ResourceId, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(query.ResourceName) ||
             string.Equals(resource.Name, query.ResourceName, StringComparison.OrdinalIgnoreCase)));
    }

    private static IReadOnlyList<ResourceUsage> ParseResources(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<ResourceUsage>();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<ResourceUsage>();
            }

            var resources = new List<ResourceUsage>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var status = GetJsonString(item, "status");
                if (status is not ("success" or "failed") ||
                    !TryGetJsonProperty(item, "objectReference", out var reference) ||
                    reference.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var kind = GetJsonString(reference, "kind");
                var providerId = GetJsonString(reference, "providerId");
                var objectId = GetJsonString(reference, "objectId");
                var displayName = GetJsonString(reference, "displayName");
                if (string.IsNullOrWhiteSpace(kind) && string.IsNullOrWhiteSpace(objectId))
                {
                    continue;
                }

                resources.Add(new ResourceUsage(
                    kind ?? string.Empty,
                    providerId ?? string.Empty,
                    objectId,
                    string.IsNullOrWhiteSpace(displayName)
                        ? objectId ?? kind ?? "未知资源"
                        : displayName,
                    status == "success",
                    GetJsonDateTimeOffset(item, "timestamp") ?? DateTimeOffset.UnixEpoch));
            }

            return resources;
        }
        catch (JsonException)
        {
            return Array.Empty<ResourceUsage>();
        }
    }

    private static string? GetJsonString(JsonElement element, string propertyName) =>
        TryGetJsonProperty(element, propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static DateTimeOffset? GetJsonDateTimeOffset(JsonElement element, string propertyName)
    {
        var value = GetJsonString(element, propertyName);
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var result)
            ? result
            : null;
    }

    private static bool TryGetJsonProperty(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string ToStatus(DateTime? finishedAt, bool? succeeded) =>
        finishedAt == null ? "running" : succeeded == true ? "success" : "failed";

    private static string ResourceTypeLabel(string type) => type?.ToLowerInvariant() switch
    {
        "agent" => "Agent",
        "agent-group" => "AgentGroup",
        "a2a" => "A2A",
        "function" => "FunctionRender",
        "workflow" => "子工作流",
        "neubell" => "NeuBell",
        _ => string.IsNullOrWhiteSpace(type) ? "未知资源" : type
    };

    private static string ResourceKey(ResourceUsage resource) =>
        string.Join(
            "|",
            resource.Type ?? string.Empty,
            resource.ProviderId ?? string.Empty,
            resource.ObjectId ?? resource.Name ?? string.Empty);

    private static bool Contains(string? source, string value) =>
        !string.IsNullOrWhiteSpace(source) &&
        source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static Guid? GetRunId(int workflowId, string? correlationId)
    {
        var prefix = $"workflow-{workflowId}-run-";
        if (string.IsNullOrWhiteSpace(correlationId) ||
            !correlationId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Guid.TryParse(correlationId[prefix.Length..], out var runId) ? runId : null;
    }

    private static string BuildCorrelationId(int workflowId, Guid runId) =>
        $"workflow-{workflowId}-run-{runId:N}";

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? ToUtcOffset(DateTime? value) =>
        value.HasValue ? ToUtcOffset(value.Value) : null;

    private static WorkflowAnalyticsResult EmptyResult(WorkflowAnalyticsQuery query) =>
        new(
            BuildSummary(Array.Empty<AnalyticsRun>()),
            Array.Empty<WorkflowAnalyticsTrendPoint>(),
            Array.Empty<WorkflowAnalyticsResourceSummary>(),
            Array.Empty<WorkflowAnalyticsWorkflowSummary>(),
            Array.Empty<WorkflowAnalyticsRunSummary>(),
            Array.Empty<WorkflowAnalyticsOption>(),
            Array.Empty<WorkflowAnalyticsOption>(),
            Math.Max(1, query.Page),
            Math.Clamp(query.PageSize, 1, 100),
            false);

    private sealed record AnalyticsRun(
        int? ExecutionLogId,
        int WorkflowId,
        string CorrelationId,
        string WorkflowName,
        string Status,
        DateTimeOffset StartedAt,
        DateTimeOffset? FinishedAt,
        bool? Succeeded,
        string? Summary,
        string? Error,
        bool ReplayAvailable,
        Guid? RunId,
        string Source,
        IReadOnlyList<ResourceUsage> Resources);

    private sealed record ResourceUsage(
        string Type,
        string ProviderId,
        string? ObjectId,
        string Name,
        bool Success,
        DateTimeOffset Timestamp);
}

public sealed record WorkflowAnalyticsQuery(
    string? From = null,
    string? To = null,
    int? WorkflowId = null,
    string? Status = null,
    string? ResourceType = null,
    string? ResourceProviderId = null,
    string? ResourceId = null,
    string? ResourceName = null,
    string? Keyword = null,
    int Page = 1,
    int PageSize = 25);

public sealed record WorkflowAnalyticsResult(
    WorkflowAnalyticsSummary Summary,
    IReadOnlyList<WorkflowAnalyticsTrendPoint> Trend,
    IReadOnlyList<WorkflowAnalyticsResourceSummary> Resources,
    IReadOnlyList<WorkflowAnalyticsWorkflowSummary> Workflows,
    IReadOnlyList<WorkflowAnalyticsRunSummary> Runs,
    IReadOnlyList<WorkflowAnalyticsOption> ResourceOptions,
    IReadOnlyList<WorkflowAnalyticsOption> WorkflowOptions,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record WorkflowAnalyticsSummary(
    int TotalRuns,
    int RunningCount,
    int SuccessCount,
    int FailedCount,
    int FinishedCount,
    double? SuccessRate,
    double? AverageDurationSeconds,
    double? MaxDurationSeconds,
    int ResourceCallCount,
    int ResourceCount);

public sealed record WorkflowAnalyticsTrendPoint(
    string Date,
    int Total,
    int Running,
    int Success,
    int Failed);

public sealed record WorkflowAnalyticsResourceSummary(
    string Type,
    string TypeLabel,
    string ProviderId,
    string? ObjectId,
    string Name,
    int CallCount,
    int SuccessCount,
    int FailedCount,
    int WorkflowCount,
    DateTimeOffset LastUsedAt);

public sealed record WorkflowAnalyticsWorkflowSummary(
    int WorkflowId,
    string WorkflowName,
    int RunCount,
    int RunningCount,
    int SuccessCount,
    int FailedCount,
    double? SuccessRate,
    double? AverageDurationSeconds,
    DateTimeOffset LastRunAt);

public sealed record WorkflowAnalyticsRunSummary(
    int? ExecutionLogId,
    int WorkflowId,
    string WorkflowName,
    string Status,
    string Source,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    double? DurationSeconds,
    string? Summary,
    string? Error,
    Guid? RunId,
    bool ReplayAvailable,
    IReadOnlyList<WorkflowAnalyticsRunResource> Resources);

public sealed record WorkflowAnalyticsRunResource(
    string Type,
    string TypeLabel,
    string ProviderId,
    string? ObjectId,
    string Name);

public sealed record WorkflowAnalyticsOption(
    string Type,
    string ProviderId,
    string? ObjectId,
    string Name,
    string Label);
