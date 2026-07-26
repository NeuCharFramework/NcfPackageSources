/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatProtocol.cs
    文件功能描述：桌面 Admin Chat API 与界面协议模型

    创建标识：Senparc - 20260726
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace NcfDesktopApp.GUI.Models;

public sealed record AdminChatAuthentication(
    string UserName,
    string AccessToken,
    DateTimeOffset? ExpiresUtc);

public sealed record AdminChatSessionSummary(
    int Id,
    string Title,
    DateTime LastMessageTime)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Title) ? $"会话 {Id}" : Title;
}

public sealed record AdminChatMessage(
    int Id,
    int SessionId,
    int RoleType,
    string Content,
    int Sequence,
    DateTime AddTime,
    string? ModelIdentifier)
{
    public bool IsUser => RoleType == 0;

    public bool IsAgent => !IsUser;

    public string SenderName => IsUser ? "我" : "NCF Agent";

    public string SenderColor => IsUser ? "#2563EB" : "#7C3AED";

    public string DisplayTime => AddTime == default ? string.Empty : AddTime.ToLocalTime().ToString("HH:mm");
}

internal sealed class AppResponseEnvelope<T>
{
    public bool? Success { get; set; }

    public string? ErrorMessage { get; set; }

    public T? Data { get; set; }
}

internal sealed class AdminLoginData
{
    public string? UserName { get; set; }

    public string? Token { get; set; }

    public DateTimeOffset? TokenExpiresUtc { get; set; }
}

internal sealed class AdminChatSessionListData
{
    public List<AdminChatSessionSummary> Sessions { get; set; } = new();
}

internal sealed class AdminChatSessionDetailData
{
    public AdminChatSessionDetail? Session { get; set; }
}

internal sealed class AdminChatSessionDetail
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<AdminChatMessage> Messages { get; set; } = new();
}

internal sealed class AdminChatCreateSessionData
{
    public int SessionId { get; set; }

    public string Title { get; set; } = string.Empty;
}

internal sealed class AdminChatSendMessageData
{
    public AdminChatMessage? UserMessage { get; set; }

    public AdminChatMessage? AssistantMessage { get; set; }
}

public sealed class AdminChatApiException : Exception
{
    public AdminChatApiException(string message, bool isAuthenticationFailure = false)
        : base(message)
    {
        IsAuthenticationFailure = isAuthenticationFailure;
    }

    public bool IsAuthenticationFailure { get; }
}
