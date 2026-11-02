/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：McpEndpoint.cs
    文件功能描述：AgentTemplate 的 MCP 端点配置模型
    
    创建标识：Senparc - 20260927
    创建描述：从 ChatGroupService 抽取到 Domain/Models，供 AgentTemplateRunner 等共享使用

    修改标识：Senparc - 20261103
    修改描述：v0.19.1 update AgentsManager with new features and improvements

----------------------------------------------------------------*/

namespace Senparc.Xncf.AgentsManager.Domain.Models;

/// <summary>
/// AgentTemplate 中单个 MCP 端点（JSON 反序列化用）。
/// </summary>
public class McpEndpoint
{
    public string url { get; set; }
}
