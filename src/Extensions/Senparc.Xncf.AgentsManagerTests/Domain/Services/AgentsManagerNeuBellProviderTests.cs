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

    [TestMethod]
    public async Task WorkflowToolApprovalReminderReturnsToTheSpecificRun()
    {
        var provider = new AgentsManagerNeuBellProvider();
        var runId = Guid.Parse("4f33d29e-185c-4f43-b67c-890af104674e");

        provider.SendWorkflowToolApproval(
            $"workflow-17-run-{runId:N}",
            "user-1",
            "资料 Agent",
            "Search");

        var snapshot = await provider.GetSnapshotAsync(new NeuBellRequestContext("user-1"));
        Assert.AreEqual(1, snapshot.Items.Count);
        Assert.AreEqual(
            $"/Admin/NeuCharWorkflow/Index?workflowId=17&runId={runId:N}",
            snapshot.Items[0].DetailUrl);
    }
}
