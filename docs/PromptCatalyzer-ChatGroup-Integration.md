# PromptCatalyzer - Complete Guide to ChatGroup & ChatTask Integration

## 📋 Overview

This document describes the complete architecture of PromptCatalyzer, including:
1. **ChatGroup Creation**: Automatically create a dedicated optimization group during initialization
2. **ChatTask Record**: Create a task record for each optimization to facilitate tracking and auditing
3. **AI Optimization**: Really call Semantic Kernel for Prompt optimization
4. **AI generated mark**: All AI generated prompts are marked in the Note field`🤖AI-Generated`

---

## 🏗️ Architecture design

### Core components

#### 1. **PromptOptimizationService** (AgentsManager)
- **Responsibilities**: Coordinate the initialization and optimization process
- **Key Methods**:
  - `EnsureInitializedAsync(int? modelId)`: Make sure Agent and ChatGroup are created
  - `OptimizePromptAsync(...)`: Initiate optimization request and wait for response
- **Initialization process** (4 steps):
  ```
Step 1/4: Check if PromptCatalyzer Agent already exists
Step 2/4: Create PromptRange and PromptItem via EventBus request
Step 3/4: Create PromptCatalyzer Agent
Step 4/4: Create ChatGroup (Admin and Enter are both set to the same Agent)
  ```

#### 2. **PromptOptimizationRequestHandler** (PromptRange)
- **Responsibilities**: Actual implementation of AI Prompt optimization
- **Optimization process** (5 steps):
  ```
Step 1/5: Get the original PromptItem
Step 2/5: Call AI (Semantic Kernel) for optimization
Step 3/5: Parse the JSON results returned by AI
Step 4/5: Create a new version of PromptItem (Tag AI generation)
Step 5/5: Publish the response event
  ```
- **AI optimized Prompt structure**:
  ```
You are a professional prompt engineer focused on optimizing the quality of AI prompts.

## Current Prompt content:
[original content]

## User optimization requirements:
[Requirements entered by user]

## Current parameters:
- Temperature: [current value]
- TopP: [Current value]
- MaxTokens: [current value]
- FrequencyPenalty: [current value]
- PresencePenalty: [current value]

## Optimization tasks:
1. Analyze the advantages and disadvantages of current Prompt
2. Optimize Prompt content according to user needs (maintain the original structure and improve clarity and effect)
3. Suggest parameter adjustments (if necessary)
4. Predict the performance score after optimization (0-1, 1 is the best)

Please return JSON format:
  {
"optimizedContent": "Optimized Prompt content",
"temperature": recommended Temperature (0.0-2.0),
"topP": recommended TopP (0.0-1.0),
"maxTokens": suggested MaxTokens (integer),
"frequencyPenalty": recommended FrequencyPenalty (-2.0 to 2.0),
"presencePenalty": recommended PresencePenalty (-2.0 to 2.0),
"score": predicted score (0.0-1.0),
"reason": "Description of optimization reasons and expected effects"
  }
  ```

#### 3. **PromptOptimizationChatTaskHandler** (AgentsManager)
- **Responsibilities**: Create ChatTask record optimization tasks
- **Listening events**:`PromptOptimizationRequestEvent`
- **operate**:
- Find PromptCatalyzer ChatGroup
- Get ModelId from OptimizationContext
- Create ChatTask (status: Chatting)

#### 4. **PromptOptimizationTaskCompletionHandler** (AgentsManager)
- **Responsibility**: Update ChatTask status after optimization is completed
- **Listening events**:`PromptOptimizationResponseEvent`
- **operate**:
- Find the corresponding ChatTask (by name matching)
- Update status is Finished or Canceled

---

## 📊 Data Model

### ChatGroup structure
```csharp
public class ChatGroup : EntityBase<int>
{
    public string Name { get; set; }                 // "PromptCatalyzer-OptimizationGroup"
    public bool Enable { get; set; }                 // true
    public ChatGroupState State { get; set; }        // Running
    public string Description { get; set; }          // 说明
    public int AdminAgentTemplateId { get; set; }    // 主持人 Agent ID
    public int EnterAgentTemplateId { get; set; }    // 对接人 Agent ID
}
```

**Special design of PromptCatalyzer**:
- Both Admin and Enter are set to the same Agent (PromptCatalyzer Agent)
- This simplifies the group structure because the optimization task only requires a single Agent to perform

### ChatTask structure
```csharp
public class ChatTask : EntityBase<int>
{
    public string Name { get; set; }                // "Prompt优化-2010.1.2.1-T1-A1"
    public int ChatGroupId { get; set; }            // 关联的 ChatGroup
    public int AiModelId { get; set; }              // 使用的 AI Model
    public ChatTask_Status Status { get; set; }     // Chatting/Finished/Cancelled
    public string PromptCommand { get; set; }       // 用户的优化需求
    public string Description { get; set; }         // 任务描述
    public DateTime StartTime { get; set; }         // 开始时间
    public DateTime EndTime { get; set; }           // 结束时间
    // ...
}
```

### AI tag for PromptItem
```csharp
Note = "🤖AI-Generated"  // 所有 AI 优化生成的 Prompt 都带此标记
```

---

## 🔄 Complete process diagram

### Initialization process (first use)

```
用户点击"优化"按钮
    ↓
前端检测 PromptCatalyzer 状态 (CheckStatus)
    ↓
未初始化 → 显示初始化对话框
    ↓
用户选择 AI Model → 点击"开始初始化"
    ↓
PromptOptimizationService.EnsureInitializedAsync()
    ├─ 【1/4】检查 Agent 是否存在 → 不存在
    ├─ 【2/4】发布 PromptInitRequestEvent → EventBus
    │       ↓
    │   PromptInitRequestHandler (PromptRange 模块)
    │       ├─ 检查 PromptRange 是否存在 → 创建
    │       ├─ 创建 PromptItem（Note="AI-Catalyzer"）
    │       └─ 发布 PromptInitResponseEvent → EventBus
    │           ↓
    │   PromptOptimizationService.CompleteInitRequest()
    │       └─ TCS 解除等待
    ├─ 【3/4】创建 AgentTemplate (Name="PromptCatalyzer")
    └─ 【4/4】创建 ChatGroup
            ├─ Name: "PromptCatalyzer-OptimizationGroup"
            ├─ AdminAgentTemplateId: newAgent.Id
            └─ EnterAgentTemplateId: newAgent.Id (同一个 Agent)
```

### Optimization process (normal use)

```
用户点击"优化"按钮
    ↓
前端检测 PromptCatalyzer 状态 (CheckStatus)
    ↓
已初始化 → 直接打开优化对话框
    ↓
用户输入优化需求 → 点击"开始优化"
    ↓
PromptOptimizationController.OptimizeAsync()
    ├─ 调用 PromptOptimizationService.OptimizePromptAsync()
    │   └─ 发布 PromptOptimizationRequestEvent → EventBus
    │
    ├─ 【并行处理1】PromptOptimizationRequestHandler (PromptRange)
    │   ├─ 【1/5】获取原始 PromptItem
    │   ├─ 【2/5】调用 Semantic Kernel 进行 AI 优化
    │   │       ↓
    │   │   AI 分析当前 Prompt
    │   │   AI 生成优化后的内容和参数
    │   │   AI 返回 JSON 格式结果
    │   ├─ 【3/5】解析 AI 返回的 JSON
    │   ├─ 【4/5】创建新 PromptItem (Note="🤖AI-Generated")
    │   └─ 【5/5】发布 PromptOptimizationResponseEvent
    │
    └─ 【并行处理2】PromptOptimizationChatTaskHandler (AgentsManager)
        ├─ 查找 PromptCatalyzer ChatGroup
        ├─ 创建 ChatTask (Status=Chatting)
        └─ 记录优化任务信息
            ↓
    PromptOptimizationTaskCompletionHandler 监听响应
        └─ 更新 ChatTask 状态 (Finished/Cancelled)
            ↓
    返回优化结果给前端
        ├─ 新 PromptCode
        ├─ 优化后的内容
        ├─ 新参数建议
        └─ 预测评分
```

---

## 🎯 Key implementation details

### 1. ChatGroup role settings

In the PromptCatalyzer scenario, both Admin and Enter are set to the same Agent:

```csharp
var chatGroup = new ChatGroup(
    name: "PromptCatalyzer-OptimizationGroup",
    enable: true,
    state: ChatGroupState.Running,
    description: "PromptCatalyzer 专用优化群组，用于执行 Prompt 优化任务",
    adminAgentTemplateId: newAgent.Id,  // 主持人 = PromptCatalyzer Agent
    enterAgentTemplateId: newAgent.Id   // 对接人 = PromptCatalyzer Agent
);
```

**Design Reason**:
- PromptCatalyzer is a single-responsibility Agent and does not require multi-member collaboration
- Simplify the group structure and reduce complexity
- Maintain compatibility with AgentsManager architecture

### 2. ChatTask creation timing

Every time a user triggers an optimization:
```csharp
var chatTaskDto = new ChatTaskDto(
    name: $"Prompt优化-{@event.PromptCode}",
    chatGroupId: chatGroup.Id,
    aiModelId: @event.Context.ModelId,  // 从优化请求上下文获取
    status: ChatTask_Status.Chatting,   // 初始状态：对话中
    promptCommand: @event.UserRequirement,  // 用户的优化需求
    description: $"优化 Prompt: {@event.PromptCode}",
    // ...
);
```

### 3. Prompt tag generated by AI

All PromptItems generated by AI optimization are clearly marked:

```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    // ... 其他字段 ...
    Note = "🤖AI-Generated",  // 清晰标记 AI 生成
    IsDraft = false,          // 优化后的版本默认为非草稿
    // ...
};
```

**UI display**:
- In the PromptRange list, AI-generated prompts will be displayed`🤖AI-Generated`mark
- Make it easier for users to differentiate between human-created and AI-optimized versions

### 4. Function-Calling extension capability (optional)

**Current Design**:
- PromptCatalyzer Agent`FunctionCallNames`set to`null`
- Optimization logic is completely handled by EventHandler and does not rely on function-calling

**OPTIONAL EXTENSIONS**:
If you need to let Agent call optimization capabilities independently in the future, you can:

```csharp
// 初始化时设置 function-calling
var newAgent = new AgentTemplate(
    name: "PromptCatalyzer",
    // ...
    functionCallNames: "PromptCatalyzerPlugin",  // 添加 Plugin 名称
    // ...
);
```

The Agent can then be called via function-calling`PromptCatalyzerPlugin.OptimizePrompt()`,accomplish:
- **Autonomous iterative optimization**: Agent can call its own optimization Prompt in multiple rounds
- **Intelligent decision-making**: Agent decides whether to continue optimization based on the score
- **Complex Scenario**: Multiple Agents collaborate to optimize different aspects

**Reasons for not using function-calling currently**:
1. The process is clearer and simpler
2. Better performance (reducing one layer of LLM calls)
3. Easy to debug and trace

---

## 📁 Add new file

### `/src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs`

**Contains two Handlers**:

#### `PromptOptimizationChatTaskHandler`
- **Listening**:`PromptOptimizationRequestEvent`
- **RESPONSIBILITIES**: Create ChatTask records
- **Key Code**:
```csharp
public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken cancellationToken)
{
    // 1. 查找 PromptCatalyzer ChatGroup
    var chatGroup = await _chatGroupService.GetObjectAsync(
        z => z.Name == "PromptCatalyzer-OptimizationGroup");
    
    // 2. 创建 ChatTask
    var chatTaskDto = new ChatTaskDto(
        name: $"Prompt优化-{@event.PromptCode}",
        chatGroupId: chatGroup.Id,
        aiModelId: @event.Context.ModelId,
        status: ChatTask_Status.Chatting,
        promptCommand: @event.UserRequirement,
        // ...
    );
    
    var chatTask = await _chatTaskService.CreateTask(chatTaskDto);
}
```

#### `PromptOptimizationTaskCompletionHandler`
- **Listening**:`PromptOptimizationResponseEvent`
- **Responsibility**: Update ChatTask status to Finished/Cancelled
- **Key Code**:
```csharp
public async Task Handle(PromptOptimizationResponseEvent @event, CancellationToken cancellationToken)
{
    // 查找最近的 Chatting 状态的优化任务
    var allTasks = await _chatTaskService.GetFullListAsync(
        z => z.Status == ChatTask_Status.Chatting && z.Name.Contains("Prompt优化"));
    
    var latestTask = allTasks.FirstOrDefault();
    
    // 根据优化结果更新状态
    var newStatus = @event.Success ? ChatTask_Status.Finished : ChatTask_Status.Cancelled;
    await _chatTaskService.SetStatus(newStatus, latestTask);
}
```

---

## 🔧Main changes

### 1. `PromptOptimizationService.cs`
**Modified content**:
- **New step 4/4**: Create ChatGroup
- **Update step number**: 1/3 → 1/4, 2/3 → 2/4, 3/3 → 3/4

**Key code**:
```120:156:src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs
                // === 步骤3：创建 Agent ===
                _logger.LogInformation("【步骤3/4】创建 PromptCatalyzer Agent...");
                _logger.LogInformation("  PromptCode: {PromptCode}", response.PromptCode);
                
                var newAgent = new AgentTemplate(
                    name: "PromptCatalyzer",
                    systemMessage: response.PromptCode,  // 存储 PromptCode 用于后续调用
                    enable: true,
                    description: "自动优化 Prompt 内容和参数（Temperature 等）的 AI Agent",
                    promptCode: response.PromptCode,
                    hookRobotType: HookRobotType.None,
                    hookRobotParameter: null,
                    avastar: null,
                    functionCallNames: null,  // 暂不使用 function-calling，优化逻辑由 EventHandler 处理
                    mcpEndpoints: null
                );
                
                await _agentsTemplateService.SaveObjectAsync(newAgent);
                _logger.LogInformation("  ✅ Agent 创建成功！AgentId: {AgentId}, PromptCode: {PromptCode}", 
                    newAgent.Id, response.PromptCode);
                
                // 6. 创建 ChatGroup（主持人、对接人都设为同一个 Agent）
                _logger.LogInformation("【步骤4/4】创建 ChatGroup...");
                
                var chatGroup = new ChatGroup(
                    name: "PromptCatalyzer-OptimizationGroup",
                    enable: true,
                    state: Models.DatabaseModel.Models.ChatGroupState.Running,
                    description: "PromptCatalyzer 专用优化群组，用于执行 Prompt 优化任务",
                    adminAgentTemplateId: newAgent.Id,  // 主持人就是 PromptCatalyzer Agent
                    enterAgentTemplateId: newAgent.Id   // 对接人也是 PromptCatalyzer Agent
                );
                
                await _chatGroupService.SaveObjectAsync(chatGroup);
                _logger.LogInformation("  ✅ ChatGroup 创建成功！GroupId: {GroupId}, Name: {Name}", 
                    chatGroup.Id, chatGroup.Name);
                
                _logger.LogInformation("========== EnsureInitializedAsync 完成 ==========");
                
                return response;
```

### 2. `PromptOptimizationRequestHandler.cs`(refactor)
**Modified content**:
- Changed from simple simulation to **real AI optimization**
- Use Semantic Kernel to call AI
- Parse the JSON results returned by AI
- Create new PromptItem, tag`🤖AI-Generated`

**AI calling code**:
```csharp
// 使用 Senparc.AI 的 SemanticKernel 进行优化
var senparcAiSetting = promptResult.SenparcAiSetting ?? Senparc.AI.Config.SenparcAiSetting;
var semanticAiHandler = new SemanticAiHandler(senparcAiSetting);

var iWantToRun = semanticAiHandler
    .IWantTo(senparcAiSetting)
    .ConfigModel(ConfigModel.Chat, "PromptOptimizer")
    .BuildKernel();

var kernel = iWantToRun.Kernel;

// 构建优化 Prompt
var optimizationPrompt = $@"你是一个专业的 Prompt 工程师...";

var aiResponse = await kernel.InvokePromptAsync(optimizationPrompt, cancellationToken);
var aiResult = aiResponse.GetValue<string>();

// 解析 JSON 结果
var optimizedContent = ExtractJsonValue(aiResult, "optimizedContent");
// ...
```

**Create new PromptItem**:
```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    // ... 复制原始 PromptItem 的配置 ...
    Content = optimizedContent,           // AI 优化后的内容
    Temperature = newTemperature,         // AI 建议的参数
    // ...
    IsDraft = false,                      // 优化后的版本默认非草稿
    Note = "🤖AI-Generated",              // 标记 AI 生成
    // ...
};

var newPromptItem = await _promptItemService.AddPromptItemAsync(newPromptItemRequest);
```

---

## ✅ EventBus automatic registration

All new EventHandlers will be automatically scanned and registered (via`Register.cs`configuration):

```csharp
// tools/NcfSimulatedSite/Senparc.Web/Register.cs
builder.Services.AddSenparcEventBus(
    options => { /* ... */ },
    AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.FullName.Contains("Senparc.Xncf.") || 
                    a.FullName.Contains("Senparc.Areas."))
        .ToArray()
);
```

Automatically registered Handler:
- ✅ `PromptInitRequestHandler` (PromptRange)
- ✅ `PromptInitResponseHandler` (AgentsManager)
- ✅ `PromptOptimizationRequestHandler`(PromptRange) - **Refactored to true AI optimization**
- ✅ `PromptOptimizationResponseHandler` (AgentsManager)
- ✅ `PromptOptimizationChatTaskHandler`(AgentsManager) - **NEW**
- ✅ `PromptOptimizationTaskCompletionHandler`(AgentsManager) - **NEW**

---

## 🧪 Test steps

### 1. Clean old data (first test)

```sql
-- 清理旧的 PromptCatalyzer 相关数据
DELETE FROM Senparc_AgentsManager_ChatTask WHERE ChatGroupId IN (
    SELECT Id FROM Senparc_AgentsManager_ChatGroup 
    WHERE Name = 'PromptCatalyzer-OptimizationGroup'
);
DELETE FROM Senparc_AgentsManager_ChatGroup WHERE Name = 'PromptCatalyzer-OptimizationGroup';
DELETE FROM Senparc_AgentsManager_AgentTemplate WHERE Name = 'PromptCatalyzer';
DELETE FROM Senparc_PromptRange_PromptItem WHERE RangeId IN (
    SELECT Id FROM Senparc_PromptRange_PromptRange WHERE NickName = 'PromptCatalyzer'
);
DELETE FROM Senparc_PromptRange_PromptRange WHERE NickName = 'PromptCatalyzer';
```

### 2. Restart the application

```bash
# 停止应用 (Ctrl+C)
# 重新启动
cd tools/NcfSimulatedSite/Senparc.Web
dotnet run
```

### 3. Test initialization

1. Open the PromptRange page
2. Select a Prompt
3. Click the "Optimize" button
4. **Expected results**: Display initialization dialog box
5. Select a Chat type AI Model
6. Click "Start Initialization"
7. **Observe console log**:
   ```
[Step 1/4] Check whether PromptCatalyzer Agent already exists...
[Step 2/4] Agent does not exist, start the initialization process...
[Step 3/4] Create PromptCatalyzer Agent...
【Step 4/4】Create ChatGroup...
✅ ChatGroup created successfully!
   ```
8. **Verify database**:
   ```sql
-- Check Agent
   SELECT * FROM Senparc_AgentsManager_AgentTemplate WHERE Name = 'PromptCatalyzer';
   
-- Check ChatGroup
   SELECT * FROM Senparc_AgentsManager_ChatGroup WHERE Name = 'PromptCatalyzer-OptimizationGroup';
   
-- Verify that Admin and Enter are the same Agent
   SELECT Id, Name, AdminAgentTemplateId, EnterAgentTemplateId 
   FROM Senparc_AgentsManager_ChatGroup 
   WHERE Name = 'PromptCatalyzer-OptimizationGroup';
   ```

### 4. Test optimization function

1. Enter the requirements in the optimization dialog box (for example: "Make this Prompt more professional and detailed")
2. Click "Start Optimization"
3. **Observe the console log**:
   ```
PromptOptimizationChatTaskHandler Start
✅ ChatTask created successfully! TaskId:xxx
   
PromptOptimizationRequestHandler Start
【Step 1/5】Get the original PromptItem...
[Step 2/5] Call AI to optimize Prompt...
[Step 3/5] Analyze AI optimization results...
[Step 4/5] Create a new version of PromptItem...
[Step 5/5] Publish optimization response...
   
PromptOptimizationTaskCompletionHandler Start
✅ ChatTask status updated: Finished
   ```
4. **Front-end verification**:
- Display optimization success message
- Show new PromptCode
- Display parameter changes (Temperature, TopP, etc.)
- Show predicted score
- Automatically refresh and switch to new prompt
5. **Database Verification**:
   ```sql
-- Check ChatTask records
   SELECT * FROM Senparc_AgentsManager_ChatTask 
WHERE Name LIKE 'Prompt optimization-%'
   ORDER BY AddTime DESC;
   
-- Check for new PromptItem (should have AI-Generated flag)
   SELECT Id, NickName, Note, IsDraft, AddTime 
   FROM Senparc_PromptRange_PromptItem 
   WHERE Note = '🤖AI-Generated' 
   ORDER BY AddTime DESC;
   ```

---

## 📊 Database table relationship

```
AgentTemplate (PromptCatalyzer Agent)
    ├─ Id: [新创建的 Agent ID]
    ├─ Name: "PromptCatalyzer"
    ├─ SystemMessage: [PromptCode]
    └─ PromptCode: [例如 "2026.03.24.1-T1-A1"]
        ↓
ChatGroup (优化群组)
    ├─ Id: [新创建的 Group ID]
    ├─ Name: "PromptCatalyzer-OptimizationGroup"
    ├─ AdminAgentTemplateId: [指向 PromptCatalyzer Agent]
    └─ EnterAgentTemplateId: [指向 PromptCatalyzer Agent]
        ↓
ChatTask (每次优化任务)
    ├─ Id: [任务 ID]
    ├─ Name: "Prompt优化-[PromptCode]"
    ├─ ChatGroupId: [指向 ChatGroup]
    ├─ AiModelId: [使用的 AI Model]
    ├─ Status: Chatting → Finished/Cancelled
    └─ PromptCommand: [用户的优化需求]
```

---

## 🚨 Notes

### 1. EventHandler registration sequence
- The new Handler will be`AddSenparcEventBus()`Automatic scan registration
- make sure`Register.cs`exist`StartWebEngine()`**after** call`AddSenparcEventBus()`
- This ensures that all module assemblies are loaded

### 2. Association between ChatTask and Request
**Current implementation**:
- ChatTask and optimization requests are related via **name fuzzy matching**
- `PromptOptimizationTaskCompletionHandler`Find recent Chatting status tasks

**Possible improvements** (future):
- Add in ChatTask`RequestId`fields to establish precise associations
- or in`OptimizationContext`Add in`ChatTaskId`

### 3. Concurrency optimization scenario
If multiple users trigger optimization at the same time:
- ✅ EventBus supports high concurrency (based on Channel)
- ✅ TaskCompletionSource exact match by RequestId
- ⚠️ ChatTask status updates are matched by name, and there may be a very small probability of mismatching

**suggestion**:
- Consider adding it to ChatTask in production environment`RequestId`Field
- or in`OptimizationContext`medium pass`UserId`, for more precise matching

### 4. AI generated tag specifications
- **Note field**:`🤖AI-Generated`
- **Use**:
- UI display distinction
- Data analysis and statistics
- Audit and traceability

---

## 📈 Performance and scalability

### Current architecture advantages
1. **Decoupled design**: PromptRange and AgentsManager communicate through EventBus
2. **Asynchronous processing**: Optimize tasks without blocking the main thread
3. **Auditable**: All optimization tasks have ChatTask records
4. **Traceability**: Prompts generated by AI are clearly marked

### Extensible direction
1. **Batch Optimization**: Supports optimizing multiple Prompts at one time
2. **A/B Test**: Generate multiple optimized versions at the same time for users to choose
3. **Automatic rating**: Automatically update the rating of PromptResult based on actual usage results
4. **Iterative Optimization**: Automatically trigger multiple rounds of optimization based on scores

---

## 🎯 Success Metrics

### Initialization success flag
- ✅ Exists in database`PromptCatalyzer` Agent
- ✅ Exists in database`PromptCatalyzer-OptimizationGroup` ChatGroup
- ✅ ChatGroup's Admin and Enter both point to the same Agent
- ✅ The console log shows that all 4 steps are completed

### Optimization success flag
- ✅ Created new PromptItem, Note =`🤖AI-Generated`
- ✅ ChatTask record created
- ✅ ChatTask status changes from Chatting → Finished
- ✅ Front-end displays optimization success message and new parameters
- ✅ Console log shows AI return results

---

## 🔍 Debugging Tips

### View EventBus log
```bash
# 在控制台中搜索关键词
grep "PromptOptimization" logs/console.log
grep "ChatTask" logs/console.log
```

### View ChatTask history
```sql
SELECT 
    CT.Id, 
    CT.Name, 
    CT.Status, 
    CT.PromptCommand, 
    CT.AddTime, 
    CT.StartTime, 
    CT.EndTime,
    CG.Name AS GroupName
FROM Senparc_AgentsManager_ChatTask CT
JOIN Senparc_AgentsManager_ChatGroup CG ON CT.ChatGroupId = CG.Id
WHERE CG.Name = 'PromptCatalyzer-OptimizationGroup'
ORDER BY CT.AddTime DESC;
```

### View the prompt generated by AI
```sql
SELECT 
    PI.Id,
    PI.NickName,
    PI.FullVersion AS PromptCode,
    PI.Note,
    PI.Temperature,
    PI.TopP,
    PI.MaxTokens,
    PI.AddTime
FROM Senparc_PromptRange_PromptItem PI
WHERE PI.Note = '🤖AI-Generated'
ORDER BY PI.AddTime DESC;
```

---

## 📝 Summary

### Completed functions
1. ✅ **ChatGroup automatically created**: Create a dedicated optimization group during initialization
2. ✅ **ChatTask Record**: Create a task record for each optimization
3. ✅ **Real AI Optimization**: Use Semantic Kernel to call AI for Prompt optimization
4. ✅ **AI generated mark**: All prompts generated by AI are marked`🤖AI-Generated`
5. ✅ **Parameter Optimization**: AI will suggest new Temperature, TopP and other parameters based on needs
6. ✅ **Rating prediction**: AI will predict the optimized performance rating

### Function-Calling extension (optional)
If you need to let PromptCatalyzer Agent invoke optimization capabilities independently in the future:
1. Modify the initialization`functionCallNames: "PromptCatalyzerPlugin"`
2. Agent can be called through function-calling`PromptCatalyzerPlugin.OptimizePrompt()`
3. Implement more complex autonomous iterative optimization scenarios

### Items to be optimized (optional)
1. Added in ChatTask`RequestId`fields to establish precise associations
2. Support batch optimization of multiple Prompts
3. Trigger automatic optimization based on actual scores
4. Add optimization history comparison function

---

## 📞Feedback

If you encounter problems, please check:
1. **EventHandler is registered**: Check the "EventBus Scanning Assembly" section in the startup log
2. **ChatGroup is created**: Query the database`Senparc_AgentsManager_ChatGroup`surface
3. **Whether the AI ​​call is successful**: Check the "Calling AI Kernel for optimization..." log in the console
4. **Whether a new PromptItem is created**: Query`Note = '🤖AI-Generated'`records
