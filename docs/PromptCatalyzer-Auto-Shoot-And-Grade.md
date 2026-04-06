# PromptCatalyzer 自动打靶与 AI 评分功能

## 📋 功能概述

在优化弹窗中新增两个选项，实现优化后的自动化测试流程：

### 选项 1：创建后立即打靶 ✅ (默认选中)
- **功能**：优化完成并创建新 PromptItem 后，自动执行一次打靶测试
- **目的**：立即验证优化后的 Prompt 效果，无需手动操作
- **等同操作**：等同于手动点击"打靶"按钮

### 选项 2：打靶后使用 AI 评分 (默认不选中)
- **功能**：打靶完成后，自动调用 AI 评分功能进行质量评估
- **依赖**：只有当"创建后立即打靶"选中时才可用（否则冻结）
- **前提**：需要在 PromptItem 中配置了"期望结果"（ExpectedResultsJson）
- **等同操作**：等同于手动点击"AI评分"按钮

---

## 🎨 前端实现

### 1️⃣ 数据字段

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

```javascript
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
```

### 2️⃣ UI 界面

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml`

```html
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
```

**UI 效果**：
- 第一个 checkbox 默认选中，用户可以取消
- 第二个 checkbox 默认不选中
- 当第一个 checkbox 未选中时，第二个自动禁用（灰色）

### 3️⃣ 数据传递

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

```javascript
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
```

---

## 🔧 后端实现

### 1️⃣ DTO 扩展

**文件**: `src/Extensions/Senparc.Xncf.PromptRange.Abstractions/Events/PromptOptimizationEvents.cs`

```csharp
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
```

### 2️⃣ 依赖注入

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

```csharp
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
```

### 3️⃣ 核心逻辑

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

```csharp
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
```

---

## 🔄 完整流程图

```
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
```

---

## 🎯 使用场景

### 场景 1：完整的自动化测试流程

**用户操作**：
1. 选择一个 PromptItem
2. 点击"开始优化"
3. ☑️ 勾选"创建后立即打靶"
4. ☑️ 勾选"打靶后使用 AI 评分"
5. 输入优化需求（例如："让回答更具创造性"）
6. 点击"开始优化"

**系统自动执行**：
1. AI 优化 Prompt 内容和参数
2. 创建新版本 PromptItem
3. 自动打靶（生成测试结果）
4. 自动 AI 评分
5. 更新 PromptItem 的平均分和最高分
6. 返回优化结果

**用户收益**：
- 一键完成从优化到评分的全流程
- 无需多次手动点击
- 立即获得评分反馈，决定是否需要进一步优化

### 场景 2：仅自动打靶

**用户操作**：
1. ☑️ 勾选"创建后立即打靶"
2. ☐ 不勾选"打靶后使用 AI 评分"

**系统执行**：
1. 优化并创建新 PromptItem
2. 自动打靶
3. **不进行 AI 评分**（用户可能想手动查看结果后再决定是否评分）

### 场景 3：仅优化，不打靶

**用户操作**：
1. ☐ 取消勾选"创建后立即打靶"

**系统执行**：
1. 仅优化并创建新 PromptItem
2. **不打靶，不评分**
3. 用户可以后续手动测试

---

## 🧪 测试步骤

### 前提条件

1. **配置期望结果**（如果要测试 AI 评分）：
   - 打开一个 PromptItem
   - 在"AI评分标准"区域添加期望结果，例如：
     ```
     - 回答要专业
     - 逻辑要清晰
     - 表达要简洁
     ```
   - 保存这些期望结果

### 测试场景 1：完整自动化流程

1. **重启应用**（确保代码更新生效）
2. **打开 PromptRange 页面**
3. **选择一个有期望结果的 PromptItem**
4. **点击"开始优化"按钮**
5. **在弹窗中**：
   - ✅ 保持"创建后立即打靶"选中
   - ✅ 勾选"打靶后使用 AI 评分"
   - 输入需求："让回答更具创造性"
6. **点击"开始优化"**
7. **观察控制台日志**：

```log
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
```

8. **验证数据库**：

```sql
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
```

**期望结果**：
- ✅ 新 PromptItem 的 `EvalAvgScore` 和 `EvalMaxScore` 应该有值（不是 -1）
- ✅ 应该有对应的 PromptResult 记录
- ✅ PromptResult 应该有 AI 评分

### 测试场景 2：仅自动打靶

1. **在弹窗中**：
   - ✅ 勾选"创建后立即打靶"
   - ☐ 不勾选"打靶后使用 AI 评分"
2. **点击"开始优化"**
3. **观察日志**：应该看到"打靶成功"，但没有"AI 自动评分"

```log
【步骤4.5/5】开始自动打靶...
  ✅ 打靶成功！PromptResultId: 5679
【步骤5/5】发布优化响应...
```

4. **验证数据库**：
   - ✅ 应该有 PromptResult 记录
   - ❌ PromptItem 的 `EvalAvgScore` 仍然是 -1（未评分）

### 测试场景 3：仅优化，不打靶

1. **在弹窗中**：
   - ☐ 取消勾选"创建后立即打靶"
2. **点击"开始优化"**
3. **观察日志**：不应该看到"步骤4.5"相关日志

```log
【步骤4/5】创建新版本 PromptItem...
  ✅ 新 PromptItem 创建成功！
【步骤5/5】发布优化响应...
```

4. **验证数据库**：
   - ✅ 应该有新 PromptItem
   - ❌ 没有对应的 PromptResult（未打靶）

---

## 📊 关键代码详解

### 打靶调用

```csharp
var shootResult = await _promptResultService.SenparcGenerateResultAsync(
    newPromptItem,       // 新创建的 PromptItem（DTO 类型）
    userMessage: null,   // 非 Chat 模式，传 null
    chatHistory: null    // 非 Chat 模式，传 null
);
```

**参数说明**：
- `newPromptItem`: 新创建的 `PromptItemDto` 对象
- `userMessage`: 如果是 Chat 模式需要提供用户消息，直接测试模式传 `null`
- `chatHistory`: 聊天历史记录，直接测试模式传 `null`

**返回值**：
- `PromptResultDto`: 包含打靶结果的 DTO，包括 `Id`, `ResultString`, `Score` 等字段

### AI 评分调用

```csharp
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
```

**参数说明**：
- `promptResultId`: 要评分的 PromptResult ID
- `isRefresh`: 是否重新评分（false = 首次评分，true = 重新评分）
- `expectedResultsJson`: 期望结果的 JSON 字符串（例如 `["回答要专业", "逻辑要清晰"]`）

**执行流程**：
1. `RobotScoringAsync`: 使用 AI 对比生成结果和期望结果，给出评分
2. `UpdateEvalScoreAsync`: 聚合该 PromptItem 下所有 PromptResult 的评分，更新 `EvalAvgScore` 和 `EvalMaxScore`

---

## ⚠️ 重要注意事项

### 1. 期望结果是必需的

**如果要使用 AI 评分**，必须事先配置期望结果：
- 在 PromptItem 编辑页面的"AI评分标准"区域添加期望结果
- 系统会将这些期望结果保存到 `ExpectedResultsJson` 字段
- 新优化的 PromptItem 会继承原 PromptItem 的期望结果

**如果没有期望结果**：
- 系统会记录警告日志：`⚠️  未设置期望结果，跳过 AI 评分`
- 打靶会正常执行，但不会进行 AI 评分
- 不会报错，不影响优化流程

### 2. 异常处理

打靶或 AI 评分失败**不会影响优化结果的返回**：
```csharp
try
{
    // 打靶和 AI 评分逻辑
}
catch (Exception shootEx)
{
    _logger.LogError(shootEx, "❌ 自动打靶或 AI 评分失败（不影响优化结果）");
    // 继续执行，返回优化结果
}
```

**设计理念**：
- 优化的核心价值是生成新的 PromptItem
- 打靶和评分是"附加服务"，失败不应导致整个优化流程失败
- 用户仍然可以手动打靶和评分

### 3. 必须重启应用

修改了以下文件，必须重启应用：
- `PromptOptimizationEvents.cs`（DTO 定义）
- `PromptOptimizationRequestHandler.cs`（核心逻辑）
- `prompt.js`（前端逻辑）
- `Prompt.cshtml`（UI 界面）

### 4. 性能考虑

**自动打靶和 AI 评分会增加执行时间**：
- 优化本身：~5-10 秒（AI 调用）
- 打靶：~3-5 秒（AI 生成结果）
- AI 评分：~5-10 秒（AI 评估）
- **总计**：~15-25 秒

**建议**：
- 如果需要快速迭代多个优化版本，可以先取消勾选自动打靶
- 完成所有优化后，再统一手动打靶和评分

---

## 💡 技术亮点

### 1. 智能模型选择

新 PromptItem 不是简单复制原 ModelId，而是根据历史评分数据智能选择：
```
原 ModelId: 1012
历史评分: Model1011=0.89, Model1012=0.82
选择: 1011（评分更高）
```

### 2. 换行符正确处理

AI 返回的 `\n` 转义字符会被正确转换为实际换行：
```
Before: "你是专业助理...\n1. 生成内容...\n2. 保持一致..."
After:  "你是专业助理...
         1. 生成内容...
         2. 保持一致..."
```

### 3. 完整的自动化链路

```
Prompt 优化 → ModelId 智能选择 → 新 PromptItem 创建 → 自动打靶 → AI 评分 → 分数更新
```

所有步骤都有详细日志，易于调试和监控。

### 4. 期望结果继承

新优化的 PromptItem 自动继承原 PromptItem 的期望结果：
- 保持评分标准一致
- 便于对比优化前后的效果
- 用户无需重新配置

### 5. UI 交互优化

- 第二个选项依赖第一个：`:disabled="!autoShootAfterOptimize"`
- 提供 Tooltip 说明每个选项的作用
- 默认值合理（立即打靶=true，AI评分=false）

---

## 🎉 总结

这次增强为 PromptCatalyzer 添加了**完整的自动化测试流程**：

| 功能 | 说明 | 默认值 |
|------|------|--------|
| **智能 ModelId 选择** | 根据历史评分自动选择最佳模型 | 自动 |
| **换行符处理** | AI 生成的多行文本正确显示 | 自动 |
| **创建后立即打靶** | 优化完成后自动测试新 Prompt | ✅ 默认开启 |
| **打靶后 AI 评分** | 测试完成后自动评分反馈 | ☐ 默认关闭 |

**用户体验提升**：
- ✅ 从优化到评分一键完成
- ✅ 减少手动操作步骤
- ✅ 立即获得质量反馈
- ✅ 灵活控制自动化级别

**现在请重启应用并测试这些新功能！**
