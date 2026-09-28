using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Domain.Services;

public sealed class WeixinClawMessageRecordService
    : ServiceBase<WeixinClawMessageRecord>, IServiceBase<WeixinClawMessageRecord>
{
    public WeixinClawMessageRecordService(
        IRepositoryBase<WeixinClawMessageRecord> repo,
        IServiceProvider serviceProvider)
        : base(repo, serviceProvider)
    {
    }

    public async Task<WeixinClawMessageRecord> AddInboundAsync(
        int accountId,
        string messageId,
        long seq,
        string fromUserId,
        string toUserId,
        string contextTokenProtected,
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
            messageType,
            messageState,
            "received",
            text,
            createdAt);
        await SaveObjectAsync(record).ConfigureAwait(false);
        return record;
    }

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
            messageType,
            messageState,
            "sending",
            text);
        await SaveObjectAsync(record).ConfigureAwait(false);
        return record;
    }

    public async Task<List<WeixinClawMessageRecordDto>> GetRecentDtosAsync(
        int accountId,
        int take = 100)
    {
        take = Math.Clamp(take, 1, 500);
        var records = await GetFullListAsync(
            item => item.WeixinClawAccountId == accountId,
            "CreatedAt DESC").ConfigureAwait(false);
        return records
            .Take(take)
            .OrderBy(item => item.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

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

    public async Task<WeixinClawMessageRecord> GetInboundRecordAsync(
        int accountId,
        int recordId)
    {
        return await GetObjectAsync(item =>
            item.Id == recordId
            && item.WeixinClawAccountId == accountId
            && item.Direction == WeixinClawMessageDirection.Inbound).ConfigureAwait(false);
    }

    private static WeixinClawMessageRecordDto ToDto(WeixinClawMessageRecord item)
    {
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
            Text = item.Text,
            Error = item.Error,
            CreatedAt = item.CreatedAt
        };
    }
}
