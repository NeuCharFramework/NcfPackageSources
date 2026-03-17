using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.CO2NET;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Microsoft.Extensions.Logging;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
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
        /// 确保 PromptCatalyzer Agent 和相关资源已初始化
        /// </summary>
        [HttpPost]
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<ActionResult<PromptInitResponseEvent>> EnsureInitializedAsync()
        {
            try
            {
                return await _promptOptimizationService.EnsureInitializedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure PromptCatalyzer initialization");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 优化指定的 Prompt（包括内容和参数如 Temperature）
        /// </summary>
        [HttpPost]
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<ActionResult<PromptOptimizationResponseEvent>> OptimizeAsync([FromBody] PromptOptimizationRequestDto request)
        {
            _logger.LogInformation("Received Prompt optimization request for PromptCode: {PromptCode}", request.PromptCode);

            try
            {
                // 验证请求参数
                if (string.IsNullOrWhiteSpace(request.PromptCode))
                {
                    return BadRequest(new { error = "PromptCode is required" });
                }

                if (string.IsNullOrWhiteSpace(request.PromptContent))
                {
                    return BadRequest(new { error = "PromptContent is required" });
                }

                if (request.Context == null)
                {
                    return BadRequest(new { error = "Context is required" });
                }

                // 调用优化服务
                var response = await _promptOptimizationService.OptimizePromptAsync(
                    request.PromptCode,
                    request.PromptContent,
                    request.UserRequirement ?? "提高 Prompt 的质量和效果",
                    request.Context);

                if (!response.Success)
                {
                    _logger.LogWarning("Prompt optimization failed: {ErrorMessage}", response.ErrorMessage);
                    return StatusCode(500, new { error = response.ErrorMessage });
                }

                return response;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Prompt optimization timeout for PromptCode: {PromptCode}", request.PromptCode);
                return StatusCode(408, new { error = "优化请求超时，请稍后重试" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize Prompt: {PromptCode}", request.PromptCode);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Prompt 优化请求 DTO
    /// </summary>
    public class PromptOptimizationRequestDto
    {
        /// <summary>
        /// Prompt 的版本号（如 "2024.1.1.1-T1-A1"）
        /// </summary>
        public string PromptCode { get; set; }

        /// <summary>
        /// Prompt 的内容
        /// </summary>
        public string PromptContent { get; set; }

        /// <summary>
        /// 用户的优化需求描述
        /// </summary>
        public string UserRequirement { get; set; }

        /// <summary>
        /// 优化上下文（当前 Prompt 的参数）
        /// </summary>
        public OptimizationContext Context { get; set; }
    }
}