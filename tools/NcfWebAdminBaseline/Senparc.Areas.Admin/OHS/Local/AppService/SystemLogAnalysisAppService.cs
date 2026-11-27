/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SystemLogAnalysisAppService.cs
    File description: Provides s ysteml oga nalysisa pps ervice functionality.

    Created by: Senparc - 20261123

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.CO2NET;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService;

[AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public class SystemLogAnalysisAppService(IServiceProvider serviceProvider) : LocalAppServiceBase(serviceProvider)
{
    private const int MaxSearchItems = 30;
    private const int MaxEvidenceCharacters = 8000;

    [FunctionRender(
        typeof(AdminResource),
        "Trace.AI.OverviewName",
        "Trace.AI.OverviewDescription",
        typeof(Register))]
    /// <summary>
    /// Produces a redacted, bounded overview of one SenparcTrace log date, including
    /// counts, time window, common exception signatures, and active threads.
    /// </summary>
    /// <param name="request">The optional date selector; the current local date is used when absent.</param>
    /// <returns>A string response containing the serialized overview and tool log.</returns>
    public Task<StringAppResponse> GetSystemLogOverview(SystemLogOverviewRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            var date = NormalizeDate(request?.Date);
            var scan = await SenparcTraceHelper.GetLogScanResultAsync(ServiceProvider, date);
            var logs = await SenparcTraceHelper.GetAllLogsAsync(ServiceProvider, date);
            var result = new
            {
                date,
                scan.TotalLogCount,
                scan.ExceptionLogCount,
                scan.NormalLogCount,
                TypeCounts = scan.TypeCounts,
                TimeWindow = new
                {
                    From = logs.LastOrDefault()?.DateTime,
                    To = logs.FirstOrDefault()?.DateTime
                },
                TopExceptions = logs
                    .Where(item => item.IsException)
                    .GroupBy(item => item.Result?.ExceptionMessage ?? item.Title ?? "Unknown")
                    .Select(group => new
                    {
                        Signature = Truncate(
                            SenparcTraceHelper.RedactSensitiveData(group.Key),
                            300),
                        Count = group.Count(),
                        FirstLine = group.Min(item => item.Line),
                        LastLine = group.Max(item => item.Line)
                    })
                    .OrderByDescending(item => item.Count)
                    .Take(10),
                TopThreads = logs
                    .GroupBy(item => item.ThreadId)
                    .Select(group => new { ThreadId = group.Key, Count = group.Count() })
                    .OrderByDescending(item => item.Count)
                    .Take(10),
                Guidance = "Use SearchSystemLogs for a focused subset. Use GetSystemLogEvidence only for relevant line numbers. All values are already redacted."
            };

            logger.Append(JsonSerializer.Serialize(result));
            await Task.CompletedTask;
            return logger.GetLogs();
        });
    }

    [FunctionRender(
        typeof(AdminResource),
        "Trace.AI.SearchName",
        "Trace.AI.SearchDescription",
        typeof(Register))]
    /// <summary>
    /// Searches one SenparcTrace log date using bounded page, keyword, type, exception,
    /// and thread filters.
    /// </summary>
    /// <param name="request">The date and optional filters controlling the search.</param>
    /// <returns>A string response containing the serialized search result and tool log.</returns>
    public Task<StringAppResponse> SearchSystemLogs(SystemLogSearchRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            var date = NormalizeDate(request?.Date);
            var result = await SenparcTraceHelper.QueryLogsAsync(
                ServiceProvider,
                date,
                pageIndex: Math.Max(1, request?.PageIndex ?? 1),
                pageSize: Math.Clamp(request?.PageSize ?? 20, 1, MaxSearchItems),
                keyword: request?.Keyword,
                traceType: request?.TraceType,
                isException: request?.ExceptionOnly,
                threadId: request?.ThreadId);

            logger.Append(JsonSerializer.Serialize(new
            {
                result.Date,
                result.Total,
                Returned = result.Items.Count,
                result.ExceptionCount,
                result.TypeCounts,
                result.Items
            }));
            await Task.CompletedTask;
            return logger.GetLogs();
        });
    }

    [FunctionRender(
        typeof(AdminResource),
        "Trace.AI.EvidenceName",
        "Trace.AI.EvidenceDescription",
        typeof(Register))]
    /// <summary>
    /// Retrieves one redacted trace record by line number and truncates its detailed result
    /// to the configured evidence limit.
    /// </summary>
    /// <param name="request">The date, line number, and maximum evidence length.</param>
    /// <returns>A string response containing the serialized evidence record and tool log.</returns>
    public Task<StringAppResponse> GetSystemLogEvidence(SystemLogEvidenceRequest request)
    {
        return this.GetStringResponseAsync(async (_, logger) =>
        {
            var date = NormalizeDate(request?.Date);
            var detail = await SenparcTraceHelper.GetLogDetailAsync(
                ServiceProvider,
                date,
                request?.Line ?? 0);
            var maxCharacters = Math.Clamp(request?.MaxCharacters ?? 4000, 500, MaxEvidenceCharacters);
            var result = new
            {
                detail.Line,
                detail.DateTime,
                detail.ThreadId,
                detail.SenparcTraceType,
                detail.IsException,
                detail.Title,
                detail.Url,
                detail.ExceptionMessage,
                TotalResult = Truncate(detail.TotalResult, maxCharacters),
                Truncated = (detail.TotalResult?.Length ?? 0) > maxCharacters
            };

            logger.Append(JsonSerializer.Serialize(result));
            await Task.CompletedTask;
            return logger.GetLogs();
        });
    }

    /// <summary>
    /// Normalizes an optional log date to the local <c>yyyyMMdd</c> representation.
    /// </summary>
    /// <param name="date">The requested date text.</param>
    /// <returns>The trimmed date or the current local date.</returns>
    private static string NormalizeDate(string date)
    {
        return string.IsNullOrWhiteSpace(date)
            ? SystemTime.Now.ToString("yyyyMMdd")
            : date.Trim();
    }

    /// <summary>
    /// Limits text to the requested character count and appends a truncation marker.
    /// </summary>
    /// <param name="content">The source text.</param>
    /// <param name="maxCharacters">The maximum number of source characters to retain.</param>
    /// <returns>The original or truncated text.</returns>
    private static string Truncate(string content, int maxCharacters)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxCharacters)
        {
            return content;
        }

        return content[..maxCharacters] + "\n...[truncated]";
    }
}

public class SystemLogOverviewRequest
{
    public string Date { get; set; }
}

public class SystemLogSearchRequest
{
    public string Date { get; set; }
    public string Keyword { get; set; }
    public SenparcTraceType? TraceType { get; set; }
    public bool? ExceptionOnly { get; set; }
    public int? ThreadId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SystemLogEvidenceRequest
{
    public string Date { get; set; }
    public int Line { get; set; }
    public int MaxCharacters { get; set; } = 4000;
}
