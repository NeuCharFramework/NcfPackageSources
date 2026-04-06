# Agent Function-Calling 架构重构文档

## 📋 概述

本次重构解决了 PromptCatalyzer 优化流程中的核心架构问题：
- **之前**：AI 优化过程被"固化"在 `PromptOptimizationRequestHandler` 中，直接调用 AI Kernel
- **现在**：Agent 通过 **function-calling** 自主执行优化任务，在 ChatTask 中进行完整的"任务-推理-调用-结果"循环

---

## 🎯 核心改进

### 1. **创建 PromptOptimizationPlugin**
   - 位置：`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/AIPlugins/PromptOptimizationPlugin.cs`
   - 包含 5 个 function-calling 方法：
     1. **GetPromptInfo**: 获取 Prompt 详细信息
     2. **AnalyzeModelScores**: 分析历史评分，选择最佳 ModelId
     3. **CreateOptimizedPrompt**: 创建优化后的新版本
     4. **ExecuteShootTest**: 执行打靶测试
     5. **ExecuteAIGrade**: 执行 AI 评分

### 2. **注册 Plugin 到 AgentsManager**
   - 位置：`src/Extensions/Senparc.Xncf.AgentsManager/Register.cs`
   - **DI 注册**：`services.AddScoped<PromptOptimizationPlugin>()`
   - **AIPluginHub 注册**：`aiPlugins.Add(typeof(PromptOptimizationPlugin))`

### 3. **Agent 设置 FunctionCallNames**
   - 位置：`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`
   - 创建 Agent 时设置：
     ```csharp
     functionCallNames: "Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins.PromptOptimizationPlugin"
     ```

### 4. **启动 ChatTask 让 Agent 自主工作**
   - 位置：`src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs`
   - **创建 ChatTask** 后立即**启动**它（调用 `ChatGroupService.RunChatGroupInThread`）
   - **构建任务指令**（`BuildAgentCommand`），告诉 Agent 具体要做什么，包括：
     - 任务目标
     - 用户需求
     - 当前信息
     - 执行步骤（1-7）
     - 可用的 Function Calls
   - **异步执行**：不阻塞主流程，Agent 在后台自主工作

---

## 🔄 工作流程

### 旧流程（固化）：
```
用户请求 → PromptOptimizationRequestHandler 
   ↓
直接调用 AI Kernel 进行优化
   ↓
解析 AI 返回的 JSON
   ↓
创建新 PromptItem
   ↓
（可选）打靶和评分
   ↓
返回结果
```

### 新流程（Agent 自主）：
```
用户请求 → PromptOptimizationRequestHandler（保留，快速响应）
             ↓
          创建 ChatTask
             ↓
        启动 ChatTask（异步）
             ↓
        Agent 收到任务指令
             ↓
    Agent 推理 → 调用 GetPromptInfo
             ↓
    Agent 推理 → 调用 AnalyzeModelScores
             ↓
    Agent 推理 → 决定优化方案
             ↓
    Agent 推理 → 调用 CreateOptimizedPrompt
             ↓
    Agent 推理 → 调用 ExecuteShootTest（如果需要）
             ↓
    Agent 推理 → 调用 ExecuteAIGrade（如果需要）
             ↓
    Agent 总结结果 → ChatTask 完成
```

---

## 📁 修改的文件

### 新增文件：
1. **`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/AIPlugins/PromptOptimizationPlugin.cs`**
   - 5 个 function-calling 方法
   - 从 PromptItemService, PromptRangeService, PromptResultService 获取数据

### 修改文件：
2. **`src/Extensions/Senparc.Xncf.AgentsManager/Register.cs`**
   - 添加 DI 注册和 AIPluginHub 注册

3. **`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`**
   - 设置 Agent 的 `FunctionCallNames`

4. **`src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs`**
   - 添加 `BuildAgentCommand` 方法
   - 启动 ChatTask（调用 `RunChatGroupInThread`）
   - 添加 using 引用

---

## 🧪 测试指南

### 前置条件：
1. **重启应用**（必需，确保 Plugin 被正确注册）
   ```bash
   # 在终端 5 按 Ctrl+C 停止（如果正在运行）
   cd tools/NcfSimulatedSite/Senparc.Web
   dotnet run
   ```

2. **清理旧的 Agent 和 ChatGroup**（可选，建议）
   - 数据库中删除旧的 PromptCatalyzer Agent 和 OptimizationGroup
   - 或者更新 Agent 的 `FunctionCallNames` 字段

### 测试步骤：

#### 🔸 步骤 1：初始化
1. 打开 PromptRange 页面
2. 选择一个 PromptItem
3. 点击"开始优化"
4. 如果是第一次，会提示初始化
5. 点击"开始初始化"
6. **验证**：
   - 控制台日志显示 Agent 创建成功
   - `FunctionCallNames` 应该包含 `PromptOptimizationPlugin`
   - ChatGroup 创建成功

#### 🔸 步骤 2：执行优化
1. 再次点击"开始优化"
2. ✅ 勾选"创建后立即打靶"（默认选中）
3. ✅ 勾选"打靶后使用 AI 评分"（默认不选中）
4. 输入优化需求（例如："提高清晰度和效果"）
5. 点击"开始优化"
6. **验证**：
   - 控制台日志显示 ChatTask 创建并启动
   - Agent 任务指令被记录
   - ChatTask 异步执行

#### 🔸 步骤 3：查看 Agent 工作过程
1. 打开 AgentsManager 模块
2. 查看 ChatGroup 列表
3. 找到"PromptCatalyzer-OptimizationGroup"
4. 点击"查看历史"或"任务列表"
5. **验证**：
   - 可以看到 ChatTask 记录
   - ChatTask 状态从 `Waiting` → `Chatting` → `Finished`
   - **关键**：对话记录中应该有 Agent 的**推理过程**和**function-calling 调用**
   - 例如：
     ```
     Agent: 我需要先获取当前 Prompt 的信息
     [Function Call: GetPromptInfo(promptCode="2025.12.28.3-T3.1-A2")]
     [Function Result: Prompt Information: Code: 2025.12.28.3-T3.1-A2, Content: ...]
     
     Agent: 接下来分析历史评分
     [Function Call: AnalyzeModelScores(rangeName="2025.12.28.3")]
     [Function Result: Model Performance Analysis: ...]
     
     Agent: 根据分析，我决定使用 ModelId=5，并优化 Prompt 内容
     [Function Call: CreateOptimizedPrompt(...)]
     [Function Result: Success! New prompt created: 2025.12.28.3-T3.1-A3]
     ```

#### 🔸 步骤 4：验证结果
1. 返回 PromptRange 页面
2. 刷新列表
3. **验证**：
   - 新的 PromptItem 已创建
   - Note 字段包含 "🤖AI-Agent-Generated"
   - ModelId 是根据历史评分智能选择的
   - 如果勾选了"打靶"，应该有 PromptResult 记录
   - 如果勾选了"AI 评分"，EvalAvgScore 应该有值

---

## 🐛 常见问题

### 1. ChatTask 对话是空的
   - **原因**：FunctionCallNames 未设置或 Plugin 未注册
   - **解决**：
     1. 检查数据库中 Agent 的 `FunctionCallNames` 字段
     2. 确保重启了应用
     3. 查看控制台日志中的 `FunctionCallNames` 输出

### 2. Agent 没有调用 function
   - **原因**：任务指令不够明确，或 Agent 的 SystemMessage 不匹配
   - **解决**：
     1. 检查 `BuildAgentCommand` 的输出
     2. 确保任务指令包含明确的步骤和可用的 function 列表

### 3. Plugin 找不到
   - **原因**：DI 注册或 AIPluginHub 注册缺失
   - **解决**：
     1. 检查 `Register.cs` 中的两处注册
     2. 重启应用

### 4. ChatTask 一直是 Waiting 状态
   - **原因**：`RunChatGroupInThread` 未被调用
   - **解决**：
     1. 检查 `PromptOptimizationChatTaskHandler` 的代码
     2. 查看控制台日志中的"启动 ChatTask"消息

---

## 🎉 优势

### 1. **真正的 Agent 自主性**
   - Agent 不再是"工具"，而是"决策者"
   - 通过推理决定何时调用哪个 function

### 2. **可追溯性**
   - 所有 Agent 的推理过程和 function 调用都记录在 ChatTask 中
   - 可以查看 Agent 是如何一步步完成任务的

### 3. **扩展性**
   - 轻松添加新的 function（例如：AnalyzePromptQuality, SuggestImprovements）
   - Agent 会自动学习使用新的 function

### 4. **并发处理**
   - 多个优化任务可以同时进行
   - 每个 ChatTask 独立执行

### 5. **错误恢复**
   - Agent 可以通过推理处理异常情况
   - 例如：GetPromptInfo 失败时，Agent 可以请求更多信息或选择其他策略

---

## 📝 未来改进

1. **Agent 团队协作**
   - 引入多个 Agent（例如：OptimizationAgent, TestAgent, ScoringAgent）
   - 让它们分工合作完成复杂任务

2. **持久化任务状态**
   - 将 Agent 的推理过程和决策记录到数据库
   - 用于训练和改进 Agent

3. **用户实时反馈**
   - 通过 SignalR 推送 Agent 的工作进度
   - 让用户看到 Agent 正在做什么

4. **自适应优化策略**
   - Agent 根据历史成功率调整优化策略
   - 学习哪些参数组合效果最好

---

## 🔧 相关代码位置速查

| 功能 | 文件路径 |
|------|---------|
| Plugin 定义 | `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/AIPlugins/PromptOptimizationPlugin.cs` |
| Plugin 注册 | `src/Extensions/Senparc.Xncf.AgentsManager/Register.cs` |
| Agent 创建（设置 FunctionCallNames） | `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs` |
| ChatTask 启动 | `src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs` |
| 任务指令构建 | `PromptOptimizationChatTaskHandler.BuildAgentCommand()` |
| Function-calling 配置 | `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/ChatGroupService.cs` (line 495-531) |

---

## ✅ 验收标准

- [x] Plugin 创建完成，包含 5 个方法
- [x] Plugin 正确注册到 DI 和 AIPluginHub
- [x] Agent 的 FunctionCallNames 被正确设置
- [x] ChatTask 被创建并启动
- [x] Agent 可以自主调用 function-calling
- [x] ChatTask 中有完整的对话记录
- [x] 优化后的 PromptItem 被创建
- [x] （可选）打靶和 AI 评分自动执行

---

## 📖 参考资料

- **Semantic Kernel Function-Calling**: [官方文档](https://learn.microsoft.com/en-us/semantic-kernel/concepts-sk/plugins)
- **AutoGen Multi-Agent**: [GitHub](https://github.com/microsoft/autogen)
- **NCF AgentsManager**: `src/Extensions/Senparc.Xncf.AgentsManager/README.md`
