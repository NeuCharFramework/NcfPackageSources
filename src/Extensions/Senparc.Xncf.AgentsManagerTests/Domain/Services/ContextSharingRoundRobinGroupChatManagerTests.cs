using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class ContextSharingRoundRobinGroupChatManagerTests
{
    [TestMethod]
    public void LegacyHistoryRemovesApprovalProtocolContentButKeepsTextAndResults()
    {
        var managerType = typeof(AgentTemplateRunner).Assembly.GetType(
            "Senparc.Xncf.AgentsManager.Domain.Services.ContextSharingRoundRobinGroupChatManager",
            throwOnError: true);
        var method = managerType.GetMethod(
            "RemoveApprovalProtocolContent",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        var functionCall = new FunctionCallContent(
            "call-approval",
            "Send",
            new Dictionary<string, object?>());
        var approvalRequest = new ToolApprovalRequestContent("ficc_call-approval", functionCall);
        var approvalResponse = approvalRequest.CreateResponse(true, "approved");
        var history = new[]
        {
            new ChatMessage(
                ChatRole.Assistant,
                new AIContent[]
                {
                    new TextContent("缓存测试已完成"),
                    approvalRequest,
                    approvalResponse,
                    new FunctionResultContent("call-approval", "ok")
                })
        };

        var filtered = ((IEnumerable<ChatMessage>)method.Invoke(null, new object[] { history })!)
            .Single();

        Assert.IsTrue(filtered.Contents.OfType<TextContent>().Any(z => z.Text == "缓存测试已完成"));
        Assert.IsTrue(filtered.Contents.OfType<FunctionResultContent>().Any(z => z.CallId == "call-approval"));
        Assert.IsFalse(filtered.Contents.OfType<ToolApprovalRequestContent>().Any());
        Assert.IsFalse(filtered.Contents.OfType<ToolApprovalResponseContent>().Any());
    }
}
