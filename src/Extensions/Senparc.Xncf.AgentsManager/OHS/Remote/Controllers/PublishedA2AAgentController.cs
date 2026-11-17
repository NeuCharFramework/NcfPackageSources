/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PublishedA2AAgentController.cs
    File description: HTTP controller and remote interface

    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
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
                //Legacy comment translated to English.
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
