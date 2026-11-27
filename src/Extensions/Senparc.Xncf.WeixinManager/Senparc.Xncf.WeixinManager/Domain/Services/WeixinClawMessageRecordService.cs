/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawMessageRecordService.cs
    File description: WeixinClawMessageRecordService.cs implementation and project behavior.

    Created by: Senparc - 20260927

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw media handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Domain.Services;

public sealed class WeixinClawMessageRecordService
    : ServiceBase<WeixinClawMessageRecord>, IServiceBase<WeixinClawMessageRecord>
{
    /// <summary>
    /// Initializes the WeixinClaw message-record persistence service.
    /// </summary>
    /// <param name="repo">The repository for message records.</param>
    /// <param name="serviceProvider">The dependency provider used by the base service.</param>
    public WeixinClawMessageRecordService(
        IRepositoryBase<WeixinClawMessageRecord> repo,
        IServiceProvider serviceProvider)
        : base(repo, serviceProvider)
    {
    }

    /// <summary>
    /// Persists one inbound WeixinClaw message with protected context and run metadata.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <param name="messageId">The provider message identifier.</param>
    /// <param name="seq">The provider sequence value.</param>
    /// <param name="fromUserId">The sender user identifier.</param>
    /// <param name="toUserId">The recipient account identifier.</param>
    /// <param name="contextTokenProtected">The protected context token.</param>
    /// <param name="runId">The associated run identifier.</param>
    /// <param name="messageType">The provider message type.</param>
    /// <param name="messageState">The provider message state.</param>
    /// <param name="text">The stored message content.</param>
    /// <param name="createdAt">The provider creation timestamp.</param>
    /// <returns>The persisted inbound record.</returns>
    public async Task<WeixinClawMessageRecord> AddInboundAsync(
        int accountId,
        string messageId,
        long seq,
        string fromUserId,
        string toUserId,
        string contextTokenProtected,
        string runId,
        int messageType,
        int messageState,
        string text,
        DateTime createdAt)
    {
        var record = new WeixinClawMessageRecord(
            accountId,
            WeixinClawMessageDirection.Inbound,
            messageId,
            seq,
            fromUserId,
            toUserId,
            null,
            contextTokenProtected,
            runId,
            messageType,
            messageState,
            "received",
            text,
            createdAt);
        await SaveObjectAsync(record).ConfigureAwait(false);
        return record;
    }

    /// <summary>
    /// Persists one outbound message in the pending/sending state.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <param name="clientId">The client correlation identifier.</param>
    /// <param name="fromUserId">The account sender identifier.</param>
    /// <param name="toUserId">The destination user identifier.</param>
    /// <param name="messageType">The outbound message type.</param>
    /// <param name="messageState">The outbound provider state.</param>
    /// <param name="text">The outbound message content.</param>
    /// <returns>The persisted outbound record.</returns>
    public async Task<WeixinClawMessageRecord> AddOutboundPendingAsync(
        int accountId,
        string clientId,
        string fromUserId,
        string toUserId,
        int messageType,
        int messageState,
        string text)
    {
        var record = new WeixinClawMessageRecord(
            accountId,
            WeixinClawMessageDirection.Outbound,
            null,
            null,
            fromUserId,
            toUserId,
            clientId,
            null,
            null,
            messageType,
            messageState,
            "sending",
            text);
        await SaveObjectAsync(record).ConfigureAwait(false);
        return record;
    }

    /// <summary>
    /// Loads a bounded recent record window for an account and optional peer.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <param name="peerUserId">Optional sender/recipient filter.</param>
    /// <param name="take">The requested record count, clamped to 1 through 500.</param>
    /// <returns>Chronologically ordered record DTOs.</returns>
    public async Task<List<WeixinClawMessageRecordDto>> GetRecentDtosAsync(
        int accountId,
        string peerUserId = null,
        int take = 100)
    {
        take = Math.Clamp(take, 1, 500);
        var records = await GetFullListAsync(
            item => item.WeixinClawAccountId == accountId
                && (string.IsNullOrWhiteSpace(peerUserId)
                    || item.FromUserId == peerUserId
                    || item.ToUserId == peerUserId),
            "CreatedAt DESC").ConfigureAwait(false);
        return records
            .Take(take)
            .OrderBy(item => item.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    /// <summary>
    /// Groups account records into peer conversations and returns the newest conversation summary first.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <returns>Conversation summaries ordered by latest message time.</returns>
    public async Task<List<WeixinClawConversationDto>> GetConversationsAsync(int accountId)
    {
        var records = await GetFullListAsync(
            item => item.WeixinClawAccountId == accountId,
            "CreatedAt DESC").ConfigureAwait(false);
        return records
            .GroupBy(item => item.Direction == WeixinClawMessageDirection.Inbound
                ? item.FromUserId
                : item.ToUserId)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(group =>
            {
                var latest = group.OrderByDescending(item => item.CreatedAt).First();
                var latestContent = WeixinClawMessageContent.Parse(latest.Text);
                return new WeixinClawConversationDto
                {
                    PeerUserId = group.Key,
                    LastText = latestContent.MediaItems.Count > 0 && string.IsNullOrWhiteSpace(latestContent.Text)
                        ? $"[{string.Join("、", latestContent.MediaItems.Select(item => item.Kind))}]"
                        : latestContent.Text,
                    LastStatus = latest.Status,
                    LastCreatedAt = latest.CreatedAt,
                    MessageCount = group.Count()
                };
            })
            .OrderByDescending(item => item.LastCreatedAt)
            .ToList();
    }

    /// <summary>
    /// Marks an outbound record as sent and stores the provider message identifier.
    /// Missing records are ignored.
    /// </summary>
    /// <param name="id">The persisted record identifier.</param>
    /// <param name="messageId">The provider message identifier.</param>
    public async Task MarkSentAsync(int id, string messageId)
    {
        var record = await GetObjectAsync(item => item.Id == id).ConfigureAwait(false);
        if (record == null)
        {
            return;
        }

        record.MarkSent(messageId);
        await SaveObjectAsync(record).ConfigureAwait(false);
    }

    /// <summary>
    /// Marks an outbound record as failed and stores its error text.
    /// Missing records are ignored.
    /// </summary>
    /// <param name="id">The persisted record identifier.</param>
    /// <param name="error">The failure detail.</param>
    public async Task MarkFailedAsync(int id, string error)
    {
        var record = await GetObjectAsync(item => item.Id == id).ConfigureAwait(false);
        if (record == null)
        {
            return;
        }

        record.MarkFailed(error);
        await SaveObjectAsync(record).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads one inbound record scoped to the specified account.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <param name="recordId">The persisted record identifier.</param>
    /// <returns>The inbound record, or <see langword="null"/> when not found.</returns>
    public async Task<WeixinClawMessageRecord> GetInboundRecordAsync(
        int accountId,
        int recordId)
    {
        return await GetObjectAsync(item =>
            item.Id == recordId
            && item.WeixinClawAccountId == accountId
            && item.Direction == WeixinClawMessageDirection.Inbound).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates FileManager import metadata for one inbound media item.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <param name="messageId">The provider message identifier.</param>
    /// <param name="storageKey">The media storage key.</param>
    /// <param name="fileManagerFileId">The imported FileManager identifier.</param>
    /// <param name="fileManagerError">The import error, when any.</param>
    /// <returns><see langword="true"/> when the record was found and updated or already matched.</returns>
    public async Task<bool> TryUpdateInboundMediaImportAsync(
        int accountId,
        string messageId,
        string storageKey,
        int? fileManagerFileId,
        string fileManagerError)
    {
        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(storageKey))
        {
            return false;
        }

        var record = await GetObjectAsync(item =>
            item.WeixinClawAccountId == accountId
            && item.Direction == WeixinClawMessageDirection.Inbound
            && item.MessageId == messageId).ConfigureAwait(false);
        if (record == null)
        {
            return false;
        }

        var content = WeixinClawMessageContent.Parse(record.Text);
        if (content.ParseError || content.MediaItems.Count == 0)
        {
            return false;
        }

        var media = content.MediaItems.FirstOrDefault(item =>
            string.Equals(item.StorageKey, storageKey, StringComparison.Ordinal));
        if (media == null)
        {
            return false;
        }

        var normalizedError = string.IsNullOrWhiteSpace(fileManagerError) ? null : fileManagerError;
        if (media.FileManagerFileId == fileManagerFileId
            && string.Equals(media.FileManagerError, normalizedError, StringComparison.Ordinal))
        {
            return true;
        }

        media.FileManagerFileId = fileManagerFileId;
        media.FileManagerError = normalizedError;
        record.SetText(WeixinClawMessageContent.Serialize(content.Text, content.MediaItems));
        await SaveObjectAsync(record).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Projects a persisted message record and parsed media payload into a transport DTO.
    /// </summary>
    /// <param name="item">The persisted record to project.</param>
    /// <returns>The client-facing record DTO.</returns>
    private static WeixinClawMessageRecordDto ToDto(WeixinClawMessageRecord item)
    {
        var content = WeixinClawMessageContent.Parse(item.Text);
        return new WeixinClawMessageRecordDto
        {
            Id = item.Id,
            Direction = item.Direction,
            MessageId = item.MessageId,
            Seq = item.Seq,
            FromUserId = item.FromUserId,
            ToUserId = item.ToUserId,
            Status = item.Status,
            MessageType = item.MessageType,
            Text = content.Text,
            Error = item.Error,
            CreatedAt = item.CreatedAt,
            MediaItems = content.MediaItems.Select(media => new WeixinClawMessageMediaDto
            {
                Kind = media.Kind,
                Name = media.Name,
                ContentType = media.ContentType,
                Size = media.Size,
                Error = media.Error
            }).ToList()
        };
    }
}
