# PromptRange 自动优化功能实施指南

## 📋 功能概述

本文档详细说明了 PromptRange 模块中新增的 AI 自动优化功能，包括：

1. **自动检测 Prompt 和 Agent 是否已创建**
2. **智能初始化流程**（首次使用时引导用户选择 AI Model）
3. **基于打分的自动优化建议**
4. **完整的优化工作流**

---

## 🎯 核心功能

### 1. 初始化检测与引导

当用户首次点击"优化"按钮时，系统会：

```
用户点击"优化" 
    ↓
检查 PromptCatalyzer 是否已初始化
    ↓
[首次使用]
    ├─ 显示"正在加载 AI Model..."
    ├─ 获取所有可用的 Chat 类型 Model
    ├─ 显示初始化对话框
    ├─ 用户选择 Model
    ├─ 创建 PromptRange、PromptItem、Agent
    └─ 自动打开优化对话框
[已初始化]
    └─ 直接打开优化对话框
```

### 2. 基于打分的智能优化建议

系统会在以下场景自动提示用户进行优化：

#### 场景 A：单次打分后的即时建议
- **触发时机**：用户完成 AI 评分或手动评分后
- **阈值**：分数 < 6.0 分
- **提示方式**：弹出确认对话框，用户可选择"立即优化"或"暂不优化"
- **用户体验**：阻塞式提示，引导用户立即采取行动

```javascript
// 示例：AI评分为 4.5 分
当前 Prompt 的 AI评分为 4.5 分（低于 6.0 分）。
是否使用 AI 自动优化功能来改进 Prompt？

[立即优化] [暂不优化]
```

#### 场景 B：切换 Prompt 时的平均分建议
- **触发时机**：用户切换到某个 Prompt 后
- **阈值**：平均分 < 6.0 分
- **提示方式**：右下角通知（非阻塞式）
- **用户体验**：温和提示，不干扰用户操作

```javascript
// 示例：某 Prompt 平均分为 5.2 分
💡 优化建议
当前 Prompt 的平均分为 5.2 分，建议使用 AI 自动优化功能来改进。
点击"优化"按钮开始。

[8秒后自动消失，可手动关闭]
```

### 3. 完整的优化流程

```
用户确认优化
    ↓
收集当前 Prompt 的完整上下文
    ├─ Prompt Code（版本号）
    ├─ Prompt Content（内容）
    ├─ 当前参数（Temperature、TopP、MaxTokens等）
    └─ 用户优化需求描述
        ↓
发送到 AgentsManager 进行 AI 分析
    ├─ 使用 PromptCatalyzer Agent
    ├─ 调用 AI 进行分析和优化
    └─ 生成新的 Prompt 版本
        ↓
创建优化后的新 Prompt
    ├─ 新的 PromptItem（继承自原 Prompt）
    ├─ 优化后的内容和参数
    └─ 预测分数和优化说明
        ↓
自动刷新列表并切换到新 Prompt
```

---

## 🔧 技术实现

### 后端实现

#### 1. PromptCatalyzerInitAppService
**文件**: `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptCatalyzerInitAppService.cs`

**提供的 API**:

##### API 1: 检查初始化状态
```http
GET /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus

Response:
{
  "isInitialized": false,
  "agentId": null,
  "promptCode": null
}
```

##### API 2: 获取可用模型
```http
GET /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels

Response:
{
  "models": [
    {
      "id": 1,
      "alias": "GPT-4",
      "deploymentName": "gpt-4",
      "aiPlatform": "AzureOpenAI",
      "note": "最强大的模型",
      "configModelType": "Chat"
    }
  ],
  "recommendedModelId": 1
}
```

##### API 3: 初始化
```http
POST /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/Initialize
Content-Type: application/json

{
  "modelId": 1
}

Response:
{
  "success": true,
  "promptCode": "PromptCatalyzer-T1-A1",
  "errorMessage": null
}
```

#### 2. PromptOptimizationAppService
**文件**: `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptOptimizationAppService.cs`

##### API 4: 优化 Prompt
```http
POST /api/Senparc.Xncf.AgentsManager/PromptOptimizationAppService/OptimizeAsync
Content-Type: application/json

{
  "promptCode": "2024.1.1.1-T1-A1",
  "promptContent": "你是一个帮助用户...",
  "userRequirement": "让回答更有创意",
  "context": {
    "modelId": 1,
    "currentTemperature": 0.7,
    "currentTopP": 0.9,
    "currentMaxTokens": 2000,
    "currentFrequencyPenalty": 0,
    "currentPresencePenalty": 0
  }
}

Response:
{
  "newPromptCode": "2024.1.1.1-T2-A1",
  "score": 8.5,
  "parameters": {
    "temperature": 0.9,
    "topP": 0.95,
    "maxTokens": 2500
  },
  "evaluationReason": "提高了 Temperature 以增加创意性..."
}
```

### 前端实现

#### 1. 新增 Data 变量
**文件**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

```javascript
data() {
    return {
        // 优化功能
        optimizeDialogVisible: false,
        optimizeRequirement: '',
        optimizing: false,
        
        // PromptCatalyzer 初始化功能
        promptCatalyzerInitVisible: false,
        availableModelsForInit: [],
        selectedModelIdForInit: null,
        loadingModels: false,
        initializing: false,
        // ... 其他变量
    }
}
```

#### 2. 新增方法

##### checkPromptCatalyzerStatus()
检查 PromptCatalyzer 是否已初始化。

##### loadAvailableModels()
加载所有可用的 Chat 类型 AI Model 列表。

##### executeInitialization()
执行初始化流程，创建必要的资源。

##### checkScoreAndSuggestOptimization(resultData, scoreType)
**功能**: 在打分完成后，根据分数自动建议优化
- **参数**:
  - `resultData`: 打分结果数据
  - `scoreType`: 评分类型（"AI评分" 或 "手动评分"）
- **逻辑**:
  - 提取 `finalScore`
  - 如果 `finalScore < 6.0`，弹出确认对话框
  - 用户确认后自动打开优化对话框

##### checkPromptAverageScoreAndSuggest()
**功能**: 根据当前 Prompt 的平均分自动建议优化
- **触发时机**: 切换 Prompt 后
- **逻辑**:
  - 获取 `evalAvgScore`
  - 如果 `evalAvgScore < 6.0`，显示右下角通知
  - 非阻塞式提示

##### openOptimizeDialog()
打开优化对话框（支持自动初始化检测）。

##### executeOptimize()
执行优化请求，包含完整的上下文信息。

#### 3. HTML 更新
**文件**: `src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml`

新增了初始化对话框（约 1289 行后）：
- Model 选择下拉框（带搜索）
- 参数预览
- 友好的提示信息

---

## 📊 使用场景

### 场景 1: 首次使用优化功能

1. 用户打开 PromptRange 页面
2. 选择一个 Prompt
3. 点击"优化"按钮
4. **系统自动检测**: 未初始化
5. **显示初始化对话框**: 引导用户选择 AI Model
6. 用户选择 Model 并点击"开始初始化"
7. 等待 30-60 秒（创建 PromptRange、PromptItem、Agent、ChatGroup）
8. 初始化成功，**自动打开优化对话框**
9. 用户输入优化需求
10. 系统生成优化后的 Prompt
11. 自动切换到新 Prompt

### 场景 2: AI 评分后的优化建议

1. 用户对某个 PromptResult 进行 AI 评分
2. 评分结果为 4.2 分
3. **系统自动弹出确认框**: "当前 Prompt 的AI评分为 4.2 分（低于 6.0 分）。是否使用 AI 自动优化功能来改进 Prompt？"
4. 用户点击"立即优化"
5. 自动打开优化对话框，进入优化流程

### 场景 3: 切换 Prompt 时的平均分提示

1. 用户切换到某个 Prompt（平均分 5.5 分）
2. **系统显示右下角通知**: "💡 优化建议：当前 Prompt 的平均分为 5.5 分，建议使用 AI 自动优化功能来改进。点击'优化'按钮开始。"
3. 通知 8 秒后自动消失，不干扰用户操作
4. 用户可随时点击"优化"按钮进行优化

---

## ✅ 验收标准

### 功能测试清单

#### 初始化功能
- [ ] 首次点击"优化"显示初始化对话框
- [ ] Model 列表正确加载（只显示 Chat 类型且已启用的 Model）
- [ ] 可以搜索过滤 Model
- [ ] 选择 Model 后显示参数预览
- [ ] 初始化成功后自动打开优化对话框
- [ ] 第二次点击"优化"直接打开对话框（不再显示初始化）

#### 优化功能
- [ ] 可以输入优化需求
- [ ] 优化过程显示加载状态
- [ ] 优化成功显示详细结果（新 Code、预测分数、参数变化、优化说明）
- [ ] 自动刷新 Prompt 列表
- [ ] 自动切换到新 Prompt

#### 自动优化建议
- [ ] AI 评分 < 6.0 时弹出确认对话框
- [ ] 手动评分 < 6.0 时弹出确认对话框
- [ ] 点击"立即优化"自动打开优化对话框
- [ ] 点击"暂不优化"关闭提示
- [ ] 切换到低分 Prompt 时显示右下角通知
- [ ] 通知 8 秒后自动消失
- [ ] 分数 >= 6.0 时不显示优化提示

#### 错误处理
- [ ] 无 Prompt 选择时提示
- [ ] 无可用 Model 时提示（并引导用户去 AIKernel 配置）
- [ ] API 调用失败时显示友好错误消息
- [ ] 初始化失败时显示具体原因
- [ ] 优化失败时显示详细错误信息

---

## 🚀 快速开始

### 前提条件

1. **AIKernel 模块**: 至少配置一个 Chat 类型的 AI Model
2. **Model 状态**: Model 必须已启用（Show = true）
3. **API Key**: AI API Key 已正确配置

### 测试步骤

#### 步骤 1: 编译项目
```bash
# 编译 AgentsManager（包含新 API）
dotnet build src/Extensions/Senparc.Xncf.AgentsManager/

# 编译 PromptRange（包含前端更新）
dotnet build src/Extensions/Senparc.Xncf.PromptRange/

# 或编译整个解决方案
dotnet build
```

#### 步骤 2: 启动应用
```bash
dotnet run --project [你的Web项目路径]
```

#### 步骤 3: 测试初始化流程
1. 打开 PromptRange 页面
2. 选择一个 Prompt
3. 点击"优化"按钮
4. 应该看到初始化对话框
5. 选择一个 AI Model
6. 点击"开始初始化"
7. 等待 30-60 秒
8. 看到成功提示并自动打开优化对话框

#### 步骤 4: 测试优化功能
1. 在优化对话框中输入需求（例如："让回答更有创意"）
2. 点击"开始优化"
3. 等待 10-30 秒
4. 查看优化结果（参数对比、预测分数、优化说明）
5. 验证是否自动切换到新 Prompt

#### 步骤 5: 测试自动建议功能
1. 选择一个测试 Prompt
2. 进行打靶测试
3. 对结果进行 AI 评分或手动评分
4. 如果分数 < 6.0，应该看到优化建议弹窗
5. 点击"立即优化"验证是否正常进入优化流程

---

## 📁 文件清单

### 新增文件
- ✅ `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptCatalyzerInitAppService.cs`

### 修改的文件
- ✅ `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`
  - 新增 `checkScoreAndSuggestOptimization()` 方法
  - 新增 `checkPromptAverageScoreAndSuggest()` 方法
  - 更新 `saveManualScore()` 方法（添加优化提示）
  - 更新 `getPromptetail()` 方法（添加平均分检查）

### 已存在的文件（之前已实现）
- ✅ `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptOptimizationAppService.cs`
- ✅ `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`
- ✅ `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`
- ✅ `src/Extensions/Senparc.Xncf.PromptRange.Abstractions/Events/PromptInitEvents.cs`

---

## 🎨 UI 效果

### 初始化对话框
```
┌─────────────────────────────────────────────┐
│  🚀 首次使用：初始化 Prompt 优化功能        │
├─────────────────────────────────────────────┤
│                                              │
│  [ℹ️ 提示框]                                 │
│  欢迎使用 AI 自动优化功能！                  │
│  首次使用需要初始化 PromptCatalyzer Agent。  │
│  请选择一个 AI Model 来驱动优化功能。        │
│  系统将自动创建相关资源。                     │
│                                              │
│  选择 AI Model:                              │
│  ┌──────────────────────────────────────┐   │
│  │ GPT-4           [AzureOpenAI]        │   │
│  │ gpt-4 | 最强大的模型                  │   │
│  └──────────────────────────────────────┘   │
│                                              │
│  💡 提示：选择的 Model 将用于创建默认 Prompt │
│                                              │
│  默认参数预览:                               │
│  ┌──────────────────────────────────────┐   │
│  │ Temperature: 0.7    TopP: 0.9        │   │
│  │ MaxTokens: 4000     FrequencyPenalty:0│   │
│  └──────────────────────────────────────┘   │
│                                              │
├─────────────────────────────────────────────┤
│               [取消]  [开始初始化]           │
└─────────────────────────────────────────────┘
```

### 优化建议对话框（打分后）
```
┌─────────────────────────────────────────┐
│          💡 建议优化                    │
├─────────────────────────────────────────┤
│  当前 Prompt 的 AI评分为 4.5 分         │
│  （低于 6.0 分）。                      │
│  是否使用 AI 自动优化功能来改进 Prompt？│
│                                         │
├─────────────────────────────────────────┤
│        [暂不优化]  [立即优化]           │
└─────────────────────────────────────────┘
```

### 优化建议通知（右下角）
```
┌────────────────────────────────────┐
│  💡 优化建议                        │
├────────────────────────────────────┤
│  当前 Prompt 的平均分为 5.2 分，    │
│  建议使用 AI 自动优化功能来改进。   │
│  点击"优化"按钮开始。               │
│                                    │
│                          [×]       │
└────────────────────────────────────┘
```

### 优化结果显示
```
✅ 优化成功！

🆕 新的 Prompt Code: 2024.1.1.1-T2-A1
📊 预测分数: 8.5

📋 优化后的参数:
  • Temperature: 0.7 → 0.9
  • TopP: 0.9 → 0.95
  • MaxTokens: 2000 → 2500

💡 优化说明: 提高了 Temperature 以增加创意性，
             同时调整 MaxTokens 以允许更完整的回答。
```

---

## ⚙️ 配置选项

### 优化阈值调整

如需调整优化建议的分数阈值，修改以下代码：

**文件**: `prompt.js`

```javascript
// checkScoreAndSuggestOptimization 方法中
const optimizationThreshold = 6.0; // 默认 6.0 分

// checkPromptAverageScoreAndSuggest 方法中
const optimizationThreshold = 6.0; // 默认 6.0 分
```

**建议值**:
- **6.0** - 标准（推荐）：分数低于6分时提示
- **7.0** - 严格：分数低于7分时提示
- **5.0** - 宽松：只在分数很低时提示

---

## 🔍 调试指南

### 浏览器 Console 日志

在开发模式下，所有关键步骤都会输出 Console 日志：

```javascript
// 检查初始化状态
console.log('检查 PromptCatalyzer 初始化状态...');

// 开始优化
console.log('开始优化 Prompt:', promptCode);
console.log('优化请求参数:', requestData);

// 打分后检查
console.log('AI评分完成，最终分数: 4.5');

// 平均分检查
console.log('当前 Prompt 平均分数: 5.2');
```

### 常见问题

#### 问题 1: 初始化对话框不显示
**原因**: Model 列表为空
**解决**:
1. 检查 AIKernel 模块是否配置了 Chat 类型的 Model
2. 确认 Model 的 Show 字段为 true
3. 查看浏览器 Console 的错误信息

#### 问题 2: 优化失败
**原因**: API 调用错误或 AI 服务不可用
**解决**:
1. 检查 AI API Key 是否正确配置
2. 查看后端日志了解详细错误
3. 确认 PromptCatalyzer Agent 已正确初始化

#### 问题 3: 优化建议不弹出
**原因**: 分数高于阈值或前端逻辑未执行
**解决**:
1. 确认 `finalScore` < 6.0
2. 查看 Console 日志："检查分数并提示优化..."
3. 确认浏览器支持 `$confirm` 和 `$notify`（Element UI）

---

## 🎯 设计原则

### 1. 用户体验优先
- **非侵入式**: 切换 Prompt 时使用通知而非弹窗
- **即时反馈**: 打分后立即建议优化
- **智能引导**: 首次使用自动引导初始化

### 2. 容错性
- **优雅降级**: 如果检测失败，不影响主流程
- **详细错误**: 提供具体的错误信息和解决建议
- **日志完善**: Console 日志帮助快速定位问题

### 3. 性能考虑
- **异步处理**: 所有 API 调用都是异步的
- **避免阻塞**: 使用通知而非弹窗（适当场景）
- **资源复用**: PromptCatalyzer Agent 只需初始化一次

---

## 📈 未来扩展

### 可能的增强功能

1. **批量优化**: 支持一次性优化多个 Prompt
2. **优化历史**: 记录每次优化的结果和对比
3. **A/B 测试**: 自动对比优化前后的效果
4. **自定义阈值**: 允许用户配置优化建议的分数阈值
5. **优化模板**: 预设常见的优化需求模板
6. **智能推荐**: 根据历史数据推荐最佳优化策略

---

## 🔐 安全性

### 数据验证
- 所有 API 输入都进行严格验证
- Model ID 必须存在且类型正确
- Prompt Code 必须有效

### 权限控制
- 继承 AppServiceBase 的权限机制
- 遵循 NCF 框架的认证授权体系

### 错误处理
- 捕获所有可能的异常
- 提供用户友好的错误消息
- 记录详细的服务端日志

---

## 📝 总结

### 已完成的工作

1. ✅ **PromptCatalyzerInitAppService**: 提供初始化相关的 3 个 API
2. ✅ **前端初始化流程**: 引导用户选择 Model 并创建资源
3. ✅ **前端优化流程**: 完整的优化请求和结果展示
4. ✅ **打分后优化建议**: 分数低时自动提示优化（确认对话框）
5. ✅ **平均分优化建议**: 切换 Prompt 时的温和提示（通知）
6. ✅ **完整的错误处理**: 各种异常场景的处理
7. ✅ **用户体验优化**: 加载状态、友好提示、自动刷新

### 技术亮点

- **智能初始化**: 自动检测并引导创建必要资源
- **多维度提示**: 单次打分 + 平均分两种触发场景
- **非侵入式设计**: 通知不阻塞用户操作
- **完整的上下文**: 优化时携带所有参数信息
- **自动化流程**: 初始化 → 优化 → 刷新 → 切换全自动

### 用户价值

1. **降低使用门槛**: 首次使用自动引导初始化
2. **提升 Prompt 质量**: 基于数据的智能优化建议
3. **节省时间**: 自动化的优化流程
4. **数据驱动**: 根据真实打分结果提供建议
5. **持续改进**: 支持迭代优化

---

## 📞 技术支持

如遇到问题，请检查：

1. **浏览器 Console** (F12): 查看前端日志和错误
2. **后端日志**: 查看详细的服务端处理过程
3. **网络请求**: 检查 API 请求和响应
4. **数据库**: 确认 Agent、PromptRange、PromptItem 是否正确创建

---

## 🎉 结语

PromptRange 的 AI 自动优化功能已全面实现，提供了从初始化到优化的完整闭环。通过智能的分数监测和优化建议，帮助用户持续改进 Prompt 质量。

**立即开始测试，体验 AI 驱动的 Prompt 优化！** 🚀
