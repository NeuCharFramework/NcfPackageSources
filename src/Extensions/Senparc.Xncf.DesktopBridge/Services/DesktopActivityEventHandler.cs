/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopActivityEventHandler.cs
    文件功能描述：将任意集成事件安全映射为桌面活动消息

    创建标识：Senparc - 20260725
----------------------------------------------------------------*/

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.DesktopBridge.Models;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopActivityEventHandler : IIntegrationEventHandler<IIntegrationEvent>
{
    private static readonly Regex WordBoundary = new("(?<=[a-z0-9])(?=[A-Z])", RegexOptions.Compiled);
    private readonly DesktopActivityHub _hub;
    private readonly ILogger<DesktopActivityEventHandler>? _logger;

    public DesktopActivityEventHandler(
        DesktopActivityHub hub,
        ILogger<DesktopActivityEventHandler>? logger = null)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task Handle(IIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            _hub.Publish(CreateMessage(@event));
        }
        catch (Exception ex)
        {
            // 桌面提示属于旁路能力，任何映射失败都不能影响原业务事件。
            _logger?.LogWarning(ex, "DesktopBridge failed to map event {EventType}", @event.GetType().FullName);
        }

        return Task.CompletedTask;
    }

    internal static DesktopActivityMessage CreateMessage(IIntegrationEvent @event)
    {
        var eventType = @event.GetType();
        var eventName = eventType.Name;
        var success = TryGetBoolean(@event, "Success");
        var errorMessage = TryGetString(@event, "ErrorMessage");
        var (state, isTerminal) = Classify(eventName, success, errorMessage);
        var correlationId = GetCorrelationId(@event);
        var detail = GetSafeDetail(@event, errorMessage);

        return new DesktopActivityMessage(
            Sequence: 0,
            ActivityId: correlationId,
            Source: GetSource(eventType),
            State: state,
            Title: Humanize(eventName),
            Detail: detail,
            Progress: TryGetProgress(@event),
            Time: new DateTimeOffset(DateTime.SpecifyKind(@event.CreationDate, DateTimeKind.Utc)),
            IsTerminal: isTerminal,
            ActionUrl: null);
    }

    private static (string State, bool IsTerminal) Classify(
        string eventName,
        bool? success,
        string? errorMessage)
    {
        if (success == false || !string.IsNullOrWhiteSpace(errorMessage) ||
            ContainsAny(eventName, "Error", "Failed", "Failure", "Exception"))
        {
            return ("Failed", true);
        }

        if (ContainsAny(eventName, "Cancel", "Cancelled", "Canceled"))
        {
            return ("Cancelled", true);
        }

        if (success == true || ContainsAny(eventName, "Response", "Completed", "Complete", "Finished", "Succeeded", "Success"))
        {
            return ("Succeeded", true);
        }

        if (ContainsAny(eventName, "Request", "Start", "Started", "Starting", "Progress", "Processing", "Running"))
        {
            return ("Working", false);
        }

        return ("Info", true);
    }

    private static string GetCorrelationId(IIntegrationEvent @event)
    {
        foreach (var propertyName in new[] { "RequestId", "CorrelationId", "StreamId", "ChatTaskId", "TaskId" })
        {
            var value = TryGetPropertyValue(@event, propertyName);
            if (value != null && !string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture)))
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture)!;
            }
        }

        return (@event.ParentEventId ?? @event.Id).ToString("N");
    }

    private static string GetSource(Type eventType)
    {
        const string marker = "Senparc.Xncf.";
        var ns = eventType.Namespace ?? eventType.Assembly.GetName().Name ?? "NCF";
        var markerIndex = ns.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex >= 0)
        {
            var remainder = ns[(markerIndex + marker.Length)..];
            var separatorIndex = remainder.IndexOf('.');
            return separatorIndex >= 0 ? remainder[..separatorIndex] : remainder;
        }

        return eventType.Assembly.GetName().Name ?? "NCF";
    }

    private static string? GetSafeDetail(IIntegrationEvent @event, string? errorMessage)
    {
        var detail = !string.IsNullOrWhiteSpace(errorMessage)
            ? errorMessage
            : (@event as IntegrationEvent)?.GetEventSummary();

        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        detail = detail.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return detail.Length <= 300 ? detail : detail[..300] + "…";
    }

    private static double? TryGetProgress(IIntegrationEvent @event)
    {
        foreach (var propertyName in new[] { "Progress", "Percentage", "Percent" })
        {
            var value = TryGetPropertyValue(@event, propertyName);
            if (value == null)
            {
                continue;
            }

            if (double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var progress))
            {
                return Math.Clamp(progress, 0, 100);
            }
        }

        return null;
    }

    private static bool? TryGetBoolean(IIntegrationEvent @event, string propertyName)
    {
        var value = TryGetPropertyValue(@event, propertyName);
        return value switch
        {
            bool boolean => boolean,
            _ when bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) => parsed,
            _ => null
        };
    }

    private static string? TryGetString(IIntegrationEvent @event, string propertyName)
    {
        return Convert.ToString(TryGetPropertyValue(@event, propertyName), CultureInfo.InvariantCulture);
    }

    private static object? TryGetPropertyValue(IIntegrationEvent @event, string propertyName)
    {
        try
        {
            return @event.GetType()
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                ?.GetValue(@event);
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static string Humanize(string eventName)
    {
        var withoutSuffix = eventName.EndsWith("Event", StringComparison.Ordinal)
            ? eventName[..^"Event".Length]
            : eventName;
        return WordBoundary.Replace(withoutSuffix, " ");
    }
}
