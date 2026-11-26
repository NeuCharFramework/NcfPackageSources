/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PublishedA2AAgentController.cs
    File description: HTTP controller and remote interface

    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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
    /// Serves the public A2A Agent Card for a published local AgentTemplate.
    /// </summary>
    [ApiController]
    [Route("a2a/{agentKey}")]
    public class PublishedA2AAgentController : ControllerBase
    {
        private readonly PublishedA2AAgentFactory _publishedA2AAgentFactory;

        /// <summary>
        /// Initializes the published A2A Agent Card controller.
        /// </summary>
        /// <param name="publishedA2AAgentFactory">The factory used to resolve active publications and build cards.</param>
        public PublishedA2AAgentController(PublishedA2AAgentFactory publishedA2AAgentFactory)
        {
            _publishedA2AAgentFactory = publishedA2AAgentFactory;
        }

        [HttpGet(".well-known/agent-card.json")]
        [HttpGet("card")]
        /// <summary>
        /// Returns the Agent Card for the public key, or HTTP 404 when the publication
        /// is unavailable or cannot be resolved.
        /// </summary>
        /// <param name="agentKey">The public A2A agent key from the route.</param>
        /// <param name="cancellationToken">Token used to cancel publication lookup.</param>
        /// <returns>An HTTP 200 Agent Card or HTTP 404.</returns>
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

        /// <summary>
        /// Builds the public Agent Card endpoint URL using configured public base URL
        /// or the current request origin.
        /// </summary>
        /// <param name="request">The current HTTP request.</param>
        /// <param name="publicAgentKey">The public agent key to escape into the route.</param>
        /// <returns>The absolute public A2A endpoint URL.</returns>
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
