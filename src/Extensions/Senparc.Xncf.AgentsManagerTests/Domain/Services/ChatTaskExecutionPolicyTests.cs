using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class ChatTaskExecutionPolicyTests
{
    [TestMethod]
    public void TaskRetainsTheEffectiveHumanInTheLoopPolicy()
    {
        var dto = new ChatTaskDto(
            "策略任务",
            12,
            34,
            ChatTask_Status.Waiting,
            "执行",
            "策略测试",
            true,
            HookPlatform.None,
            string.Empty,
            false,
            DateTime.Now,
            DateTime.Now,
            null)
        {
            ExecutionPolicyCaptured = true,
            RequireHumanApproval = false,
            HumanInTheLoopLevel = HumanInTheLoopLevel.ToolApproval,
            PluginToolPermission = ToolPermissionMode.RequireApproval,
            McpToolPermission = ToolPermissionMode.Deny,
            IncludeHumanParticipant = false,
            ChatMaxRound = 4
        };

        var task = new ChatTask(dto);
        var roundTrip = new ChatTaskDto(task);

        Assert.IsTrue(roundTrip.ExecutionPolicyCaptured);
        Assert.AreEqual(HumanInTheLoopLevel.ToolApproval, roundTrip.HumanInTheLoopLevel);
        Assert.AreEqual(ToolPermissionMode.RequireApproval, roundTrip.PluginToolPermission);
        Assert.AreEqual(ToolPermissionMode.Deny, roundTrip.McpToolPermission);
        Assert.AreEqual(4, roundTrip.ChatMaxRound);
    }
}
