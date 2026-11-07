using Senparc.Xncf.AgentsManager.Application.Dtos;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class ChatTaskStreamHubTests
{
    [TestMethod]
    public async Task TerminalStatusIsDeliveredBeforeStreamCompletes()
    {
        var hub = new ChatTaskStreamHub();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var subscription = hub
            .Subscribe(42, replayBuffered: false, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        var firstMove = subscription.MoveNextAsync().AsTask();
        hub.Publish(new ChatTaskStreamEvent
        {
            EventType = "status",
            ChatTaskId = 42,
            Text = ChatTask_Status.Failed.ToString(),
            IsFinal = true
        });

        Assert.IsTrue(await firstMove);
        Assert.AreEqual("Failed", subscription.Current.Text);
        Assert.IsFalse(await subscription.MoveNextAsync());
    }

    [TestMethod]
    public async Task ClosingASubscriberDoesNotFinishTheTaskStream()
    {
        var hub = new ChatTaskStreamHub();
        using var disconnected = new CancellationTokenSource();
        await using var first = hub.Subscribe(7, replayBuffered: false, disconnected.Token).GetAsyncEnumerator(disconnected.Token);
        var waiting = first.MoveNextAsync().AsTask();
        disconnected.Cancel();
        try { await waiting; } catch (OperationCanceledException) { }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var second = hub.Subscribe(7, replayBuffered: false, timeout.Token).GetAsyncEnumerator(timeout.Token);
        var next = second.MoveNextAsync().AsTask();
        hub.Publish(new ChatTaskStreamEvent
        {
            EventType = "chunk",
            ChatTaskId = 7,
            Text = "still running"
        });

        Assert.IsTrue(await next);
        Assert.AreEqual("still running", second.Current.Text);
    }
}

[TestClass]
public class ChatTaskWindowIndependenceTests
{
    [TestMethod]
    public void HttpRunRequestDoesNotCarryTheBrowserCancellationToken()
    {
        var request = new ChatGroup_RunGroupRequest();
        Assert.IsFalse(request.CancellationToken.CanBeCanceled);
    }

    [TestMethod]
    public void BackgroundTaskCancelComesFromTheRegistryRatherThanADisconnectedPage()
    {
        var registry = new ChatTaskCancellationRegistry();
        var token = registry.Register(11, CancellationToken.None);
        Assert.IsTrue(token.CanBeCanceled);
        Assert.IsFalse(token.IsCancellationRequested);

        using var page = new CancellationTokenSource();
        page.Cancel();
        Assert.IsFalse(token.IsCancellationRequested, "关闭页面不能取消已经启动的 ChatTask。");

        Assert.IsTrue(registry.TryCancel(11));
        Assert.IsTrue(token.IsCancellationRequested);
        registry.Unregister(11);
    }
}
