/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowHumanInputService.cs
    File description: Native Workflow human-input suspension, resumption, and controlled external access

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support Human Input node suspension and external resumption

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance Workflow function calls, task control, and replay management

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

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

/// <summary>
/// Represents a human approval decision and optional input/reason text.
/// </summary>
public sealed record WorkflowHumanInputDecision(bool Approved, string? Input = null, string? Reason = null);

/// <summary>
/// Represents the result of resolving a pending Workflow human-input request.
/// </summary>
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

    /// <summary>
    /// Initializes the in-process human-input wait queue and optional notification services.
    /// </summary>
    /// <param name="neuBellProvider">Optional provider used to consume approval notifications.</param>
    /// <param name="neuBellPublisher">Optional publisher used to refresh notifications.</param>
    public NeuCharWorkflowHumanInputService(
        NeuCharWorkflowNeuBellProvider? neuBellProvider = null,
        INeuBellPublisher? neuBellPublisher = null)
    {
        _neuBellProvider = neuBellProvider;
        _neuBellPublisher = neuBellPublisher;
    }

    /// <summary>
    /// Creates a bounded pending human-input request for a workflow run.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="correlationId">The workflow run correlation identifier.</param>
    /// <param name="adminUserId">The owner/admin user identifier.</param>
    /// <param name="nodeId">The human-input node identifier.</param>
    /// <param name="nodeName">The human-input node display name.</param>
    /// <param name="prompt">The prompt shown to the human.</param>
    /// <param name="externalResumeEnabled">Whether external resumption is allowed.</param>
    /// <param name="externalResumeKey">The secret key used for external resumption.</param>
    /// <returns>The pending request object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when context, external key, or queue capacity constraints fail.</exception>
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

    /// <summary>
    /// Returns pending interactions visible to an authenticated owner.
    /// </summary>
    /// <param name="correlationId">The workflow run correlation identifier.</param>
    /// <param name="userId">The owner/user identifier.</param>
    /// <returns>Pending interaction views ordered by creation time.</returns>
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

    /// <summary>
    /// Returns pending interactions accessible with an external resume key without exposing the key.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="externalResumeKey">The external resume key.</param>
    /// <returns>Minimal pending interaction views.</returns>
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

    /// <summary>
    /// Resolves a pending request for its authenticated workflow owner.
    /// </summary>
    /// <param name="correlationId">The workflow run correlation identifier.</param>
    /// <param name="userId">The owner/user identifier.</param>
    /// <param name="requestId">The pending request identifier.</param>
    /// <param name="approved">Whether the human input is approved.</param>
    /// <param name="input">The optional human input.</param>
    /// <param name="reason">The optional rejection/approval reason.</param>
    /// <param name="cancellationToken">Token used to cancel notification cleanup.</param>
    /// <returns>The resolution result.</returns>
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

    /// <summary>
    /// Resolves a pending request using its one-time external resume key.
    /// </summary>
    /// <param name="requestId">The pending request identifier.</param>
    /// <param name="externalResumeKey">The configured external resume key.</param>
    /// <param name="approved">Whether the human input is approved.</param>
    /// <param name="input">The optional human input.</param>
    /// <param name="reason">The optional rejection/approval reason.</param>
    /// <param name="cancellationToken">Token used to cancel notification cleanup.</param>
    /// <returns>The resolution result.</returns>
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

    /// <summary>
    /// Removes and cancels a pending human-input request.
    /// </summary>
    /// <param name="requestId">The pending request identifier.</param>
    public void Cancel(string? requestId)
    {
        if (!string.IsNullOrWhiteSpace(requestId) && _pending.TryRemove(requestId.Trim(), out var request))
        {
            request.Cancel();
        }
    }

    /// <summary>
    /// Validates, atomically consumes, and completes a pending human-input request.
    /// </summary>
    /// <param name="request">The pending request to resolve.</param>
    /// <param name="approved">Whether the submitted decision approves continuation.</param>
    /// <param name="input">The optional human input required for an approval.</param>
    /// <param name="reason">The optional decision explanation.</param>
    /// <param name="cancellationToken">Token used for best-effort notification cleanup.</param>
    /// <returns>The resolution result visible to the caller.</returns>
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

    /// <summary>
    /// Looks up a pending request by its normalized one-time identifier.
    /// </summary>
    /// <param name="requestId">The request identifier to locate.</param>
    /// <param name="request">Receives the pending request when found.</param>
    /// <returns><see langword="true"/> when an active request exists.</returns>
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

    /// <summary>
    /// Best-effort consumes the notification associated with a resolved request.
    /// </summary>
    /// <param name="request">The resolved request and optional notification identifier.</param>
    /// <param name="cancellationToken">Token used to cancel notification cleanup.</param>
    /// <returns>A task that completes after cleanup or ignored non-cancellation failures.</returns>
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

    /// <summary>
    /// Creates the standard result for a missing, expired, or already-consumed request.
    /// </summary>
    /// <param name="message">The caller-facing explanation.</param>
    /// <returns>A resolution marked as not handled and unsuccessful.</returns>
    private static WorkflowHumanInputResolution NotHandled(string message)
        => new(false, false, false, null, null, message);

    /// <summary>
    /// Creates the standard result for a request that exists but cannot be resolved.
    /// </summary>
    /// <param name="message">The caller-facing failure explanation.</param>
    /// <returns>A resolution marked as handled but unsuccessful.</returns>
    private static WorkflowHumanInputResolution Failure(string message)
        => new(true, false, false, null, null, message);

    /// <summary>
    /// Hashes a resume key after trimming it for constant-time comparison.
    /// </summary>
    /// <param name="value">The resume key to hash.</param>
    /// <returns>The SHA-256 digest of the normalized key.</returns>
    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));

    /// <summary>
    /// Trims input, substitutes a fallback for blank text, and bounds the result length.
    /// </summary>
    /// <param name="value">The text to normalize.</param>
    /// <param name="maxLength">The maximum number of characters to retain.</param>
    /// <param name="fallback">The value used for blank input.</param>
    /// <returns>The normalized bounded text.</returns>
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

        /// <summary>
        /// Creates an in-process request and prepares its one-time completion source.
        /// </summary>
        /// <param name="workflowId">The workflow identifier.</param>
        /// <param name="correlationId">The run correlation identifier.</param>
        /// <param name="ownerUserId">The owner identifier.</param>
        /// <param name="nodeId">The human-input node identifier.</param>
        /// <param name="nodeName">The human-input node display name.</param>
        /// <param name="prompt">The bounded prompt text.</param>
        /// <param name="externalResumeKeyHash">The optional hashed external resume key.</param>
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

        /// <summary>
        /// Associates the request with a notification item for later best-effort consumption.
        /// </summary>
        /// <param name="itemId">The notification item identifier.</param>
        public void SetNeuBellItemId(string? itemId) =>
            NeuBellItemId = string.IsNullOrWhiteSpace(itemId) ? null : itemId.Trim();

        /// <summary>
        /// Completes the request with a human decision.
        /// </summary>
        /// <param name="decision">The decision delivered to the waiting workflow.</param>
        internal void Complete(WorkflowHumanInputDecision decision) => _completion.TrySetResult(decision);

        /// <summary>
        /// Cancels the waiting workflow without producing a decision.
        /// </summary>
        internal void Cancel() => _completion.TrySetCanceled();

        /// <summary>
        /// Compares a candidate external key with the stored digest in constant time.
        /// </summary>
        /// <param name="key">The candidate external resume key.</param>
        /// <returns><see langword="true"/> when the key matches an enabled request.</returns>
        internal bool MatchesExternalKey(string? key) =>
            _externalResumeKeyHash != null
            && !string.IsNullOrWhiteSpace(key)
            && CryptographicOperations.FixedTimeEquals(_externalResumeKeyHash, NeuCharWorkflowHumanInputService.Hash(key));

        /// <summary>
        /// Projects the request into the owner-visible interaction contract without exposing the resume key.
        /// </summary>
        /// <returns>The interaction view returned to authenticated callers.</returns>
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
