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
}
