/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: NeuCharWorkflowChatSessionService.cs
    File description: In-process Chat trigger sessions, message history, and run-state tracking

    Created by: Senparc - 20260909
    Creation description: v0.4.0 add the Chat trigger

    Modified by: Senparc - 20260913
    Change description: v0.4.0 support participant SHA256 digests and historical message injection

    Modified by: Senparc - 20260915
    Change description: v0.4.0 improve Chat trigger message persistence and recovery

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// A message in a Chat session. Roles are user, assistant, or error; content
/// is truncated to a safe length on the server.
/// </summary>
public sealed record WorkflowChatSessionMessage(
    string Role,
    string Content,
    DateTimeOffset Timestamp);

/// <summary>
/// Session storage for the Chat trigger. Sessions live only in the current
/// Host process, consistent with the run coordinator, and use workflow plus
/// participant as the key: authenticated users use user:{adminUserId}, while
/// guests use guest:{sessionToken}. Each session tracks one run at a time.
/// </summary>
public sealed class NeuCharWorkflowChatSessionService
{
    public const string RoleUser = "user";
    public const string RoleAssistant = "assistant";
    public const string RoleError = "error";

    /// <summary>
    /// Compute the participant-key hash (the SHA256 hexadecimal digest after
    /// trimming whitespace). Session keys and database persistence use the same
    /// algorithm so restart recovery can find the original session.
    /// </summary>
    public static string HashParticipant(string participantKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(participantKey.Trim())));

    private const int Capacity = 500;
    private const int MaxMessagesPerSession = 200;
    private const int MaxContentLength = 8_000;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new(StringComparer.Ordinal);

    private ChatSession GetOrCreate(int workflowId, string participantKey)
    {
        Validate(workflowId, participantKey);
        Cleanup();
        var key = BuildKey(workflowId, participantKey);
        return _sessions.GetOrAdd(key, _ => new ChatSession(workflowId, participantKey));
    }

    private ChatSession? Get(int workflowId, string participantKey)
    {
        if (workflowId <= 0 || string.IsNullOrWhiteSpace(participantKey))
        {
            return null;
        }
        _sessions.TryGetValue(BuildKey(workflowId, participantKey), out var session);
        return session;
    }

    /// <summary>
    /// Append a message and return the current snapshot; discard the oldest
    /// messages when the limit is exceeded.
    /// </summary>
    public IReadOnlyList<WorkflowChatSessionMessage> AddMessage(
        int workflowId,
        string participantKey,
        string role,
        string content)
    {
        var session = GetOrCreate(workflowId, participantKey);
        var normalizedRole = role?.Trim().ToLowerInvariant() switch
        {
            "user" => RoleUser,
            "assistant" => RoleAssistant,
            "error" => RoleError,
            _ => throw new InvalidOperationException("Chat 消息角色无效。")
        };
        var normalizedContent = (content ?? string.Empty).Trim();
        if (normalizedContent.Length > MaxContentLength)
        {
            normalizedContent = normalizedContent[..MaxContentLength];
        }
        var message = new WorkflowChatSessionMessage(normalizedRole, normalizedContent, DateTimeOffset.UtcNow);
        lock (session.SyncRoot)
        {
            session.LastActiveAt = DateTimeOffset.UtcNow;
            session.Messages.Add(message);
            while (session.Messages.Count > MaxMessagesPerSession)
            {
                session.Messages.RemoveAt(0);
            }
            return session.Messages.ToArray();
        }
    }

    public IReadOnlyList<WorkflowChatSessionMessage> GetMessages(int workflowId, string participantKey)
    {
        var session = Get(workflowId, participantKey);
        if (session == null)
        {
            return Array.Empty<WorkflowChatSessionMessage>();
        }
        lock (session.SyncRoot)
        {
            session.LastActiveAt = DateTimeOffset.UtcNow;
            return session.Messages.ToArray();
        }
    }

    /// <summary>
    /// Record the run currently associated with the session. Only one run may
    /// be active for a session.
    /// </summary>
    public void SetPendingRun(int workflowId, string participantKey, Guid runId)
    {
        var session = GetOrCreate(workflowId, participantKey);
        lock (session.SyncRoot)
        {
            session.LastActiveAt = DateTimeOffset.UtcNow;
            session.PendingRunId = runId;
        }
    }

    public bool HasPendingRun(int workflowId, string participantKey)
    {
        var session = Get(workflowId, participantKey);
        if (session == null)
        {
            return false;
        }
        lock (session.SyncRoot)
        {
            return session.PendingRunId.HasValue;
        }
    }

    /// <summary>
    /// Return the run associated with the session so polling can resume after
    /// a page refresh.
    /// </summary>
    public Guid? GetPendingRun(int workflowId, string participantKey)
    {
        var session = Get(workflowId, participantKey);
        if (session == null)
        {
            return null;
        }
        lock (session.SyncRoot)
        {
            return session.PendingRunId;
        }
    }

    /// <summary>
    /// Clear the association only while the session still points to this run.
    /// False means another run has taken over or the session was reset.
    /// </summary>
    public bool ClearPendingRun(int workflowId, string participantKey, Guid runId)
    {
        var session = Get(workflowId, participantKey);
        if (session == null)
        {
            return false;
        }
        lock (session.SyncRoot)
        {
            if (!session.PendingRunId.HasValue || session.PendingRunId.Value != runId)
            {
                return false;
            }
            session.LastActiveAt = DateTimeOffset.UtcNow;
            session.PendingRunId = null;
            return true;
        }
    }

    public bool HasPendingRunMatching(int workflowId, string participantKey, Guid runId)
    {
        var session = Get(workflowId, participantKey);
        if (session == null)
        {
            return false;
        }
        lock (session.SyncRoot)
        {
            return session.PendingRunId == runId;
        }
    }

    /// <summary>
    /// Clear session messages and the active-run association. The workflow run
    /// itself is not aborted.
    /// </summary>
    public void Reset(int workflowId, string participantKey)
    {
        var session = GetOrCreate(workflowId, participantKey);
        lock (session.SyncRoot)
        {
            session.Messages.Clear();
            session.PendingRunId = null;
            session.LastActiveAt = DateTimeOffset.UtcNow;
        }
    }

    private void Validate(int workflowId, string participantKey)
    {
        if (workflowId <= 0 || string.IsNullOrWhiteSpace(participantKey) || participantKey.Trim().Length > 256)
        {
            throw new InvalidOperationException("Chat 会话标识无效。");
        }
        if (_sessions.Count >= Capacity)
        {
            throw new InvalidOperationException("当前活跃的 Chat 会话过多，请稍后再试。");
        }
    }

    private void Cleanup()
    {
        var threshold = DateTimeOffset.UtcNow - SessionTtl;
        foreach (var item in _sessions.Values)
        {
            lock (item.SyncRoot)
            {
                if (item.LastActiveAt < threshold)
                {
                    item.Messages.Clear();
                    item.PendingRunId = null;
                    _sessions.TryRemove(item.Key, out _);
                }
            }
        }
    }

    private static string BuildKey(int workflowId, string participantKey) =>
        $"{workflowId}:{HashParticipant(participantKey)}";

    /// <summary>
    /// Restore database history into the in-memory session after a Host restart.
    /// Write only when the session has no messages, avoiding replacement of
    /// messages created in the current process.
    /// </summary>
    public void SeedMessages(
        int workflowId,
        string participantKey,
        IReadOnlyList<WorkflowChatSessionMessage> messages)
    {
        if (workflowId <= 0 || string.IsNullOrWhiteSpace(participantKey) || messages == null || messages.Count == 0)
        {
            return;
        }
        var session = GetOrCreate(workflowId, participantKey);
        lock (session.SyncRoot)
        {
            if (session.Messages.Count > 0)
            {
                return;
            }
            foreach (var message in messages)
            {
                session.Messages.Add(message);
                while (session.Messages.Count > MaxMessagesPerSession)
                {
                    session.Messages.RemoveAt(0);
                }
            }
            session.LastActiveAt = DateTimeOffset.UtcNow;
        }
    }

    private sealed class ChatSession
    {
        public ChatSession(int workflowId, string participantKey)
        {
            Key = BuildKey(workflowId, participantKey);
            WorkflowId = workflowId;
            ParticipantKey = participantKey.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            LastActiveAt = CreatedAt;
        }

        public string Key { get; }
        public int WorkflowId { get; }
        public string ParticipantKey { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset LastActiveAt { get; internal set; }
        public Guid? PendingRunId { get; internal set; }
        public List<WorkflowChatSessionMessage> Messages { get; } = new();
        public object SyncRoot { get; } = new();
    }
}
