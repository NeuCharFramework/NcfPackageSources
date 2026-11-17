/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowHumanInputService.cs
    File description: Native Workflow human-input suspension, resumption, and controlled external access

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support Human Input node suspension and external resumption

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance Workflow function calls, task control, and replay management

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

/// <summary>Minimal pending-input view for controlled external callers; never return the resume key.</summary>
public sealed record WorkflowExternalHumanInput(
    string RequestId,
    int WorkflowId,
    string NodeId,
    string NodeName,
    string Prompt,
    DateTimeOffset CreatedAt);

/// <summary>
/// The Workflow-owned human-input wait queue.
/// Each wait item exists only in the current host process; restarts, scaling, and cross-instance requests do not preserve the execution handle.
/// External resumption requires both the resume key stored on the node and a one-time request ID.
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
            throw new InvalidOperationException("Cannot create a human-input request without Workflow run context.");
        }
        if (externalResumeEnabled && string.IsNullOrWhiteSpace(externalResumeKey))
        {
            throw new InvalidOperationException("External resumption is enabled, but no resume key is configured.");
        }
        if (_pending.Count >= Capacity)
        {
            throw new InvalidOperationException("Too many Workflow requests are waiting for human input; handle the existing requests first.");
        }

        var request = new PendingRequest(
            workflowId,
            correlationId,
            adminUserId.ToString(),
            nodeId,
            nodeName,
            Limit(prompt, 4_000, "Workflow is waiting for human input."),
            externalResumeEnabled ? Hash(externalResumeKey!) : null);
        if (!_pending.TryAdd(request.RequestId, request))
        {
            throw new InvalidOperationException("Could not create the human-input request. Please retry.");
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
            return NotHandled("The human-input request does not exist, was already handled, or has expired.");
        }
        if (!string.Equals(request.CorrelationId, correlationId?.Trim(), StringComparison.Ordinal))
        {
            return Failure("The human-input request does not belong to the current Workflow run.");
        }
        if (!string.Equals(request.OwnerUserId, userId?.Trim(), StringComparison.Ordinal))
        {
            return Failure("The current account cannot handle this human-input request.");
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
            return NotHandled("The human-input request does not exist, was already handled, or has expired.");
        }
        if (!request.MatchesExternalKey(externalResumeKey))
        {
            return Failure("The external resume key is invalid, or external resumption is disabled for this node.");
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
            return Failure("Enter the human input before continuing the Workflow.");
        }
        if (normalizedInput?.Length > MaxInputLength || normalizedReason?.Length > MaxReasonLength)
        {
            return Failure("The human input or explanation exceeds the allowed length.");
        }
        if (!_pending.TryRemove(request.RequestId, out var removed))
        {
            return NotHandled("The human-input request was handled by another entry point.");
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
            decision.Approved ? "Human input submitted; the Workflow will continue." : "This human input was rejected; the Workflow will end the current branch.");
    }

    private bool TryGet(string? requestId, out PendingRequest request)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            request = null!;
            return false;
        }

        if (_pending.TryGetValue(requestId.Trim(), out var found))
        {
            request = found;
            return true;
        }

        request = null!;
        return false;
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
            // Notification cleanup is best-effort; the human decision is already effective and must not block the Workflow again.
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
            NodeName = string.IsNullOrWhiteSpace(nodeName) ? "Waiting for human input" : nodeName.Trim();
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
        /// <summary>Return the human decision after completion; callers may wait but cannot alter the decision.</summary>
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
