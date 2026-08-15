using Senparc.Xncf.AgentsManager.Domain.Services;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class HumanParticipantAgentTests
{
    [TestMethod]
    public async Task HumanAgentWaitsForAndReturnsSubmittedText()
    {
        var store = new HumanInTheLoopRequestStore();
        var requestCreated = new TaskCompletionSource<PendingHumanRequest>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var agent = new HumanParticipantAgent(
            store,
            42,
            "human:7",
            pending =>
            {
                requestCreated.TrySetResult(pending);
                return Task.CompletedTask;
            },
            (_, _) => Task.CompletedTask);

        var runTask = agent.RunAsync("上一位 Agent 的消息");
        var pending = await requestCreated.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(42, pending.ChatTaskId);
        Assert.AreEqual("human:7", pending.ParticipantKey);
        Assert.AreEqual("humanTurn", pending.RequestType);
        Assert.IsTrue(pending.Prompt.Contains("上一位 Agent 的消息", StringComparison.Ordinal));

        Assert.IsTrue(store.TryResolve(
            pending.RequestId,
            new HumanInTheLoopDecision(true, "测试回复", "用户实际输入"),
            out _));

        var response = await runTask;
        Assert.AreEqual("用户实际输入", response.Text);
        Assert.AreEqual(0, store.GetPending(42).Count);
    }
}
