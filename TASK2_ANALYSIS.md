# 任务 2 分析：PromptRange 与 AgentsManager 自动优化实现

## 📋 需求分析

### 用户期望功能
1. 在 PromptRange 的 Prompt.cshtml 页面点击"优化"按钮
2. 自动调用 AgentsManager 模块的 Agent 进行 Prompt 优化
3. 优化过程包括：
   - 调整 Prompt 内容
   - 优化 Temperature 等参数
4. 如果相关的 Range、Prompt、Agent 不存在，自动创建
5. 通过 EventBus 解决模块间循环依赖问题

## 🔍 当前实现分析

### 1. 前端实现（已完成 ✅）

**文件：** `Prompt.cshtml`（行 68-70）
```html
<el-button type="primary" size="mini" icon="el-icon-magic-stick" @@click="openOptimizeDialog">
    优化
</el-button>
```

**文件：** `prompt.js`（行 3002-3050）
```javascript
openOptimizeDialog() {
    if (!this.promptid) {
        this.$message.warning('请先选择一个Prompt！');
        return;
    }
    this.optimizeRequirement = '';
    this.optimizeDialogVisible = true;
},

async executeOptimize() {
    // 获取当前 Prompt Code (fullVersion)
    let promptCode = selectedPrompt.fullVersion || selectedPrompt.label;
    
    // 调用后端 API
    const response = await servicePR.post(
        '/api/Senparc.Xncf.AgentsManager/PromptOptimizationAppService/OptimizeAsync',
        {
            promptCode: promptCode,
            userRequirement: this.optimizeRequirement
        }
    );
    
    // 处理响应，显示新的 Prompt Code
    this.$message.success(`优化成功！新的 Prompt Code: ${response.data.newPromptCode}`);
}
```

### 2. 后端实现（部分完成 ⚠️）

#### ✅ 已实现的部分

**AgentsManager 模块：**

1. **API 入口：** `PromptOptimizationAppService.cs`
   ```csharp
   [HttpPost]
   public async Task<ActionResult<PromptOptimizationResponseEvent>> OptimizeAsync(
       [FromBody] PromptOptimizationRequestDto request)
   {
       return await _promptOptimizationService.OptimizePromptAsync(
           request.PromptCode, 
           request.UserRequirement);
   }
   ```

2. **业务逻辑：** `PromptOptimizationService.cs`
   ```csharp
   public async Task<PromptOptimizationResponseEvent> OptimizePromptAsync(
       string promptCode, 
       string userRequirement)
   {
       var requestId = Guid.NewGuid().ToString();
       var tcs = new TaskCompletionSource<PromptOptimizationResponseEvent>();
       
       // 注册挂起请求
       _pendingRequests.TryAdd(requestId, tcs);
       
       // 发布请求事件
       await _eventBus.PublishAsync(
           new PromptOptimizationRequestEvent(requestId, promptCode, userRequirement));
       
       // 等待响应
       return await tcs.Task;
   }
   
   // 响应处理器调用此方法完成请求
   public void CompleteRequest(string requestId, PromptOptimizationResponseEvent response)
   {
       if (_pendingRequests.TryRemove(requestId, out var tcs))
       {
           tcs.TrySetResult(response);
       }
   }
   ```

3. **事件处理器：** `PromptOptimizationResponseHandler.cs`
   ```csharp
   public class PromptOptimizationResponseHandler 
       : IIntegrationEventHandler<PromptOptimizationResponseEvent>
   {
       public Task Handle(PromptOptimizationResponseEvent @event, CancellationToken ct)
       {
           _optimizationService.CompleteRequest(@event.RequestId, @event);
           return Task.CompletedTask;
       }
   }
   ```

**PromptRange 模块：**

1. **事件处理器：** `PromptOptimizationRequestHandler.cs`
   ```csharp
   public class PromptOptimizationRequestHandler 
       : IIntegrationEventHandler<PromptOptimizationRequestEvent>
   {
       public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken ct)
       {
           // ⚠️ 当前实现：模拟优化，未真正调用 AI
           string newPromptContent = $"Optimized content based on: {@event.UserRequirement}";
           string newPromptCode = $"{@event.PromptCode}-Opt-{DateTime.Now.Ticks % 1000}";
           
           // 发布响应事件
           var responseEvent = new PromptOptimizationResponseEvent(
               @event.RequestId,
               newPromptCode,
               0.85,  // 模拟分数
               "Optimization successful"
           );
           
           await _eventBus.PublishAsync(responseEvent);
       }
   }
   ```

#### ⚠️ 缺失的部分

1. **自动创建 Agent 和 ChatGroup**
   - `PromptOptimizationService.EnsureInitializedAsync()` 中的 TODO 未实现
   - 需要在首次使用时自动创建 "PromptCatalyzer" Agent

2. **真正的 AI 优化逻辑**
   - `PromptOptimizationRequestHandler` 目前只是模拟
   - 需要调用 AIKernel 进行真实的 Prompt 优化

3. **自动创建 PromptRange 和 PromptItem**
   - `PromptInitRequestHandler` 已实现自动创建逻辑 ✅
   - 但未完全测试

4. **参数优化（Temperature 等）**
   - 只优化了 Prompt 内容
   - 未优化 Temperature、TopP 等参数

## 🐛 问题诊断

### 问题 1：Agent 未自动创建
**症状：** 首次调用优化功能时，找不到 "PromptCatalyzer" Agent

**原因：** `EnsureInitializedAsync` 中的 Agent 创建代码被注释为 TODO

### 问题 2：优化逻辑不完整
**症状：** 返回的是模拟数据，未真正调用 AI

**原因：** `PromptOptimizationRequestHandler` 只是占位实现

### 问题 3：参数未优化
**症状：** 只返回新的 Prompt Code，参数未改变

**原因：** 事件定义中没有包含参数信息

## 🛠️ 解决方案

### 改进 1：完善事件定义

**文件：** `Senparc.Xncf.PromptRange.Abstractions/Events/PromptOptimizationEvents.cs`

```csharp
// 请求事件（增加更多上下文信息）
public record PromptOptimizationRequestEvent(
    string RequestId,
    string PromptCode,
    string PromptContent,           // 当前 Prompt 内容
    string UserRequirement,         // 用户优化需求
    OptimizationContext Context     // 优化上下文
) : IntegrationEvent;

// 响应事件（增加参数优化结果）
public record PromptOptimizationResponseEvent(
    string RequestId,
    string NewPromptCode,
    string NewPromptContent,        // 优化后的 Prompt 内容
    OptimizedParameters Parameters, // 优化后的参数
    double Score,
    string EvaluationReason
) : IntegrationEvent;

// 优化上下文
public record OptimizationContext(
    string ModelId,
    float CurrentTemperature,
    float CurrentTopP,
    int CurrentMaxTokens,
    float CurrentFrequencyPenalty,
    float CurrentPresencePenalty
);

// 优化后的参数
public record OptimizedParameters(
    float Temperature,
    float TopP,
    int MaxTokens,
    float FrequencyPenalty,
    float PresencePenalty
);
```

### 改进 2：实现 Agent 自动创建

**文件：** `Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`

```csharp
public async Task<PromptInitResponseEvent> EnsureInitializedAsync()
{
    // 1. 检查 Agent 是否存在
    var agent = _agentsTemplateService.GetObject(z => z.Name == "PromptCatalyzer");
    if (agent != null)
    {
        return new PromptInitResponseEvent(
            Guid.Empty.ToString(), 
            agent.SystemMessage, 
            true, 
            "Already initialized");
    }
    
    // 2. Agent 不存在，发布初始化请求
    var requestId = Guid.NewGuid().ToString();
    var tcs = new TaskCompletionSource<PromptInitResponseEvent>(
        TaskCreationOptions.RunContinuationsAsynchronously);
    
    if (!_pendingInitRequests.TryAdd(requestId, tcs))
    {
        throw new NcfExceptionBase("Failed to register init request ID.");
    }
    
    var requestEvent = new PromptInitRequestEvent(requestId);
    await _eventBus.PublishAsync(requestEvent);
    
    // 3. 等待 PromptRange 创建 Prompt 并返回 PromptCode
    var response = await tcs.Task.WaitAsync(TimeSpan.FromMinutes(2)); // 设置超时
    
    if (!response.Success)
    {
        throw new Exception($"Failed to initialize Prompt: {response.ErrorMessage}");
    }
    
    // 4. 创建 Agent
    var newAgent = new AgentTemplate
    {
        Name = "PromptCatalyzer",
        DisplayName = "Prompt 优化专家",
        Description = "自动优化 Prompt 的 AI Agent",
        SystemMessage = response.PromptCode,  // 存储 PromptCode
        ModelType = AgentModelType.Chat,
        IsActive = true,
        AddTime = DateTime.Now,
        LastUpdateTime = DateTime.Now
    };
    
    await _agentsTemplateService.SaveObjectAsync(newAgent);
    _logger.LogInformation("Successfully created PromptCatalyzer Agent with PromptCode: {PromptCode}", 
        response.PromptCode);
    
    // 5. 创建 ChatGroup
    var chatGroup = new ChatGroup
    {
        Name = "PromptCatalyzerChat",
        DisplayName = "Prompt 优化对话组",
        Description = "用于 Prompt 自动优化的对话组",
        IsActive = true,
        AddTime = DateTime.Now,
        LastUpdateTime = DateTime.Now
    };
    
    await _chatGroupService.SaveObjectAsync(chatGroup);
    
    // 6. 绑定 Agent 到 ChatGroup
    var member = new ChatGroupMember
    {
        ChatGroupId = chatGroup.Id,
        AgentTemplateId = newAgent.Id,
        IsActive = true,
        AddTime = DateTime.Now
    };
    
    await _chatGroupMemberService.SaveObjectAsync(member);
    _logger.LogInformation("Successfully created ChatGroup and bound PromptCatalyzer Agent");
    
    return response;
}
```

### 改进 3：实现真实的 AI 优化逻辑

**文件：** `Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

```csharp
public class PromptOptimizationRequestHandler 
    : IIntegrationEventHandler<PromptOptimizationRequestEvent>
{
    private readonly PromptItemService _promptItemService;
    private readonly PromptRangeService _promptRangeService;
    private readonly AIKernelService _aiKernelService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PromptOptimizationRequestHandler> _logger;

    public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("收到 Prompt 优化请求: {RequestId}, PromptCode: {PromptCode}", 
            @event.RequestId, @event.PromptCode);

        try
        {
            // 1. 解析 PromptCode 获取当前 PromptItem
            var promptItem = await GetPromptItemByCode(@event.PromptCode);
            if (promptItem == null)
            {
                throw new Exception($"Prompt not found: {@event.PromptCode}");
            }

            // 2. 构建优化请求的 System Prompt
            var systemPrompt = BuildOptimizationSystemPrompt();
            
            // 3. 构建用户输入
            var userInput = BuildOptimizationUserInput(
                @event.PromptContent,
                @event.UserRequirement,
                @event.Context);

            // 4. 调用 AI 进行优化
            var aiResponse = await _aiKernelService.ChatAsync(
                systemPrompt: systemPrompt,
                userMessage: userInput,
                modelId: @event.Context.ModelId,
                temperature: 0.7f,  // 优化过程使用固定参数
                maxTokens: 4000);

            // 5. 解析 AI 返回的优化结果
            var optimizationResult = ParseOptimizationResponse(aiResponse);

            // 6. 创建新的 PromptItem
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
                IsDraft = false  // 自动生成的，非草稿
            };

            await _promptItemService.AddPromptItemAsync(newPromptRequest);
            
            // 7. 获取新创建的 PromptItem
            var newPromptItem = await _promptItemService.GetObjectAsync(
                z => z.RangeId == promptItem.RangeId && 
                     z.ParentId == promptItem.Id,
                z => z.AddTime,
                Senparc.Ncf.Core.Enums.OrderingType.Descending);

            // 8. 发布响应事件
            var responseEvent = new PromptOptimizationResponseEvent(
                @event.RequestId,
                newPromptItem.FullVersion,
                optimizationResult.OptimizedContent,
                optimizationResult.Parameters,
                optimizationResult.PredictedScore,
                optimizationResult.Explanation
            );

            await _eventBus.PublishAsync(responseEvent);
            
            _logger.LogInformation("Prompt 优化完成: {NewPromptCode}", newPromptItem.FullVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prompt 优化失败: {RequestId}", @event.RequestId);
            
            // 发布失败响应
            var errorResponse = new PromptOptimizationResponseEvent(
                @event.RequestId,
                null,
                null,
                null,
                0,
                $"优化失败: {ex.Message}"
            );
            
            await _eventBus.PublishAsync(errorResponse);
        }
    }

    private string BuildOptimizationSystemPrompt()
    {
        return @"You are an expert Prompt Engineer (PromptCatalyzer). 
Your goal is to optimize the user's prompt and parameters to achieve better results.

Your response MUST be in the following JSON format:
{
    ""optimizedContent"": ""<optimized prompt>"",
    ""parameters"": {
        ""temperature"": 0.7,
        ""topP"": 0.9,
        ""maxTokens"": 4000,
        ""frequencyPenalty"": 0,
        ""presencePenalty"": 0
    },
    ""predictedScore"": 8.5,
    ""explanation"": ""<why these changes improve the prompt>""
}";
    }

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

Please optimize the prompt and parameters to better meet the user's requirement.";
    }

    private OptimizationResult ParseOptimizationResponse(string aiResponse)
    {
        // 解析 AI 返回的 JSON
        var json = System.Text.Json.JsonDocument.Parse(aiResponse);
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
}

internal record OptimizationResult
{
    public string OptimizedContent { get; init; }
    public OptimizedParameters Parameters { get; init; }
    public double PredictedScore { get; init; }
    public string Explanation { get; init; }
}
```

### 改进 4：前端显示优化结果

**文件：** `prompt.js`

```javascript
async executeOptimize() {
    // ... 前面的代码保持不变 ...

    this.optimizing = true;
    try {
        // 获取当前 Prompt 详情（包括参数）
        const promptDetail = this.promptDetail;
        
        const response = await servicePR.post(
            '/api/Senparc.Xncf.AgentsManager/PromptOptimizationAppService/OptimizeAsync',
            {
                promptCode: promptCode,
                promptContent: promptDetail.promptContent,
                userRequirement: this.optimizeRequirement,
                context: {
                    modelId: promptDetail.modelId,
                    currentTemperature: promptDetail.temperature,
                    currentTopP: promptDetail.topP,
                    currentMaxTokens: promptDetail.maxTokens,
                    currentFrequencyPenalty: promptDetail.frequencyPenalty,
                    currentPresencePenalty: promptDetail.presencePenalty
                }
            }
        );

        if (response.data && response.data.newPromptCode) {
            this.$message({
                message: `优化成功！
                新的 Prompt Code: ${response.data.newPromptCode}
                预测分数: ${response.data.score.toFixed(1)}
                说明: ${response.data.evaluationReason}`,
                type: 'success',
                duration: 8000,
                showClose: true
            });
            
            // 刷新 Prompt 列表
            await this.getPromptList();
            
            // 可选：自动切换到新创建的 Prompt
            const newPrompt = this.promptOpt.find(p => p.label === response.data.newPromptCode);
            if (newPrompt) {
                this.promptid = newPrompt.value;
                await this.promptChangeHandel(newPrompt.value, 'promptid');
            }
            
            this.optimizeDialogVisible = false;
        }
    } catch (error) {
        this.$message.error('优化失败: ' + (error.response?.data?.message || error.message));
    } finally {
        this.optimizing = false;
    }
}
```

## 📊 完整流程图

```
用户点击"优化"按钮
    ↓
前端调用 AgentsManager API
    ↓
PromptOptimizationService.EnsureInitializedAsync()
    ↓ (首次调用)
发布 PromptInitRequestEvent
    ↓
PromptInitRequestHandler 处理
    ├─ 检查 "PromptCatalyzer" Range 是否存在
    │   └─ 不存在则创建
    ├─ 检查 PromptItem 是否存在
    │   └─ 不存在则创建默认 Prompt
    └─ 发布 PromptInitResponseEvent
        ↓
PromptInitResponseHandler 处理
    ├─ 创建 "PromptCatalyzer" Agent
    ├─ 创建 ChatGroup
    └─ 绑定 Agent 到 ChatGroup
        ↓
PromptOptimizationService.OptimizePromptAsync()
    ↓
发布 PromptOptimizationRequestEvent
    ↓
PromptOptimizationRequestHandler 处理
    ├─ 解析当前 Prompt
    ├─ 构建优化请求
    ├─ 调用 AIKernel 进行优化
    ├─ 解析优化结果
    ├─ 创建新的 PromptItem
    └─ 发布 PromptOptimizationResponseEvent
        ↓
PromptOptimizationResponseHandler 处理
    └─ 完成挂起的请求
        ↓
返回结果给前端
    ↓
前端显示优化结果
```

## ✅ 验收标准

1. **首次调用**
   - [ ] 自动创建 "PromptCatalyzer" Range
   - [ ] 自动创建默认 PromptItem
   - [ ] 自动创建 "PromptCatalyzer" Agent
   - [ ] 自动创建并绑定 ChatGroup

2. **优化功能**
   - [ ] 能够读取当前 Prompt 内容和参数
   - [ ] 调用 AI 进行真实的优化
   - [ ] 优化包括 Prompt 内容和参数（Temperature 等）
   - [ ] 创建新的 PromptItem 存储优化结果
   - [ ] 返回新的 PromptCode

3. **用户体验**
   - [ ] 前端显示优化进度
   - [ ] 显示优化结果（新 PromptCode、预测分数、说明）
   - [ ] 支持自动切换到优化后的 Prompt
   - [ ] 错误处理和友好提示

4. **性能和可靠性**
   - [ ] 支持并发优化请求
   - [ ] 超时保护（2 分钟）
   - [ ] 失败重试机制
   - [ ] 详细的日志记录

## 🎯 下一步行动

1. 实现改进的事件定义
2. 完善 Agent 自动创建逻辑
3. 实现真实的 AI 优化逻辑
4. 更新前端代码以支持新的响应格式
5. 编写单元测试和集成测试
6. 更新文档和用户指南
