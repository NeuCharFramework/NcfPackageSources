[English](PromptCatalyzer-Critical-Fixes.md)

# PromptCatalyzer 关键修复文档

## 📋 问题概述

根据控制台日志分析，发现了两个关键问题：

### 问题 1：ChatGroup 未创建
```
warn: PromptOptimizationChatTaskHandler[0]
      ⚠️  PromptCatalyzer ChatGroup 未找到，跳过 ChatTask 创建
```

### 问题 2：优化失败 - 版本号生成错误
```
fail: PromptOptimizationRequestHandler[0]
      ❌ Prompt 优化失败
      Senparc.Ncf.Core.Exceptions.NcfExceptionBase: 版号生成错误，请重新打靶
         at PromptItemService.AddPromptItemAsync
```

```
Success: False, NewPromptCode=(null), Score=0
```

---

## 🔧 修复方案

### 修复 1：Controller 添加初始化调用

**文件**: `src/Extensions/Senparc.Xncf.AgentsManager/OHS/Remote/Controllers/PromptOptimizationController.cs`

**问题根因**:
- `OptimizeAsync` 方法直接调用 `OptimizePromptAsync`，没有先确保 Agent 和 ChatGroup 已初始化
- 导致每次优化时都不会创建 ChatGroup

**修复内容**:
```csharp
[HttpPost("OptimizeAsync")]
public async Task<IActionResult> OptimizeAsync([FromBody] PromptOptimizationRequestDto request)
{
    // ... 参数验证 ...
    
    // 🔥 关键修复：确保 Agent 和 ChatGroup 已初始化
    _logger.LogInformation("  开始确保初始化状态...");
    await _promptOptimizationService.EnsureInitializedAsync();
    _logger.LogInformation("  ✅ 初始化状态确认完成");
    
    // 调用优化服务
    var result = await _promptOptimizationService.OptimizePromptAsync(...);
    // ...
}
```

**修复效果**:
- 每次优化前都会检查并创建 ChatGroup（如果不存在）
- `PromptOptimizationChatTaskHandler` 将能够找到 ChatGroup 并创建 ChatTask

---

### 修复 2：设置基础 PromptItem ID

**文件**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

**问题根因**:
版本号生成逻辑依赖于 `request.Id` 来确定如何计算新版本号：
```csharp
// PromptItemService.AddPromptItemAsync
if (request.Id == null)
{
    // 创建全新 PromptItem，版本号从头开始
    toSavePromptItem = new PromptItem(
        rangeName: promptRange.RangeName,
        tactic: "1",
        aiming: 1,
        ...
    );
}
else if (request.IsNewAiming)  // ⬅️ 需要 Id 才能进入这个分支
{
    // 基于现有 PromptItem 创建新 Aiming 版本
    var basePrompt = await this.GetAsync(request.Id.Value);
    List<PromptItem> fullList = await base.GetFullListAsync(p =>
        p.FullVersion.StartsWith($"{basePrompt.RangeName}-T{basePrompt.Tactic}-A")
    );
    var maxAiming = fullList.Count == 0 ? 0 : fullList.Select(p => p.Aiming).Max();
    toSavePromptItem = new PromptItem(
        rangeName: rangeName,
        tactic: basePrompt.Tactic,
        aiming: maxAiming + 1,  // ⬅️ 正确递增 Aiming
        ...
    );
}
```

**原代码问题**:
```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    // ❌ 缺少 Id，导致系统创建全新 PromptItem 而不是新版本
    RangeId = originalItem.RangeId,
    IsNewAiming = true,
    // ...
};
```

**修复后**:
```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    Id = originalItem.Id,  // ✅ 设置基础 PromptItem ID
    RangeId = originalItem.RangeId,
    IsNewAiming = true,  // 基于当前 Tactic 创建新的 Aiming 版本
    // ...
};
```

**修复效果**:
- 系统将正确进入 `IsNewAiming` 分支
- 自动查找当前 Tactic 下的最大 Aiming 号
- 生成递增的版本号，例如：
  - 原版本: `2025.12.28.3-T3.1-A2`
  - 新版本: `2025.12.28.3-T3.1-A3` （自动递增 Aiming）

---

## 🔄 完整优化流程（修复后）

```
用户点击"开始优化"
    ↓
PromptOptimizationController.OptimizeAsync
    ↓
[1] 验证请求参数
    ↓
[2] 🆕 调用 EnsureInitializedAsync()
    ├─ 检查 Agent 存在（已存在，跳过创建）
    └─ 检查 ChatGroup
        ├─ 不存在 → 创建 "PromptCatalyzer-OptimizationGroup"
        └─ 已存在 → 跳过
    ↓
[3] 调用 OptimizePromptAsync
    ├─ 发布 PromptOptimizationRequestEvent
    │   ├─ PromptOptimizationRequestHandler（优化 Prompt）
    │   │   ├─ 获取原始 PromptItem
    │   │   ├─ 调用 AI Kernel 优化
    │   │   ├─ 解析 AI 结果（content, temperature, topP, 等）
    │   │   └─ 创建新版本 PromptItem（✅ 现在设置了 Id，版本号正确递增）
    │   │
    │   ├─ PromptOptimizationChatTaskHandler（创建 ChatTask）
    │   │   ├─ 查找 ChatGroup（✅ 现在能找到）
    │   │   └─ 创建 ChatTask 记录优化过程
    │   │
    │   └─ 返回 PromptOptimizationResponseEvent
    │
    └─ 等待并返回优化结果
    ↓
返回给前端（Success: true, NewPromptCode: xxx, Score: 0.92）
```

---

## ⚠️ 重要注意事项

### 必须重启应用！
```bash
# 在运行 dotnet run 的终端中按 Ctrl+C 停止
# 然后重新运行
cd tools/NcfSimulatedSite/Senparc.Web
dotnet run
```

**为什么必须重启**:
1. `PromptOptimizationController` 是在应用启动时注册的
2. 新代码不会自动热加载到正在运行的应用中
3. EventBus handlers 也需要重新注册

---

## 🧪 测试步骤

### 1️⃣ 重启应用后首次测试

1. **打开 PromptRange 页面**
2. **选择一个已有的 Prompt**（例如 `2025.12.28.3-T3.1-A2`）
3. **点击"开始优化"按钮**
4. **观察控制台日志**，应该能看到：

```
========== 收到 Prompt 优化请求 ==========
  开始确保初始化状态...
========== EnsureInitializedAsync 开始（细粒度检查）==========
【步骤1/3】检查 PromptCatalyzer Agent 是否已存在...
  ✅ Agent 已存在，ID: 1011
【步骤3/3】检查 ChatGroup 是否已存在...
  ChatGroup 不存在，开始创建...
  ✅ ChatGroup 创建成功！GroupId: xxxx, Name: PromptCatalyzer-OptimizationGroup
========== EnsureInitializedAsync 完成 ==========
  ✅ 初始化状态确认完成
  开始调用 OptimizePromptAsync...
========== PromptOptimizationRequestHandler 开始 ==========
【步骤1/5】获取原始 PromptItem...
【步骤2/5】调用 AI 进行 Prompt 优化...
【步骤3/5】解析 AI 优化结果...
【步骤4/5】创建新版本 PromptItem...
  准备创建新版本：BaseId=8099, RangeName=2025.12.28.3, Tactic=3.1, 期望 Aiming+1
  ✅ 新 PromptItem 创建成功！NewPromptCode: 2025.12.28.3-T3.1-A3, ItemId: xxxx
【步骤5/5】发布优化完成事件...
========== PromptOptimizationChatTaskHandler 开始 ==========
【步骤1/3】查找 PromptCatalyzer ChatGroup...
  ✅ 找到 ChatGroup: 1005, Agent: 1011
【步骤2/3】创建 ChatTask...
  ✅ ChatTask 创建成功！TaskId: xxxx, Name: Prompt优化-2025.12.28.3-T3.1-A2
========== PromptOptimizationTaskCompletionHandler 开始 ==========
【步骤1/2】查找对应的 ChatTask...
  ✅ 找到 ChatTask: xxxx
【步骤2/2】更新 ChatTask 状态为 Finished...
  ✅ ChatTask 状态更新成功！Status: Finished
```

5. **验证数据库**:
```sql
-- 检查 ChatGroup 是否创建
SELECT * FROM [NcfAiDB_Simulate_Thesis2].[dbo].[Senparc_AgentsManager_ChatGroup]
WHERE Name = 'PromptCatalyzer-OptimizationGroup'

-- 检查 ChatTask 是否创建
SELECT * FROM [NcfAiDB_Simulate_Thesis2].[dbo].[Senparc_AgentsManager_ChatTask]
WHERE Name LIKE 'Prompt优化-%'
ORDER BY AddTime DESC

-- 检查新 PromptItem 是否创建（版本号应该递增）
SELECT TOP 5 Id, FullVersion, Note, AddTime
FROM [NcfAiDB_Simulate_Thesis2].[dbo].[Senparc_PromptRange_PromptItem]
WHERE RangeName = '2025.12.28.3'
ORDER BY Id DESC
```

### 2️⃣ 验证 AI 生成标记

新创建的 PromptItem 应该在 `Note` 字段显示 `🤖AI-Generated`。

---

## 📝 修复详细说明

### 修复项 1: Controller 初始化检查
- **位置**: Line 43-46 in `PromptOptimizationController.cs`
- **变更**: 在调用 `OptimizePromptAsync` 之前添加了 `EnsureInitializedAsync()` 调用
- **影响**: 确保每次优化前都会检查并创建 ChatGroup

### 修复项 2: 版本号生成
- **位置**: Line 123 in `PromptOptimizationRequestHandler.cs`
- **变更**: 添加了 `Id = originalItem.Id` 字段
- **影响**: 
  - 系统将基于原 PromptItem 创建新版本（Aiming 递增）
  - 避免版本号冲突错误
  - 正确维护版本演化关系

---

## 🎯 预期结果

完成上述修复并**重启应用**后：

1. ✅ ChatGroup 将在首次优化时自动创建
2. ✅ 每次优化都会创建对应的 ChatTask 记录
3. ✅ 新 PromptItem 版本号正确递增（例如 A2 → A3）
4. ✅ AI 生成的 Prompt 在 Note 字段标记为 `🤖AI-Generated`
5. ✅ 优化成功后返回正确的 `NewPromptCode` 和 `Score`

---

## ⚠️ 下一步操作

**请按照以下步骤操作**：

1. **停止当前运行的应用**（在终端 5 中按 `Ctrl+C`）
2. **重新启动应用**:
   ```bash
   cd tools/NcfSimulatedSite/Senparc.Web
   dotnet run
   ```
3. **等待应用完全启动**（看到 "自检完成" 消息）
4. **刷新浏览器页面**
5. **再次点击"开始优化"按钮**
6. **观察控制台日志**，应该能看到完整的成功流程

---

## 🐛 之前失败的原因总结

### 为什么 ChatGroup 没有创建？
- `PromptOptimizationController` 没有调用 `EnsureInitializedAsync()`
- 之前的测试可能是直接测试 `Initialize` 接口（初始化按钮），而不是"优化"功能
- 优化功能有独立的代码路径，需要单独添加初始化检查

### 为什么版本号生成错误？
- `PromptItem_AddRequest` 没有设置 `Id` 字段
- 系统误以为这是创建全新的 PromptItem（而不是基于现有版本创建新版本）
- 尝试创建 `2025.12.28.3-T1-A1` 这样的版本号，但该版本号可能已存在
- 导致唯一性检查失败，抛出"版号生成错误"

### 正确的版本生成流程：
1. **传入 `Id`** → 获取 `basePrompt`
2. **`IsNewAiming = true`** → 进入 NewAiming 分支
3. **查找同 Tactic 下所有 Aiming** → 获取 `maxAiming`
4. **生成新版本** → `Aiming = maxAiming + 1`
5. **例如**: `2025.12.28.3-T3.1-A2` → `2025.12.28.3-T3.1-A3`

---

## 📊 关键代码对比

### Before (有 Bug)
```csharp
// ❌ Controller 缺少初始化调用
public async Task<IActionResult> OptimizeAsync(...)
{
    // 直接调用优化
    var result = await _promptOptimizationService.OptimizePromptAsync(...);
}

// ❌ Handler 缺少 Id
var newPromptItemRequest = new PromptItem_AddRequest
{
    RangeId = originalItem.RangeId,
    IsNewAiming = true,
    // 没有 Id！
};
```

### After (已修复)
```csharp
// ✅ Controller 添加初始化检查
public async Task<IActionResult> OptimizeAsync(...)
{
    await _promptOptimizationService.EnsureInitializedAsync();
    var result = await _promptOptimizationService.OptimizePromptAsync(...);
}

// ✅ Handler 设置 Id
var newPromptItemRequest = new PromptItem_AddRequest
{
    Id = originalItem.Id,  // ✅ 关键修复
    RangeId = originalItem.RangeId,
    IsNewAiming = true,
};
```

---

## 🎉 结论

这两个修复解决了：
1. **ChatGroup 未创建** → 现在会在优化前自动创建
2. **版本号生成错误** → 现在正确递增 Aiming 版本号
3. **优化失败** → AI 优化的结果现在能正确保存到数据库

**现在请重启应用并重新测试！**
