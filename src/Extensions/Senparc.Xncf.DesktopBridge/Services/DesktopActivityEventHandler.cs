/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopActivityEventHandler.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.1.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

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
    private readonly DesktopAuthorizedSyncHub _authorizedSyncHub;
    private readonly ILogger<DesktopActivityEventHandler>? _logger;

    /// <summary>
    /// Initializes the event handler that maps integration events to desktop activity or authorized-sync streams.
    /// </summary>
    /// <param name="hub">Unrestricted desktop activity hub.</param>
    /// <param name="authorizedSyncHub">Owner/policy-filtered synchronization hub.</param>
    /// <param name="logger">Optional mapping-failure logger.</param>
    public DesktopActivityEventHandler(
        DesktopActivityHub hub,
        DesktopAuthorizedSyncHub authorizedSyncHub,
        ILogger<DesktopActivityEventHandler>? logger = null)
    {
        _hub = hub;
        _authorizedSyncHub = authorizedSyncHub;
        _logger = logger;
    }

    /// <summary>
    /// Publishes an integration event to the appropriate desktop stream.
    /// Mapping failures are logged and do not fail event-bus processing.
    /// </summary>
    /// <param name="event">The integration event to map.</param>
    /// <param name="cancellationToken">The event-handler cancellation token.</param>
    /// <returns>A completed task after publishing.</returns>
    public Task Handle(IIntegrationEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            if (@event is IAuthorizedIntegrationSyncEvent authorizedSyncEvent)
            {
                _authorizedSyncHub.Publish(authorizedSyncEvent);
            }
            else
            {
                _hub.Publish(CreateMessage(@event));
            }
        }
        catch (Exception ex)
        {
            //Legacy comment translated to English.
            _logger?.LogWarning(ex, "DesktopBridge failed to map event {EventType}", @event.GetType().FullName);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Maps a generic integration event to a bounded desktop activity message,
    /// extracting status, progress, correlation, source, and safe detail fields.
    /// </summary>
    /// <param name="event">The integration event to map.</param>
    /// <returns>The activity message with sequence zero; the hub assigns its final sequence.</returns>
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

    /// <summary>
    /// Classifies an integration event into terminal/working activity state using
    /// success, error, and event-name signals.
    /// </summary>
    /// <param name="eventName">The integration event type name.</param>
    /// <param name="success">An optional success flag.</param>
    /// <param name="errorMessage">An optional error message.</param>
    /// <returns>The state label and terminal flag.</returns>
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

    /// <summary>
    /// Resolves a stable correlation identifier from common event identity properties.
    /// </summary>
    /// <param name="event">The integration event.</param>
    /// <returns>The first usable correlation value or the event/parent ID.</returns>
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

    /// <summary>
    /// Derives a concise source name from the event namespace or assembly.
    /// </summary>
    /// <param name="eventType">The integration event type.</param>
    /// <returns>The source label.</returns>
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

    /// <summary>
    /// Selects and bounds event detail text for desktop clients.
    /// </summary>
    /// <param name="event">The integration event.</param>
    /// <param name="errorMessage">The explicit error message, when any.</param>
    /// <returns>A single-line detail string limited to 300 characters.</returns>
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

    /// <summary>
    /// Reads and clamps a progress-like property from the event.
    /// </summary>
    /// <param name="event">The integration event.</param>
    /// <returns>A percentage from 0 to 100, or <see langword="null"/> when unavailable.</returns>
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

    /// <summary>
    /// Reads a Boolean event property through case-insensitive reflection.
    /// </summary>
    /// <param name="event">The integration event.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The parsed value, or <see langword="null"/> when absent/invalid.</returns>
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

    /// <summary>
    /// Reads an event property and converts it to invariant-culture text.
    /// </summary>
    /// <param name="event">The integration event.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property text, or <see langword="null"/>.</returns>
    private static string? TryGetString(IIntegrationEvent @event, string propertyName)
    {
        return Convert.ToString(TryGetPropertyValue(@event, propertyName), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reads a public event property through reflection without propagating getter failures.
    /// </summary>
    /// <param name="event">The integration event.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property value, or <see langword="null"/> when unavailable.</returns>
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

    /// <summary>
    /// Performs a case-insensitive substring check against candidate markers.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="candidates">The markers to find.</param>
    /// <returns><see langword="true"/> when any marker occurs.</returns>
    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Converts a PascalCase event name into a readable title and removes an Event suffix.
    /// </summary>
    /// <param name="eventName">The event type name.</param>
    /// <returns>A human-readable event title.</returns>
    private static string Humanize(string eventName)
    {
        var withoutSuffix = eventName.EndsWith("Event", StringComparison.Ordinal)
            ? eventName[..^"Event".Length]
            : eventName;
        return WordBoundary.Replace(withoutSuffix, " ");
    }
}
