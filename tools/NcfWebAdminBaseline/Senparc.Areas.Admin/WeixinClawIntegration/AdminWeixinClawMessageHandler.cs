/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminWeixinClawMessageHandler.cs
    File description: AdminWeixinClawMessageHandler.cs implementation and project behavior.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 enhance WeixinClaw media handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.WeixinManager.Domain.Services;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.WeixinClawIntegration;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class AdminWeixinClawMessageHandler : IWeixinClawMessageHandler
{
    private readonly IWeixinClawMessageSender _sender;
    private readonly IWeixinClawBindingProfileResolver _bindingProfileResolver;
    private readonly WeixinClawBindingProfileService _bindingProfileService;
    private readonly WeixinClawAdminBindingService _bindingService;
    private readonly AdminChatSessionService _sessionService;
    private readonly AdminChatMessageService _messageService;
    private readonly AdminChatAiService _chatAiService;
    private readonly AdminChatSessionWorkflowService _workflowSessionService;
    private readonly NeuBellSnapshotService _snapshotService;
    private readonly IWorkflowFunctionCallingProvider _workflowProvider;
    private readonly WeixinClawAdminIntegrationOptions _options;
    private readonly ILogger<AdminWeixinClawMessageHandler> _logger;
    private readonly IStringLocalizer<AdminResource> _localizer;

    public AdminWeixinClawMessageHandler(
        IWeixinClawMessageSender sender,
        IWeixinClawBindingProfileResolver bindingProfileResolver,
        WeixinClawBindingProfileService bindingProfileService,
        WeixinClawAdminBindingService bindingService,
        AdminChatSessionService sessionService,
        AdminChatMessageService messageService,
        AdminChatAiService chatAiService,
        AdminChatSessionWorkflowService workflowSessionService,
        NeuBellSnapshotService snapshotService,
        IOptions<WeixinClawAdminIntegrationOptions> options,
        ILogger<AdminWeixinClawMessageHandler> logger,
        IStringLocalizer<AdminResource> localizer,
        IWorkflowFunctionCallingProvider workflowProvider = null)
    {
        _sender = sender;
        _bindingProfileResolver = bindingProfileResolver;
        _bindingProfileService = bindingProfileService;
        _bindingService = bindingService;
        _sessionService = sessionService;
        _messageService = messageService;
        _chatAiService = chatAiService;
        _workflowSessionService = workflowSessionService;
        _snapshotService = snapshotService;
        _options = options.Value;
        _logger = logger;
        _localizer = localizer;
        _workflowProvider = workflowProvider;
    }

    public async Task HandleAsync(
        WeixinClawMessageReceivedContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || context == null || string.IsNullOrWhiteSpace(context.Text))
        {
            return;
        }

        var text = context.Text.Trim();
        if (text.Length > Math.Max(100, _options.MaxInputLength))
        {
            await ReplyAsync(context, "消息过长，请拆分后再发送。", cancellationToken).ConfigureAwait(false);
            return;
        }

        var prefix = NormalizePrefix(_options.CommandPrefix);
        var isCommand = text.StartsWith(prefix, StringComparison.Ordinal);
        var binding = await _bindingService.GetBindingAsync(
            context.AccountId,
            context.FromUserId,
            context.GroupId).ConfigureAwait(false);
        if (_options.RequireCommandPrefix && !isCommand)
        {
            if (binding == null
                && await HasBindingProfilesAsync(context.AccountId).ConfigureAwait(false))
            {
                await ReplyAsync(
                    context,
                    $"此微信会话尚未绑定 Admin。请发送 {prefix}bind <绑定码> 完成绑定。",
                    cancellationToken).ConfigureAwait(false);
            }
            return;
        }

        var commandText = isCommand ? text[prefix.Length..].Trim() : "chat " + text;
        var command = ReadCommand(commandText, out var argument);

        if (string.Equals(command, "bind", StringComparison.OrdinalIgnoreCase))
        {
            await BindAsync(context, argument, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (binding == null)
        {
            if (string.Equals(command, "help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "bind", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync(
                    context,
                    BuildUnboundHelp(prefix),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            await ReplyAsync(
                context,
                $"此微信会话尚未绑定 Admin。请发送 {prefix}bind <绑定码> 完成绑定。",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        await _bindingService.SaveContextAsync(
            binding,
            context.ContextToken,
            DateTime.UtcNow).ConfigureAwait(false);

        try
        {
            switch (command.ToLowerInvariant())
            {
                case "help":
                    await ReplyAsync(context, BuildHelp(prefix, binding), cancellationToken).ConfigureAwait(false);
                    return;
                case "status":
                    await ReplyAsync(context, BuildStatus(context, binding), cancellationToken).ConfigureAwait(false);
                    return;
                case "bell":
                case "neubell":
                    await ReplyAsync(context, await BuildNeuBellAsync(binding, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return;
                case "chat":
                    await RunChatAsync(context, binding, argument, cancellationToken).ConfigureAwait(false);
                    return;
                case "harness":
                    await RunHarnessAsync(context, binding, argument, cancellationToken).ConfigureAwait(false);
                    return;
                case "approve":
                case "reject":
                    await ResumeHarnessAsync(context, binding, command, argument, cancellationToken).ConfigureAwait(false);
                    return;
                case "workflow":
                    await RunWorkflowAsync(context, binding, argument, cancellationToken).ConfigureAwait(false);
                    return;
                case "unbind":
                    binding.Disable();
                    await _bindingService.SaveObjectAsync(binding).ConfigureAwait(false);
                    await ReplyAsync(context, "已解除此微信会话与 Admin 的绑定。", cancellationToken).ConfigureAwait(false);
                    return;
                default:
                    if (_options.AllowPlainChat && !isCommand)
                    {
                        await RunChatAsync(context, binding, text, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyAsync(context, $"未知命令：{command}。发送 {prefix}help 查看帮助。", cancellationToken).ConfigureAwait(false);
                    }
                    return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Admin 个人微信 Claw 消息处理失败：AccountId={AccountId}, FromUserId={FromUserId}, Command={Command}",
                context.AccountId,
                context.FromUserId,
                command);
            await ReplyAsync(context, "处理失败，请稍后重试；详细错误已记录到服务日志。", cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task BindAsync(
        WeixinClawMessageReceivedContext context,
        string argument,
        CancellationToken cancellationToken)
    {
        var profile = await _bindingProfileResolver.ResolveAsync(
            context.AccountId,
            argument,
            cancellationToken).ConfigureAwait(false);
        if (profile == null)
        {
            await ReplyAsync(
                context,
                "绑定码无效，或当前微信账号没有启用对应的数据库绑定配置。请管理员打开 Admin → 个人微信集成或微信管理 → 个人微信 Bot → 绑定配置进行检查。",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var binding = await _bindingService.BindAsync(
            context.AccountId,
            context.FromUserId,
            context.GroupId,
            profile.AdminUserId,
            profile.EnableNeuBell && _options.EnableNeuBell,
            profile.EnableWorkflow && _options.EnableWorkflow,
            profile.WorkflowId,
            profile.AiModelId,
            profile.Mode).ConfigureAwait(false);
        await _bindingService.SaveContextAsync(
            binding,
            context.ContextToken,
            DateTime.UtcNow).ConfigureAwait(false);
        await ReplyAsync(
            context,
            $"绑定成功。发送 {NormalizePrefix(_options.CommandPrefix)}help 查看可用功能。",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task RunChatAsync(
        WeixinClawMessageReceivedContext context,
        WeixinClawAdminBinding binding,
        string argument,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            await ReplyAsync(context, "请输入 Chat 内容。示例：/chat 查询当前系统状态。", cancellationToken).ConfigureAwait(false);
            return;
        }

        var session = await EnsureChatSessionAsync(binding, argument).ConfigureAwait(false);
        await _messageService.AddMessageAsync(session.Id, ChatMessageRoleType.User, argument).ConfigureAwait(false);
        await _sessionService.UpdateLastMessageTimeAsync(session.Id).ConfigureAwait(false);

        var (response, modelIdentifier) = await _chatAiService.GenerateResponseAsync(
            session.Id,
            binding.AdminUserId,
            argument,
            binding.AiModelId > 0 ? binding.AiModelId : _options.DefaultAiModelId,
            generationOptions: new AdminChatGenerationOptions
            {
                AllowFunctionInvocation = binding.EnableWorkflow && _options.EnableWorkflow
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        response = string.IsNullOrWhiteSpace(response) ? "Admin Chat 没有返回内容。" : response.Trim();
        await _messageService.AddMessageAsync(
            session.Id,
            ChatMessageRoleType.Assistant,
            response,
            modelIdentifier).ConfigureAwait(false);
        await _sessionService.UpdateLastMessageTimeAsync(session.Id).ConfigureAwait(false);

        binding.SetConversationState(
            context.ContextToken,
            null,
            0,
            DateTime.Now);
        await _bindingService.SaveObjectAsync(binding).ConfigureAwait(false);
        await ReplyAsync(context, response, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunHarnessAsync(
        WeixinClawMessageReceivedContext context,
        WeixinClawAdminBinding binding,
        string argument,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableHarness)
        {
            await ReplyAsync(context, "Harness 默认关闭。确认要启用时，请设置 WeixinClawAdminIntegration:EnableHarness=true。", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(argument))
        {
            await ReplyAsync(context, "请输入 Harness 任务。示例：/harness 检查最近的系统异常并给出处理建议。", cancellationToken).ConfigureAwait(false);
            return;
        }

        var session = await EnsureChatSessionAsync(binding, argument).ConfigureAwait(false);
        await _messageService.AddMessageAsync(session.Id, ChatMessageRoleType.User, argument).ConfigureAwait(false);
        var result = await _chatAiService.GenerateNativeHarnessResponseAsync(
            session.Id,
            binding.AdminUserId,
            argument,
            binding.AiModelId > 0 ? binding.AiModelId : _options.DefaultAiModelId,
            Math.Clamp(_options.MaxHarnessIterations, 1, 64),
            _options.HarnessTimeout,
            cancellationToken).ConfigureAwait(false);

        var response = result.response;
        if (result.harness.PendingApprovals.Count > 0)
        {
            response += Environment.NewLine + "存在待审批工具调用；请发送 /approve 或 /reject。";
            var approval = result.harness.PendingApprovals[0];
            binding.SetPendingApproval(
                approval.RequestId,
                approval.ToolCallId,
                approval.ToolName,
                approval.ArgumentsJson);
        }
        else
        {
            binding.ClearPendingApproval();
        }

        await _messageService.AddMessageAsync(
            session.Id,
            ChatMessageRoleType.Assistant,
            response,
            result.modelIdentifier,
            result.harness.TrajectoryId,
            result.harness.TrajectorySequence).ConfigureAwait(false);
        binding.SetConversationState(
            context.ContextToken,
            result.harness.TrajectoryId > 0 ? result.harness.TrajectoryId : null,
            result.harness.TrajectorySequence,
            DateTime.Now);
        await _bindingService.SaveObjectAsync(binding).ConfigureAwait(false);
        await ReplyAsync(context, response, cancellationToken).ConfigureAwait(false);
    }

    private async Task ResumeHarnessAsync(
        WeixinClawMessageReceivedContext context,
        WeixinClawAdminBinding binding,
        string command,
        string argument,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableHarness
            || !binding.LastTrajectoryId.HasValue
            || string.IsNullOrWhiteSpace(binding.PendingApprovalRequestId)
            || string.IsNullOrWhiteSpace(binding.PendingApprovalToolCallId)
            || string.IsNullOrWhiteSpace(binding.PendingApprovalToolName))
        {
            await ReplyAsync(context, "当前没有可审批或可继续的 Harness 任务。", cancellationToken).ConfigureAwait(false);
            return;
        }

        var approved = string.Equals(command, "approve", StringComparison.OrdinalIgnoreCase);
        var result = await _chatAiService.RespondNativeHarnessApprovalAsync(
            binding.LastTrajectoryId.Value,
            binding.AdminUserId,
            binding.PendingApprovalRequestId,
            binding.PendingApprovalToolCallId,
            binding.PendingApprovalToolName,
            binding.PendingApprovalArgumentsJson,
            approved,
            string.IsNullOrWhiteSpace(argument) ? null : argument.Trim(),
            binding.AiModelId > 0 ? binding.AiModelId : _options.DefaultAiModelId,
            Math.Clamp(_options.MaxHarnessIterations, 1, 64),
            _options.HarnessTimeout,
            cancellationToken).ConfigureAwait(false);

        var response = result.response;
        if (result.harness.PendingApprovals.Count > 0)
        {
            response += Environment.NewLine + "仍有待审批工具调用；请继续使用 /approve 或 /reject。";
            var approval = result.harness.PendingApprovals[0];
            binding.SetPendingApproval(
                approval.RequestId,
                approval.ToolCallId,
                approval.ToolName,
                approval.ArgumentsJson);
        }
        else
        {
            binding.ClearPendingApproval();
        }

        binding.SetConversationState(
            context.ContextToken,
            result.harness.TrajectoryId > 0 ? result.harness.TrajectoryId : binding.LastTrajectoryId,
            result.harness.TrajectorySequence,
            DateTime.Now);
        await _bindingService.SaveObjectAsync(binding).ConfigureAwait(false);
        await ReplyAsync(context, response, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunWorkflowAsync(
        WeixinClawMessageReceivedContext context,
        WeixinClawAdminBinding binding,
        string argument,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableWorkflow || _workflowProvider == null)
        {
            await ReplyAsync(context, _localizer["WeixinClaw.Workflow.IntegrationDisabled"].Value, cancellationToken).ConfigureAwait(false);
            return;
        }

        var parts = (argument ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var workflowId = binding.WorkflowId.GetValueOrDefault();
        var input = argument ?? string.Empty;
        if (parts.Length > 0 && int.TryParse(parts[0], out var requestedWorkflowId))
        {
            workflowId = requestedWorkflowId;
            input = parts.Length > 1 ? parts[1] : string.Empty;
        }

        var available = await _workflowProvider.GetAvailableAsync(
            binding.AdminUserId,
            cancellationToken).ConfigureAwait(false);
        if (workflowId <= 0)
        {
            var names = available.Count == 0
                ? _localizer["WeixinClaw.Workflow.NoAvailable"].Value
                : string.Join(Environment.NewLine, available.Select(item => $"{item.Id}: {item.Name}"));
            await ReplyAsync(
                context,
                _localizer["WeixinClaw.Workflow.ListPrompt"].Value + Environment.NewLine + names,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var descriptor = available.FirstOrDefault(item => item.Id == workflowId);
        if (descriptor == null)
        {
            await ReplyAsync(context, _localizer["WeixinClaw.Workflow.NotFound"].Value, cancellationToken).ConfigureAwait(false);
            return;
        }

        var result = await _workflowProvider.ExecuteAsync(
            workflowId,
            binding.AdminUserId,
            input,
            new Dictionary<string, object?>(),
            cancellationToken).ConfigureAwait(false);
        var response = result.Success
            ? _localizer["WeixinClaw.Workflow.Completed", descriptor.Name, result.Output].Value
            : _localizer["WeixinClaw.Workflow.Failed", descriptor.Name, result.ErrorMessage].Value;
        await ReplyAsync(context, response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AdminChatSession> EnsureChatSessionAsync(
        WeixinClawAdminBinding binding,
        string title)
    {
        if (binding.AdminChatSessionId.HasValue)
        {
            var existing = await _sessionService.GetSessionByIdAsync(
                binding.AdminChatSessionId.Value,
                binding.AdminUserId).ConfigureAwait(false);
            if (existing != null)
            {
                return existing;
            }
        }

        var session = await _sessionService.CreateSessionAsync(
            $"微信 Claw：{title}",
            binding.AdminUserId).ConfigureAwait(false);
        binding.SetChatSession(session.Id);

        if (binding.EnableWorkflow && binding.WorkflowId.HasValue && _workflowProvider != null)
        {
            var available = await _workflowProvider.GetAvailableAsync(binding.AdminUserId).ConfigureAwait(false);
            var workflow = available.FirstOrDefault(item => item.Id == binding.WorkflowId.Value);
            if (workflow != null)
            {
                await _workflowSessionService.AddWorkflowsToSessionAsync(
                    session.Id,
                    new[] { (workflow.Id, workflow.Name, workflow.Description ?? string.Empty) }).ConfigureAwait(false);
            }
        }

        await _bindingService.SaveObjectAsync(binding).ConfigureAwait(false);
        return session;
    }

    private async Task<string> BuildNeuBellAsync(
        WeixinClawAdminBinding binding,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableNeuBell || !binding.EnableNeuBell)
        {
            return "NeuBell 推送未启用。";
        }

        var snapshots = await _snapshotService.GetSnapshotsAsync(
            new NeuBellRequestContext(binding.AdminUserId.ToString()),
            cancellationToken).ConfigureAwait(false);
        var items = snapshots
            .SelectMany(snapshot => snapshot.Items.Select(item => (snapshot, item)))
            .Where(pair => pair.item.Count > 0 || !string.Equals(pair.item.Severity, "info", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(pair => pair.item.UpdatedAt)
            .Take(20)
            .ToList();
        if (items.Count == 0)
        {
            return "当前没有需要处理的 NeuBell 提醒。";
        }

        var builder = new StringBuilder("NeuBell 当前提醒：");
        foreach (var pair in items)
        {
            builder.AppendLine();
            builder.Append("- ");
            builder.Append(pair.snapshot.DisplayName);
            builder.Append(" / ");
            builder.Append(pair.item.Title);
            builder.Append("：");
            builder.Append(pair.item.Summary);
            if (pair.item.Count > 0)
            {
                builder.Append("（");
                builder.Append(pair.item.Count);
                builder.Append("）");
            }
        }
        return builder.ToString();
    }

    private string BuildStatus(
        WeixinClawMessageReceivedContext context,
        WeixinClawAdminBinding binding)
    {
        return string.Join(
            Environment.NewLine,
            "个人微信 Claw 已连接并完成 Admin 绑定。",
            $"账号：{context.AccountName}",
            $"微信用户：{context.FromUserId}",
            $"AdminUserId：{binding.AdminUserId}",
            $"Chat SessionId：{binding.AdminChatSessionId?.ToString() ?? "尚未创建"}",
            $"NeuBell：{(binding.EnableNeuBell ? "已启用" : "已关闭")}",
            $"Workflow：{(binding.EnableWorkflow ? "已启用" : "已关闭")}",
            $"最后交互：{binding.LastMessageAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "暂无"}");
    }

    /// <summary>
    /// Builds command help for a bound WeixinClaw administrator, including
    /// optional Workflow and Harness commands enabled by configuration.
    /// </summary>
    /// <param name="prefix">The command prefix shown to the user.</param>
    /// <param name="binding">The current binding and feature flags.</param>
    /// <returns>The help message text.</returns>
    private string BuildHelp(string prefix, WeixinClawAdminBinding binding)
    {
        var lines = new List<string>
        {
            "可用命令：",
            $"{prefix}status - 查看连接和绑定状态",
            $"{prefix}bell - 查看 NeuBell 提醒",
            $"{prefix}chat <内容> - 使用 Admin Chat",
            $"{prefix}help - 查看帮助",
            $"{prefix}unbind - 解除当前绑定"
        };
        if (binding.EnableWorkflow && _options.EnableWorkflow)
        {
            lines.Add(_localizer["WeixinClaw.Workflow.HelpList", prefix].Value);
            lines.Add(_localizer["WeixinClaw.Workflow.HelpExecute", prefix].Value);
        }
        if (_options.EnableHarness)
        {
            lines.Add($"{prefix}harness <任务> - 启动需审批的长任务");
            lines.Add($"{prefix}approve / {prefix}reject - 继续或拒绝待审批任务");
        }
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Builds the instructions shown when the current WeixinClaw session has no binding.
    /// </summary>
    /// <param name="prefix">The command prefix shown to the user.</param>
    /// <returns>The binding setup instructions.</returns>
    private string BuildUnboundHelp(string prefix)
    {
        return string.Join(
            Environment.NewLine,
            "当前微信会话尚未绑定 Admin。",
            "1. 管理员在 Admin → 个人微信集成或微信管理 → 个人微信 Bot → 绑定配置中创建绑定配置。",
            $"2. 将配置中的绑定码发送：{prefix}bind <绑定码>",
            $"3. 绑定成功后发送 {prefix}help 查看 Admin Chat、NeuBell 和 Workflow 功能。",
            "绑定码只保存为不可逆哈希；管理员需要在创建或修改时记住它。");
    }

    /// <summary>
    /// Determines whether an account has at least one enabled binding profile.
    /// </summary>
    /// <param name="accountId">The Weixin account identifier.</param>
    /// <returns><see langword="true"/> when an enabled profile exists.</returns>
    private async Task<bool> HasBindingProfilesAsync(int accountId)
    {
        return (await _bindingProfileService.GetDtosAsync(accountId).ConfigureAwait(false))
            .Any(item => item.Enabled);
    }

    private async Task ReplyAsync(
        WeixinClawMessageReceivedContext context,
        string text,
        CancellationToken cancellationToken)
    {
        var maxLength = Math.Clamp(_options.MaxReplyLength, 200, 4000);
        foreach (var chunk in SplitText(text, maxLength))
        {
            await _sender.SendTextAsync(
                context.AccountId,
                context.FromUserId,
                chunk,
                context.ContextToken,
                context.RunId,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Splits reply text into bounded chunks, preferring a nearby newline so
    /// transport limits do not cut a message at an arbitrary boundary.
    /// </summary>
    /// <param name="text">The reply text to split.</param>
    /// <param name="maxLength">The maximum character count per chunk.</param>
    /// <returns>The ordered reply chunks.</returns>
    private static IEnumerable<string> SplitText(string text, int maxLength)
    {
        text ??= string.Empty;
        if (text.Length <= maxLength)
        {
            yield return text;
            yield break;
        }

        for (var offset = 0; offset < text.Length;)
        {
            var length = Math.Min(maxLength, text.Length - offset);
            var end = offset + length;
            if (end < text.Length)
            {
                var newline = text.LastIndexOf('\n', end - 1, length);
                if (newline > offset + maxLength / 3)
                {
                    end = newline + 1;
                }
            }

            yield return text[offset..end].Trim();
            offset = end;
        }
    }

    /// <summary>
    /// Normalizes an optional command prefix to a trimmed value or the default slash.
    /// </summary>
    /// <param name="prefix">The configured command prefix.</param>
    /// <returns>The normalized prefix.</returns>
    private static string NormalizePrefix(string prefix)
    {
        return string.IsNullOrWhiteSpace(prefix) ? "/" : prefix.Trim();
    }

    /// <summary>
    /// Separates the first command token from its remaining argument text.
    /// </summary>
    /// <param name="text">The incoming message text.</param>
    /// <param name="argument">Receives the trimmed text after the command token.</param>
    /// <returns>The command token, or <c>help</c> for empty input.</returns>
    private static string ReadCommand(string text, out string argument)
    {
        var parts = (text ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        return parts.Length > 0 ? parts[0].Trim() : "help";
    }
}
