using Senparc.Xncf.AgentsManager.Domain.Services;
using System.Threading;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class ChatTaskCancellationRegistryTests
{
    [TestMethod]
    public void TryCancel_ShouldCancelTheRegisteredTaskToken()
    {
        var registry = new ChatTaskCancellationRegistry();
        var token = registry.Register(101, CancellationToken.None);

        Assert.IsFalse(token.IsCancellationRequested);
        Assert.IsTrue(registry.IsRegistered(101));
        Assert.IsTrue(registry.TryCancel(101));
        Assert.IsTrue(token.IsCancellationRequested);
    }

    [TestMethod]
    public void RegisteredToken_ShouldFollowTheExternalToken()
    {
        var registry = new ChatTaskCancellationRegistry();
        using var external = new CancellationTokenSource();
        var token = registry.Register(102, external.Token);

        external.Cancel();
        Assert.IsTrue(token.IsCancellationRequested);
    }

    [TestMethod]
    public void Unregister_ShouldReleaseAndRejectFurtherCancel()
    {
        var registry = new ChatTaskCancellationRegistry();
        var token = registry.Register(103, CancellationToken.None);

        registry.Unregister(103);
        Assert.IsFalse(registry.IsRegistered(103));
        Assert.IsFalse(registry.TryCancel(103));
    }

    [TestMethod]
    public void RegisterTwice_ShouldCancelAndReplaceThePreviousSource()
    {
        var registry = new ChatTaskCancellationRegistry();
        var first = registry.Register(104, CancellationToken.None);
        var second = registry.Register(104, CancellationToken.None);

        // 同一任务重复注册时，旧的取消源应立即被取消，避免同一任务出现两个并发的运行上下文。
        Assert.IsTrue(first.IsCancellationRequested);
        Assert.IsFalse(second.IsCancellationRequested);

        registry.TryCancel(104);
        Assert.IsTrue(second.IsCancellationRequested);
    }
}
