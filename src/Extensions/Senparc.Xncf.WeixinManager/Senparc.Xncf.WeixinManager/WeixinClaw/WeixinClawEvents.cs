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
