/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowChatSessionService.cs
    文件功能描述：Chat 触发器的进程内会话、消息历史与运行状态跟踪

    创建标识：Senparc - 20260909
    创建描述：v0.4.0 新增 Chat 触发器

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Chat 会话中的一条消息。角色只有 user、assistant 与 error 三种，内容已被服务端截断到安全长度。
/// </summary>
public sealed record WorkflowChatSessionMessage(
    string Role,
    string Content,
    DateTimeOffset Timestamp);

/// <summary>
/// Chat 触发器的会话存储。会话只存在于当前 Host 进程（与运行协调器一致），
/// 以“工作流 + 参与者”为键：登录用户使用 user:{adminUserId}，访客使用 guest:{会话令牌}。
/// 每个会话同一时间只跟踪一个运行，避免并发发送导致状态互相覆盖。
/// </summary>
public sealed class NeuCharWorkflowChatSessionService
{
    public const string RoleUser = "user";
    public const string RoleAssistant = "assistant";
    public const string RoleError = "error";

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
    /// 追加一条消息并返回当前消息快照；超出上限时丢弃最早的消息。
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
    /// 记录会话当前关联的运行。同一会话只允许一个进行中的运行。
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
    /// 返回会话当前关联的运行标识，用于页面刷新后恢复轮询。
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
    /// 仅当会话仍然指向该运行时清除关联；返回 false 表示会话已被其他运行接管或重置。
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
    /// 清空会话消息并取消进行中的运行关联。工作流本身的运行不会被中止。
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
        $"{workflowId}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(participantKey.Trim())))}";

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
