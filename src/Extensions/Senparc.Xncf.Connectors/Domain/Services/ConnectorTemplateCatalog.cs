/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorTemplateCatalog.cs
    File description: built-in MCP connector templates

    Created by: Senparc - 20261126
    Change description: v0.2.0 provide ready-to-configure MCP connector templates

    Modified by: Senparc - 20261127
    Change description: v0.1.1.4 Extend MCP connector management and localization coverage.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Connectors.Domain.Services;

public static class ConnectorTemplateCatalog
{
    public static IReadOnlyList<ConnectorTemplate> All =>
    [
        new()
        {
            Key = "github-copilot-mcp",
            Name = "GitHub",
            Description = ConnectorsResource.Get(
                "Template.GitHub.Description",
                "Use GitHub MCP tools with a centrally managed endpoint and protected token."),
            Icon = "fa fa-github",
            Endpoint = "https://api.githubcopilot.com/mcp/",
            EndpointType = "http",
            AuthLabel = ConnectorsResource.Get("Template.GitHub.Auth", "GitHub access token"),
            Category = "Developer"
        },
        new()
        {
            Key = "custom-http-mcp",
            Name = "MCP HTTP",
            Description = ConnectorsResource.Get(
                "Template.CustomHttp.Description",
                "Connect any MCP server that supports streamable HTTP."),
            Icon = "fa fa-cloud",
            Endpoint = "",
            EndpointType = "http",
            AuthLabel = ConnectorsResource.Get("Template.CustomHttp.Auth", "Bearer token (optional)"),
            Category = "Custom"
        },
        new()
        {
            Key = "custom-sse-mcp",
            Name = "MCP SSE",
            Description = ConnectorsResource.Get(
                "Template.CustomSse.Description",
                "Connect a legacy MCP server over Server-Sent Events."),
            Icon = "fa fa-rss",
            Endpoint = "",
            EndpointType = "sse",
            AuthLabel = ConnectorsResource.Get("Template.CustomSse.Auth", "Bearer token (optional)"),
            Category = "Custom"
        },
        new()
        {
            Key = "local-ncf-mcp",
            Name = "NCF MCP",
            Description = ConnectorsResource.Get(
                "Template.Ncf.Description",
                "Create a connector for an MCP server published by this NCF installation."),
            Icon = "fa fa-cubes",
            Endpoint = "",
            EndpointType = "http",
            AuthLabel = ConnectorsResource.Get("Template.Ncf.Auth", "Access token (optional)"),
            Category = "NCF"
        }
    ];
}

public sealed class ConnectorTemplate
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public required string EndpointType { get; init; }
    public required string AuthLabel { get; init; }
    public required string Category { get; init; }
}
