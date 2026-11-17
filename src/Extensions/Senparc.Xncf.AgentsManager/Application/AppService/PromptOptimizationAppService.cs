/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptOptimizationAppService.cs
    File description: PromptOptimizationAppService related implementation

    Created by: Senparc - 20260306

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
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
    /// Prompt optimizerequest DTO
    /// </summary>
    public class PromptOptimizationRequestDto
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string PromptCode { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string PromptContent { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string UserRequirement { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public OptimizationContext Context { get; set; }
    }
}