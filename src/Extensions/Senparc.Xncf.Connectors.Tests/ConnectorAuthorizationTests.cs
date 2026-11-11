using Senparc.Xncf.Connectors.Abstractions;

namespace Senparc.Xncf.Connectors.Tests;

[TestClass]
public class ConnectorAuthorizationTests
{
    private static readonly AgentConnectorBinding Headquarters = new()
    {
        InstanceId = "hq",
        Enabled = true
    };

    [TestMethod]
    public void UnboundInstanceIsDenied()
    {
        Assert.IsFalse(ConnectorAuthorization.Allows([Headquarters], "other", "get-checkin-records", false, null));
    }

    [TestMethod]
    public void EmptyActionListAllowsEveryAction()
    {
        Assert.IsTrue(ConnectorAuthorization.Allows([Headquarters], "hq", "get-checkin-records", false, null));
    }

    [TestMethod]
    public void ChatSelectionIntersectsAgentPermission()
    {
        Assert.IsFalse(ConnectorAuthorization.Allows([Headquarters], "hq", "get-checkin-records", true, ["branch"]));
        Assert.IsTrue(ConnectorAuthorization.Allows([Headquarters], "hq", "get-checkin-records", true, ["hq"]));
    }

    [TestMethod]
    public void EmptyChatSelectionUsesNoConnector()
    {
        Assert.IsFalse(ConnectorAuthorization.Allows([Headquarters], "hq", "get-checkin-records", true, []));
    }

    [TestMethod]
    public void DisabledBindingIsDenied()
    {
        var binding = new AgentConnectorBinding { InstanceId = "hq", Enabled = false };
        Assert.IsFalse(ConnectorAuthorization.Allows([binding], "hq", "get-checkin-records", false, null));
    }
}
