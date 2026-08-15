using Microsoft.Extensions.AI;
using Senparc.Xncf.AgentsManager.Domain.Services;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class HumanInTheLoopRequestStoreTests
{
    [TestMethod]
    public async Task ResolveToolApprovalCompletesWaitingRequest()
    {
        var store = new HumanInTheLoopRequestStore();
        var toolCall = new FunctionCallContent(
            "call-1",
            "delete_file",
            new Dictionary<string, object?> { ["path"] = "/tmp/example.txt" });
        var request = new ToolApprovalRequestContent("approval-1", toolCall);

        var pending = store.RegisterToolApproval(
            42,
            "审核智能体",
            request,
            decision => request.CreateResponse(decision.Approved, decision.Reason));

        Assert.AreEqual(1, store.GetPending(42).Count);
        Assert.IsTrue(store.TryResolve(
            pending.RequestId,
            new HumanInTheLoopDecision(false, "用户拒绝"),
            out var resolved));
        Assert.AreSame(pending, resolved);

        var decision = await pending.Completion;
        Assert.IsFalse(decision.Approved);
        Assert.AreEqual("用户拒绝", decision.Reason);
        Assert.IsInstanceOfType<ToolApprovalResponseContent>(pending.CreateResponseFor(decision));
        Assert.AreEqual(0, store.GetPending(42).Count);
    }
}
