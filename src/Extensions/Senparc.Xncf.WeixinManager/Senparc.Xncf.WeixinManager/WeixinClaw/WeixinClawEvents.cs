/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawEvents.cs
    文件功能描述：WeixinClawEvents.cs implementation and project behavior.


    创建标识：Senparc - 20260920

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

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
        string runId = null,
        CancellationToken cancellationToken = default);
}
