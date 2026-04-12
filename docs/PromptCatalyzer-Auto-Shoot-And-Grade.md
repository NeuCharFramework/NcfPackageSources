[中文版](PromptCatalyzer-Auto-Shoot-And-Grade.cn.md)

# PromptCatalyzer automatic target shooting and AI scoring function

## 📋 Function Overview

Two new options are added in the optimization pop-up window to implement the optimized automated testing process:

### Option 1: Shoot immediately after creation ✅ (selected by default)
- **Function**: After optimization is completed and a new PromptItem is created, automatically perform a target shooting test
- **Purpose**: Immediately verify the optimized Prompt effect without manual operation
- **Equivalent operation**: Equivalent to manually clicking the "Target Shooting" button

### Option 2: Use AI scoring after shooting (not selected by default)
- **Function**: After target practice is completed, the AI scoring function is automatically called for quality assessment.
- **Dependencies**: Only available if "Target immediately after creation" is selected (otherwise frozen)
- **Prerequisite**: "Expected results" (ExpectedResultsJson) need to be configured in PromptItem
- **Equivalent operation**: Equivalent to manually clicking the "AI Rating" button

---

## 🎨 Front-end implementation

### 1️⃣ Data field

**File**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js````javascript
data() {
    return {
        // 优化功能相关
        optimizeDialogVisible: false,
        optimizeRequirement: '',
        optimizing: false,
        autoShootAfterOptimize: true,      // 🆕 创建后立即打靶（默认选中）
        autoAIGradeAfterShoot: false,      // 🆕 打靶后 AI 评分（默认不选中）
        // ...
    }
}
```### 2️⃣ UI interface

**File**: `src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml````html
<el-dialog title="AI 自动优化 Prompt" :visible.sync="optimizeDialogVisible" width="50%">
    <div class="optimize-dialog-content">
        <!-- 优化需求描述 -->
        <el-form-item label="优化需求描述">
            <el-input type="textarea" :rows="4" v-model="optimizeRequirement"></el-input>
        </el-form-item>
        
        <!-- 🆕 新增：优化后处理选项 -->
        <el-form-item label="优化后处理">
            <!-- 选项1：立即打靶 -->
            <el-checkbox v-model="autoShootAfterOptimize" style="display: block; margin-bottom: 10px;">
                <span style="font-weight: 500;">创建后立即打靶</span>
                <el-tooltip content="优化完成后自动执行一次打靶测试，验证新 Prompt 的效果" placement="top">
                    <i class="el-icon-question" style="color: #909399; margin-left: 5px;"></i>
                </el-tooltip>
            </el-checkbox>
            
            <!-- 选项2：AI 评分（依赖选项1） -->
            <el-checkbox v-model="autoAIGradeAfterShoot" 
                         :disabled="!autoShootAfterOptimize" 
                         style="margin-left: 20px;">
                <span style="font-weight: 500;">打靶后使用 AI 评分</span>
                <el-tooltip content="打靶完成后自动调用 AI 评分功能，快速获得质量反馈" placement="top">
                    <i class="el-icon-question" style="color: #909399; margin-left: 5px;"></i>
                </el-tooltip>
            </el-checkbox>
        </el-form-item>
    </div>
    
    <span slot="footer" class="dialog-footer">
        <el-button @click="optimizeDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="executeOptimize" :loading="optimizing">开始优化</el-button>
    </span>
</el-dialog>
```**UI effect**:
- The first checkbox is selected by default and the user can cancel it
- The second checkbox is not selected by default
- When the first checkbox is unchecked, the second one is automatically disabled (grey)

### 3️⃣ Data transfer

**File**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js````javascript
async executeOptimize() {
    // ... 参数构建 ...
    
    const requestData = {
        promptCode: promptCode,
        promptContent: promptDetail.promptContent || this.content,
        userRequirement: this.optimizeRequirement || '提高 Prompt 的质量和效果',
        context: {
            modelId: this.modelid || promptDetail.modelId,
            currentTemperature: ...,
            currentTopP: ...,
            // 🆕 新增：传递自动打靶和 AI 评分选项
            autoShootAfterOptimize: this.autoShootAfterOptimize,
            autoAIGradeAfterShoot: this.autoAIGradeAfterShoot
        }
    };
    
    // 调用后端接口
    const response = await servicePR.post('/api/.../OptimizeAsync', requestData);
    // ...
}
```---

## 🔧 Backend implementation

### 1️⃣ DTO extension

**File**: `src/Extensions/Senparc.Xncf.PromptRange.Abstractions/Events/PromptOptimizationEvents.cs````csharp
/// <summary>
/// 优化上下文（当前 Prompt 的参数）
/// </summary>
public record OptimizationContext(
    int ModelId,
    float CurrentTemperature,
    float CurrentTopP,
    int CurrentMaxTokens,
    float CurrentFrequencyPenalty,
    float CurrentPresencePenalty,
    bool AutoShootAfterOptimize = true,      // 🆕 创建后立即打靶（默认 true）
    bool AutoAIGradeAfterShoot = false       // 🆕 打靶后 AI 评分（默认 false）
);
```### 2️⃣ Dependency injection

**File**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs````csharp
public class PromptOptimizationRequestHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
{
    private readonly PromptItemService _promptItemService;
    private readonly PromptRangeService _promptRangeService;
    private readonly PromptResultService _promptResultService;  // 🆕 用于打靶和 AI 评分
    private readonly IEventBus _eventBus;
    private readonly ILogger<PromptOptimizationRequestHandler> _logger;

    public PromptOptimizationRequestHandler(
        PromptItemService promptItemService,
        PromptRangeService promptRangeService,
        PromptResultService promptResultService,  // 🆕 注入
        IEventBus eventBus,
        ILogger<PromptOptimizationRequestHandler> logger)
    {
        _promptItemService = promptItemService;
        _promptRangeService = promptRangeService;
        _promptResultService = promptResultService;  // 🆕
        _eventBus = eventBus;
        _logger = logger;
    }
```### 3️⃣ Core logic

**File**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs````csharp
// 【步骤4/5】创建新版本的 PromptItem
var newPromptItem = await _promptItemService.AddPromptItemAsync(newPromptItemRequest);
var newPromptCode = newPromptItem.FullVersion;

// 🆕 【步骤4.5/5】自动打靶和 AI 评分（如果选项开启）
int? shootResultId = null;
if (@event.Context.AutoShootAfterOptimize)
{
    _logger.LogInformation("【步骤4.5/5】开始自动打靶...");
    try
    {
        // 1️⃣ 打靶：生成一次测试结果
        var shootResult = await _promptResultService.SenparcGenerateResultAsync(
            newPromptItem, 
            userMessage: null,      // 非 Chat 模式
            chatHistory: null
        );
        shootResultId = shootResult.Id;
        _logger.LogInformation("  ✅ 打靶成功！PromptResultId: {ResultId}", shootResult.Id);

        // 2️⃣ AI 评分（如果选项也开启）
        if (@event.Context.AutoAIGradeAfterShoot)
        {
            _logger.LogInformation("  开始 AI 自动评分...");
            
            // 获取期望结果（从原 PromptItem 继承）
            var expectedResultsJson = originalItem.ExpectedResultsJson;
            if (!string.IsNullOrWhiteSpace(expectedResultsJson))
            {
                var expectedResults = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(expectedResultsJson);
                
                if (expectedResults != null && expectedResults.Count > 0)
                {
                    // 调用 AI 评分
                    await _promptResultService.RobotScoringAsync(
                        shootResult.Id, 
                        isRefresh: false, 
                        expectedResultsJson
                    );
                    
                    // 更新 PromptItem 的平均分和最高分
                    await _promptResultService.UpdateEvalScoreAsync(newPromptItem.Id);
                    
                    _logger.LogInformation("  ✅ AI 评分完成！");
                }
                else
                {
                    _logger.LogWarning("  ⚠️  期望结果列表为空，跳过 AI 评分");
                }
            }
            else
            {
                _logger.LogWarning("  ⚠️  未设置期望结果，跳过 AI 评分");
            }
        }
    }
    catch (Exception shootEx)
    {
        _logger.LogError(shootEx, "  ❌ 自动打靶或 AI 评分失败（不影响优化结果）");
        // 打靶失败不影响优化结果的返回
    }
}

// 【步骤5/5】发布响应事件（包含优化结果）
// ...
```---

## 🔄 Complete flow chart```
用户点击"开始优化"
    ↓
[用户选择]
  ☑️ 创建后立即打靶（默认选中）
  ☐ 打靶后使用 AI 评分（默认不选中）
    ↓
PromptOptimizationController.OptimizeAsync
    ↓
【步骤1】获取原始 PromptItem
    ↓
【步骤2】调用 AI Kernel 优化
    ↓
【步骤3】解析 AI 结果
    ├─ 智能选择 ModelId（基于历史评分）
    └─ 处理 JSON 转义字符（\n → 实际换行）
    ↓
【步骤4】创建新版本 PromptItem
    ├─ 设置智能选择的 ModelId
    ├─ 设置优化后的 Content（实际换行）
    ├─ 设置优化后的参数
    └─ 标记 Note = "🤖AI-Generated"
    ↓
【步骤4.5】🆕 自动打靶和 AI 评分
    │
    ├─ 如果 AutoShootAfterOptimize == true:
    │   ├─ 调用 PromptResultService.SenparcGenerateResultAsync
    │   ├─ 生成 PromptResult（打靶结果）
    │   └─ 记录 PromptResultId
    │
    └─ 如果 AutoAIGradeAfterShoot == true:
        ├─ 获取 ExpectedResultsJson（从原 PromptItem）
        ├─ 调用 PromptResultService.RobotScoringAsync
        ├─ AI 对打靶结果进行评分
        └─ 调用 UpdateEvalScoreAsync 更新 PromptItem 的平均分
    ↓
【步骤5】发布优化响应事件
    ├─ PromptOptimizationChatTaskHandler（创建 ChatTask）
    └─ PromptOptimizationTaskCompletionHandler（更新 ChatTask）
    ↓
返回给前端
    ↓
前端自动刷新 Prompt 列表
    ↓
前端自动切换到新 Prompt
```---

## 🎯 Usage scenarios

### Scenario 1: Complete automated testing process

**User Operation**:
1. Select a PromptItem
2. Click "Start Optimization"
3. ☑️ Check "Target shooting immediately after creation"
4. ☑️ Check "Use AI scoring after shooting"
5. Enter optimization requirements (for example: "Make answers more creative")
6. Click "Start Optimization"

**System automatically executes**:
1. AI optimizes Prompt content and parameters
2. Create a new version of PromptItem
3. Automatic target shooting (generating test results)
4. Automatic AI scoring
5. Update the average score and maximum score of PromptItem
6. Return optimization results

**User Benefits**:
- Complete the entire process from optimization to scoring with one click
- No need for multiple manual clicks
- Get instant feedback on ratings to decide if further optimization is needed

### Scenario 2: Automatic target shooting only

**User Operation**:
1. ☑️ Check "Target shooting immediately after creation"
2. ☐ Uncheck "Use AI scoring after shooting"

**System execution**:
1. Optimize and create new PromptItem
2. Automatic target shooting
3. **No AI scoring** (Users may want to manually view the results before deciding whether to rate)

### Scenario 3: Optimization only, no target practice

**User Operation**:
1. ☐ Uncheck "Target shooting immediately after creation"

**System execution**:
1. Only optimize and create new PromptItem
2. **No shooting, no scoring**
3. Users can perform subsequent manual testing

---

## 🧪 Test steps

### Prerequisites

1. **Configure expected results** (if you want to test AI scoring):
   - Open a PromptItem
   - Add desired results in the "AI Scoring Criteria" area, for example:```
     - 回答要专业
     - 逻辑要清晰
     - 表达要简洁
     ```-Save these desired results

### Test scenario 1: Complete automated process

1. **Restart the application** (make sure the code update takes effect)
2. **Open the PromptRange page**
3. **Choose a PromptItem with the desired result**
4. **Click the "Start Optimization" button**
5. **In the pop-up window**:
   - ✅ Keep "Target shooting immediately after creation" selected
   - ✅ Check "Use AI scoring after shooting"
   - Input requirements: "Make answers more creative"
6. **Click "Start Optimization"**
7. **Observe console log**:```log
========== 收到 Prompt 优化请求 ==========
  开始确保初始化状态...
========== PromptOptimizationRequestHandler 开始 ==========
【步骤1/5】获取原始 PromptItem...
【步骤2/5】调用 AI 进行 Prompt 优化...
【步骤3/5】解析 AI 优化结果...
  智能选择 ModelId: 原=1012, 选择=1011
  Range 中模型评分统计：Model1011=0.89(5次), Model1012=0.82(3次)
【步骤4/5】创建新版本 PromptItem...
  ✅ 新 PromptItem 创建成功！NewPromptCode: 2025.12.28.3-T3.1-A4, ItemId: 8102
【步骤4.5/5】开始自动打靶...
  ✅ 打靶成功！PromptResultId: 5678, Output Length: 1234
  开始 AI 自动评分...
  ✅ AI 评分完成！
【步骤5/5】发布优化响应...
========== PromptOptimizationChatTaskHandler 开始 ==========
  ✅ ChatTask 创建成功！TaskId: 1234
```8. **Verify database**:```sql
-- 检查新 PromptItem（应该有评分）
SELECT TOP 1 Id, FullVersion, Note, EvalAvgScore, EvalMaxScore, AddTime
FROM [dbo].[Senparc_PromptRange_PromptItem]
WHERE Note = '🤖AI-Generated'
ORDER BY Id DESC

-- 检查 PromptResult（打靶结果）
SELECT TOP 1 Id, PromptItemId, ResultString, Score, AddTime
FROM [dbo].[Senparc_PromptRange_PromptResult]
WHERE PromptItemId = (
    SELECT TOP 1 Id FROM [dbo].[Senparc_PromptRange_PromptItem]
    WHERE Note = '🤖AI-Generated'
    ORDER BY Id DESC
)
```**Expected results**:
- ✅ `EvalAvgScore` and `EvalMaxScore` of new PromptItem should have values (not -1)
- ✅ There should be a corresponding PromptResult record
- ✅ PromptResult should have AI score

### Test scenario 2: automatic target shooting only

1. **In the pop-up window**:
   - ✅ Check "Target shooting immediately after creation"
   - ☐ Uncheck "Use AI scoring after shooting"
2. **Click "Start Optimization"**
3. **Observation log**: You should see "Target shooting successful", but there is no "AI automatic scoring"```log
【步骤4.5/5】开始自动打靶...
  ✅ 打靶成功！PromptResultId: 5679
【步骤5/5】发布优化响应...
```4. **Verify database**:
   - ✅ There should be a PromptResult record
   - ❌ PromptItem's `EvalAvgScore` is still -1 (not scored)

### Test scenario 3: Optimization only, no target shooting

1. **In the pop-up window**:
   - ☐ Uncheck "Target shooting immediately after creation"
2. **Click "Start Optimization"**
3. **Observe logs**: You should not see "Step 4.5" related logs```log
【步骤4/5】创建新版本 PromptItem...
  ✅ 新 PromptItem 创建成功！
【步骤5/5】发布优化响应...
```4. **Verify database**:
   - ✅ There should be new PromptItem
   - ❌ No corresponding PromptResult (not hit)

---

## 📊 Detailed explanation of key codes

### Targeting call```csharp
var shootResult = await _promptResultService.SenparcGenerateResultAsync(
    newPromptItem,       // 新创建的 PromptItem（DTO 类型）
    userMessage: null,   // 非 Chat 模式，传 null
    chatHistory: null    // 非 Chat 模式，传 null
);
```**Parameter Description**:
- `newPromptItem`: newly created `PromptItemDto` object
- `userMessage`: If user message needs to be provided in Chat mode, pass `null` directly in test mode
- `chatHistory`: Chat history, pass `null` in direct test mode

**Return Value**:
- `PromptResultDto`: DTO containing target practice results, including `Id`, `ResultString`, `Score` and other fields

### AI scoring call```csharp
// 1. 获取期望结果（JSON 字符串）
var expectedResultsJson = originalItem.ExpectedResultsJson;

// 2. 反序列化为列表
var expectedResults = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(expectedResultsJson);

// 3. 调用 AI 评分
await _promptResultService.RobotScoringAsync(
    shootResult.Id,           // PromptResult ID
    isRefresh: false,         // 是否刷新（false 表示首次评分）
    expectedResultsJson       // 期望结果（JSON 字符串）
);

// 4. 更新 PromptItem 的聚合分数
await _promptResultService.UpdateEvalScoreAsync(newPromptItem.Id);
```**Parameter Description**:
- `promptResultId`: PromptResult ID to be scored
- `isRefresh`: whether to re-score (false = first rating, true = re-score)
- `expectedResultsJson`: JSON string of expected results (for example `["Answers should be professional", "Logic should be clear"]`)

**Execution process**:
1. `RobotScoringAsync`: Use AI to compare generated results with expected results and give a score
2. `UpdateEvalScoreAsync`: Aggregate the scores of all PromptResult under the PromptItem and update `EvalAvgScore` and `EvalMaxScore`

---

## ⚠️ IMPORTANT NOTES

### 1. Expected results are required

**If you want to use AI scoring**, you must configure the desired results in advance:
- Add desired results in the "AI scoring criteria" area of the PromptItem editing page
- The system will save these expected results to the `ExpectedResultsJson` field
- The newly optimized PromptItem will inherit the expected results of the original PromptItem

**If no result is expected**:
- The system will record a warning log: `⚠️ The expected result is not set, AI scoring is skipped`
- Target practice will be performed normally, but AI scoring will not be performed
- No errors will be reported and the optimization process will not be affected.

### 2. Exception handling

Failure in target practice or AI scoring** will not affect the return of optimization results**:```csharp
try
{
    // 打靶和 AI 评分逻辑
}
catch (Exception shootEx)
{
    _logger.LogError(shootEx, "❌ 自动打靶或 AI 评分失败（不影响优化结果）");
    // 继续执行，返回优化结果
}
```**Design Concept**:
- The core value of optimization is to generate new PromptItem
- Target practice and scoring are "additional services", and failure should not cause the entire optimization process to fail.
- Users can still manually target and score

### 3. The application must be restarted

The following files have been modified and the application must be restarted:
- `PromptOptimizationEvents.cs` (DTO definition)
- `PromptOptimizationRequestHandler.cs` (core logic)
- `prompt.js` (front-end logic)
- `Prompt.cshtml` (UI interface)

### 4. Performance considerations

**Automatic targeting and AI scoring will increase execution time**:
- Optimization itself: ~5-10 seconds (AI call)
- Target practice: ~3-5 seconds (AI generated results)
- AI scoring: ~5-10 seconds (AI evaluation)
- **Total**: ~15-25 seconds

**Suggestion**:
- If you need to quickly iterate multiple optimized versions, you can first uncheck Automatic Targeting
- After all optimizations are completed, manual targeting and scoring will be unified

---

## 💡Technical Highlights

### 1. Intelligent model selection

The new PromptItem does not simply copy the original ModelId, but intelligently selects it based on historical rating data:```
原 ModelId: 1012
历史评分: Model1011=0.89, Model1012=0.82
选择: 1011（评分更高）
```### 2. Correct processing of line breaks

The `\n` escape characters returned by AI are correctly converted to actual newlines:```
Before: "你是专业助理...\n1. 生成内容...\n2. 保持一致..."
After:  "你是专业助理...
         1. 生成内容...
         2. 保持一致..."
```### 3. Complete automation link```
Prompt 优化 → ModelId 智能选择 → 新 PromptItem 创建 → 自动打靶 → AI 评分 → 分数更新
```All steps are logged in detail for easy debugging and monitoring.

### 4. Expected result inheritance

The newly optimized PromptItem automatically inherits the expected results of the original PromptItem:
- Keep grading standards consistent
- Convenient to compare the effects before and after optimization
- User does not need to reconfigure

### 5. UI interaction optimization

- The second option depends on the first one: `:disabled="!autoShootAfterOptimize"`
- Provide tooltip to explain the function of each option
- The default values are reasonable (shoot immediately=true, AI score=false)

---

## 🎉 Summary

This enhancement adds a **complete automated testing process** to PromptCatalyzer:

| Function | Description | Default Value |
|------|------|--------|
| **Smart ModelId Selection** | Automatically select the best model based on historical ratings | Automatically |
| **Line Break Handling** | Multi-line text generated by AI is displayed correctly | Automatically |
| **Target shooting immediately after creation** | Automatically test new Prompt after optimization is completed | ✅ Enabled by default |
| **AI scoring after target practice** | Automatic scoring feedback after test completion | ☐ Closed by default |

**User experience improvement**:
- ✅ Complete the process from optimization to scoring with one click
- ✅ Reduce manual steps
- ✅ Get quality feedback instantly
- ✅ Flexible control of automation levels

**Please restart the app now and test these new features! **
