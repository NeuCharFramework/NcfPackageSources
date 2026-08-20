/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfModulesInventoryRequestWaiter.cs
    文件功能描述：XncfModulesInventoryRequestWaiter 相关实现
    
    
    创建标识：Senparc - 20260510
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.XncfBuilder.Abstractions.Events;

namespace Senparc.Xncf.XncfBuilder.Abstractions
{
    /// <summary>
    /// 将 <see cref="XncfModulesInventoryRequestEvent"/> / <see cref="XncfModulesInventoryResponseEvent"/> 与等待中的调用方关联。
    /// </summary>
    public interface IXncfModulesInventoryRequestWaiter
    {
        void RegisterRequest(string requestId);

        bool TrySetResult(
            string requestId,
            bool success,
            string message,
            XncfModuleInventoryItem[] installed,
            XncfModuleInventoryItem[] notInstalled);

        Task<(bool Ok, string Message, XncfModuleInventoryItem[] Installed, XncfModuleInventoryItem[] NotInstalled)>
            WaitForResponseAsync(string requestId, TimeSpan timeout, CancellationToken cancellationToken = default);
    }

    public sealed class XncfModulesInventoryRequestWaiter : IXncfModulesInventoryRequestWaiter
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<(bool Ok, string Message, XncfModuleInventoryItem[] Installed, XncfModuleInventoryItem[] NotInstalled)>> _pending =
            new(StringComparer.Ordinal);

        public void RegisterRequest(string requestId)
        {
            var tcs = new TaskCompletionSource<(bool Ok, string Message, XncfModuleInventoryItem[] Installed, XncfModuleInventoryItem[] NotInstalled)>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[requestId] = tcs;
        }

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
