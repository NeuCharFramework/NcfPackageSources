using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.AgentsManager.Domain.Services;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class AgentsManagerNeuBellProviderTests
{
    [TestMethod]
    public async Task HumanReminderIsVisibleUntilTheBusinessReplyConsumesIt()
    {
        var provider = new AgentsManagerNeuBellProvider();
        var itemId = provider.Send(42, "user-1", "Human");
        var context = new NeuBellRequestContext("user-1");

        var snapshot = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(1, snapshot.Items.Count);
        Assert.IsTrue(snapshot.Items[0].DetailUrl.Contains("#tab=third&taskId=42", StringComparison.Ordinal));

        Assert.AreEqual(0, await provider.ConsumeItemAsync(new NeuBellRequestContext("user-2"), itemId));
        Assert.AreEqual(1, await provider.ConsumeItemAsync(context, itemId));
        Assert.AreEqual(0, (await provider.GetSnapshotAsync(context)).Items.Count);
    }
}
