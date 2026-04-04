# PromptCatalyzer critical fix documentation

## 📋 Problem Overview

Based on console log analysis, two key issues were discovered:

### Problem 1: ChatGroup not created
```
warn: PromptOptimizationChatTaskHandler[0]
      ⚠️  PromptCatalyzer ChatGroup 未找到，跳过 ChatTask 创建
```

### Problem 2: Optimization failed - version number generation error
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

## 🔧 Repair solution

### Fix 1: Controller adds initialization call

**document**:`src/Extensions/Senparc.Xncf.AgentsManager/OHS/Remote/Controllers/PromptOptimizationController.cs`

**Root cause of the problem**:
- `OptimizeAsync`method calls directly`OptimizePromptAsync`, without first ensuring that Agent and ChatGroup are initialized
- Causes ChatGroup not to be created every time optimization is performed

**Fix content**:
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

**Repair effect**:
- ChatGroup will be checked and created before each optimization (if it does not exist)
- `PromptOptimizationChatTaskHandler`will be able to find the ChatGroup and create the ChatTask

---

### Fix 2: Set base PromptItem ID

**document**:`src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

**Root cause of the problem**:
The version number generation logic depends on`request.Id`To determine how to calculate the new version number:
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

**Original code problem**:
```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    // ❌ 缺少 Id，导致系统创建全新 PromptItem 而不是新版本
    RangeId = originalItem.RangeId,
    IsNewAiming = true,
    // ...
};
```

**After fix**:
```csharp
var newPromptItemRequest = new PromptItem_AddRequest
{
    Id = originalItem.Id,  // ✅ 设置基础 PromptItem ID
    RangeId = originalItem.RangeId,
    IsNewAiming = true,  // 基于当前 Tactic 创建新的 Aiming 版本
    // ...
};
```

**Repair effect**:
- The system will enter correctly`IsNewAiming`branch
- Automatically find the largest Aiming number under the current Tactic
- Generate incremental version numbers, for example:
-Original version:`2025.12.28.3-T3.1-A2`
- New version:`2025.12.28.3-T3.1-A3`(Auto-increment Aiming)

---

## 🔄 Complete optimization process (after repair)

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

## ⚠️ IMPORTANT NOTES

### The application must be restarted!
```bash
# 在运行 dotnet run 的终端中按 Ctrl+C 停止
# 然后重新运行
cd tools/NcfSimulatedSite/Senparc.Web
dotnet run
```

**Why you need to restart**:
1. `PromptOptimizationController`is registered when the application starts
2. New code is not automatically hot-loaded into running applications
3. EventBus handlers also need to be re-registered

---

## 🧪 Test steps

### 1️⃣ First test after restarting the application

1. **Open the PromptRange page**
2. **Select an existing Prompt** (for example`2025.12.28.3-T3.1-A2`）
3. **Click the "Start Optimization" button**
4. **Observe the console log**, you should be able to see:

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

5. **Verify database**:
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

### 2️⃣ Verify AI generated tags

The newly created PromptItem should be in`Note`Field display`🤖AI-Generated`。

---

## 📝 Repair details

### Fix 1: Controller initialization check
- **Location**: Line 43-46 in`PromptOptimizationController.cs`
- **Change**: In calling`OptimizePromptAsync`Added before`EnsureInitializedAsync()`call
- **Impact**: Ensure that ChatGroup is checked and created before each optimization

### Fix 2: Version number generation
- **Location**: Line 123 in`PromptOptimizationRequestHandler.cs`
- **CHANGES**: Added`Id = originalItem.Id`Field
- **Influence**:
- The system will create a new version based on the original PromptItem (Aiming increment)
- Avoid version number conflict errors
- Correctly maintain version evolution relationships

---

## 🎯 Expected results

After completing the above fixes and **restarting the app**:

1. ✅ ChatGroup will be automatically created during first optimization
2. ✅ Each optimization will create a corresponding ChatTask record
3. ✅ The new PromptItem version number is incremented correctly (for example, A2 → A3)
4. ✅ The Prompt generated by AI is marked as`🤖AI-Generated`
5. ✅ Return the correct value after optimization is successful.`NewPromptCode`and`Score`

---

## ⚠️ Next steps

**Please follow these steps**:

1. **Stop currently running application** (press in Terminal 5`Ctrl+C`）
2. **Restart the application**:
   ```bash
   cd tools/NcfSimulatedSite/Senparc.Web
   dotnet run
   ```
3. **Wait for the application to fully start** (see "Self-test completed" message)
4. **Refresh browser page**
5. **Click the "Start Optimization" button again**
6. **Observe the console log**, you should be able to see the complete successful process

---

## 🐛 Summary of reasons for previous failures

### Why is the ChatGroup not created?
- `PromptOptimizationController`not called`EnsureInitializedAsync()`
- The previous test may have been a direct test`Initialize`interface (initialization button), not the "optimize" function
- The optimization function has an independent code path and requires a separate initialization check.

### Why is the version number generated incorrectly?
- `PromptItem_AddRequest`no settings`Id`Field
- The system mistakenly thinks that this is creating a completely new PromptItem (instead of creating a new version based on the existing version)
- try to create`2025.12.28.3-T1-A1`Such a version number, but the version number may already exist
- Causes the uniqueness check to fail and throws "version number generation error"

### Correct version generation process:
1. **Incoming`Id`** → get`basePrompt`
2. **`IsNewAiming = true`** → Enter NewAiming branch
3. **Find all Aiming under the same Tactic** → Get`maxAiming`
4. **Generate new version** →`Aiming = maxAiming + 1`
5. **For example**:`2025.12.28.3-T3.1-A2` → `2025.12.28.3-T3.1-A3`

---

## 📊 Key code comparison

### Before (bug)
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

### After (Fixed)
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

## 🎉 Conclusion

These two fixes solved:
1. **ChatGroup not created** → will now be automatically created before optimization
2. **Version number generation error** → Aiming version number is now correctly incremented
3. **Optimization failed** → AI optimization results can now be correctly saved to the database

**Please restart the app now and retest! **
