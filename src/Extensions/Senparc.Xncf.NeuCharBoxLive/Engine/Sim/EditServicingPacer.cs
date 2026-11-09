/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：EditServicingPacer.cs
    文件功能描述：NeuCharBox Live 引擎（Sim）：EditServicingPacer


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

/// <summary>
/// A pacer whose waits also serve the visitor's hand edits: each time <paramref name="whenPending"/> completes while the
/// inner wait runs, <paramref name="service"/> applies what is waiting, and once more after the wait if an edit still
/// waits (an instant wait ends before the loop can see an edit made during it). The request runner's fast-forward
/// paces with one, so an edit lands at the current tick. Contract: <paramref name="service"/> must drain the queue
/// (EditQueue.TakeAll): <paramref name="whenPending"/> stays complete while an edit waits, so one left behind spins the wait.
/// </summary>
public sealed class EditServicingPacer(IPacer inner, Func<Task> whenPending, Action service) : IPacer
{
    public Task WaitAsync(TimeSpan d, CancellationToken ct) => Serve(inner.WaitAsync(d, ct), whenPending, service);

    /// <summary>Awaits <paramref name="work"/> the same way: serving edits while it runs and once after. <paramref name="service"/> must drain the queue, or this spins.</summary>
    public static async Task Serve(Task work, Func<Task> whenPending, Action service)
    {
        while (!work.IsCompleted)
            if (await Task.WhenAny(work, whenPending()) != work) service();
        if (whenPending().IsCompleted) service();
        await work;
    }

    /// <inheritdoc cref="Serve(Task, Func{Task}, Action)"/>
    public static async Task<T> Serve<T>(Task<T> work, Func<Task> whenPending, Action service)
    {
        await Serve((Task)work, whenPending, service);
        return await work;
    }
}
