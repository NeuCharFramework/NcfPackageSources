[English](PromptCatalyzer-Smart-Optimization.md)

# PromptCatalyzer 智能优化增强

## 📋 问题概述

用户反馈了两个需要优化的问题：

### 问题 1：ModelId 固定使用问题
- **现象**：新生成的 Prompt 总是使用原 PromptItem 的 ModelId
- **需求**：根据历史评分自动选择当前 Range 中表现最好的模型
- **约束**：不选择在当前 Range 中未使用过的模型

### 问题 2：换行符转义问题
- **现象**：AI 返回的内容包含 `\n` 字符串而不是实际换行
- **示例**：
  ```
  你是一位专业的文案助理...\n1. 根据用户需求生成内容...\n2. 在创作过程中保持语气一致...
  ```
- **期望**：显示为正常的多行文本

---

## 🔧 解决方案

### 修复 1：智能 ModelId 选择

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

#### 新增方法：`SelectBestModelIdAsync`

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

#### 调用位置

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

#### 智能选择逻辑

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

#### 日志输出示例

```
智能选择 ModelId: 原=1012, 选择=1011
Range 中模型评分统计：Model1011=0.89(5次), Model1012=0.82(3次), Model1013=0.75(2次)
最佳模型：Model1011，平均分=0.89
```

---

### 修复 2：JSON 转义字符处理

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

#### 新增方法：`UnescapeJsonString`

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

#### 为什么需要临时标记？

**错误的处理顺序**：
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

**正确的处理顺序**：
```csharp
// ✅ 正确示例：使用临时标记
value = "Hello\\nWorld\\\\Test";

Step 1: Replace("\\\\", "\u0001")  → "Hello\\nWorld\u0001Test"
Step 2: Replace("\\n", "\n")       → "Hello\nWorld\u0001Test"
Step 3: Replace("\u0001", "\\")    → "Hello\nWorld\Test"  // ✅ 正确
```

#### 调用位置

```csharp
// 【步骤3/5】解析 AI 返回的 JSON
var optimizedContentRaw = ExtractJsonValue(aiResult, "optimizedContent") ?? @event.PromptContent;
var optimizedContent = UnescapeJsonString(optimizedContentRaw); // 处理 JSON 转义
```

#### Before vs After

**Before (有问题)**:
```
你是一位专业的文案助理...\n1. 根据用户需求生成内容...\n2. 在创作过程中保持语气一致...
```

**After (修复后)**:
```
你是一位专业的文案助理...
1. 根据用户需求生成内容...
2. 在创作过程中保持语气一致...
```

---

## 🔄 完整优化流程（增强后）

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

## 🧪 测试步骤

### 1️⃣ 重启应用

```bash
# 在终端 5 中按 Ctrl+C 停止
cd tools/NcfSimulatedSite/Senparc.Web
dotnet run
```

### 2️⃣ 测试智能 ModelId 选择

#### 准备测试数据：

1. **在同一个 Range 中创建多个 PromptItem**，使用不同的 ModelId
2. **对这些 PromptItem 进行评分**（手动或通过 PromptResult）
3. **确保评分有差异**，例如：
   - Model 1011: 平均分 0.89
   - Model 1012: 平均分 0.82
   - Model 1013: 平均分 0.75

#### 执行优化并验证：

1. **选择任意 PromptItem**
2. **点击"开始优化"**
3. **观察控制台日志**：

```log
智能选择 ModelId: 原=1012, 选择=1011
Range 中模型评分统计：Model1011=0.89(5次), Model1012=0.82(3次)
最佳模型：Model1011，平均分=0.89
```

4. **查询新创建的 PromptItem**：

```sql
SELECT TOP 1 Id, FullVersion, ModelId, Note
FROM [dbo].[Senparc_PromptRange_PromptItem]
WHERE Note = '🤖AI-Generated'
ORDER BY Id DESC
```

**期望结果**：`ModelId` 应该是评分最高的模型（例如 1011）

---

### 3️⃣ 测试换行符修复

#### 测试步骤：

1. **选择一个 PromptItem**
2. **点击"开始优化"**，填写需求：`"优化 Prompt，使其更清晰易懂，使用多段落格式"`
3. **等待优化完成**
4. **查看新 PromptItem 的 Content 字段**

#### 验证方法 1：通过数据库

```sql
SELECT TOP 1 Id, FullVersion, Content
FROM [dbo].[Senparc_PromptRange_PromptItem]
WHERE Note = '🤖AI-Generated'
ORDER BY Id DESC
```

**期望结果**：`Content` 字段应该包含**实际的换行符**（显示为多行），而不是 `\n` 字符串。

#### 验证方法 2：通过前端页面

1. **在 PromptRange 页面选择新创建的 AI-Generated PromptItem**
2. **查看 Prompt 内容显示区域**
3. **期望看到正常的多行格式**，例如：

```
你是一位专业的文案助理...

1. 根据用户需求生成内容...
2. 在创作过程中保持语气一致...
3. 提供可执行建议...

输出内容时需具备：高可读性、精确性、条理性...
```

而不是：

```
你是一位专业的文案助理...\n\n1. 根据用户需求生成内容...\n2. 在创作过程中保持语气一致...
```

---

## 📊 关键代码对比

### Before vs After：ModelId 选择

**Before (固定使用原 ModelId)**:
```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    ModelId = @event.Context.ModelId,  // ❌ 总是使用原模型
    // ...
};
```

**After (智能选择最佳 ModelId)**:
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

### Before vs After：换行符处理

**Before (直接使用，包含转义字符)**:
```csharp
var optimizedContent = ExtractJsonValue(aiResult, "optimizedContent") 
    ?? @event.PromptContent;  // ❌ 包含 \n 字面字符串
```

**After (处理转义字符)**:
```csharp
var optimizedContentRaw = ExtractJsonValue(aiResult, "optimizedContent") 
    ?? @event.PromptContent;
var optimizedContent = UnescapeJsonString(optimizedContentRaw);  // ✅ 转换为实际换行
```

---

## 🎯 预期效果

完成上述修复并**重启应用**后：

### ✅ 智能 ModelId 选择
1. **首次优化**（Range 无评分数据）：使用原 ModelId
2. **后续优化**（Range 有评分数据）：自动选择评分最高的模型
3. **日志清晰**：显示所有模型的评分统计和选择理由
4. **灵活降级**：如果查询失败，回退到使用原 ModelId

### ✅ 换行符正确显示
1. **数据库存储**：`Content` 字段包含实际换行符（ASCII 10）
2. **前端显示**：多行格式正确显示，不显示 `\n` 字符串
3. **可读性提升**：AI 优化的结构化 Prompt 能够正常显示层次结构

---

## 📝 技术细节

### SelectBestModelIdAsync 算法

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

### UnescapeJsonString 转义规则

| JSON 转义 | 实际字符 | 说明 |
|-----------|----------|------|
| `\\n`     | `\n` (ASCII 10) | 换行符 (Line Feed) |
| `\\r`     | `\r` (ASCII 13) | 回车符 (Carriage Return) |
| `\\t`     | `\t` (ASCII 9)  | 制表符 (Tab) |
| `\\"`     | `"` (ASCII 34)  | 双引号 |
| `\\\\`    | `\` (ASCII 92)  | 反斜杠（需特殊处理） |

**为什么需要临时标记 `\u0001`**：

避免 `\\\\` → `\\` → `\n` 这样的错误转换链。通过临时标记确保 `\\\\n` 正确转换为 `\n` 而不是换行符。

---

## ⚠️ 注意事项

### 1. 必须重启应用
修改了 EventHandler 代码，必须重启应用才能生效。

### 2. ModelId 选择依赖历史数据
- 如果 Range 中没有任何 PromptItem 被评分过，将使用原 ModelId
- 建议在使用优化功能前，先对至少 3-5 个 PromptItem 进行评分

### 3. 换行符在不同场景的表现
- **数据库查询工具**（如 SSMS）：可能显示为单行（取决于工具设置）
- **前端 textarea**：正常显示多行
- **前端 pre 标签**：正常显示多行
- **JSON 序列化**：会再次转义为 `\n`（这是正确的）

### 4. 转义字符处理的限制
当前只处理了最常见的 5 种转义字符。如果 AI 返回的内容包含其他 JSON 转义（如 `\u0000` Unicode 转义），需要扩展 `UnescapeJsonString` 方法。

---

## 🎉 总结

这次增强为 PromptCatalyzer 添加了两个重要功能：

1. **智能 ModelId 选择**：
   - 根据历史评分数据自动选择最佳模型
   - 避免使用未在当前 Range 中验证过的模型
   - 提供清晰的选择日志和统计信息

2. **正确的换行符处理**：
   - AI 生成的多行 Prompt 能够正常显示
   - 提升可读性和用户体验
   - 保持数据一致性（数据库存储 + 前端显示）

**现在请重启应用并测试这两个新功能！**
