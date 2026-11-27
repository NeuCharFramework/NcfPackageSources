/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawEvents.cs
    File description: WeixinClawEvents.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw media handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

/// <summary>
/// Carries one normalized inbound Weixin Claw message to application handlers.
/// </summary>
/// <param name="AccountId">The local Weixin account identifier.</param>
/// <param name="AccountName">The local account display name.</param>
/// <param name="MessageId">The provider message identifier.</param>
/// <param name="Sequence">The provider update sequence.</param>
/// <param name="FromUserId">The sender identifier.</param>
/// <param name="ToUserId">The recipient identifier.</param>
/// <param name="GroupId">The group identifier, when the message is from a group.</param>
/// <param name="ContextToken">The conversation context token used for replies.</param>
/// <param name="RunId">The provider run identifier, when supplied.</param>
/// <param name="Text">The normalized text content.</param>
/// <param name="CreatedAt">The UTC message creation timestamp.</param>
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

/// <summary>
/// Handles normalized inbound Weixin Claw messages.
/// </summary>
public interface IWeixinClawMessageHandler
{
    /// <summary>
    /// Processes one inbound message.
    /// </summary>
    /// <param name="context">The normalized inbound message context.</param>
    /// <param name="cancellationToken">Token used to cancel processing.</param>
    /// <returns>A task that completes when message processing finishes.</returns>
    Task HandleAsync(
        WeixinClawMessageReceivedContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the outbound text-message boundary used by Weixin Claw handlers.
/// </summary>
public interface IWeixinClawMessageSender
{
    /// <summary>
    /// Sends text to a Weixin user or group.
    /// </summary>
    /// <param name="accountId">The local Weixin account identifier.</param>
    /// <param name="toUserId">The recipient identifier.</param>
    /// <param name="text">The text content to send.</param>
    /// <param name="contextToken">Optional provider conversation context token.</param>
    /// <param name="runId">Optional provider run identifier.</param>
    /// <param name="cancellationToken">Token used to cancel sending.</param>
    /// <returns>A task that completes after the provider request finishes.</returns>
    Task SendTextAsync(
        int accountId,
        string toUserId,
        string text,
        string contextToken = null,
        string runId = null,
        CancellationToken cancellationToken = default);
}
