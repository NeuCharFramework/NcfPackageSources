/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinWorkCheckinConnector.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.24.18 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Weixin.Work.AdvancedAPIs.OaDataOpen;
using Senparc.Weixin.Work.CommonAPIs;
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.WeixinManager;

namespace Senparc.Xncf.WeixinManager.Connectors;

/// <summary>
/// Provides a WeCom attendance connector backed by the Weixin Work API.
/// </summary>
public sealed class WeixinWorkCheckinConnector : IConnectorProvider
{
    public const string Key = "weixin.work.checkin";
    public const string GetRecordsAction = "get-checkin-records";

    /// <summary>
    /// Describes the WeCom credentials, attendance parameters, and supported action.
    /// </summary>
    /// <returns>The connector descriptor shown to configuration and tool callers.</returns>
    public ConnectorDescriptor Describe() => new()
    {
        Key = Key,
        Name = WeixinManagerResource.Get("Connector.WeCom.Name", "WeCom attendance"),
        Description = WeixinManagerResource.Get(
            "Connector.WeCom.Description",
            "Use a WeCom application's CorpId and Secret to retrieve attendance records. After configuration, it can be selected in Chat, Workflow, and Agent templates."),
        Icon = "fa fa-weixin",
        Fields =
        [
            new ConnectorFieldDescriptor
            {
                Key = "corpId",
                Label = WeixinManagerResource.Get("Connector.WeCom.CorpId.Label", "Corporate ID"),
                Description = WeixinManagerResource.Get(
                    "Connector.WeCom.CorpId.Description",
                    "The CorpId from the WeCom administration console."),
                Required = true,
                Placeholder = "ww1234567890abcdef"
            },
            new ConnectorFieldDescriptor
            {
                Key = "corpSecret",
                Label = WeixinManagerResource.Get("Connector.WeCom.CorpSecret.Label", "Application Secret"),
                Description = WeixinManagerResource.Get(
                    "Connector.WeCom.CorpSecret.Description",
                    "The Secret of a self-built application with attendance-data permission. Leave it empty while editing to keep the saved secret."),
                Kind = ConnectorFieldKind.Secret,
                Required = true,
                Placeholder = WeixinManagerResource.Get("Connector.WeCom.CorpSecret.Placeholder", "Application Secret")
            },
            new ConnectorFieldDescriptor
            {
                Key = "defaultCheckinType",
                Label = WeixinManagerResource.Get("Connector.WeCom.DefaultType.Label", "Default attendance type"),
                Description = WeixinManagerResource.Get(
                    "Connector.WeCom.DefaultType.Description",
                    "Used when the call does not specify another type."),
                Kind = ConnectorFieldKind.Select,
                Options =
                [
                    new ConnectorOption { Value = "3", Label = WeixinManagerResource.Get("Connector.WeCom.Type.All", "All attendance") },
                    new ConnectorOption { Value = "1", Label = WeixinManagerResource.Get("Connector.WeCom.Type.Work", "Work attendance") },
                    new ConnectorOption { Value = "2", Label = WeixinManagerResource.Get("Connector.WeCom.Type.Outside", "Off-site attendance") }
                ]
            }
        ],
        Actions =
        [
            new ConnectorActionDescriptor
            {
                Key = GetRecordsAction,
                Name = WeixinManagerResource.Get("Connector.WeCom.Action.GetRecords.Name", "Get attendance records"),
                Description = WeixinManagerResource.Get(
                    "Connector.WeCom.Action.GetRecords.Description",
                    "Retrieve WeCom attendance records by member account and time range, up to 30 days per request."),
                Parameters =
                [
                    new ConnectorParameterDescriptor
                    {
                        Key = "userIds",
                        Label = WeixinManagerResource.Get("Connector.WeCom.Parameter.UserIds.Label", "Member accounts"),
                        Description = WeixinManagerResource.Get(
                            "Connector.WeCom.Parameter.UserIds.Description",
                            "WeCom userids separated by commas or spaces."),
                        Required = true
                    },
                    new ConnectorParameterDescriptor
                    {
                        Key = "startTime",
                        Label = WeixinManagerResource.Get("Connector.WeCom.Parameter.StartTime.Label", "Start time"),
                        Description = WeixinManagerResource.Get(
                            "Connector.WeCom.Parameter.StartTime.Description",
                            "For example, 2026-11-01 or 2026-11-01 09:00. Leave empty to start at midnight today.")
                    },
                    new ConnectorParameterDescriptor
                    {
                        Key = "endTime",
                        Label = WeixinManagerResource.Get("Connector.WeCom.Parameter.EndTime.Label", "End time"),
                        Description = WeixinManagerResource.Get(
                            "Connector.WeCom.Parameter.EndTime.Description",
                            "Leave empty to use the current time.")
                    },
                    new ConnectorParameterDescriptor
                    {
                        Key = "checkinType",
                        Label = WeixinManagerResource.Get("Connector.WeCom.Parameter.Type.Label", "Attendance type"),
                        Description = WeixinManagerResource.Get(
                            "Connector.WeCom.Parameter.Type.Description",
                            "1 work, 2 off-site, 3 all.")
                    }
                ]
            }
        ]
    };

    /// <summary>
    /// Tests the configured CorpId/Secret by requesting a WeCom access token.
    /// </summary>
    /// <param name="configuration">The connector configuration.</param>
    /// <param name="cancellationToken">Token used to cancel the test.</param>
    /// <returns>A task containing credential validity and a localized diagnostic message.</returns>
    public async Task<ConnectorTestResult> TestAsync(
        IReadOnlyDictionary<string, string> configuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configuration.TryGetValue("corpId", out var corpId) || string.IsNullOrWhiteSpace(corpId)
            || !configuration.TryGetValue("corpSecret", out var corpSecret) || string.IsNullOrWhiteSpace(corpSecret))
        {
            return new ConnectorTestResult(false, WeixinManagerResource.Get(
                "Connector.WeCom.Error.MissingCredentials",
                "Enter the Corporate ID and application Secret first."));
        }

        try
        {
            var token = await CommonApi.GetTokenAsync(corpId.Trim(), corpSecret.Trim()).ConfigureAwait(false);
            var code = Convert.ToInt32(token.errcode, CultureInfo.InvariantCulture);
            Console.WriteLine($"[Connector] weixin.work.checkin test corpId={corpId.Trim()} errcode={code}");
            return code == 0
                ? new ConnectorTestResult(true, WeixinManagerResource.Get(
                    "Connector.WeCom.Test.Valid",
                    "The WeCom credential is valid and can retrieve attendance records."))
                : new ConnectorTestResult(false, WeixinManagerResource.Format(
                    "Connector.WeCom.Test.Rejected",
                    "WeCom rejected the credential: {0}",
                    token.errmsg));
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[Connector] weixin.work.checkin test failed: {exception.Message}");
            return new ConnectorTestResult(false, WeixinManagerResource.Get(
                "Connector.WeCom.Test.Unreachable",
                "Unable to connect to WeCom. Check the CorpId, Secret, and server network."));
        }
    }

    /// <summary>
    /// Retrieves bounded WeCom attendance records for validated member/time/type arguments.
    /// </summary>
    /// <param name="context">Connector configuration, action, arguments, and instance name.</param>
    /// <param name="cancellationToken">Token used to cancel invocation.</param>
    /// <returns>A task containing serialized attendance records or a localized failure.</returns>
    public async Task<ConnectorInvocationResult> InvokeAsync(
        ConnectorInvocationContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(context.ActionKey, GetRecordsAction, StringComparison.OrdinalIgnoreCase))
        {
            return ConnectorInvocationResult.Fail(WeixinManagerResource.Get(
                "Connector.WeCom.Error.UnsupportedAction",
                "The WeCom attendance connector currently only provides attendance record retrieval."));
        }

        var defaultType = ReadInt(context.Configuration, "defaultCheckinType", 3);
        if (!WeixinWorkCheckinArguments.TryCreate(context.Arguments, defaultType, out var query, out var error))
        {
            return ConnectorInvocationResult.Fail(error ?? "打卡查询参数无效。");
        }

        if (!context.Configuration.TryGetValue("corpId", out var corpId)
            || !context.Configuration.TryGetValue("corpSecret", out var corpSecret))
        {
            return ConnectorInvocationResult.Fail(WeixinManagerResource.Get(
                "Connector.WeCom.Error.MissingCredentials",
                "The connector is missing the Corporate ID or application Secret."));
        }

        try
        {
            var token = await CommonApi.GetTokenAsync(corpId.Trim(), corpSecret.Trim()).ConfigureAwait(false);
            if (Convert.ToInt32(token.errcode, CultureInfo.InvariantCulture) != 0 || string.IsNullOrWhiteSpace(token.access_token))
            {
                return ConnectorInvocationResult.Fail(WeixinManagerResource.Format(
                    "Connector.WeCom.Error.Token",
                    "Failed to obtain the WeCom access credential: {0}",
                    token.errmsg));
            }

            var result = await OaDataOpenApi.GetCheckinDataAsync(
                token.access_token,
                (OaDataOpenApi.OpenCheckinDataType)query.CheckinType,
                query.Start,
                query.End,
                query.UserIds,
                10000).ConfigureAwait(false);
            var code = Convert.ToInt32(result.errcode, CultureInfo.InvariantCulture);
            Console.WriteLine($"[Connector] weixin.work.checkin records corpId={corpId.Trim()} users={query.UserIds.Length} errcode={code}");
            if (code != 0)
            {
                return ConnectorInvocationResult.Fail(WeixinManagerResource.Format(
                    "Connector.WeCom.Error.Api",
                    "WeCom returned an error: {0}",
                    result.errmsg));
            }

            var checkinData = result.checkindata ?? [];
            var records = checkinData
                .Take(50)
                .Select(item => new
                {
                    userId = item.userid,
                    groupName = item.groupname,
                    checkinType = item.checkin_type,
                    exceptionType = item.exception_type,
                    checkinTime = FormatUnixTime(item.checkin_time),
                    location = item.location_title
                })
                .ToArray();
            var output = JsonSerializer.Serialize(new
            {
                connector = context.InstanceName,
                total = checkinData.Length,
                returned = records.Length,
                start = query.Start.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                end = query.End.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                records
            });
            return ConnectorInvocationResult.Ok(output);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[Connector] weixin.work.checkin invoke failed: {exception.Message}");
            return ConnectorInvocationResult.Fail(WeixinManagerResource.Format(
                "Connector.WeCom.Error.Invocation",
                "Failed to retrieve attendance records. Confirm that the application has attendance permission and that the member accounts are correct. Details: {0}",
                exception.Message));
        }
    }

    /// <summary>Reads an invariant-culture integer connector option.</summary>
    /// <param name="values">The option map.</param><param name="key">The option key.</param><param name="fallback">The fallback value.</param>
    private static int ReadInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
        => values.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    /// <summary>Converts a Unix timestamp to local display time.</summary>
    /// <param name="unixTime">The Unix seconds value.</param>
    /// <returns>A formatted local timestamp, or empty text for non-positive input.</returns>
    private static string FormatUnixTime(long unixTime)
    {
        if (unixTime <= 0)
        {
            return string.Empty;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}

public static class WeixinWorkCheckinArguments
{
    /// <summary>
    /// Parses and validates WeCom member IDs, time range, and attendance type.
    /// </summary>
    /// <param name="arguments">The connector arguments.</param>
    /// <param name="defaultCheckinType">The configured default attendance type.</param>
    /// <param name="query">Receives the normalized query.</param>
    /// <param name="error">Receives a localized validation error.</param>
    /// <returns><see langword="true"/> when the query is valid.</returns>
    public static bool TryCreate(
        IReadOnlyDictionary<string, string> arguments,
        int defaultCheckinType,
        out WeixinWorkCheckinQuery query,
        out string? error)
    {
        query = new WeixinWorkCheckinQuery(DateTime.Today, DateTime.Now, [], defaultCheckinType);
        var userIds = SplitUserIds(Read(arguments, "userIds"));
        if (userIds.Length == 0)
        {
            error = WeixinManagerResource.Get(
                "Connector.WeCom.Error.UserRequired",
                "Provide at least one WeCom member userid.");
            return false;
        }

        if (userIds.Length > 100)
        {
            error = WeixinManagerResource.Get(
                "Connector.WeCom.Error.UserLimit",
                "A single request can query at most 100 members.");
            return false;
        }

        var now = DateTime.Now;
        if (!TryReadTime(Read(arguments, "startTime"), DateTime.Today, out var start)
            || !TryReadTime(Read(arguments, "endTime"), now, out var end))
        {
            error = WeixinManagerResource.Get(
                "Connector.WeCom.Error.InvalidTime",
                "The time format is not recognized. Use 2026-11-01 or 2026-11-01 09:00.");
            return false;
        }

        if (end < start)
        {
            error = WeixinManagerResource.Get(
                "Connector.WeCom.Error.TimeOrder",
                "The end time must be later than the start time.");
            return false;
        }

        if (end - start > TimeSpan.FromDays(30))
        {
            error = WeixinManagerResource.Get(
                "Connector.WeCom.Error.TimeRange",
                "WeCom allows at most 30 days of attendance records per request.");
            return false;
        }

        var checkinType = defaultCheckinType;
        var rawType = Read(arguments, "checkinType");
        if (!string.IsNullOrWhiteSpace(rawType))
        {
            if (!int.TryParse(rawType, NumberStyles.Integer, CultureInfo.InvariantCulture, out checkinType)
                || checkinType is < 1 or > 3)
            {
                error = WeixinManagerResource.Get(
                    "Connector.WeCom.Error.InvalidType",
                    "The attendance type must be 1 (work), 2 (off-site), or 3 (all).");
                return false;
            }
        }
        else if (checkinType is < 1 or > 3)
        {
            checkinType = 3;
        }

        query = new WeixinWorkCheckinQuery(start, end, userIds, checkinType);
        error = null;
        return true;
    }

    /// <summary>Splits and de-duplicates comma/whitespace-delimited WeCom user IDs.</summary>
    /// <param name="raw">The raw user ID input.</param>
    /// <returns>Distinct non-empty user IDs.</returns>
    public static string[] SplitUserIds(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split([',', '，', ';', '；', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    /// <summary>Reads a connector argument or returns empty text.</summary>
    /// <param name="arguments">The argument map.</param><param name="key">The argument key.</param>
    private static string Read(IReadOnlyDictionary<string, string> arguments, string key)
        => arguments.TryGetValue(key, out var value) ? value : string.Empty;

    /// <summary>Parses a culture-aware date/time value with a fallback.</summary>
    /// <param name="raw">The raw date/time text.</param>
    /// <param name="fallback">The fallback value for blank input.</param>
    /// <param name="value">Receives the parsed value.</param>
    /// <returns><see langword="true"/> when parsing succeeds.</returns>
    private static bool TryReadTime(string raw, DateTime fallback, out DateTime value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = fallback;
            return true;
        }

        return DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out value)
               || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out value);
    }
}

public sealed record WeixinWorkCheckinQuery(DateTime Start, DateTime End, string[] UserIds, int CheckinType);
