/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawEvents.cs
    File description: WeixinClawEvents.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw media handling and UI improvements

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed record WeixinClawMessageReceivedContext(
    int AccountId,
    string AccountName,
    string MessageId,
    long Sequence,
    string FromUserId,
    string ToUserId,
    string GroupId,
    string ContextToken,
    string RunId,
    string Text,
    DateTimeOffset CreatedAt);

public interface IWeixinClawMessageHandler
{
    Task HandleAsync(
        WeixinClawMessageReceivedContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public interface IWeixinClawMessageSender
{
    Task SendTextAsync(
        int accountId,
        string toUserId,
        string text,
        string contextToken = null,
        string runId = null,
        CancellationToken cancellationToken = default);
}
