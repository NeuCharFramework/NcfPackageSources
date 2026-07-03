/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptOptimizationAppService.cs
    文件功能描述：PromptOptimizationAppService 相关实现
    
    
    创建标识：Senparc - 20260306
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    /// Prompt 优化 AppService
    /// TODO: 需要权限验证
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
        /// 确保 PromptCatalyzer Agent 和相关资源已初始化
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
        /// 优化指定的 Prompt（包括内容和参数如 Temperature）
        /// </summary>
        [HttpPost]
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptOptimizationResponseEvent>> OptimizeAsync([FromBody] PromptOptimizationRequestDto request)
        {
            return await this.GetResponseAsync<PromptOptimizationResponseEvent>(
                async (response, logger) =>
                {
                    _logger.LogInformation("Received Prompt optimization request for PromptCode: {PromptCode}", request.PromptCode);

                    // 验证请求参数
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

                    // 调用优化服务
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