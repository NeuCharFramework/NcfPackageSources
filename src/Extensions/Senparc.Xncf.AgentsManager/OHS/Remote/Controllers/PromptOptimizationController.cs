using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.OHS.Remote.Controllers
{
    /// <summary>
    /// Prompt 优化 API Controller
    /// </summary>
    [ApiController]
    [Route("api/Senparc.Xncf.AgentsManager/PromptOptimizationAppService")]
    public class PromptOptimizationController : ControllerBase
    {
        private readonly PromptOptimizationService _promptOptimizationService;
        private readonly ILogger<PromptOptimizationController> _logger;

        public PromptOptimizationController(
            PromptOptimizationService promptOptimizationService,
            ILogger<PromptOptimizationController> logger)
        {
            _promptOptimizationService = promptOptimizationService;
            _logger = logger;
        }

        /// <summary>
        /// 优化指定的 Prompt（包括内容和参数如 Temperature）
        /// </summary>
        [HttpPost("OptimizeAsync")]
        public async Task<IActionResult> OptimizeAsync([FromBody] PromptOptimizationRequestDto request)
        {
            try
            {
                _logger.LogInformation("========== 收到 Prompt 优化请求 ==========");
                _logger.LogInformation("PromptCode: {PromptCode}, UserRequirement: {UserRequirement}", 
                    request.PromptCode, request.UserRequirement);

                // 验证请求参数
                if (string.IsNullOrWhiteSpace(request.PromptCode))
                {
                    _logger.LogWarning("  ❌ PromptCode 为空");
                    return Ok(new AppResponseBase<PromptOptimizationResponseEvent>
                    {
                        Success = false,
                        ErrorMessage = "PromptCode is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PromptContent))
                {
                    _logger.LogWarning("  ❌ PromptContent 为空");
                    return Ok(new AppResponseBase<PromptOptimizationResponseEvent>
                    {
                        Success = false,
                        ErrorMessage = "PromptContent is required"
                    });
                }

                if (request.Context == null)
                {
                    _logger.LogWarning("  ❌ Context 为空");
                    return Ok(new AppResponseBase<PromptOptimizationResponseEvent>
                    {
                        Success = false,
                        ErrorMessage = "Context is required"
                    });
                }

                _logger.LogInformation("  请求参数验证通过");
                _logger.LogInformation("  Context: ModelId={ModelId}, Temperature={Temp}, TopP={TopP}, MaxTokens={MaxTokens}",
                    request.Context.ModelId,
                    request.Context.CurrentTemperature,
                    request.Context.CurrentTopP,
                    request.Context.CurrentMaxTokens);

                // 🔥 关键修复：确保 Agent 和 ChatGroup 已初始化
                _logger.LogInformation("  开始确保初始化状态...");
                await _promptOptimizationService.EnsureInitializedAsync();
                _logger.LogInformation("  ✅ 初始化状态确认完成");

                // 调用优化服务
                _logger.LogInformation("  开始调用 OptimizePromptAsync...");
                var result = await _promptOptimizationService.OptimizePromptAsync(
                    request.PromptCode,
                    request.PromptContent,
                    request.UserRequirement ?? "提高 Prompt 的质量和效果",
                    request.Context);

                if (result.Success)
                {
                    _logger.LogInformation("  ✅ 优化成功。NewPromptCode: {NewPromptCode}, Score: {Score}",
                        result.NewPromptCode, result.Score);
                }
                else
                {
                    _logger.LogWarning("  优化未成功。NewPromptCode: {NewPromptCode}, Error: {Error}",
                        result.NewPromptCode, result.ErrorMessage ?? result.EvaluationReason);
                }

                _logger.LogInformation("========== Prompt 优化请求处理完成 ==========");

                return Ok(new AppResponseBase<PromptOptimizationResponseEvent>
                {
                    Success = result.Success,
                    ErrorMessage = result.Success ? null : (result.ErrorMessage ?? result.EvaluationReason),
                    Data = result
                });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "❌ Prompt 优化超时");
                return Ok(new AppResponseBase<PromptOptimizationResponseEvent>
                {
                    Success = false,
                    ErrorMessage = $"优化请求超时: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                
                _logger.LogError(ex, "❌ Prompt 优化失败: {ErrorMessage}", errorMessage);
                return Ok(new AppResponseBase<PromptOptimizationResponseEvent>
                {
                    Success = false,
                    ErrorMessage = errorMessage
                });
            }
        }
    }

    #region DTOs

    /// <summary>
    /// Prompt 优化请求 DTO
    /// </summary>
    public class PromptOptimizationRequestDto
    {
        public string PromptCode { get; set; }
        public string PromptContent { get; set; }
        public string UserRequirement { get; set; }
        public OptimizationContext Context { get; set; }
    }

    #endregion
}
