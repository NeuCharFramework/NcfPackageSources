using Senparc.Xncf.AgentsManager.Domain.Services;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class HumanInTheLoopPolicyTests
{
    [TestMethod]
    public void AutomaticLevelAllowsBothToolKinds()
    {
        var policy = HumanInTheLoopPolicyResolver.Resolve(HumanInTheLoopLevel.Automatic, ToolPermissionMode.Inherit, ToolPermissionMode.Inherit);

        Assert.AreEqual(ToolPermissionMode.Automatic, policy.PluginTools);
        Assert.AreEqual(ToolPermissionMode.Automatic, policy.McpTools);
        Assert.IsFalse(policy.IncludeHumanParticipant);
    }

    [TestMethod]
    public void RiskBasedLevelOnlyApprovesMcpTools()
    {
        var policy = HumanInTheLoopPolicyResolver.Resolve(HumanInTheLoopLevel.RiskBased, ToolPermissionMode.Inherit, ToolPermissionMode.Inherit);

        Assert.AreEqual(ToolPermissionMode.Automatic, policy.PluginTools);
        Assert.AreEqual(ToolPermissionMode.RequireApproval, policy.McpTools);
    }

    [TestMethod]
    public void ApprovalLevelCannotBeWeakenedByAutomaticOverride()
    {
        var policy = HumanInTheLoopPolicyResolver.Resolve(
            HumanInTheLoopLevel.ToolApproval,
            ToolPermissionMode.Automatic,
            ToolPermissionMode.Deny);

        Assert.AreEqual(ToolPermissionMode.RequireApproval, policy.PluginTools);
        Assert.AreEqual(ToolPermissionMode.Deny, policy.McpTools);
    }

    [TestMethod]
    public void LegacyApprovalFlagKeepsAllToolApprovalSemantics()
    {
        var policy = HumanInTheLoopPolicyResolver.Resolve(
            HumanInTheLoopLevel.Automatic,
            ToolPermissionMode.Inherit,
            ToolPermissionMode.Inherit,
            legacyRequireHumanApproval: true);

        Assert.AreEqual(HumanInTheLoopLevel.ToolApproval, policy.Level);
        Assert.AreEqual(ToolPermissionMode.RequireApproval, policy.PluginTools);
        Assert.AreEqual(ToolPermissionMode.RequireApproval, policy.McpTools);
    }

    [TestMethod]
    public void HumanParticipantLevelIncludesHumanParticipant()
    {
        var policy = HumanInTheLoopPolicyResolver.Resolve(
            HumanInTheLoopLevel.HumanParticipant,
            ToolPermissionMode.Inherit,
            ToolPermissionMode.Inherit);

        Assert.IsTrue(policy.IncludeHumanParticipant);
        Assert.AreEqual(ToolPermissionMode.RequireApproval, policy.PluginTools);
        Assert.AreEqual(ToolPermissionMode.RequireApproval, policy.McpTools);
    }
}
