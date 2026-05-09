# 📝 实施指南 - Step 1: 更新 PromptOptimizationRequestHandler

## 🎯 目标
实现真正的 AI 优化逻辑，替换当前的模拟代码

## 📄 完整实现代码

将以下代码**完整替换** `PromptOptimizationRequestHandler.cs` 的内容：

```csharp
using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Entities;
using Senparc.AI;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xncf.PromptRange.Application.EventHandlers
{
    public class PromptOptimizationRequestHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
    {
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly AIModelService _aiModelService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<PromptOptimizationRequestHandler> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PromptOptimizationRequestHandler(
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            AIModelService aiModelService,
            IEventBus eventBus,
            ILogger<PromptOptimizationRequestHandler> logger,
            IServiceProvider serviceProvider)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _aiModelService = aiModelService;
            _eventBus = eventBus;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("收到 Prompt 优化请求: RequestId={RequestId}, PromptCode={PromptCode}", 
                @event.RequestId, @event.PromptCode);

            try
            {
                // 1. 获取当前 PromptItem
                var promptItem = await GetPromptItemByCode(@event.PromptCode);
                if (promptItem == null)
                {
                    throw new NcfExceptionBase($"未找到 Prompt: {@event.PromptCode}");
                }

                _logger.LogInformation("找到 PromptItem: Id={Id}, Content长度={Length}", 
                    promptItem.Id, @event.PromptContent?.Length ?? 0);

                // 2. 构建优化请求
                var systemPrompt = BuildOptimizationSystemPrompt();
                var userInput = BuildOptimizationUserInput(
                    @event.PromptContent,
                    @event.UserRequirement,
                    @event.Context);

                _logger.LogDebug("System Prompt: {SystemPrompt}", systemPrompt);
                _logger.LogDebug("User Input: {UserInput}", userInput);

                // 3. 调用 AI 进行优化
                var aiResponse = await CallAIForOptimizationAsync(
                    @event.Context.ModelId,
                    systemPrompt,
                    userInput);

                _logger.LogInformation("AI 返回结果长度: {Length}", aiResponse?.Length ?? 0);

                // 4. 解析 AI 返回的优化结果
                var optimizationResult = ParseOptimizationResponse(aiResponse);

                _logger.LogInformation("解析优化结果: 预测分数={Score}, Temperature={Temperature}", 
                    optimizationResult.PredictedScore, optimizationResult.Parameters.Temperature);

                // 5. 创建新的 PromptItem
                var newPromptRequest = new PromptItem_AddRequest
                {
                    RangeId = promptItem.RangeId,
                    ModelId = promptItem.ModelId,
                    Content = optimizationResult.OptimizedContent,
                    IsTopTactic = false,  // 作为子战术
                    ParentId = promptItem.Id,
                    NumsOfResults = 0,
                    MaxToken = optimizationResult.Parameters.MaxTokens,
                    Temperature = optimizationResult.Parameters.Temperature,
                    TopP = optimizationResult.Parameters.TopP,
                    FrequencyPenalty = optimizationResult.Parameters.FrequencyPenalty,
                    PresencePenalty = optimizationResult.Parameters.PresencePenalty,
                    StopSequences = null,
                    IsDraft = false,  // 自动生成的，非草稿
                    AdminRemark = $"AI 自动优化于 {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                };

                await _promptItemService.AddPromptItemAsync(newPromptRequest);
                
                // 6. 获取新创建的 PromptItem
                var newPromptItem = await _promptItemService.GetObjectAsync(
                    z => z.RangeId == promptItem.RangeId && 
                         z.ParentId == promptItem.Id,
                    z => z.AddTime,
                    Senparc.Ncf.Core.Enums.OrderingType.Descending);

                if (newPromptItem == null)
                {
                    throw new NcfExceptionBase("创建新 PromptItem 失败");
                }

                _logger.LogInformation("成功创建新 PromptItem: Id={Id}, FullVersion={FullVersion}", 
                    newPromptItem.Id, newPromptItem.FullVersion);

                // 7. 发布响应事件
                var responseEvent = new PromptOptimizationResponseEvent(
                    @event.RequestId,
                    newPromptItem.FullVersion,
                    optimizationResult.OptimizedContent,
                    optimizationResult.Parameters,
                    optimizationResult.PredictedScore,
                    optimizationResult.Explanation,
                    Success: true,
                    ErrorMessage: null
                );

                await _eventBus.PublishAsync(responseEvent);
                
                _logger.LogInformation("Prompt 优化完成: NewPromptCode={NewPromptCode}", newPromptItem.FullVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prompt 优化失败: RequestId={RequestId}", @event.RequestId);
                
                // 发布失败响应
                var errorResponse = new PromptOptimizationResponseEvent(
                    @event.RequestId,
                    null,
                    null,
                    null,
                    0,
                    null,
                    Success: false,
                    ErrorMessage: $"优化失败: {ex.Message}"
                );
                
                await _eventBus.PublishAsync(errorResponse);
            }
        }

        /// <summary>
        /// 通过 PromptCode（FullVersion）查询 PromptItem
        /// </summary>
        private async Task<Domain.Models.DatabaseModel.PromptItem> GetPromptItemByCode(string promptCode)
        {
            // PromptCode 格式示例: "2024.1.1.1-T1-A1"
            // 直接通过 FullVersion 查询
            var promptItem = await _promptItemService.GetObjectAsync(
                z => z.FullVersion == promptCode);

            return promptItem;
        }

        /// <summary>
        /// 构建优化请求的 System Prompt
        /// </summary>
        private string BuildOptimizationSystemPrompt()
        {
            return @"You are an expert Prompt Engineer (PromptCatalyzer). 
Your goal is to optimize the user's prompt and parameters to achieve better results.

Your response MUST be in the following JSON format (do not include any other text):
{
    ""optimizedContent"": ""<optimized prompt content>"",
    ""parameters"": {
        ""temperature"": 0.7,
        ""topP"": 0.9,
        ""maxTokens"": 4000,
        ""frequencyPenalty"": 0.0,
        ""presencePenalty"": 0.0
    },
    ""predictedScore"": 8.5,
    ""explanation"": ""<why these changes improve the prompt>""
}

Guidelines for optimization:
1. Make the prompt more clear and specific
2. Add context or examples if needed
3. Adjust temperature based on creativity requirements (lower for factual, higher for creative)
4. Adjust topP to control diversity
5. Set appropriate maxTokens based on expected output length
6. Use frequencyPenalty to reduce repetition if needed
7. Use presencePenalty to encourage topic diversity if needed

Your response must be valid JSON only, no additional explanation outside the JSON.";
        }

        /// <summary>
        /// 构建优化请求的用户输入
        /// </summary>
        private string BuildOptimizationUserInput(
            string currentPrompt,
            string userRequirement,
            OptimizationContext context)
        {
            return $@"Current Prompt:
{currentPrompt}

Current Parameters:
- Temperature: {context.CurrentTemperature}
- TopP: {context.CurrentTopP}
- MaxTokens: {context.CurrentMaxTokens}
- FrequencyPenalty: {context.CurrentFrequencyPenalty}
- PresencePenalty: {context.CurrentPresencePenalty}

User Requirement:
{userRequirement}

Please optimize the prompt and parameters to better meet the user's requirement. Return valid JSON only.";
        }

        /// <summary>
        /// 调用 AI 进行优化
        /// </summary>
        private async Task<string> CallAIForOptimizationAsync(
            int modelId,
            string systemPrompt,
            string userInput)
        {
            try
            {
                // 1. 获取 AI Model
                var aiModel = await _aiModelService.GetObjectAsync(z => z.Id == modelId);
                if (aiModel == null)
                {
                    throw new NcfExceptionBase($"未找到 AI Model: {modelId}");
                }

                var aiModelDto = new Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto.AIModelDto(aiModel);

                // 2. 构建 SenparcAiSetting
                var senparcAiSetting = _aiModelService.BuildSenparcAiSetting(aiModelDto);

                // 3. 构建 Prompt 参数（使用较高的 Temperature 以获得创造性优化）
                var promptParameter = new PromptConfigParameter()
                {
                    MaxTokens = 2000,
                    Temperature = 0.8,  // 较高的创造性
                    TopP = 0.9,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                // 4. 调用 SemanticAiHandler
                var semanticAiHandler = _serviceProvider.GetService<SemanticAiHandler>();
                var chatConfig = semanticAiHandler.ChatConfig(
                    promptParameter,
                    userId: "PromptCatalyzer",
                    chatSystemMessage: systemPrompt,
                    promptTemplate: "{0}",  // 简单模板
                    maxHistoryStore: 0,  // 不需要历史记录
                    senparcAiSetting: senparcAiSetting);

                // 5. 创建请求并运行
                var request = chatConfig.CreateRequest(userInput);
                var aiResult = await chatConfig.RunAsync(request);

                if (!aiResult.Success)
                {
                    throw new Exception($"AI 调用失败: {aiResult.ErrorMessage}");
                }

                return aiResult.Output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用 AI 失败");
                throw new NcfExceptionBase($"AI 调用失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析 AI 返回的优化结果
        /// </summary>
        private OptimizationResult ParseOptimizationResponse(string aiResponse)
        {
            try
            {
                // 清理可能的 Markdown 代码块标记
                var jsonContent = aiResponse.Trim();
                if (jsonContent.StartsWith("```json"))
                {
                    jsonContent = jsonContent.Substring(7);
                }
                if (jsonContent.StartsWith("```"))
                {
                    jsonContent = jsonContent.Substring(3);
                }
                if (jsonContent.EndsWith("```"))
                {
                    jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
                }
                jsonContent = jsonContent.Trim();

                // 解析 JSON
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                var json = JsonDocument.Parse(jsonContent);
                var root = json.RootElement;

                return new OptimizationResult
                {
                    OptimizedContent = root.GetProperty("optimizedContent").GetString(),
                    Parameters = new OptimizedParameters(
                        Temperature: (float)root.GetProperty("parameters").GetProperty("temperature").GetDouble(),
                        TopP: (float)root.GetProperty("parameters").GetProperty("topP").GetDouble(),
                        MaxTokens: root.GetProperty("parameters").GetProperty("maxTokens").GetInt32(),
                        FrequencyPenalty: (float)root.GetProperty("parameters").GetProperty("frequencyPenalty").GetDouble(),
                        PresencePenalty: (float)root.GetProperty("parameters").GetProperty("presencePenalty").GetDouble()
                    ),
                    PredictedScore = root.GetProperty("predictedScore").GetDouble(),
                    Explanation = root.GetProperty("explanation").GetString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 AI 响应失败: {Response}", aiResponse);
                
                // 如果解析失败，返回默认优化结果
                _logger.LogWarning("使用默认优化结果");
                return new OptimizationResult
                {
                    OptimizedContent = aiResponse, // 直接使用 AI 返回的文本
                    Parameters = new OptimizedParameters(
                        Temperature: 0.7f,
                        TopP: 0.9f,
                        MaxTokens: 2000,
                        FrequencyPenalty: 0f,
                        PresencePenalty: 0f
                    ),
                    PredictedScore = 7.0,
                    Explanation = "AI 返回格式不正确，使用默认参数"
                };
            }
        }
    }

    /// <summary>
    /// 优化结果内部类
    /// </summary>
    internal class OptimizationResult
    {
        public string OptimizedContent { get; init; }
        public OptimizedParameters Parameters { get; init; }
        public double PredictedScore { get; init; }
        public string Explanation { get; init; }
    }
}
```

## ✅ 实施步骤

### Step 1: 备份原文件
```bash
cp src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs \
   src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs.backup
```

### Step 2: 替换文件内容
将上面的完整代码复制并替换 `PromptOptimizationRequestHandler.cs` 的全部内容

### Step 3: 编译检查
```bash
dotnet build src/Extensions/Senparc.Xncf.PromptRange/Senparc.Xncf.PromptRange.csproj
```

## 🔍 代码说明

### 核心改进：
1. **真实 AI 调用**：使用 `SemanticAiHandler` 调用 AI 模型
2. **智能 System Prompt**：引导 AI 返回 JSON 格式的优化结果
3. **参数优化**：AI 会根据需求优化 Temperature 等参数
4. **错误处理**：完善的异常处理和失败响应
5. **日志记录**：详细的调试和跟踪日志

### 关键依赖：
- `AIModelService` - 获取 AI 模型配置
- `SemanticAiHandler` - 调用 AI 的核心服务
- `PromptItemService` - 操作 Prompt 数据

## ⚠️ 注意事项

1. **AI 配额**：确保您的 AI Model 有足够的 API 调用配额
2. **超时时间**：AI 调用可能需要 10-30 秒
3. **JSON 解析**：AI 可能不总是返回完美的 JSON，代码中有容错处理

## 🧪 测试建议

完成后，请告诉我，我将帮您：
1. 检查编译错误
2. 提供前端代码更新
3. 进行端到端测试

准备好后回复：**"Step 1 完成"**，我们继续下一步！
