/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowHumanInputService.cs
    文件功能描述：Workflow 原生人工输入请求的暂停、恢复与受控外部访问

    创建标识：Senparc - 20260815

    修改标识：Senparc - 20260817
    修改描述：v0.2.0-preview2 支持 Human Input 人工节点暂停与外部恢复

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

public sealed record WorkflowHumanInputDecision(bool Approved, string? Input = null, string? Reason = null);

public sealed record WorkflowHumanInputResolution(
    bool Handled,
    bool Success,
    bool Approved,
    string? Input,
    string? Reason,
    string Message);

/// <summary>供外部受控调用方轮询的最小待输入视图，绝不返回恢复密钥。</summary>
public sealed record WorkflowExternalHumanInput(
    string RequestId,
    int WorkflowId,
    string NodeId,
    string NodeName,
    string Prompt,
    DateTimeOffset CreatedAt);

/// <summary>
/// Workflow 自身的人工输入等待队列。
/// 每个等待项只存在于当前 Host 进程：重启、扩缩容或跨实例请求不会保留执行句柄。
/// 外部恢复必须同时提供节点保存的恢复密钥和一次性的请求 ID。
/// </summary>
public sealed class NeuCharWorkflowHumanInputService
{
    public const string RequestType = "workflowInput";
    private const int Capacity = 200;
    private const int MaxInputLength = 8_000;
    private const int MaxReasonLength = 2_000;

    private readonly ConcurrentDictionary<string, PendingRequest> _pending = new(StringComparer.Ordinal);
    private readonly NeuCharWorkflowNeuBellProvider? _neuBellProvider;
    private readonly INeuBellPublisher? _neuBellPublisher;

    public NeuCharWorkflowHumanInputService(
        NeuCharWorkflowNeuBellProvider? neuBellProvider = null,
        INeuBellPublisher? neuBellPublisher = null)
    {
        _neuBellProvider = neuBellProvider;
        _neuBellPublisher = neuBellPublisher;
    }

    public PendingRequest Create(
        int workflowId,
        string correlationId,
        int adminUserId,
        string nodeId,
        string nodeName,
        string prompt,
        bool externalResumeEnabled,
        string? externalResumeKey)
    {
        if (workflowId <= 0 || string.IsNullOrWhiteSpace(correlationId) || adminUserId <= 0)
        {
            throw new InvalidOperationException("无法为缺少 Workflow 运行关联的信息创建人工输入请求。");
        }
        if (externalResumeEnabled && string.IsNullOrWhiteSpace(externalResumeKey))
        {
            throw new InvalidOperationException("已启用外部恢复，但未配置恢复密钥。");
        }
        if (_pending.Count >= Capacity)
        {
            throw new InvalidOperationException("当前等待人工输入的 Workflow 请求过多，请先处理已有请求。");
        }

        var request = new PendingRequest(
            workflowId,
            correlationId,
            adminUserId.ToString(),
            nodeId,
            nodeName,
            Limit(prompt, 4_000, "Workflow 正在等待人工输入。"),
            externalResumeEnabled ? Hash(externalResumeKey!) : null);
        if (!_pending.TryAdd(request.RequestId, request))
        {
            throw new InvalidOperationException("无法创建人工输入请求，请重试。");
        }
        return request;
    }

    public IReadOnlyList<WorkflowHumanInteraction> GetPendingInteractions(string correlationId, string userId)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<WorkflowHumanInteraction>();
        }

        return _pending.Values
            .Where(request => string.Equals(request.CorrelationId, correlationId.Trim(), StringComparison.Ordinal)
                              && string.Equals(request.OwnerUserId, userId.Trim(), StringComparison.Ordinal))
            .OrderBy(request => request.CreatedAt)
            .Select(request => request.ToInteraction())
            .ToList();
    }

    public IReadOnlyList<WorkflowExternalHumanInput> GetExternalPending(int workflowId, string? externalResumeKey)
    {
        if (workflowId <= 0 || string.IsNullOrWhiteSpace(externalResumeKey))
        {
            return Array.Empty<WorkflowExternalHumanInput>();
        }

        return _pending.Values
            .Where(request => request.WorkflowId == workflowId && request.MatchesExternalKey(externalResumeKey))
            .OrderBy(request => request.CreatedAt)
            .Select(request => new WorkflowExternalHumanInput(
                request.RequestId,
                request.WorkflowId,
                request.NodeId,
                request.NodeName,
                request.Prompt,
                request.CreatedAt))
            .ToList();
    }

    public async Task<WorkflowHumanInputResolution> ResolveForAdminAsync(
        string correlationId,
        string userId,
        string requestId,
        bool approved,
        string? input = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGet(requestId, out var request))
        {
            return NotHandled("人工输入请求不存在、已处理或已失效。");
        }
        if (!string.Equals(request.CorrelationId, correlationId?.Trim(), StringComparison.Ordinal))
        {
            return Failure("人工输入请求不属于当前 Workflow 运行。");
        }
        if (!string.Equals(request.OwnerUserId, userId?.Trim(), StringComparison.Ordinal))
        {
            return Failure("当前账号无权处理该人工输入请求。");
        }
        return await ResolveCoreAsync(request, approved, input, reason, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkflowHumanInputResolution> ResolveFromExternalAsync(
        string requestId,
        string? externalResumeKey,
        bool approved,
        string? input = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGet(requestId, out var request))
        {
            return NotHandled("人工输入请求不存在、已处理或已失效。");
        }
        if (!request.MatchesExternalKey(externalResumeKey))
        {
            return Failure("外部恢复密钥无效，或该节点未启用外部恢复。");
        }
        return await ResolveCoreAsync(request, approved, input, reason, cancellationToken).ConfigureAwait(false);
    }

    public void Cancel(string? requestId)
    {
        if (!string.IsNullOrWhiteSpace(requestId) && _pending.TryRemove(requestId.Trim(), out var request))
        {
            request.Cancel();
        }
    }

    private async Task<WorkflowHumanInputResolution> ResolveCoreAsync(
        PendingRequest request,
        bool approved,
        string? input,
        string? reason,
        CancellationToken cancellationToken)
    {
        var normalizedInput = input?.Trim();
        var normalizedReason = reason?.Trim();
        if (approved && string.IsNullOrWhiteSpace(normalizedInput))
        {
            return Failure("请填写人工输入后再继续 Workflow。");
        }
        if (normalizedInput?.Length > MaxInputLength || normalizedReason?.Length > MaxReasonLength)
        {
            return Failure("人工输入或说明超过允许长度。");
        }
        if (!_pending.TryRemove(request.RequestId, out var removed))
        {
            return NotHandled("人工输入请求已被其他入口处理。");
        }

        var decision = new WorkflowHumanInputDecision(approved, normalizedInput, normalizedReason);
        removed.Complete(decision);
        await ConsumeNeuBellAsync(removed, cancellationToken).ConfigureAwait(false);
        return new WorkflowHumanInputResolution(
            true,
            true,
            decision.Approved,
            decision.Input,
            decision.Reason,
            decision.Approved ? "人工输入已提交，Workflow 将继续执行。" : "已拒绝本次人工输入，Workflow 将结束当前分支。");
    }

    private bool TryGet(string? requestId, out PendingRequest request)
    {
        request = null!;
        return !string.IsNullOrWhiteSpace(requestId)
               && _pending.TryGetValue(requestId.Trim(), out request);
    }

    private async Task ConsumeNeuBellAsync(PendingRequest request, CancellationToken cancellationToken)
    {
        if (_neuBellProvider == null || string.IsNullOrWhiteSpace(request.NeuBellItemId))
        {
            return;
        }

        try
        {
            await _neuBellProvider.ConsumeItemAsync(
                new NeuBellRequestContext(request.OwnerUserId),
                request.NeuBellItemId,
                cancellationToken).ConfigureAwait(false);
            if (_neuBellPublisher != null)
            {
                await _neuBellPublisher.NotifyChangedAsync(
                    NeuCharWorkflowNeuBellProvider.ProviderIdValue,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // 提醒清理由最佳努力完成；人工决策已经生效，不能因此再次阻断 Workflow。
        }
    }

    private static WorkflowHumanInputResolution NotHandled(string message)
        => new(false, false, false, null, null, message);

    private static WorkflowHumanInputResolution Failure(string message)
        => new(true, false, false, null, null, message);

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));

    private static string Limit(string? value, int maxLength, string fallback)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    public sealed class PendingRequest
    {
        private readonly byte[]? _externalResumeKeyHash;
        private readonly TaskCompletionSource<WorkflowHumanInputDecision> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal PendingRequest(
            int workflowId,
            string correlationId,
            string ownerUserId,
            string nodeId,
            string nodeName,
            string prompt,
            byte[]? externalResumeKeyHash)
        {
            WorkflowId = workflowId;
            CorrelationId = correlationId.Trim();
            OwnerUserId = ownerUserId.Trim();
            NodeId = nodeId?.Trim() ?? string.Empty;
            NodeName = string.IsNullOrWhiteSpace(nodeName) ? "等待人工输入" : nodeName.Trim();
            Prompt = prompt;
            _externalResumeKeyHash = externalResumeKeyHash;
            RequestId = Guid.NewGuid().ToString("N");
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public string RequestId { get; }
        public int WorkflowId { get; }
        public string CorrelationId { get; }
        public string OwnerUserId { get; }
        public string NodeId { get; }
        public string NodeName { get; }
        public string Prompt { get; }
        public string? NeuBellItemId { get; private set; }
        public DateTimeOffset CreatedAt { get; }
        /// <summary>完成后返回人工决策；调用方只能等待，不能篡改决策。</summary>
        public Task<WorkflowHumanInputDecision> Completion => _completion.Task;

        public void SetNeuBellItemId(string? itemId) =>
            NeuBellItemId = string.IsNullOrWhiteSpace(itemId) ? null : itemId.Trim();

        internal void Complete(WorkflowHumanInputDecision decision) => _completion.TrySetResult(decision);

        internal void Cancel() => _completion.TrySetCanceled();

        internal bool MatchesExternalKey(string? key) =>
            _externalResumeKeyHash != null
            && !string.IsNullOrWhiteSpace(key)
            && CryptographicOperations.FixedTimeEquals(_externalResumeKeyHash, NeuCharWorkflowHumanInputService.Hash(key));

        internal WorkflowHumanInteraction ToInteraction() => new(
            RequestId,
            0,
            CorrelationId,
            RequestType,
            NodeName,
            string.Empty,
            string.Empty,
            Prompt,
            string.Empty,
            NeuBellItemId ?? string.Empty,
            CreatedAt);
    }
}
