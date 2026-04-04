# PromptCatalyzer - ChatGroup & ChatTask 集成完整指南

## 📋 概述

本文档描述了 PromptCatalyzer 的完整架构，包括：
1. **ChatGroup 创建**：初始化时自动创建专用优化群组
2. **ChatTask 记录**：每次优化都创建任务记录，便于追踪和审计
3. **AI 优化**：真实调用 Semantic Kernel 进行 Prompt 优化
4. **AI 生成标记**：所有 AI 生成的 Prompt 都在 Note 字段标记 `🤖AI-Generated`

---

## 🏗️ 架构设计

### 核心组件

#### 1. **PromptOptimizationService** (AgentsManager)
- **职责**：协调初始化和优化流程
- **关键方法**：
  - `EnsureInitializedAsync(int? modelId)`：确保 Agent 和 ChatGroup 已创建
  - `OptimizePromptAsync(...)`：发起优化请求并等待响应
- **初始化流程**（4步骤）：
  ```
  步骤1/4：检查 PromptCatalyzer Agent 是否已存在
  步骤2/4：通过 EventBus 请求创建 PromptRange 和 PromptItem
  步骤3/4：创建 PromptCatalyzer Agent
  步骤4/4：创建 ChatGroup（Admin 和 Enter 都设为同一个 Agent）
  ```

#### 2. **PromptOptimizationRequestHandler** (PromptRange)
- **职责**：实际执行 AI Prompt 优化
- **优化流程**（5步骤）：
  ```
  步骤1/5：获取原始 PromptItem
  步骤2/5：调用 AI（Semantic Kernel）进行优化
  步骤3/5：解析 AI 返回的 JSON 结果
  步骤4/5：创建新版本 PromptItem（标记 AI 生成）
  步骤5/5：发布响应事件
  ```
- **AI 优化 Prompt 结构**：
  ```
  你是一个专业的 Prompt 工程师，专注于优化 AI Prompt 的质量。

  ## 当前 Prompt 内容：
  [原始内容]

  ## 用户优化需求：
  [用户输入的需求]

  ## 当前参数：
  - Temperature: [当前值]
  - TopP: [当前值]
  - MaxTokens: [当前值]
  - FrequencyPenalty: [当前值]
  - PresencePenalty: [当前值]

  ## 优化任务：
  1. 分析当前 Prompt 的优缺点
  2. 根据用户需求优化 Prompt 内容（保持原有结构，提升清晰度和效果）
  3. 建议参数调整（如需要）
  4. 预测优化后的效果评分（0-1，1为最佳）

  请返回 JSON 格式：
  {
      "optimizedContent": "优化后的 Prompt 内容",
      "temperature": 建议的 Temperature（0.0-2.0）,
      "topP": 建议的 TopP（0.0-1.0）,
      "maxTokens": 建议的 MaxTokens（整数）,
      "frequencyPenalty": 建议的 FrequencyPenalty（-2.0 到 2.0）,
      "presencePenalty": 建议的 PresencePenalty（-2.0 到 2.0）,
      "score": 预测评分（0.0-1.0）,
      "reason": "优化原因和预期效果说明"
  }
  ```

#### 3. **PromptOptimizationChatTaskHandler** (AgentsManager)
- **职责**：创建 ChatTask 记录优化任务
- **监听事件**：`PromptOptimizationRequestEvent`
- **操作**：
  - 查找 PromptCatalyzer ChatGroup
  - 从 OptimizationContext 获取 ModelId
  - 创建 ChatTask（状态：Chatting）

#### 4. **PromptOptimizationTaskCompletionHandler** (AgentsManager)
- **职责**：优化完成后更新 ChatTask 状态
- **监听事件**：`PromptOptimizationResponseEvent`
- **操作**：
  - 查找对应的 ChatTask（通过名称匹配）
  - 更新状态为 Finished 或 Cancelled

---

## 📊 数据模型

### ChatGroup 结构
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

**PromptCatalyzer 的特殊设计**：
- Admin 和 Enter 都设为同一个 Agent（PromptCatalyzer Agent）
- 这样简化了群组结构，因为优化任务只需要单个 Agent 执行

### ChatTask 结构
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

### PromptItem 的 AI 标记
```csharp
Note = "🤖AI-Generated"  // 所有 AI 优化生成的 Prompt 都带此标记
```

---

## 🔄 完整流程示意图

### 初始化流程（首次使用）

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

### 优化流程（正常使用）

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

## 🎯 关键实现细节

### 1. ChatGroup 的角色设置

在 PromptCatalyzer 场景中，Admin 和 Enter 都设为同一个 Agent：

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

**设计原因**：
- PromptCatalyzer 是单一职责 Agent，不需要多成员协作
- 简化群组结构，降低复杂度
- 保持与 AgentsManager 架构的兼容性

### 2. ChatTask 的创建时机

每次用户触发优化时：
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

### 3. AI 生成的 Prompt 标记

所有 AI 优化生成的 PromptItem 都有明确标记：

```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    // ... 其他字段 ...
    Note = "🤖AI-Generated",  // 清晰标记 AI 生成
    IsDraft = false,          // 优化后的版本默认为非草稿
    // ...
};
```

**UI 显示**：
- 在 PromptRange 列表中，AI 生成的 Prompt 会显示 `🤖AI-Generated` 标记
- 便于用户区分人工创建和 AI 优化的版本

### 4. Function-Calling 扩展能力（可选）

**当前设计**：
- PromptCatalyzer Agent 的 `FunctionCallNames` 设为 `null`
- 优化逻辑完全由 EventHandler 处理，不依赖 function-calling

**可选扩展**：
如果未来需要让 Agent 自主调用优化能力，可以：

```csharp
// 初始化时设置 function-calling
var newAgent = new AgentTemplate(
    name: "PromptCatalyzer",
    // ...
    functionCallNames: "PromptCatalyzerPlugin",  // 添加 Plugin 名称
    // ...
);
```

然后 Agent 可以通过 function-calling 调用 `PromptCatalyzerPlugin.OptimizePrompt()`，实现：
- **自主迭代优化**：Agent 可以多轮调用自己优化 Prompt
- **智能决策**：Agent 根据评分决定是否继续优化
- **复杂场景**：多个 Agent 协作优化不同方面

**当前不使用 function-calling 的原因**：
1. 流程更清晰简单
2. 性能更好（减少一层 LLM 调用）
3. 便于调试和追踪

---

## 📁 新增文件

### `/src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs`

**包含两个 Handler**：

#### `PromptOptimizationChatTaskHandler`
- **监听**：`PromptOptimizationRequestEvent`
- **职责**：创建 ChatTask 记录
- **关键代码**：
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
- **监听**：`PromptOptimizationResponseEvent`
- **职责**：更新 ChatTask 状态为 Finished/Cancelled
- **关键代码**：
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

## 🔧 主要修改

### 1. `PromptOptimizationService.cs`
**修改内容**：
- **新增步骤4/4**：创建 ChatGroup
- **更新步骤标号**：1/3 → 1/4, 2/3 → 2/4, 3/3 → 3/4

**关键代码**：
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

### 2. `PromptOptimizationRequestHandler.cs` (重构)
**修改内容**：
- 从简单模拟改为**真实 AI 优化**
- 使用 Semantic Kernel 调用 AI
- 解析 AI 返回的 JSON 结果
- 创建新 PromptItem，标记 `🤖AI-Generated`

**AI 调用代码**：
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

**创建新 PromptItem**：
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

## ✅ EventBus 自动注册

所有新增的 EventHandler 会被自动扫描和注册（通过 `Register.cs` 的配置）：

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

自动注册的 Handler：
- ✅ `PromptInitRequestHandler` (PromptRange)
- ✅ `PromptInitResponseHandler` (AgentsManager)
- ✅ `PromptOptimizationRequestHandler` (PromptRange) - **已重构为真实 AI 优化**
- ✅ `PromptOptimizationResponseHandler` (AgentsManager)
- ✅ `PromptOptimizationChatTaskHandler` (AgentsManager) - **新增**
- ✅ `PromptOptimizationTaskCompletionHandler` (AgentsManager) - **新增**

---

## 🧪 测试步骤

### 1. 清理旧数据（首次测试）

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

### 2. 重启应用

```bash
# 停止应用 (Ctrl+C)
# 重新启动
cd tools/NcfSimulatedSite/Senparc.Web
dotnet run
```

### 3. 测试初始化

1. 打开 PromptRange 页面
2. 选择一个 Prompt
3. 点击"优化"按钮
4. **预期结果**：显示初始化对话框
5. 选择一个 Chat 类型的 AI Model
6. 点击"开始初始化"
7. **观察控制台日志**：
   ```
   【步骤1/4】检查 PromptCatalyzer Agent 是否已存在...
   【步骤2/4】Agent 不存在，开始初始化流程...
   【步骤3/4】创建 PromptCatalyzer Agent...
   【步骤4/4】创建 ChatGroup...
   ✅ ChatGroup 创建成功！
   ```
8. **验证数据库**：
   ```sql
   -- 检查 Agent
   SELECT * FROM Senparc_AgentsManager_AgentTemplate WHERE Name = 'PromptCatalyzer';
   
   -- 检查 ChatGroup
   SELECT * FROM Senparc_AgentsManager_ChatGroup WHERE Name = 'PromptCatalyzer-OptimizationGroup';
   
   -- 验证 Admin 和 Enter 是同一个 Agent
   SELECT Id, Name, AdminAgentTemplateId, EnterAgentTemplateId 
   FROM Senparc_AgentsManager_ChatGroup 
   WHERE Name = 'PromptCatalyzer-OptimizationGroup';
   ```

### 4. 测试优化功能

1. 在优化对话框中输入需求（例如："让这个 Prompt 更加专业和详细"）
2. 点击"开始优化"
3. **观察控制台日志**：
   ```
   PromptOptimizationChatTaskHandler 开始
   ✅ ChatTask 创建成功！TaskId: xxx
   
   PromptOptimizationRequestHandler 开始
   【步骤1/5】获取原始 PromptItem...
   【步骤2/5】调用 AI 进行 Prompt 优化...
   【步骤3/5】解析 AI 优化结果...
   【步骤4/5】创建新版本 PromptItem...
   【步骤5/5】发布优化响应...
   
   PromptOptimizationTaskCompletionHandler 开始
   ✅ ChatTask 状态已更新: Finished
   ```
4. **前端验证**：
   - 显示优化成功消息
   - 显示新 PromptCode
   - 显示参数变化（Temperature、TopP 等）
   - 显示预测评分
   - 自动刷新并切换到新 Prompt
5. **数据库验证**：
   ```sql
   -- 检查 ChatTask 记录
   SELECT * FROM Senparc_AgentsManager_ChatTask 
   WHERE Name LIKE 'Prompt优化-%' 
   ORDER BY AddTime DESC;
   
   -- 检查新的 PromptItem（应该有 AI-Generated 标记）
   SELECT Id, NickName, Note, IsDraft, AddTime 
   FROM Senparc_PromptRange_PromptItem 
   WHERE Note = '🤖AI-Generated' 
   ORDER BY AddTime DESC;
   ```

---

## 📊 数据库表关系

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

## 🚨 注意事项

### 1. EventHandler 注册顺序
- 新的 Handler 会被 `AddSenparcEventBus()` 自动扫描注册
- 确保 `Register.cs` 在 `StartWebEngine()` **之后**调用 `AddSenparcEventBus()`
- 这样可以确保所有模块程序集都已加载

### 2. ChatTask 与 Request 的关联
**当前实现**：
- ChatTask 和优化请求通过**名称模糊匹配**关联
- `PromptOptimizationTaskCompletionHandler` 查找最近的 Chatting 状态任务

**可能的改进**（未来）：
- 在 ChatTask 中添加 `RequestId` 字段，建立精确关联
- 或者在 `OptimizationContext` 中添加 `ChatTaskId`

### 3. 并发优化场景
如果多个用户同时触发优化：
- ✅ EventBus 支持高并发（基于 Channel）
- ✅ TaskCompletionSource 通过 RequestId 精确匹配
- ⚠️ ChatTask 状态更新通过名称匹配，可能有极小概率误匹配

**建议**：
- 生产环境中考虑在 ChatTask 中添加 `RequestId` 字段
- 或者在 `OptimizationContext` 中传递 `UserId`，用于更精确的匹配

### 4. AI 生成标记规范
- **Note 字段**：`🤖AI-Generated`
- **用途**：
  - UI 显示区分
  - 数据分析和统计
  - 审计和追溯

---

## 📈 性能和可扩展性

### 当前架构优势
1. **解耦设计**：PromptRange 和 AgentsManager 通过 EventBus 通信
2. **异步处理**：优化任务不阻塞主线程
3. **可审计**：所有优化任务都有 ChatTask 记录
4. **可追溯**：AI 生成的 Prompt 都有明确标记

### 可扩展方向
1. **批量优化**：支持一次优化多个 Prompt
2. **A/B 测试**：同时生成多个优化版本，让用户选择
3. **自动评分**：根据实际使用效果自动更新 PromptResult 的评分
4. **迭代优化**：基于评分自动触发多轮优化

---

## 🎯 成功指标

### 初始化成功标志
- ✅ 数据库中存在 `PromptCatalyzer` Agent
- ✅ 数据库中存在 `PromptCatalyzer-OptimizationGroup` ChatGroup
- ✅ ChatGroup 的 Admin 和 Enter 都指向同一个 Agent
- ✅ 控制台日志显示 4 个步骤都完成

### 优化成功标志
- ✅ 创建了新的 PromptItem，Note = `🤖AI-Generated`
- ✅ 创建了 ChatTask 记录
- ✅ ChatTask 状态从 Chatting → Finished
- ✅ 前端显示优化成功消息和新参数
- ✅ 控制台日志显示 AI 返回结果

---

## 🔍 调试技巧

### 查看 EventBus 日志
```bash
# 在控制台中搜索关键词
grep "PromptOptimization" logs/console.log
grep "ChatTask" logs/console.log
```

### 查看 ChatTask 历史
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

### 查看 AI 生成的 Prompt
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

## 📝 总结

### 完成的功能
1. ✅ **ChatGroup 自动创建**：初始化时创建专用优化群组
2. ✅ **ChatTask 记录**：每次优化都创建任务记录
3. ✅ **真实 AI 优化**：使用 Semantic Kernel 调用 AI 进行 Prompt 优化
4. ✅ **AI 生成标记**：所有 AI 生成的 Prompt 都标记 `🤖AI-Generated`
5. ✅ **参数优化**：AI 会根据需求建议新的 Temperature、TopP 等参数
6. ✅ **评分预测**：AI 会预测优化后的效果评分

### Function-Calling 扩展（可选）
如果未来需要让 PromptCatalyzer Agent 自主调用优化能力：
1. 修改初始化时的 `functionCallNames: "PromptCatalyzerPlugin"`
2. Agent 可以通过 function-calling 调用 `PromptCatalyzerPlugin.OptimizePrompt()`
3. 实现更复杂的自主迭代优化场景

### 待优化项（可选）
1. ChatTask 中添加 `RequestId` 字段，建立精确关联
2. 支持批量优化多个 Prompt
3. 基于实际评分触发自动优化
4. 添加优化历史对比功能

---

## 📞 问题反馈

如果遇到问题，请检查：
1. **EventHandler 是否注册**：查看启动日志中的 "EventBus 扫描程序集" 部分
2. **ChatGroup 是否创建**：查询数据库 `Senparc_AgentsManager_ChatGroup` 表
3. **AI 调用是否成功**：查看控制台中的 "调用 AI Kernel 进行优化..." 日志
4. **新 PromptItem 是否创建**：查询 `Note = '🤖AI-Generated'` 的记录
