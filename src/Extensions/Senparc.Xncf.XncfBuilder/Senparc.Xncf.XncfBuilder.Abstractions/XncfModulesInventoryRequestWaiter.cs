/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModulesInventoryRequestWaiter.cs
    File description: XncfModulesInventoryRequestWaiter related implementation

    Created by: Senparc - 20260510

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.1.6-preview7 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.1.6.2-preview8 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.XncfBuilder.Abstractions.Events;

namespace Senparc.Xncf.XncfBuilder.Abstractions
{
    /// <summary>
    /// Coordinates request/response correlation for asynchronous XNCF module inventory queries.
    /// </summary>
    public interface IXncfModulesInventoryRequestWaiter
    {
        /// <summary>
        /// Registers a request identifier and creates its asynchronous completion source.
        /// </summary>
        /// <param name="requestId">The unique inventory request identifier.</param>
        void RegisterRequest(string requestId);

        /// <summary>
        /// Completes a registered request when an inventory response arrives.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        /// <param name="success">Whether inventory collection succeeded.</param>
        /// <param name="message">The response message.</param>
        /// <param name="installed">Installed module records.</param>
        /// <param name="notInstalled">Missing module records.</param>
        /// <returns><see langword="true"/> when a registered waiter accepted the result.</returns>
        bool TrySetResult(
            string requestId,
            bool success,
            string message,
            XncfModuleInventoryItem[] installed,
            XncfModuleInventoryItem[] notInstalled);

        /// <summary>
        /// Waits for a registered inventory response until timeout or cancellation.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        /// <param name="timeout">The maximum wait duration.</param>
        /// <param name="cancellationToken">Token used to cancel waiting.</param>
        /// <returns>The response tuple, or a timeout/missing-request result.</returns>
        Task<(bool Ok, string Message, XncfModuleInventoryItem[] Installed, XncfModuleInventoryItem[] NotInstalled)>
            WaitForResponseAsync(string requestId, TimeSpan timeout, CancellationToken cancellationToken = default);
    }

    public sealed class XncfModulesInventoryRequestWaiter : IXncfModulesInventoryRequestWaiter
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<(bool Ok, string Message, XncfModuleInventoryItem[] Installed, XncfModuleInventoryItem[] NotInstalled)>> _pending =
            new(StringComparer.Ordinal);

        /// <inheritdoc />
        public void RegisterRequest(string requestId)
        {
            var tcs = new TaskCompletionSource<(bool Ok, string Message, XncfModuleInventoryItem[] Installed, XncfModuleInventoryItem[] NotInstalled)>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[requestId] = tcs;
        }

        /// <inheritdoc />
        public bool TrySetResult(
            string requestId,
            bool success,
            string message,
            XncfModuleInventoryItem[] installed,
            XncfModuleInventoryItem[] notInstalled)
        {
            if (_pending.TryGetValue(requestId, out var tcs))
            {
                return tcs.TrySetResult((success, message, installed ?? Array.Empty<XncfModuleInventoryItem>(), notInstalled ?? Array.Empty<XncfModuleInventoryItem>()));
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<(bool Ok, string Message, XncfModuleInventoryItem[] Installed, XncfModuleInventoryItem[] NotInstalled)> WaitForResponseAsync(
            string requestId,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            if (!_pending.TryGetValue(requestId, out var tcs))
            {
                return (false, "未找到对应的等待项，请确认已调用 RegisterRequest。", Array.Empty<XncfModuleInventoryItem>(), Array.Empty<XncfModuleInventoryItem>());
            }

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linked.CancelAfter(timeout);
                return await tcs.Task.WaitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return (false, $"等待 XncfBuilder 响应超时（{timeout.TotalSeconds} 秒）。", Array.Empty<XncfModuleInventoryItem>(), Array.Empty<XncfModuleInventoryItem>());
            }
            finally
            {
                _pending.TryRemove(requestId, out _);
            }
        }
    }
}
