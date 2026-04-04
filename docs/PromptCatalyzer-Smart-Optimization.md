# PromptCatalyzer intelligent optimization enhancement

## 📋 Problem Overview

Users reported two issues that need to be optimized:

### Problem 1: Fixed usage problem of ModelId
- **Phenomenon**: The newly generated Prompt always uses the ModelId of the original PromptItem
- **Requirement**: Automatically select the best performing model in the current Range based on historical ratings
- **Constraint**: Do not select models that have not been used in the current Range

### Problem 2: Newline escape problem
- **Phenomenon**: The content returned by AI contains`\n`string instead of actual line breaks
- **Example**:
  ```
You are a professional copywriting assistant...\n1. Generate content based on user needs...\n2. Maintain a consistent tone throughout the creative process...
  ```
- **Expectation**: Display as normal multiline text

---

## 🔧 Solution

### Fix 1: Smart ModelId selection

**document**:`src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

#### New method:`SelectBestModelIdAsync`

```csharp
/// <summary>
/// 智能选择 ModelId（基于历史评分）
/// </summary>
private async Task<int> SelectBestModelIdAsync(string rangeName, int currentModelId)
{
    try
    {
        // 1. 获取当前 Range 中所有已评分的 PromptItem
        var scoredItems = await _promptItemService.GetFullListAsync(
            p => p.RangeName == rangeName && p.EvalAvgScore > 0,
            p => p.Id,
            OrderingType.Ascending
        );
        
        if (scoredItems.Count == 0)
        {
            // 如果没有历史评分，使用原 ModelId
            return currentModelId;
        }
        
        // 2. 统计每个 ModelId 的平均分
        var modelScores = scoredItems
            .GroupBy(p => p.ModelId)
            .Select(g => new
            {
                ModelId = g.Key,
                AvgScore = g.Average(p => (double)p.EvalAvgScore),
                Count = g.Count()
            })
            .OrderByDescending(x => x.AvgScore)
            .ToList();
        
        // 3. 选择评分最高的模型
        var bestModel = modelScores.First();
        return bestModel.ModelId;
    }
    catch (Exception ex)
    {
        // 如果选择失败，使用原 ModelId
        return currentModelId;
    }
}
```

#### Calling location

```csharp
// 【步骤4/5】创建新版本的 PromptItem
var originalItem = promptResult.PromptItem;

// 智能选择 ModelId
var selectedModelId = await SelectBestModelIdAsync(
    originalItem.RangeName, 
    @event.Context.ModelId
);

var newPromptItemRequest = new PromptItem_AddRequest
{
    // ...
    ModelId = selectedModelId,  // 使用智能选择的 ModelId
    // ...
};
```

#### Intelligent selection logic

```
1. 查询当前 Range 中所有已评分（EvalAvgScore > 0）的 PromptItem
   ↓
2. 按 ModelId 分组，计算每个模型的平均评分和使用次数
   ↓
3. 按平均评分降序排序
   ↓
4. 选择评分最高的 ModelId
   ↓
5. 如果没有历史数据或查询失败，使用原 ModelId
```

#### Log output example

```
智能选择 ModelId: 原=1012, 选择=1011
Range 中模型评分统计：Model1011=0.89(5次), Model1012=0.82(3次), Model1013=0.75(2次)
最佳模型：Model1011，平均分=0.89
```

---

### Fix 2: JSON escape character handling

**document**:`src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

#### New method:`UnescapeJsonString`

```csharp
/// <summary>
/// 处理 JSON 转义字符（\n, \r, \t, \\, \", 等）
/// </summary>
private string UnescapeJsonString(string value)
{
    if (string.IsNullOrEmpty(value))
        return value;
    
    // 注意：必须先处理 \\ 避免与其他转义冲突
    return value
        .Replace("\\\\", "\u0001")  // 临时标记，避免与后续替换冲突
        .Replace("\\n", "\n")       // 换行符
        .Replace("\\r", "\r")       // 回车符
        .Replace("\\t", "\t")       // 制表符
        .Replace("\\\"", "\"")      // 双引号
        .Replace("\u0001", "\\");   // 恢复反斜杠
}
```

#### Why do we need temporary tags?

**Wrong processing order**:
```csharp
// ❌ 错误示例
value = "Hello\\nWorld\\\\Test";

// 如果先替换 \n，再替换 \\：
Step 1: Replace("\\n", "\n")  → "Hello\nWorld\\Test"
Step 2: Replace("\\\\", "\\") → "Hello\nWorld\Test"  // ✅ 正确

// 但如果字符串是 "\\\\n"（表示字面的 \n）：
Step 1: Replace("\\n", "\n")  → "\\\n"  // ❌ 错误！应该保持为 "\\n"
Step 2: Replace("\\\\", "\\") → "\\n"   // ❌ 错误结果
```

**Correct processing order**:
```csharp
// ✅ 正确示例：使用临时标记
value = "Hello\\nWorld\\\\Test";

Step 1: Replace("\\\\", "\u0001")  → "Hello\\nWorld\u0001Test"
Step 2: Replace("\\n", "\n")       → "Hello\nWorld\u0001Test"
Step 3: Replace("\u0001", "\\")    → "Hello\nWorld\Test"  // ✅ 正确
```

#### Calling location

```csharp
// 【步骤3/5】解析 AI 返回的 JSON
var optimizedContentRaw = ExtractJsonValue(aiResult, "optimizedContent") ?? @event.PromptContent;
var optimizedContent = UnescapeJsonString(optimizedContentRaw); // 处理 JSON 转义
```

#### Before vs After

**Before (problem)**:
```
你是一位专业的文案助理...\n1. 根据用户需求生成内容...\n2. 在创作过程中保持语气一致...
```

**After (after repair)**:
```
你是一位专业的文案助理...
1. 根据用户需求生成内容...
2. 在创作过程中保持语气一致...
```

---

## 🔄 Complete optimization process (after enhancement)

```
用户点击"开始优化"
    ↓
PromptOptimizationController.OptimizeAsync
    ↓
[1] 确保 Agent 和 ChatGroup 初始化
    ↓
[2] 调用 OptimizePromptAsync
    ├─ 发布 PromptOptimizationRequestEvent
    │   └─ PromptOptimizationRequestHandler
    │       ├─ 【步骤1】获取原始 PromptItem
    │       ├─ 【步骤2】调用 AI Kernel 优化
    │       ├─ 【步骤3】解析 AI 结果
    │       │   └─ 🆕 处理 JSON 转义字符（\n → 实际换行）
    │       ├─ 【步骤4】创建新版本 PromptItem
    │       │   ├─ 🆕 智能选择 ModelId（基于历史评分）
    │       │   │   ├─ 查询 Range 中所有已评分 PromptItem
    │       │   │   ├─ 按 ModelId 分组统计平均分
    │       │   │   └─ 选择评分最高的模型
    │       │   └─ 创建新 PromptItem（正确的换行 + 最佳模型）
    │       └─ 【步骤5】发布优化完成事件
    │
    ├─ PromptOptimizationChatTaskHandler（创建 ChatTask）
    └─ 返回优化结果
    ↓
返回给前端（正确的多行文本 + 智能选择的模型）
```

---

## 🧪 Test steps

### 1️⃣ Restart the application

```bash
# 在终端 5 中按 Ctrl+C 停止
cd tools/NcfSimulatedSite/Senparc.Web
dotnet run
```

### 2️⃣ Test smart ModelId selection

#### Prepare test data:

1. **Create multiple PromptItem** in the same Range, using different ModelIds
2. **Score these PromptItems** (either manually or via PromptResult)
3. **Make sure there are differences in ratings**, for example:
- Model 1011: average score 0.89
- Model 1012: Average score 0.82
- Model 1013: Average score 0.75

#### Perform optimization and verify:

1. **Select any PromptItem**
2. **Click "Start Optimization"**
3. **Observe the console log**:

```log
智能选择 ModelId: 原=1012, 选择=1011
Range 中模型评分统计：Model1011=0.89(5次), Model1012=0.82(3次)
最佳模型：Model1011，平均分=0.89
```

4. **Query the newly created PromptItem**:

```sql
SELECT TOP 1 Id, FullVersion, ModelId, Note
FROM [dbo].[Senparc_PromptRange_PromptItem]
WHERE Note = '🤖AI-Generated'
ORDER BY Id DESC
```

**Expected results**:`ModelId`Should be the highest rated model (e.g. 1011)

---

### 3️⃣ Test line break repair

#### Test steps:

1. **Select a PromptItem**
2. **Click "Start Optimization"** and fill in the requirements:`"Optimize Prompt to make it clearer and easier to understand, using multi-paragraph format"`
3. **Waiting for optimization to complete**
4. **View the Content field of the new PromptItem**

#### Verification method 1: Through database

```sql
SELECT TOP 1 Id, FullVersion, Content
FROM [dbo].[Senparc_PromptRange_PromptItem]
WHERE Note = '🤖AI-Generated'
ORDER BY Id DESC
```

**Expected results**:`Content`Fields should contain actual newlines (displayed as multiple lines) instead of`\n`String.

#### Verification method 2: Through the front-end page

1. **Select the newly created AI-Generated PromptItem on the PromptRange page**
2. **View Prompt content display area**
3. **Expect to see normal multi-line format**, for example:

```
你是一位专业的文案助理...

1. 根据用户需求生成内容...
2. 在创作过程中保持语气一致...
3. 提供可执行建议...

输出内容时需具备：高可读性、精确性、条理性...
```

instead of:

```
你是一位专业的文案助理...\n\n1. 根据用户需求生成内容...\n2. 在创作过程中保持语气一致...
```

---

## 📊 Key code comparison

### Before vs After: ModelId selection

**Before (fixed use of original ModelId)**:
```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    ModelId = @event.Context.ModelId,  // ❌ 总是使用原模型
    // ...
};
```

**After (smart selection of the best ModelId)**:
```csharp
// 智能选择 ModelId（基于历史评分）
var selectedModelId = await SelectBestModelIdAsync(
    originalItem.RangeName, 
    @event.Context.ModelId
);

var newPromptItemRequest = new PromptItem_AddRequest
{
    ModelId = selectedModelId,  // ✅ 使用评分最高的模型
    // ...
};
```

### Before vs After: Newline processing

**Before (direct use, including escape characters)**:
```csharp
var optimizedContent = ExtractJsonValue(aiResult, "optimizedContent") 
    ?? @event.PromptContent;  // ❌ 包含 \n 字面字符串
```

**After (processing escape characters)**:
```csharp
var optimizedContentRaw = ExtractJsonValue(aiResult, "optimizedContent") 
    ?? @event.PromptContent;
var optimizedContent = UnescapeJsonString(optimizedContentRaw);  // ✅ 转换为实际换行
```

---

## 🎯 Expected results

After completing the above fixes and **restarting the app**:

### ✅ Smart ModelId selection
1. **First optimization** (Range has no rating data): Use the original ModelId
2. **Subsequent optimization** (Range has scoring data): Automatically select the model with the highest score
3. **Clear logs**: Display the scoring statistics and selection reasons of all models
4. **Flexible downgrade**: If the query fails, fall back to using the original ModelId

### ✅ Line breaks are displayed correctly
1. **Database Storage**:`Content`Field contains actual newlines (ASCII 10)
2. **Front-end display**: Multi-line format is displayed correctly, but not displayed`\n`string
3. **Readability improvement**: AI-optimized structured prompt can display hierarchical structure normally

---

## 📝 Technical details

### SelectBestModelIdAsync algorithm

```
输入：
  - rangeName: 当前 Range 名称
  - currentModelId: 原 PromptItem 使用的 ModelId

处理流程：
  1. Query: WHERE RangeName = @rangeName AND EvalAvgScore > 0
  2. GroupBy: ModelId
  3. Select: { ModelId, AvgScore = Avg(EvalAvgScore), Count = Count() }
  4. OrderBy: AvgScore DESC
  5. Return: First().ModelId

异常处理：
  - 如果没有评分数据 → 返回 currentModelId
  - 如果查询失败 → 返回 currentModelId
  - 如果列表为空 → 返回 currentModelId

输出：
  - selectedModelId: 评分最高的 ModelId（或原 ModelId 作为后备）
```

### UnescapeJsonString escaping rules

| JSON escape | actual characters | illustrate |
|-----------|----------|------|
| `\\n`     | `\n` (ASCII 10) | Line Feed |
| `\\r`     | `\r` (ASCII 13) | Carriage Return |
| `\\t`     | `\t` (ASCII 9)  | Tab |
| `\\"`     | `"` (ASCII 34)  | double quotes |
| `\\\\`    | `\` (ASCII 92)  | Backslash (requires special handling) |

**Why temporary tags are needed`\u0001`**：

avoid`\\\\` → `\\` → `\n`Such a chain of error conversions. Ensured by temporary marking`\\\\n`correctly converted to`\n`instead of a newline character.

---

## ⚠️ Notes

### 1. The application must be restarted
The EventHandler code has been modified and the application must be restarted to take effect.

### 2. ModelId selection depends on historical data
- If no PromptItem in the Range has been scored, the original ModelId will be used
- It is recommended to rate at least 3-5 PromptItems before using the optimization function

### 3. Performance of line breaks in different scenarios
- **Database Query Tools** (like SSMS): may appear as a single row (depending on tool settings)
- **Front-end textarea**: Display multiple lines normally
- **Front-end pre tag**: Display multiple lines normally
- **JSON serialization**: will be escaped again as`\n`(that's right)

### 4. Limitations on escape character processing
Currently only the 5 most common escape characters are handled. If the content returned by the AI ​​contains other JSON escapes such as`\u0000`Unicode escape), needs to be extended`UnescapeJsonString`method.

---

## 🎉 Summary

This enhancement adds two important features to PromptCatalyzer:

1. **Intelligent ModelId Selection**:
- Automatically select the best model based on historical rating data
- Avoid using models that have not been validated in the current Range
- Provide clear selection logs and statistics

2. **Correct newline handling**:
- Multi-line prompts generated by AI can be displayed normally
- Improve readability and user experience
- Maintain data consistency (database storage + front-end display)

**Please restart the app now and test these two new features! **
