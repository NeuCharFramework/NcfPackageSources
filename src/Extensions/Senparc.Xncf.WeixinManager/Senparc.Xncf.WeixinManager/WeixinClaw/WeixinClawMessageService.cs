using Senparc.Xncf.WeixinManager.Domain.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawMessageService
{
    private readonly WeixinClawAccountService _accountService;
    private readonly WeixinClawApi _api;

    public WeixinClawMessageService(
        WeixinClawAccountService accountService,
        WeixinClawApi api)
    {
        _accountService = accountService;
        _api = api;
    }

    public async Task SendTextAsync(
        int accountId,
        string toUserId,
        string text,
        string contextToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toUserId))
        {
            throw new ArgumentException("toUserId is required.", nameof(toUserId));
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("text is required.", nameof(text));
        }

        var account = await _accountService.GetObjectAsync(z => z.Id == accountId).ConfigureAwait(false);
        var token = _accountService.UnprotectToken(account);
        if (account == null || string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("个人微信账号未连接或 token 无法解密。");
        }

        var message = new WeixinClawMessage
        {
            ToUserId = toUserId,
            ClientId = Guid.NewGuid().ToString("N"),
            MessageType = 2,
            MessageState = 2,
            ContextToken = contextToken,
            ItemList =
            [
                new WeixinClawMessageItem
                {
                    Type = 1,
                    TextItem = new WeixinClawTextItem { Text = text }
                }
            ]
        };

        var result = await _api.SendMessageAsync(
            account.BaseUrl,
            token,
            message,
            cancellationToken).ConfigureAwait(false);
        if (result.Ret != 0)
        {
            throw new InvalidOperationException(
                $"发送个人微信消息失败：{result.Ret} {result.Errmsg}");
        }
    }
}
