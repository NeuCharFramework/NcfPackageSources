using Senparc.Xncf.WeixinManager.Domain.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawMessageService : IWeixinClawMessageSender
{
    private readonly WeixinClawAccountService _accountService;
    private readonly WeixinClawMessageRecordService _recordService;
    private readonly WeixinClawApi _api;

    public WeixinClawMessageService(
        WeixinClawAccountService accountService,
        WeixinClawMessageRecordService recordService,
        WeixinClawApi api)
    {
        _accountService = accountService;
        _recordService = recordService;
        _api = api;
    }

    public async Task SendTextAsync(
        int accountId,
        string toUserId,
        string text,
        string contextToken = null,
        CancellationToken cancellationToken = default)
    {
        await SendTextWithResultAsync(
            accountId,
            toUserId,
            text,
            contextToken,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<WeixinClawSendTextResult> SendTextWithResultAsync(
        int accountId,
        string toUserId,
        string text,
        string contextToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("text is required.", nameof(text));
        }

        var account = await _accountService.GetObjectAsync(z => z.Id == accountId).ConfigureAwait(false);
        if (account == null)
        {
            throw new InvalidOperationException("个人微信账号不存在或已删除。");
        }

        var token = _accountService.UnprotectToken(account);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("个人微信账号未连接或 token 无法解密。");
        }

        var targetUserId = string.IsNullOrWhiteSpace(toUserId)
            ? account.LastMessageFromUserId ?? account.IlinkUserId
            : toUserId.Trim();
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            throw new ArgumentException(
                "toUserId 不能为空，且当前账号没有可用的最近入站用户。",
                nameof(toUserId));
        }

        var effectiveContextToken = contextToken;
        if (string.IsNullOrWhiteSpace(effectiveContextToken)
            && string.Equals(
                targetUserId,
                account.LastMessageFromUserId,
                StringComparison.Ordinal))
        {
            effectiveContextToken = _accountService.UnprotectContextToken(account);
        }
        var contextTokenUsed = !string.IsNullOrWhiteSpace(effectiveContextToken);
        if (string.IsNullOrWhiteSpace(effectiveContextToken))
        {
            throw new InvalidOperationException(
                "当前没有可用的微信会话上下文。请先在手机端向“微信 ClawBot”发送一条文字消息，再发送回复。");
        }

        var message = new WeixinClawMessage
        {
            ToUserId = targetUserId,
            ClientId = Guid.NewGuid().ToString("N"),
            MessageType = 2,
            MessageState = 2,
            ContextToken = effectiveContextToken,
            ItemList =
            [
                new WeixinClawMessageItem
                {
                    Type = 1,
                    TextItem = new WeixinClawTextItem { Text = text }
                }
            ]
        };

        var record = await _recordService.AddOutboundPendingAsync(
            accountId,
            message.ClientId,
            account.IlinkUserId,
            targetUserId,
            message.MessageType,
            message.MessageState,
            text).ConfigureAwait(false);
        try
        {
            var result = await _api.SendMessageAsync(
                account.BaseUrl,
                token,
                message,
                cancellationToken).ConfigureAwait(false);
            if (result.Ret != 0)
            {
                var error = $"发送个人微信消息失败：{result.Ret} {result.Errmsg}";
                await _recordService.MarkFailedAsync(record.Id, error).ConfigureAwait(false);
                throw new InvalidOperationException(error);
            }

            await _recordService.MarkSentAsync(record.Id, result.MessageId).ConfigureAwait(false);
            return new WeixinClawSendTextResult(
                record.Id,
                result.MessageId,
                targetUserId,
                contextTokenUsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _recordService.MarkFailedAsync(record.Id, ex.Message).ConfigureAwait(false);
            throw;
        }
    }
}

public sealed record WeixinClawSendTextResult(
    int RecordId,
    string MessageId,
    string TargetUserId,
    bool ContextTokenUsed);
