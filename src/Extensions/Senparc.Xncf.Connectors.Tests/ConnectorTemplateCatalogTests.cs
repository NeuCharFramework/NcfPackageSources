using Senparc.Xncf.Connectors.Domain.Services;

namespace Senparc.Xncf.Connectors.Tests;

[TestClass]
public class ConnectorTemplateCatalogTests
{
    [TestMethod]
    public void CatalogProvidesBuiltInAndCustomMcpTemplates()
    {
        var templates = ConnectorTemplateCatalog.All;

        Assert.IsTrue(templates.Count >= 4);
        Assert.IsTrue(templates.Any(item => item.Key == "github-copilot-mcp"));
        Assert.IsTrue(templates.Any(item => item.Key == "custom-http-mcp"));
        Assert.IsTrue(templates.All(item =>
            item.EndpointType is "http" or "sse"
            && !string.IsNullOrWhiteSpace(item.Name)));
    }

    [TestMethod]
    public void GitHubTemplateUsesRemoteMcpEndpoint()
    {
        var github = ConnectorTemplateCatalog.All.Single(item => item.Key == "github-copilot-mcp");

        Assert.AreEqual("https://api.githubcopilot.com/mcp/", github.Endpoint);
        Assert.AreEqual("http", github.EndpointType);
        Assert.IsFalse(string.IsNullOrWhiteSpace(github.AuthLabel));
    }
}
