using System;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.AI.Interfaces;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Services;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

/// <summary>
/// 个人微信 AI 自动回复处理器：收到一对一文本消息后，按账号配置的 PromptRangeCode
/// 调用 AI（与公众号 XncfMpMessageHandler 相同的 AgentAiHandler + PromptRange 链路）并回复。
/// 群组消息不自动回复（协议文本项无法可靠识别 @ 提醒，避免群内刷屏）。
/// </summary>
public sealed class WeixinClawAiMessageHandler : IWeixinClawMessageHandler
{
    private static readonly ConcurrentDictionary<string, IWantToRun> s_iWantToRuns = new();

    private readonly WeixinClawAccountService _accountService;
    private readonly WeixinClawMessageService _messageService;
    private readonly PromptItemService _promptItemService;
    private readonly AgentAiHandler _agentAiHandler;
    private readonly ILogger<WeixinClawAiMessageHandler> _logger;

    public WeixinClawAiMessageHandler(
        WeixinClawAccountService accountService,
        WeixinClawMessageService messageService,
        PromptItemService promptItemService,
        IAiHandler aiHandler,
        ILogger<WeixinClawAiMessageHandler> logger)
    {
        _accountService = accountService;
        _messageService = messageService;
        _promptItemService = promptItemService;
        _agentAiHandler = (AgentAiHandler)aiHandler;
        _logger = logger;
    }

    public async Task HandleAsync(WeixinClawMessageReceivedContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrEmpty(context.GroupId))
            {
                _logger.LogDebug("跳过个人微信群组消息的 AI 回复（{MessageId}）。", context.MessageId);
                return;
            }

            var account = await _accountService.GetObjectAsync(z => z.Id == context.AccountId).ConfigureAwait(false);
            if (account == null || string.IsNullOrWhiteSpace(account.PromptRangeCode))
            {
                return;
            }

            var iWantToRun = await GetOrCreateIWantToRunAsync(account, context).ConfigureAwait(false);
            var result = await iWantToRun.RunChatAsync(context.Text).ConfigureAwait(false);
            var reply = result?.OutputString?.Trim();
            if (string.IsNullOrWhiteSpace(reply))
            {
                _logger.LogWarning("个人微信 AI 未返回有效回复（{AccountId}/{MessageId}）。", context.AccountId, context.MessageId);
                return;
            }

            await _messageService.SendTextAsync(
                context.AccountId,
                context.FromUserId,
                reply,
                context.ContextToken,
                cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("个人微信 AI 回复已发送（{AccountId}/{MessageId}）。", context.AccountId, context.MessageId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 不向外抛异常，避免单条消息的 AI 失败中断整个账号的消息轮询
            _logger.LogError(ex, "个人微信 AI 自动回复失败（{AccountId}/{MessageId}）。", context.AccountId, context.MessageId);
        }
    }

    private async Task<IWantToRun> GetOrCreateIWantToRunAsync(WeixinClawAccount account, WeixinClawMessageReceivedContext context)
    {
        // 与 WechatAiContext 一致：以联系人 + PromptRangeCode 作为会话 Key，Prompt 更新后会重建
        var key = $"{context.FromUserId}-{account.PromptRangeCode}";
        if (s_iWantToRuns.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var promptResult = await _promptItemService.GetWithVersionAsync(account.PromptRangeCode, isAvg: true).ConfigureAwait(false);
        var iWantToRun = _agentAiHandler.IWantTo(promptResult.SenparcAiSetting)
            .ConfigChatModel(context.FromUserId, new Microsoft.Agents.AI.ChatClientAgentOptions
            {
                ChatOptions = new Microsoft.Extensions.AI.ChatOptions
                {
                    Instructions = promptResult.PromptItem?.Content,
                    MaxOutputTokens = 2000,
                    Temperature = 0.7f,
                    TopP = 0.5f
                }
            }).BuildKernel();

        s_iWantToRuns[key] = iWantToRun;
        return iWantToRun;
    }
}
