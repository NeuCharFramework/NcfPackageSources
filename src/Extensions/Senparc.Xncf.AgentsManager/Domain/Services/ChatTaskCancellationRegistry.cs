/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ChatTaskCancellationRegistry.cs
    文件功能描述：按 ChatTask 维度的进程内取消源注册表

    创建标识：Senparc - 20260927
    修改描述：v0.18.1 为运行中的 ChatTask 提供可真正中断模型调用的取消令牌
----------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Threading;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// 进程内按 ChatTask 维度维护取消源（CancellationTokenSource）。
/// 任务启动时注册一个与调用方外部令牌链接的 CTS；ForceStop 时取消对应令牌，
/// 使工作流运行（含进行中的模型 HTTP 调用）能够立即中断，而不是仅在轮询点退出。
/// 单例；每个任务的 CTS 在任务结束时注销并释放。
/// </summary>
public sealed class ChatTaskCancellationRegistry
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _sources = new();

    /// <summary>
    /// 为指定任务注册取消源（与外部令牌链接），并返回生效的令牌。
    /// 若同一任务已存在取消源，旧的会被取消并替换。
    /// </summary>
    public CancellationToken Register(int chatTaskId, CancellationToken externalToken)
    {
        CancellationTokenSource source = externalToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(externalToken)
            : new CancellationTokenSource();

        _sources.AddOrUpdate(chatTaskId, source, (_, existing) =>
        {
            existing.Cancel();
            existing.Dispose();
            return source;
        });

        return source.Token;
    }

    /// <summary>
    /// 尝试取消指定任务；返回是否找到并触发了取消源。
    /// </summary>
    public bool TryCancel(int chatTaskId)
    {
        if (_sources.TryGetValue(chatTaskId, out var source) && !source.IsCancellationRequested)
        {
            source.Cancel();
            return true;
        }

        return _sources.ContainsKey(chatTaskId);
    }

    /// <summary>
    /// 注销并释放指定任务的取消源（任务结束时调用）。
    /// </summary>
    public void Unregister(int chatTaskId)
    {
        if (_sources.TryRemove(chatTaskId, out var source))
        {
            source.Dispose();
        }
    }

    /// <summary>
    /// 指定任务当前是否存在已注册的取消源。
    /// </summary>
    public bool IsRegistered(int chatTaskId) => _sources.ContainsKey(chatTaskId);
}
