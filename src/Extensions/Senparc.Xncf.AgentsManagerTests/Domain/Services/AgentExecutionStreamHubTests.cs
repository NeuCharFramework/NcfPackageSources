using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class AgentExecutionStreamHubTests
{
    [TestMethod]
    public async Task TerminalEventIsDeliveredBeforeStreamCompletes()
    {
        var hub = new AgentExecutionStreamHub();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var subscription = hub
            .Subscribe(42, replayBuffered: false, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        var firstMove = subscription.MoveNextAsync().AsTask();
        hub.Publish(new AgentExecutionStreamEvent
        {
            AgentExecutionTaskId = 42,
            Sequence = 1,
            EventType = "status",
            Status = "Finished",
            IsFinal = true
        });

        Assert.IsTrue(await firstMove);
        Assert.AreEqual("Finished", subscription.Current.Status);
        Assert.IsFalse(await subscription.MoveNextAsync());
    }
}
