/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptOptimizationAppService.cs
    File description: PromptOptimizationAppService related implementation

    Created by: Senparc - 20260306

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.CO2NET;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AreaBase.Admin.Filters;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// Prompt optimize AppService
    /// TODO: permission validation is required
    /// </summary>
    [ApiAuthorize]
    public class PromptOptimizationAppService : AppServiceBase
    {
        private readonly PromptOptimizationService _promptOptimizationService;
        private readonly ILogger<PromptOptimizationAppService> _logger;

        /// <summary>
        /// Initializes the Prompt optimization application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="promptOptimizationService">The service that initializes and optimizes prompts.</param>
        /// <param name="logger">The logger used for request diagnostics.</param>
        public PromptOptimizationAppService(
            IServiceProvider serviceProvider, 
            PromptOptimizationService promptOptimizationService,
            ILogger<PromptOptimizationAppService> logger)
            : base(serviceProvider)
        {
            _promptOptimizationService = promptOptimizationService;
            _logger = logger;
        }

        /// <summary>
        /// Ensures the built-in Prompt optimization Agent and supporting PromptRange data exist.
        /// </summary>
        /// <returns>A response containing the initialization event.</returns>
        [HttpPost]
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptInitResponseEvent>> EnsureInitializedAsync()
        {
            return await this.GetResponseAsync<PromptInitResponseEvent>(
                async (response, logger) =>
                {
                    return await _promptOptimizationService.EnsureInitializedAsync();
                }
            );
        }

        /// <summary>
        /// Optimizes prompt content using the supplied requirement and optimization context.
        /// </summary>
        /// <param name="request">The PromptCode, current content, user requirement, and context.</param>
        /// <returns>A response containing the optimization event and generated content.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when required PromptCode, content, or context values are missing.</exception>
        [HttpPost]
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptOptimizationResponseEvent>> OptimizeAsync([FromBody] PromptOptimizationRequestDto request)
        {
            return await this.GetResponseAsync<PromptOptimizationResponseEvent>(
                async (response, logger) =>
                {
                    _logger.LogInformation("Received Prompt optimization request for PromptCode: {PromptCode}", request.PromptCode);

                    // validationrequestparameters
                    if (string.IsNullOrWhiteSpace(request.PromptCode))
                    {
                        throw new NcfExceptionBase("PromptCode is required");
                    }

                    if (string.IsNullOrWhiteSpace(request.PromptContent))
                    {
                        throw new NcfExceptionBase("PromptContent is required");
                    }

                    if (request.Context == null)
                    {
                        throw new NcfExceptionBase("Context is required");
                    }

                    //Legacy comment translated to English.
                    var result = await _promptOptimizationService.OptimizePromptAsync(
                        request.PromptCode,
                        request.PromptContent,
                        request.UserRequirement ?? "提高 Prompt 的质量和效果",
                        request.Context);

                    return result;
                }
            );
        }
    }

    /// <summary>
    /// Carries the prompt content, user requirement, and model context used for optimization.
    /// </summary>
    public class PromptOptimizationRequestDto
    {
        /// <summary>
        /// Gets or sets the prompt code being optimized.
        /// </summary>
        public string PromptCode { get; set; }

        /// <summary>
        /// Gets or sets the current prompt content.
        /// </summary>
        public string PromptContent { get; set; }

        /// <summary>
        /// Gets or sets the requested optimization goal.
        /// </summary>
        public string UserRequirement { get; set; }

        /// <summary>
        /// Gets or sets optional model and optimization context.
        /// </summary>
        public OptimizationContext Context { get; set; }
    }
}
