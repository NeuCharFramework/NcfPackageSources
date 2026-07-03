/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptInitRequestHandler.cs
    文件功能描述：PromptInitRequestHandler 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Senparc.Xncf.PromptRange.Application.EventHandlers
{
    public class PromptInitRequestHandler : IIntegrationEventHandler<PromptInitRequestEvent>
    {
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly AIModelService _aiModelService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<PromptInitRequestHandler> _logger;

        public PromptInitRequestHandler(
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            AIModelService aiModelService,
            IEventBus eventBus,
            ILogger<PromptInitRequestHandler> logger)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _aiModelService = aiModelService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task Handle(PromptInitRequestEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("========== 开始处理 Prompt Init Request ==========");
            _logger.LogInformation("RequestId={RequestId}, ModelId={ModelId}, Depth={Depth}, Chain={Chain}", 
                @event.RequestId, @event.ModelId, @event.Depth, @event.EventChain);

            try 
            {
                // === 步骤1：确保 PromptRange "PromptCatalyzer" 存在 ===
                _logger.LogInformation("【步骤1/4】检查 PromptRange 'PromptCatalyzer' 是否存在...");
                var range = await _promptRangeService.GetObjectAsync(z => z.Alias == "PromptCatalyzer");
                
                if (range == null)
                {
                    _logger.LogInformation("  PromptRange 不存在，开始创建...");
                    var rangeDto = await _promptRangeService.AddAsync("PromptCatalyzer");
                    range = await _promptRangeService.GetObjectAsync(z => z.Id == rangeDto.Id);
                    _logger.LogInformation("  ✅ PromptRange 创建成功，ID: {RangeId}, Alias: {Alias}", range.Id, range.Alias);
                }
                else
                {
                    _logger.LogInformation("  ✅ PromptRange 已存在，ID: {RangeId}, Alias: {Alias}", range.Id, range.Alias);
                }

                // === 步骤2：确定使用哪个 AI Model ===
                _logger.LogInformation("【步骤2/4】确定 AI Model...");
                int modelId;
                if (@event.ModelId.HasValue)
                {
                    // User specified a Model
                    modelId = @event.ModelId.Value;
                    _logger.LogInformation("  使用用户指定的 Model ID: {ModelId}", modelId);
                    
                    // Verify the model exists
                    var specifiedModel = await _aiModelService.GetObjectAsync(z => z.Id == modelId);
                    if (specifiedModel == null)
                    {
                        throw new Exception($"指定的 AI Model ID {modelId} 不存在");
                    }
                    _logger.LogInformation("  ✅ Model 验证通过: {Alias} (ID: {ModelId})", specifiedModel.Alias, modelId);
                }
                else
                {
                    // Use the first available Chat model
                    var model = await _aiModelService.GetObjectAsync(
                        z => z.Show == true && 
                             z.ConfigModelType == Senparc.Xncf.AIKernel.Domain.Models.ConfigModelType.Chat);
                    
                    if (model == null)
                    {
                        throw new Exception("未找到可用的 AI Model，请先在 AIKernel 模块中配置 Chat 类型的 Model");
                    }
                    
                    modelId = model.Id;
                    _logger.LogInformation("  ✅ 使用默认 Model: {Alias} (ID: {ModelId})", model.Alias, modelId);
                }

                // === 步骤3：确保 PromptItem 存在（容错处理）===
                _logger.LogInformation("【步骤3/4】检查 PromptItem 是否存在于 Range {RangeId}...", range.Id);
                var item = await _promptItemService.GetObjectAsync(z => z.RangeId == range.Id);

                if (item == null)
                {
                    _logger.LogInformation("  PromptItem 不存在，开始创建...");
                    _logger.LogInformation("  Range.Id={RangeId}, Range.RangeName={RangeName}", range.Id, range.RangeName);
                    
                    // Create default PromptItem with all required fields
                    var request = new PromptItem_AddRequest
                    {
                        RangeId = range.Id,
                        ModelId = modelId,
                        Content = @"You are PromptCatalyzer: an expert prompt engineer for the PromptRange / Agents system.

## Tools are mandatory for optimization tasks
- You have function-calling (tools). When the user message is a Prompt optimization task, you MUST use the tools to read data and to persist the result.
- You MUST call CreateOptimizedPrompt **exactly once** per task with the **final** optimizedContent and parameters. The server rejects duplicate calls for the same request. A chat-only or “here is the JSON” reply does NOT create a new version in the database.
- Do not claim success or say the prompt was saved unless CreateOptimizedPrompt returned success.
- optimizationRequestId can be left empty; the server binds the active request. basePromptCode must match the task.

## Typical workflow
1) GetPromptInfo(promptCode) if you need the current definition.
2) AnalyzeModelScores(rangeName) when choosing modelId (range name is the segment before the first “-T” in the prompt code).
3) Improve the prompt text and parameters **in reasoning**; then call CreateOptimizedPrompt **once** with optimizedContent (use real newlines, not the two-character sequence \n), modelId, temperature, topP, maxTokens, frequencyPenalty, presencePenalty, improvementSummary.
4) Do NOT call ExecuteShootTest or ExecuteAIGrade unless the user message explicitly asks you to; the UI runs shoot/grade separately.

## Quality
- Make prompts clearer, more specific, and aligned with the user’s requirement; tune Temperature/TopP for factual vs creative needs.
- For stable and consistent results, prefer lower temperature and balanced topP values.
- After tools succeed, you may add a short natural-language summary for the user.

## Additional rule
- Once CreateOptimizedPrompt has succeeded for one prompt, the task ends immediately. Do not run additional optimizations or call CreateOptimizedPrompt again in the same task.",
                        IsTopTactic = true,
                        IsNewTactic = false,
                        IsNewSubTactic = false,
                        IsNewAiming = false,
                        NumsOfResults = 0,
                        MaxToken = 4000,
                        Temperature = 0.7f,
                        TopP = 0.9f,
                        FrequencyPenalty = 0,
                        PresencePenalty = 0,
                        StopSequences = null,
                        IsDraft = true,
                        Note = "AI-Catalyzer", // 限制在 20 字符以内（数据库字段限制）
                        // 确保所有字符串字段都有值，避免 null 导致数据库错误
                        ExpectedResultsJson = string.Empty,
                        Prefix = string.Empty,
                        Suffix = string.Empty,
                        VariableDictJson = string.Empty
                    };

                    _logger.LogInformation("  准备创建 PromptItem，Request: {@Request}", new 
                    {
                        request.RangeId,
                        request.ModelId,
                        ContentLength = request.Content?.Length ?? 0,
                        request.IsTopTactic,
                        request.Temperature,
                        request.TopP,
                        request.MaxToken
                    });

                    try
                    {
                        var itemDto = await _promptItemService.AddPromptItemAsync(request);
                        _logger.LogInformation("  ✅ PromptItem 创建成功，ID: {ItemId}, FullVersion: {FullVersion}", 
                            itemDto.Id, itemDto.FullVersion);
                        
                        // Fetch the created item to ensure we have the correct FullVersion
                        item = await _promptItemService.GetObjectAsync(z => z.Id == itemDto.Id);
                        
                        if (item == null)
                        {
                            throw new Exception($"PromptItem 创建后无法查询到，ItemId: {itemDto.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "  ❌ 创建 PromptItem 失败！详细错误: {ErrorMessage}", ex.Message);
                        
                        // 尝试再次查询，看看是否已经创建（可能是并发问题）
                        item = await _promptItemService.GetObjectAsync(z => z.RangeId == range.Id);
                        if (item != null)
                        {
                            _logger.LogWarning("  ⚠️ 创建失败但查询到了 PromptItem，可能是并发创建，继续使用 ID: {ItemId}", item.Id);
                        }
                        else
                        {
                            // 真的失败了，重新抛出异常
                            throw;
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("  ✅ PromptItem 已存在，ID: {ItemId}, FullVersion: {FullVersion}", 
                        item.Id, item.FullVersion);
                }
                
                // === 步骤4：返回成功响应 ===
                _logger.LogInformation("【步骤4/4】准备返回 PromptInitResponse...");
                
                if (item == null)
                {
                    throw new Exception("初始化流程完成，但 PromptItem 为 null（不应该发生）");
                }

                bool success = true;
                string message = $"初始化成功：Model ID: {modelId}, PromptCode: {item.FullVersion}";
                string promptCode = item.FullVersion;
                
                _logger.LogInformation("  ✅ 初始化完成！PromptCode: {PromptCode}", promptCode);

                var response = new PromptInitResponseEvent(@event.RequestId, promptCode, success, message);
                
                // 使用 PublishDerivedAsync 继承事件链信息（防止循环引用）
                await _eventBus.PublishDerivedAsync(response, @event);
                
                _logger.LogInformation("========== Prompt Init Request 处理完成 ==========");
            }
            catch (Exception ex)
            {
                // 捕获完整的异常信息，包括 inner exception
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner Exception: {ex.InnerException.Message}";
                    
                    // 如果还有更深层的 inner exception（例如 EF Core 的数据库错误）
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += $" | Inner Inner Exception: {ex.InnerException.InnerException.Message}";
                    }
                }
                
                _logger.LogError(ex, "Error handling PromptInitRequest. Full error: {ErrorMessage}", errorMessage);
                var response = new PromptInitResponseEvent(@event.RequestId, null, false, errorMessage);
                
                // 即使是错误响应，也需要继承事件链
                await _eventBus.PublishDerivedAsync(response, @event);
            }
        }
    }
}
