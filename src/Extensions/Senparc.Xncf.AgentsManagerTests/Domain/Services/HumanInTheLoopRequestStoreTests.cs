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

        var responseFactoryCalls = 0;
        var pending = store.RegisterToolApproval(
            42,
            "审核智能体",
            request,
            decision =>
            {
                responseFactoryCalls++;
                return request.CreateResponse(decision.Approved, decision.Reason);
            });

        Assert.AreEqual(1, store.GetPending(42).Count);
        Assert.IsTrue(store.TryResolve(
            pending.RequestId,
            new HumanInTheLoopDecision(false, "用户拒绝"),
            out var resolved));
        Assert.AreSame(pending, resolved);

        var decision = await pending.Completion;
        Assert.IsFalse(decision.Approved);
        Assert.AreEqual("用户拒绝", decision.Reason);
        Assert.IsInstanceOfType<ToolApprovalResponseContent>(pending.ResolvedResponse);
        Assert.AreEqual(1, responseFactoryCalls);
        Assert.AreEqual(0, store.GetPending(42).Count);
    }

    [TestMethod]
    public void ToolApprovalDtoKeepsReadableUnicodeAndLargeArguments()
    {
        var store = new HumanInTheLoopRequestStore();
        var largeText = string.Concat(Enumerable.Repeat("长参数", 2500));
        var request = new ToolApprovalRequestContent(
            "approval-unicode",
            new FunctionCallContent(
                "call-unicode",
                "Translate",
                new Dictionary<string, object?>
                {
                    ["text"] = "香港理工大学",
                    ["content"] = largeText
                }));

        var pending = store.RegisterToolApproval(
            43,
            "翻译 Agent",
            request,
            decision => request.CreateResponse(decision.Approved, decision.Reason));
        var dto = pending.ToDto();

        StringAssert.Contains(dto.ToolArguments, "香港理工大学");
        Assert.IsFalse(dto.ToolArguments.Contains("\\u9999", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(dto.ToolArguments.Length > 5000);
    }
}
