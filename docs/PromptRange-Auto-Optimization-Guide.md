[中文版](PromptRange-Auto-Optimization-Guide.cn.md)

# PromptRange Automatic Optimization Function Implementation Guide

## 📋 Function Overview

This document details the new AI automatic optimization features in the PromptRange module, including:

1. **Automatically detect whether Prompt and Agent have been created**
2. **Intelligent initialization process** (guide the user to select AI Model when using it for the first time)
3. **Automatic optimization suggestions based on scoring**
4. **Complete optimization workflow**

---

## 🎯 Core Functions

### 1. Initialization detection and guidance

When the user clicks the "Optimize" button for the first time, the system will:```
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
```### 2. Intelligent optimization suggestions based on scoring

The system will automatically prompt users to optimize in the following scenarios:

#### Scenario A: Instant suggestions after a single score
- **Trigger timing**: After the user completes AI scoring or manual scoring
- **Threshold**: score < 6.0 points
- **Prompt method**: A confirmation dialog box pops up, the user can choose "Optimize now" or "Don't optimize yet"
- **User Experience**: blocking prompts to guide users to take immediate action```javascript
// 示例：AI评分为 4.5 分
当前 Prompt 的 AI评分为 4.5 分（低于 6.0 分）。
是否使用 AI 自动优化功能来改进 Prompt？

[立即优化] [暂不优化]
```#### Scenario B: Average score suggestion when switching Prompt
- **Trigger timing**: After the user switches to a prompt
- **Threshold**: Average score < 6.0 points
- **Prompt method**: Notification in the lower right corner (non-blocking)
- **User Experience**: Gentle prompts, does not interfere with user operations```javascript
// 示例：某 Prompt 平均分为 5.2 分
💡 优化建议
当前 Prompt 的平均分为 5.2 分，建议使用 AI 自动优化功能来改进。
点击"优化"按钮开始。

[8秒后自动消失，可手动关闭]
```### 3. Complete optimization process```
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
```---

## 🔧 Technical implementation

### Backend implementation

#### 1. PromptCatalyzerInitAppService
**File**: `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptCatalyzerInitAppService.cs`

**API provided**:

##### API 1: Check initialization status```http
GET /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus

Response:
{
  "isInitialized": false,
  "agentId": null,
  "promptCode": null
}
```##### API 2: Get available models```http
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
```##### API 3: Initialization```http
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
```#### 2. PromptOptimizationAppService
**File**: `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptOptimizationAppService.cs`

##### API 4: Optimize Prompt```http
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
```### Front-end implementation

#### 1. Add new Data variable
**File**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js````javascript
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
```#### 2. New method

##### checkPromptCatalyzerStatus()
Check if PromptCatalyzer has been initialized.

##### loadAvailableModels()
Loads a list of all available Chat type AI Models.

##### executeInitialization()
Execute the initialization process and create necessary resources.

##### checkScoreAndSuggestOptimization(resultData, scoreType)
**Function**: After the scoring is completed, optimization will be automatically suggested based on the score.
- **Parameters**:
  - `resultData`: scoring result data
  - `scoreType`: score type ("AI scoring" or "manual scoring")
- **Logic**:
  - Extract `finalScore`
  - If `finalScore < 6.0`, a confirmation dialog box will pop up
  - Automatically open the optimization dialog box after user confirmation

##### checkPromptAverageScoreAndSuggest()
**Function**: Automatically suggest optimizations based on the average score of the current Prompt
- **Trigger timing**: after switching Prompt
- **Logic**:
  - Get `evalAvgScore`
  - If `evalAvgScore < 6.0`, display the lower right corner notification
  - Non-blocking prompts

##### openOptimizeDialog()
Opens the optimization dialog (supports automatic initialization detection).

##### executeOptimize()
Execute optimization requests with complete context information.

#### 3. HTML update
**File**: `src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml`

Added new initialization dialog (about line 1289 later):
- Model selection drop-down box (with search)
-Parameter preview
- Friendly reminder messages

---

## 📊 Usage scenarios

### Scenario 1: First use of optimization function

1. The user opens the PromptRange page
2. Select a Prompt
3. Click the "Optimize" button
4. **System automatic detection**: Not initialized
5. **Display initialization dialog**: Guide the user to select AI Model
6. The user selects the Model and clicks "Start Initialization"
7. Wait 30-60 seconds (create PromptRange, PromptItem, Agent, ChatGroup)
8. Initialization successful, **automatically opens the optimization dialog**
9. User input optimization requirements
10. The system generates optimized prompts
11. Automatically switch to new prompt

### Scenario 2: Optimization suggestions after AI scoring

1. The user performs an AI rating on a PromptResult
2. The score is 4.2 points
3. **The system automatically pops up a confirmation box**: "The current AI score of Prompt is 4.2 points (lower than 6.0 points). Do you want to use the AI automatic optimization function to improve Prompt?"
4. The user clicks "Optimize Now"
5. Automatically open the optimization dialog box and enter the optimization process

### Scenario 3: Average score prompt when switching Prompt

1. The user switches to a prompt (average score 5.5 points)
2. **The system displays a notification in the lower right corner**: "💡 Optimization suggestion: The current average score of Prompt is 5.5 points. It is recommended to use the AI automatic optimization function to improve it. Click the 'Optimize' button to start."
3. The notification disappears automatically after 8 seconds and does not interfere with user operations.
4. Users can click the "Optimize" button at any time to optimize

---

## ✅ Acceptance Criteria

### Functional test checklist

#### Initialization function
- [ ] Click "Optimize" for the first time to display the initialization dialog box
- [ ] Model list loads correctly (only models of Chat type and enabled are shown)
- [ ] can search and filter Model
- [ ] Display parameter preview after selecting Model
- [ ] Automatically open the optimization dialog box after successful initialization
- [ ] Click "Optimize" for the second time to directly open the dialog box (initialization is no longer displayed)

#### Optimization function
- [ ] can input optimization requirements
- [ ] Display loading status during optimization process
- [ ] Detailed results (new code, prediction scores, parameter changes, optimization instructions) are displayed if the optimization is successful.
- [ ] Automatically refresh the Prompt list
- [ ] Automatically switch to new prompt

#### Automatic optimization suggestions
- [ ] Confirmation dialog box pops up when AI score < 6.0
- [ ] Confirmation dialog box pops up when manual rating < 6.0
- [ ] Click "Optimize Now" to automatically open the optimization dialog box
- [ ] Click "Don't optimize yet" to close the prompt
- [ ] Display the notification in the lower right corner when switching to low score Prompt
- [ ] notificationAutomatically disappear after 8 seconds
- [ ] No optimization tips are displayed when score >= 6.0

#### Error handling
- [ ] None Prompt Prompt when selecting
- [ ] prompts when there is no available Model (and guides the user to AIKernel configuration)
- [ ] Show friendly error message when API call fails
- [ ] Display the specific reason when initialization fails.
- [ ] Display detailed error information when optimization fails

---

## 🚀 Quick Start

### Prerequisites

1. **AIKernel module**: Configure at least one Chat type AI Model
2. **Model status**: Model must be enabled (Show = true)
3. **API Key**: AI API Key has been configured correctly

### Test steps

#### Step 1: Compile the project```bash
# 编译 AgentsManager（包含新 API）
dotnet build src/Extensions/Senparc.Xncf.AgentsManager/

# 编译 PromptRange（包含前端更新）
dotnet build src/Extensions/Senparc.Xncf.PromptRange/

# 或编译整个解决方案
dotnet build
```#### Step 2: Launch the application```bash
dotnet run --project [你的Web项目路径]
```#### Step 3: Test initialization process
1. Open the PromptRange page
2. Select a Prompt
3. Click the "Optimize" button
4. You should see the initialization dialog box
5. Select an AI Model
6. Click "Start Initialization"
7. Wait 30-60 seconds
8. See the success prompt and automatically open the optimization dialog box

#### Step 4: Test optimization features
1. Enter the requirements in the optimization dialog box (for example: "Make answers more creative")
2. Click "Start Optimization"
3. Wait 10-30 seconds
4. View optimization results (parameter comparison, prediction scores, optimization instructions)
5. Verify automatic switching to new prompt

#### Step 5: Test the auto-suggest feature
1. Select a test Prompt
2. Conduct target shooting test
3. AI scoring or manual scoring of results
4. If the score is < 6.0, you should see an optimization suggestion pop-up window
5. Click "Optimize Now" to verify whether the optimization process has entered normally.

---

## 📁 File list

### Add new file
- ✅ `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptCatalyzerInitAppService.cs`

### Modified files
- ✅ `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`
  - Added `checkScoreAndSuggestOptimization()` method
  - Added `checkPromptAverageScoreAndSuggest()` method
  - Updated `saveManualScore()` method (added optimization tips)
  - Updated `getPromptetail()` method (added average score check)

### Existing files (implemented previously)
- ✅ `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptOptimizationAppService.cs`
- ✅ `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`
- ✅ `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`
- ✅ `src/Extensions/Senparc.Xncf.PromptRange.Abstractions/Events/PromptInitEvents.cs`

---

## 🎨 UI effect

### Initialization dialog box```
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
```### Optimization suggestion dialog box (after scoring)```
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
```### Optimization suggestion notification (lower right corner)```
┌────────────────────────────────────┐
│  💡 优化建议                        │
├────────────────────────────────────┤
│  当前 Prompt 的平均分为 5.2 分，    │
│  建议使用 AI 自动优化功能来改进。   │
│  点击"优化"按钮开始。               │
│                                    │
│                          [×]       │
└────────────────────────────────────┘
```### Optimization result display```
✅ 优化成功！

🆕 新的 Prompt Code: 2024.1.1.1-T2-A1
📊 预测分数: 8.5

📋 优化后的参数:
  • Temperature: 0.7 → 0.9
  • TopP: 0.9 → 0.95
  • MaxTokens: 2000 → 2500

💡 优化说明: 提高了 Temperature 以增加创意性，
             同时调整 MaxTokens 以允许更完整的回答。
```---

## ⚙️ Configuration options

### Optimization threshold adjustment

If you need to adjust the score threshold of optimization suggestions, modify the following code:

**File**: `prompt.js````javascript
// checkScoreAndSuggestOptimization 方法中
const optimizationThreshold = 6.0; // 默认 6.0 分

// checkPromptAverageScoreAndSuggest 方法中
const optimizationThreshold = 6.0; // 默认 6.0 分
```**Recommended values**:
- **6.0** - Standard (recommended): Prompt when the score is less than 6 points
- **7.0** - Strict: Prompt when the score is less than 7 points
- **5.0** - Loose: only prompt if the score is very low

---

## 🔍 Debugging Guide

### Browser Console Log

In development mode, all critical steps output Console logs:```javascript
// 检查初始化状态
console.log('检查 PromptCatalyzer 初始化状态...');

// 开始优化
console.log('开始优化 Prompt:', promptCode);
console.log('优化请求参数:', requestData);

// 打分后检查
console.log('AI评分完成，最终分数: 4.5');

// 平均分检查
console.log('当前 Prompt 平均分数: 5.2');
```### FAQ

#### Problem 1: The initialization dialog box does not display
**Cause**: Model list is empty
**Solution**:
1. Check whether the AIKernel module is configured with a Chat type Model
2. Confirm that the Show field of the Model is true
3. Check the browser console for error messages

#### Problem 2: Optimization failed
**Cause**: API call error or AI service unavailable
**Solution**:
1. Check whether the AI API Key is configured correctly
2. Check the backend log for detailed errors
3. Confirm that PromptCatalyzer Agent has been initialized correctly

#### Problem 3: Optimization suggestions do not pop up
**Cause**: The score is higher than the threshold or the front-end logic is not executed
**Solution**:
1. Confirm `finalScore` < 6.0
2. View the Console log: "Check scores and prompt optimization..."
3. Confirm that the browser supports `$confirm` and `$notify` (Element UI)

---

## 🎯 Design principles

### 1. User experience first
- **Non-intrusive**: Use notifications instead of pop-ups when switching Prompts
- **Instant Feedback**: Suggest optimizations immediately after scoring
- **Smart Boot**: Use automatic boot initialization for the first time

### 2. Fault tolerance
- **Graceful downgrade**: If the detection fails, the main process will not be affected.
- **Detailed Error**: Provide specific error information and solution suggestions
- **Log improvement**: Console logs help quickly locate problems

### 3. Performance considerations
- **Asynchronous processing**: All API calls are asynchronous
- **Avoid blocking**: Use notifications instead of pop-ups (appropriate scenarios)
- **Resource Reuse**: PromptCatalyzer Agent only needs to be initialized once

---

## 📈 Future expansion

### Possible enhancements

1. **Batch Optimization**: Supports optimizing multiple Prompts at one time
2. **Optimization History**: Record the results and comparison of each optimization
3. **A/B Test**: Automatically compare the effects before and after optimization
4. **Custom Threshold**: Allows users to configure the score threshold for optimization recommendations
5. **Optimization Template**: Preset common optimization requirement templates
6. **Smart Recommendation**: Recommend the best optimization strategy based on historical data

---

## 🔐 Security

### Data validation
- All API inputs undergo strict validation
- Model ID must exist and be of the correct type
- Prompt Code must be valid

### Permission control
- Inherit the permission mechanism of AppServiceBase
- Certification and authorization system that follows the NCF framework

### Error handling
- Catch all possible exceptions
- Provide user-friendly error messages
- Record detailed server logs

---

## 📝 Summary

### Completed work

1. ✅ **PromptCatalyzerInitAppService**: Provides 3 APIs related to initialization
2. ✅ **Front-end initialization process**: Guide users to select Model and create resources
3. ✅ **Front-end optimization process**: complete optimization request and result display
4. ✅ **Optimization suggestions after scoring**: Automatically prompt for optimization when the score is low (confirmation dialog box)
5. ✅ **Average Score Optimization Suggestions**: Gentle prompts (notifications) when switching Prompt
6. ✅ **Complete error handling**: Handling of various abnormal scenarios
7. ✅ **User experience optimization**: loading status, friendly prompts, automatic refresh

### Technical Highlights

- **Intelligent Initialization**: Automatically detect and guide the creation of necessary resources
- **Multi-dimensional tips**: Single scoring + average scoring two trigger scenarios
- **Non-intrusive design**: notifications do not block user operations
- **Complete context**: carry all parameter information when optimizing
- **Automated process**: Initialization → Optimization → Refresh → Switch to full automation

### User value

1. **Lower the usage threshold**: Automatic boot initialization for first time use
2. **Improve Prompt quality**: Intelligent optimization suggestions based on data
3. **Save time**: Automated optimization process
4. **Data-driven**: Provide suggestions based on real scoring results
5. **Continuous Improvement**: Support iterative optimization

---

## 📞Technical Support

If you encounter problems, please check:

1. **Browser Console** (F12): View front-end logs and errors
2. **Back-end log**: View detailed server-side processing process
3. **Network Request**: Check API requests and responses
4. **Database**: Confirm whether Agent, PromptRange, and PromptItem are created correctly

---

## 🎉 Conclusion

PromptRange's AI automatic optimization function has been fully implemented, providing a complete closed loop from initialization to optimization. Help users continuously improve Prompt quality through intelligent score monitoring and optimization suggestions.

**Start testing now to experience AI-driven Prompt optimization! ** 🚀
