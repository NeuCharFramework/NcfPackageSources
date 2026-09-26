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
    string Text,
    DateTimeOffset CreatedAt);

public interface IWeixinClawMessageHandler
{
    Task HandleAsync(
        WeixinClawMessageReceivedContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 个人微信 Claw 的出站消息抽象。上层模块只依赖此接口，不直接依赖
/// WeixinClaw 的账号存储、令牌解密和 iLink HTTP 实现。
/// </summary>
public interface IWeixinClawMessageSender
{
    Task SendTextAsync(
        int accountId,
        string toUserId,
        string text,
        string contextToken = null,
        CancellationToken cancellationToken = default);
}
