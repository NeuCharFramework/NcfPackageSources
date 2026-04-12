[中文版](Agent-FunctionCalling-Architecture.cn.md)

# Agent Function-Calling architecture reconstruction document

## 📋 Overview

This reconstruction solves the core architectural issues in the PromptCatalyzer optimization process:
- **Before**: The AI optimization process is "solidified" in `PromptOptimizationRequestHandler` and calls AI Kernel directly
- **Now**: Agent independently performs optimization tasks through **function-calling** and performs a complete "task-inference-call-result" cycle in ChatTask

---

## 🎯 Core improvements

### 1. **Create PromptOptimizationPlugin**
   - Location: `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/AIPlugins/PromptOptimizationPlugin.cs`
   - Contains 5 function-calling methods:
     1. **GetPromptInfo**: Get Prompt details
     2. **AnalyzeModelScores**: Analyze historical scores and select the best ModelId
     3. **CreateOptimizedPrompt**: Create a new optimized version
     4. **ExecuteShootTest**: Execute shooting test
     5. **ExecuteAIGrade**: Execute AI scoring

### 2. **Register Plugin to AgentsManager**
   - Location: `src/Extensions/Senparc.Xncf.AgentsManager/Register.cs`
   - **DI Registration**: `services.AddScoped<PromptOptimizationPlugin>()`
   - **AIPluginHub registration**: `aiPlugins.Add(typeof(PromptOptimizationPlugin))`

### 3. **Agent settings FunctionCallNames**
   - Location: `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`
   - Set when creating Agent:```csharp
     functionCallNames: "Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins.PromptOptimizationPlugin"
     ```### 4. **Start ChatTask and let Agent work autonomously**
   - Location: `src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs`
   - **Start** the ChatTask** immediately after creating it (call `ChatGroupService.RunChatGroupInThread`)
   - **Build task command** (`BuildAgentCommand`), tells the Agent what to do specifically, including:
     - Mission objectives
     - User needs
     - Current information
     - Perform steps (1-7)
     - Available Function Calls
   - **Asynchronous execution**: Does not block the main process, Agent works autonomously in the background

---

## 🔄 Workflow

### Old process (solidification):```
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
```### New process (Agent autonomous):```
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
```---

## 📁 Modified files

### Add new files:
1. **`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/AIPlugins/PromptOptimizationPlugin.cs`**
   - 5 function-calling methods
   - Get data from PromptItemService, PromptRangeService, PromptResultService

### Modify files:
2. **`src/Extensions/Senparc.Xncf.AgentsManager/Register.cs`**
   - Added DI registration and AIPluginHub registration

3. **`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`**
   - Set Agent's `FunctionCallNames`

4. **`src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs`**
   - Added `BuildAgentCommand` method
   - Start ChatTask (call `RunChatGroupInThread`)
   - Add using reference

---

## 🧪 Testing Guide

### Preconditions:
1. **Restart the application** (required, make sure the Plugin is correctly registered)```bash
   # 在终端 5 按 Ctrl+C 停止（如果正在运行）
   cd tools/NcfSimulatedSite/Senparc.Web
   dotnet run
   ```2. **Clean up old Agents and ChatGroups** (optional, recommended)
   - Delete old PromptCatalyzer Agent and OptimizationGroup from database
   - Or update the Agent's `FunctionCallNames` field

### Test steps:

#### 🔸 Step 1: Initialization
1. Open the PromptRange page
2. Select a PromptItem
3. Click "Start Optimization"
4. If it is the first time, you will be prompted to initialize.
5. Click "Start Initialization"
6. **Verification**:
   - The console log shows that the Agent was created successfully
   - `FunctionCallNames` should contain `PromptOptimizationPlugin`
   - ChatGroup created successfully

#### 🔸 Step 2: Perform optimization
1. Click "Start Optimization" again
2. ✅ Check "Target shooting immediately after creation" (selected by default)
3. ✅ Check "Use AI scoring after shooting" (not checked by default)
4. Enter optimization requirements (for example: "Improve clarity and effect")
5. Click "Start Optimization"
6. **Verification**:
   - Console log shows ChatTask created and started
   - Agent task instructions are recorded
   - ChatTask asynchronous execution

#### 🔸 Step 3: View the Agent working process
1. Open the AgentsManager module
2. View the ChatGroup list
3. Find "PromptCatalyzer-OptimizationGroup"
4. Click "View History" or "Task List"
5. **Verification**:
   - Can see ChatTask records
   - ChatTask status changes from `Waiting` → `Chatting` → `Finished`
   - **Key**: The conversation record should contain the Agent's **reasoning process** and **function-calling**
   - For example:```
     Agent: 我需要先获取当前 Prompt 的信息
     [Function Call: GetPromptInfo(promptCode="2025.12.28.3-T3.1-A2")]
     [Function Result: Prompt Information: Code: 2025.12.28.3-T3.1-A2, Content: ...]
     
     Agent: 接下来分析历史评分
     [Function Call: AnalyzeModelScores(rangeName="2025.12.28.3")]
     [Function Result: Model Performance Analysis: ...]
     
     Agent: 根据分析，我决定使用 ModelId=5，并优化 Prompt 内容
     [Function Call: CreateOptimizedPrompt(...)]
     [Function Result: Success! New prompt created: 2025.12.28.3-T3.1-A3]
     ```#### 🔸 Step 4: Verify results
1. Return to the PromptRange page
2. Refresh the list
3. **Verification**:
   - A new PromptItem is created
   - Note field contains "🤖AI-Agent-Generated"
   - ModelId is intelligently selected based on historical ratings
   - If "Target Shooting" is checked, there should be a PromptResult record
   - If "AI Score" is checked, EvalAvgScore should have a value

---

## 🐛 FAQ

### 1. ChatTask conversation is empty
   - **Cause**: FunctionCallNames is not set or Plugin is not registered
   - **SOLVED**:
     1. Check the `FunctionCallNames` field of Agent in the database
     2. Make sure you restart the app
     3. View the `FunctionCallNames` output in the console log

### 2. Agent does not call function
   - **Cause**: The task instructions are not clear enough, or the Agent’s SystemMessage does not match
   - **SOLVED**:
     1. Check the output of `BuildAgentCommand`
     2. Make sure task instructions contain clear steps and a list of available functions

### 3. Plugin not found
   - **Cause**: Missing DI registration or AIPluginHub registration
   - **SOLVED**:
     1. Check the two registrations in `Register.cs`
     2. Restart the application

### 4. ChatTask is always in Waiting state
   - **Cause**: `RunChatGroupInThread` was not called
   - **SOLVED**:
     1. Check the code of `PromptOptimizationChatTaskHandler`
     2. View the "Start ChatTask" message in the console log

---

## 🎉 Advantages

### 1. **True Agent Autonomy**
   - Agent is no longer a "tool" but a "decision maker"
   - Use reasoning to decide when to call which function

### 2. **Traceability**
   - All Agent's reasoning processes and function calls are recorded in ChatTask
   - You can see how the Agent completes the task step by step

### 3. **Extensibility**
   - Easily add new functions (for example: AnalyzePromptQuality, SuggestImprovements)
   - Agent will automatically learn to use new functions

### 4. **Concurrent processing**
   - Multiple optimization tasks can be performed simultaneously
   - Each ChatTask is executed independently

### 5. **Error Recovery**
   - Agent can handle abnormal situations through reasoning
   - For example: when GetPromptInfo fails, the Agent can request more information or choose another strategy

---

## 📝 Future improvements

1. **Agent Team Collaboration**
   -Introduce multiple Agents (for example: OptimizationAgent, TestAgent, ScoringAgent)
   - Let them work together to complete complex tasks

2. **Persistent task status**
   - Record the Agent's reasoning process and decision-making to the database
   - Used to train and improve Agent

3. **Real-time user feedback**
   - Push Agent's work progress through SignalR
   - Let users see what the Agent is doing

4. **Adaptive optimization strategy**
   - Agent adjusts optimization strategy based on historical success rate
   - Learn which parameter combinations work best

---

## 🔧 Quick check of related code locations

| Function | File Path |
|------|---------|
| Plugin definition | `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/AIPlugins/PromptOptimizationPlugin.cs` |
| Plugin registration | `src/Extensions/Senparc.Xncf.AgentsManager/Register.cs` |
| Agent creation (setting FunctionCallNames) | `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs` |
| ChatTask start | `src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs` |
| Task command construction | `PromptOptimizationChatTaskHandler.BuildAgentCommand()` |
| Function-calling configuration | `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/ChatGroupService.cs` (line 495-531) |

---

## ✅ Acceptance Criteria

- [x] Plugin is created and contains 5 methods
- [x] Plugin properly registered to DI and AIPluginHub
- [x] Agent's FunctionCallNames are set correctly
- [x] ChatTask is created and started
- [x] Agent can call function-calling independently
- [x] Complete conversation records in ChatTask
- [x] Optimized PromptItem is created
- [x] (optional) Automatic execution of target practice and AI scoring

---

## 📖 References

- **Semantic Kernel Function-Calling**: [Official Document](https://learn.microsoft.com/en-us/semantic-kernel/concepts-sk/plugins)
- **AutoGen Multi-Agent**: [GitHub](https://github.com/microsoft/autogen)
- **NCF AgentsManager**: `src/Extensions/Senparc.Xncf.AgentsManager/README.md`
