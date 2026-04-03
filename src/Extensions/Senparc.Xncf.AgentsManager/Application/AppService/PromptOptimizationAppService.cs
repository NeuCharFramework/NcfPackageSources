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

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    ///Prompt Optimize AppService
    /// TODO: Permission verification required
    /// </summary>
    //[ApiAuthorize("AdminOnly")]
    public class PromptOptimizationAppService : AppServiceBase
    {
        private readonly PromptOptimizationService _promptOptimizationService;
        private readonly ILogger<PromptOptimizationAppService> _logger;

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
        /// Ensure that the PromptCatalyzer Agent and related resources are initialized
        /// </summary>
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
        /// Optimize the specified Prompt (including content and parameters such as Temperature)
        /// </summary>
        [HttpPost]
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptOptimizationResponseEvent>> OptimizeAsync([FromBody] PromptOptimizationRequestDto request)
        {
            return await this.GetResponseAsync<PromptOptimizationResponseEvent>(
                async (response, logger) =>
                {
                    _logger.LogInformation("Received Prompt optimization request for PromptCode: {PromptCode}", request.PromptCode);

                    // Verify request parameters
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

                    // Call optimization service
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
    ///Prompt optimization request DTO
    /// </summary>
    public class PromptOptimizationRequestDto
    {
        /// <summary>
        /// Prompt version number (such as "2024.1.1.1-T1-A1")
        /// </summary>
        public string PromptCode { get; set; }

        /// <summary>
        ///Prompt content
        /// </summary>
        public string PromptContent { get; set; }

        /// <summary>
        ///Description of user optimization needs
        /// </summary>
        public string UserRequirement { get; set; }

        /// <summary>
        /// Optimization context (parameters of the current Prompt)
        /// </summary>
        public OptimizationContext Context { get; set; }
    }
}