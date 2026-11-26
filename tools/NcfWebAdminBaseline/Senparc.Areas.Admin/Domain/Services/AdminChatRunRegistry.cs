/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatRunRegistry.cs
    File description: Related implementation.

    Created by: Senparc - 20261107

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class AdminChatRunRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan FinishedVisibility = TimeSpan.FromSeconds(20);
    private readonly ConcurrentDictionary<int, AdminChatRun> _runs = new();
    private readonly ConcurrentDictionary<int, DateTimeOffset> _finishedAt = new();

    /// <summary>
    /// Initializes the in-memory run registry and cancels active runs when the host stops.
    /// </summary>
    /// <param name="lifetime">The host lifetime used for shutdown cancellation.</param>
    public AdminChatRunRegistry(IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStopping.Register(() =>
        {
            foreach (var run in _runs.Values)
                run.Cancellation.Cancel();
        });
    }

    /// <summary>
    /// Attempts to register one active run per session/user pair.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="userId">The owning administrator identifier.</param>
    /// <returns>The new run, or <see langword="null"/> when the session already has a run.</returns>
    public AdminChatRun TryBegin(int sessionId, int userId)
    {
        var run = new AdminChatRun(sessionId, userId);
        if (!_runs.TryAdd(sessionId, run))
        {
            run.Cancellation.Dispose();
            return null;
        }

        _finishedAt.TryRemove(sessionId, out _);
        return run;
    }

    /// <summary>
    /// Finds an active run only when it belongs to the requested administrator.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="userId">The owner identifier.</param>
    /// <returns>The active run, or <see langword="null"/>.</returns>
    public AdminChatRun Find(int sessionId, int userId)
    {
        if (sessionId <= 0 || userId <= 0) return null;
        return _runs.TryGetValue(sessionId, out var run) && run.UserId == userId ? run : null;
    }

    /// <summary>
    /// Requests cancellation of an owner-scoped active run.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="userId">The owner identifier.</param>
    /// <returns><see langword="true"/> when an active run accepted the cancellation request.</returns>
    public bool Cancel(int sessionId, int userId)
    {
        var run = Find(sessionId, userId);
        if (run == null) return false;
        try
        {
            run.Cancellation.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns current text/status for an owner-scoped run, retaining a short
    /// just-finished indication after removal.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="userId">The owner identifier.</param>
    /// <returns>The current run status.</returns>
    public AdminChatRunStatus GetStatus(int sessionId, int userId)
    {
        var run = Find(sessionId, userId);
        if (run == null)
        {
            var justFinished = _finishedAt.TryGetValue(sessionId, out var finishedAt)
                && DateTimeOffset.UtcNow - finishedAt <= FinishedVisibility;
            return new AdminChatRunStatus(false, justFinished, "", "", "");
        }

        lock (run.Gate)
        {
            return new AdminChatRunStatus(true, false, run.AssistantText, run.ThinkingText, run.StatusText);
        }
    }

    /// <summary>
    /// Subscribes to a run's event channel and captures an initial state snapshot.
    /// </summary>
    /// <param name="run">The active run to observe.</param>
    /// <returns>A disposable listener containing the channel and initial snapshot JSON.</returns>
    public AdminChatRunListener Listen(AdminChatRun run)
    {
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        string snapshot;
        lock (run.Gate)
        {
            run.Subscribers.Add(channel);
            snapshot = JsonSerializer.Serialize(new
            {
                text = run.AssistantText,
                thinking = run.ThinkingText,
                status = string.IsNullOrEmpty(run.StatusText) ? "任务仍在执行…" : run.StatusText
            }, JsonOptions);
        }

        return new AdminChatRunListener(channel, snapshot, () =>
        {
            lock (run.Gate) run.Subscribers.Remove(channel);
            channel.Writer.TryComplete();
        });
    }

    /// <summary>
    /// Mutates run state under its gate and publishes an event frame to subscribers.
    /// </summary>
    /// <param name="run">The active run.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="json">The serialized event payload.</param>
    /// <param name="mutate">Optional state mutation performed before publication.</param>
    public void Publish(AdminChatRun run, string eventName, string json, Action<AdminChatRun> mutate = null)
    {
        Channel<string>[] targets;
        lock (run.Gate)
        {
            mutate?.Invoke(run);
            targets = run.Subscribers.ToArray();
        }

        var frame = eventName + "\n" + json;
        foreach (var target in targets)
            target.Writer.TryWrite(frame);
    }

    /// <summary>
    /// Closes subscribers, removes the run, records a short just-finished window,
    /// and disposes the run cancellation source.
    /// </summary>
    /// <param name="run">The run to finish.</param>
    public void Finish(AdminChatRun run)
    {
        if (run == null) return;
        Channel<string>[] targets;
        lock (run.Gate)
        {
            if (run.Closed) return;
            run.Closed = true;
            targets = run.Subscribers.ToArray();
        }

        _runs.TryRemove(run.SessionId, out _);
        _finishedAt[run.SessionId] = DateTimeOffset.UtcNow;
        foreach (var target in targets)
            target.Writer.TryComplete();
        run.Cancellation.Dispose();
    }
}

public sealed class AdminChatRun
{
    /// <summary>
    /// Creates an active in-memory run for one owner-scoped chat session.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="userId">The owning administrator identifier.</param>
    public AdminChatRun(int sessionId, int userId)
    {
        SessionId = sessionId;
        UserId = userId;
        Cancellation = new CancellationTokenSource();
    }

    public int SessionId { get; }
    public int UserId { get; }
    public CancellationTokenSource Cancellation { get; }
    public object Gate { get; } = new();
    public List<Channel<string>> Subscribers { get; } = new();
    public string AssistantText { get; set; } = "";
    public string ThinkingText { get; set; } = "";
    public string StatusText { get; set; } = "";
    public bool Closed { get; set; }
    public CancellationToken Token => Cancellation.Token;
}

public sealed record AdminChatRunStatus(
    bool Running, bool JustFinished, string Text, string Thinking, string Status);

public sealed class AdminChatRunListener : IDisposable
{
    private readonly Action _dispose;

    /// <summary>
    /// Initializes a run listener with its event channel, initial snapshot, and cleanup callback.
    /// </summary>
    /// <param name="channel">The channel receiving serialized event frames.</param>
    /// <param name="snapshotJson">The initial run-state snapshot JSON.</param>
    /// <param name="dispose">The callback that unregisters and completes the channel.</param>
    public AdminChatRunListener(Channel<string> channel, string snapshotJson, Action dispose)
    {
        Channel = channel;
        SnapshotJson = snapshotJson;
        _dispose = dispose;
    }

    public Channel<string> Channel { get; }
    public string SnapshotJson { get; }
    /// <inheritdoc/>
    public void Dispose() => _dispose();
}
