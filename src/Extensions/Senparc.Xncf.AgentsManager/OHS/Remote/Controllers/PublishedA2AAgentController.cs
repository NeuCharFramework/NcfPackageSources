/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PublishedA2AAgentController.cs
    文件功能描述：HTTP 控制器与远程接口


    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Remote.Controllers
{
    /// <summary>
    /// 标准 A2A Agent Card 发现端点。
    /// 仅返回管理员定义的公开元数据，绝不返回本地 Prompt、密钥或工具清单。
    /// </summary>
    [ApiController]
    [Route("a2a/{agentKey}")]
    public class PublishedA2AAgentController : ControllerBase
    {
        private readonly PublishedA2AAgentFactory _publishedA2AAgentFactory;

        public PublishedA2AAgentController(PublishedA2AAgentFactory publishedA2AAgentFactory)
        {
            _publishedA2AAgentFactory = publishedA2AAgentFactory;
        }

        [HttpGet(".well-known/agent-card.json")]
        [HttpGet("card")]
        public async Task<IActionResult> GetAgentCard(string agentKey, CancellationToken cancellationToken)
        {
            try
            {
                var (publishedAgent, template) = await _publishedA2AAgentFactory.GetActiveAgentAsync(agentKey);
                var endpointUrl = BuildEndpointUrl(Request, publishedAgent.PublicAgentKey);
                return Ok(_publishedA2AAgentFactory.BuildAgentCard(publishedAgent, template, endpointUrl));
            }
            catch
            {
                // 避免通过发现端点枚举或探测未发布的本地 Agent。
                return NotFound();
            }
        }

        public static string BuildEndpointUrl(HttpRequest request, string publicAgentKey)
        {
            var publicBaseUrl = request.HttpContext.RequestServices
                .GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
            var configuredBaseUrl = publicBaseUrl?["A2A:PublicBaseUrl"]?.Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                return $"{configuredBaseUrl}/a2a/{Uri.EscapeDataString(publicAgentKey)}";
            }

            return $"{request.Scheme}://{request.Host}{request.PathBase}/a2a/{Uri.EscapeDataString(publicAgentKey)}";
        }
    }
}
