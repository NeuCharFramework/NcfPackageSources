using Microsoft.Extensions.AI;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.AgentsManager.Domain.Services;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class AgentsManagerHumanInteractionServiceTests
{
    [TestMethod]
    public async Task WorkflowResolutionUsesTheSameRequestAndConsumesAgentsManagerReminder()
    {
        var store = new HumanInTheLoopRequestStore();
        var provider = new AgentsManagerNeuBellProvider();
        var service = new AgentsManagerHumanInteractionService(store, provider);
        var correlationId = "workflow-7-run-abc";
        var pending = store.RegisterHumanTurn(
            42,
            "Human",
            "human:1",
            "请输入确认意见",
            correlationId,
            "user-1");
        var itemId = provider.Send(42, "user-1", "Human");
        pending.SetNeuBellItemId(itemId);

        var result = await service.ResolveWorkflowAsync(
            correlationId,
            "user-1",
            pending.RequestId,
            true,
            "同意继续",
            "Workflow 快速输入");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("同意继续", result.Input);
        Assert.AreEqual(0, store.GetPendingByCorrelationId(correlationId).Count);
        Assert.AreEqual(0, (await provider.GetSnapshotAsync(new NeuBellRequestContext("user-1"))).Items.Count);
        Assert.IsFalse((await service.ResolveWorkflowAsync(
            correlationId,
            "user-1",
            pending.RequestId,
            true,
            "重复提交")).Success);
    }

    [TestMethod]
    public async Task WorkflowResolutionRejectsWrongCorrelationOrRecipient()
    {
        var store = new HumanInTheLoopRequestStore();
        var service = new AgentsManagerHumanInteractionService(
            store,
            new AgentsManagerNeuBellProvider());
        var pending = store.RegisterHumanTurn(
            42,
            "Human",
            "human:1",
            "请输入确认意见",
            "workflow-7-run-abc",
            "user-1");

        var wrongCorrelation = await service.ResolveWorkflowAsync(
            "workflow-7-run-other",
            "user-1",
            pending.RequestId,
            true,
            "同意");
        var wrongRecipient = await service.ResolveWorkflowAsync(
            "workflow-7-run-abc",
            "user-2",
            pending.RequestId,
            true,
            "同意");

        Assert.IsFalse(wrongCorrelation.Success);
        Assert.IsFalse(wrongRecipient.Success);
        Assert.AreEqual(1, store.GetPendingByCorrelationId("workflow-7-run-abc").Count);
    }

    [TestMethod]
    public async Task WorkflowCanResolveAgentGroupToolApprovalAndConsumeReminder()
    {
        var store = new HumanInTheLoopRequestStore();
        var provider = new AgentsManagerNeuBellProvider();
        var service = new AgentsManagerHumanInteractionService(store, provider);
        var correlationId = "workflow-17-run-4f33d29e185c4f43b67c890af104674e";
        var approval = new ToolApprovalRequestContent(
            "approval-1",
            new FunctionCallContent(
                "call-1",
                "Translate",
                new Dictionary<string, object?>
                {
                    ["text"] = "香港理工大学"
                }));
        var pending = store.RegisterToolApproval(
            18229,
            "翻译 Agent",
            approval,
            decision => approval.CreateResponse(decision.Approved, decision.Reason),
            correlationId,
            "user-1");
        pending.SetNeuBellItemId(provider.SendWorkflowToolApproval(
            correlationId,
            "user-1",
            "翻译 Agent",
            "Translate"));

        var interactions = await service.GetWorkflowPendingAsync(
            correlationId,
            "user-1");
        Assert.AreEqual(pending.RequestId, interactions.Single().RequestId);

        var result = await service.ResolveWorkflowAsync(
            correlationId,
            "user-1",
            pending.RequestId,
            true,
            reason: "Workflow 快速审批");
        var decision = await pending.Completion;

        Assert.IsTrue(result.Success);
        Assert.IsTrue(decision.Approved);
        Assert.IsInstanceOfType<ToolApprovalResponseContent>(pending.ResolvedResponse);
        Assert.AreEqual(
            0,
            (await provider.GetSnapshotAsync(new NeuBellRequestContext("user-1"))).Items.Count);
    }
}
