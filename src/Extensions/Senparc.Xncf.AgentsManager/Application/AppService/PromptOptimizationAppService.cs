using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.CO2NET;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    public class PromptOptimizationAppService : AppServiceBase
    {
        private readonly PromptOptimizationService _promptOptimizationService;

        public PromptOptimizationAppService(IServiceProvider serviceProvider, PromptOptimizationService promptOptimizationService)
            : base(serviceProvider)
        {
            _promptOptimizationService = promptOptimizationService;
        }

        [HttpPost]
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<ActionResult<PromptInitResponseEvent>> EnsureInitializedAsync()
        {
             return await _promptOptimizationService.EnsureInitializedAsync();
        }

        [HttpPost]
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<ActionResult<PromptOptimizationResponseEvent>> OptimizeAsync([FromBody] PromptOptimizationRequestDto request)
        {
            return await _promptOptimizationService.OptimizePromptAsync(request.PromptCode, request.UserRequirement);
        }
    }

    public class PromptOptimizationRequestDto
    {
        public string PromptCode { get; set; }
        public string UserRequirement { get; set; }
    }
}