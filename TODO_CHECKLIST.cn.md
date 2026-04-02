# 🎯 Prompt 优化功能实施清单

## 📋 准备工作

### Step 0: 确认依赖服务 ⏳

**需要您提供的信息：**

1. **AI 调用服务**
   - [ ] 确认项目中用于调用 AI 的服务类名
   - [ ] 确认该服务的调用方法签名（如 `ChatAsync`, `CompletionAsync` 等）
   - [ ] 确认如何在 Handler 中注入该服务
   
   **示例问题**：
   ```csharp
   // 是这样的吗？
   public class AIKernelService
   {
       public Task<string> ChatAsync(string systemPrompt, string userMessage, ...);
   }
   
   // 还是这样？
   public class ChatService
   {
       public Task<string> SendAsync(ChatRequest request);
   }
   ```

2. **PromptItem 查询方法**
   - [ ] 确认 `PromptItemService` 是否有通过 `FullVersion` 查询的方法
   - [ ] 如果没有，我将帮您实现一个

3. **测试环境**
   - [ ] 确认有可用的 AI Model 配置
   - [ ] 确认 AI API Key 已配置

---

## 🔧 实施任务

### Task 1: 更新 PromptOptimizationRequestHandler ⏳

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

**当前问题**：
- 使用旧的事件格式（缺少 `NewPromptContent` 和 `Parameters`）
- 只是模拟优化，没有真正调用 AI

**需要实现**：
1. [ ] 添加必要的服务依赖注入（AI 服务、PromptRangeService 等）
2. [ ] 实现 `GetPromptItemByCode()` 方法来解析和查询 PromptItem
3. [ ] 实现 `BuildOptimizationSystemPrompt()` 方法
4. [ ] 实现 `BuildOptimizationUserInput()` 方法
5. [ ] 调用 AI 服务进行优化
6. [ ] 解析 AI 返回的 JSON（包含优化后的内容和参数）
7. [ ] 创建新的 PromptItem
8. [ ] 发布正确格式的响应事件

**我将提供**：完整的实现代码模板（需要您填入 AI 服务调用部分）

---

### Task 2: 更新前端 JavaScript ⏳

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

**需要修改的方法**: `executeOptimize()` (约在第 3010 行)

**需要实现**：
1. [ ] 获取当前 Prompt 的完整信息（包括参数）
2. [ ] 构建包含 `promptContent` 和 `context` 的请求对象
3. [ ] 显示更详细的优化结果（包括参数变化）
4. [ ] 刷新 Prompt 列表
5. [ ] 可选：自动切换到新的 Prompt

**我将提供**：完整的 JavaScript 代码

---

### Task 3: 测试和验证 ⏳

**测试步骤**：
1. [ ] 单元测试：测试 AI 优化逻辑
2. [ ] 集成测试：测试完整流程
3. [ ] 端到端测试：从前端点击到看到结果

**验收标准**：
- [ ] 首次调用时自动创建 "PromptCatalyzer" Agent
- [ ] 能够调用 AI 优化 Prompt 内容
- [ ] 能够优化参数（Temperature 等）
- [ ] 创建新的 PromptItem 并返回 PromptCode
- [ ] 前端显示优化结果

---

## 📊 进度追踪

| 任务 | 状态 | 完成时间 |
|------|------|----------|
| Step 0: 确认依赖 | ⏳ 等待中 | - |
| Task 1: 更新 Handler | ⏳ 待开始 | - |
| Task 2: 更新前端 | ⏳ 待开始 | - |
| Task 3: 测试验证 | ⏳ 待开始 | - |

---

## 🚀 开始行动

**当前步骤**: Step 0 - 需要您提供 AI 服务的信息

**请您做的事**：
1. 查看您的项目中 AI 调用服务的代码
2. 回复以下信息：
   - AI 服务的类名
   - AI 服务的调用方法签名
   - 如何注入该服务

**示例回复**：
```
我的 AI 服务是 `Senparc.AI.Kernel.AIChatService`，
调用方法是 `Task<string> ChatAsync(string prompt, ChatOptions options)`，
通过构造函数注入。
```

收到您的信息后，我将立即为您提供完整的实现代码！

---

## 📞 需要帮助？

如果遇到任何问题，请随时告诉我：
- AI 服务相关问题
- 代码实现疑问
- 测试过程中的错误
- 任何其他问题

我会逐步帮您解决！
