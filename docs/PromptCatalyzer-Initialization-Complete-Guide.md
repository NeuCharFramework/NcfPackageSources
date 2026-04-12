[中文版](PromptCatalyzer-Initialization-Complete-Guide.cn.md)

# PromptCatalyzer Initialization Integrity Check and Repair Guide

## Current status analysis

**Known facts**:
- ✅ PromptCatalyzer Range has been created
- ❌ PromptItem(target) is empty
- ❌ Previous initialization interrupted by database error

---

## 🔍 Data integrity check

### Check the current status of the database```sql
-- 1. 检查 PromptRange 表
SELECT * FROM [Xncf_PromptRange_PromptRange] WHERE Alias = 'PromptCatalyzer';

-- 2. 检查 PromptItem 表（靶道）
SELECT * FROM [Xncf_PromptRange_PromptItem] 
WHERE RangeId IN (SELECT Id FROM [Xncf_PromptRange_PromptRange] WHERE Alias = 'PromptCatalyzer');

-- 3. 检查 AgentTemplate 表
SELECT * FROM [Xncf_AgentsManager_AgentTemplate] WHERE Name = 'PromptCatalyzer';
```### Expected results

**After normal initialization is complete there should be**:
1. **PromptRange records**: 1, Alias = "PromptCatalyzer"
2. **PromptItem record**: at least 1, associated to the above Range
3. **AgentTemplate records**: 1, Name = "PromptCatalyzer"

---

## 🛠️ Summary of repair content

### Fix 1: EventHandler registration (core issue)
**File**: `tools/NcfSimulatedSite/Senparc.Web/Register.cs`
- Use `AddSenparcEventBus()` to automatically scan and register Handler
- Called after `StartWebEngine()` to ensure all modules are loaded

### Fix 2: PromptItem field initialization
**File**: `src/Extensions/Senparc.Xncf.PromptRange/Domain/Models/DatabaseModel/PromptItem.cs`
- Add `NickName = string.Empty;` in the constructor

### Fix 3: PromptInitRequestHandler enhanced fault tolerance
**File**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptInitRequestHandler.cs`
- Set default values for all string fields on the request object
- Add detailed step log (step 1/4, 2/4, etc.)
- Add try-catch fault tolerance processing
- catch the complete inner exception

### Fix 4: PromptOptimizationService enhancement log
**File**: `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`
- Add detailed step log
- Verify that the returned PromptCode is not empty

---

## 🚀 Complete testing process

### Step 1: Restart the application```bash
# 停止当前运行的应用（Ctrl+C）
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources
dotnet run --project tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj
```### Step 2: Check the startup log (key!)

**The following output must be seen**:```
EventBus 扫描程序集:
  - Senparc.Xncf.PromptRange        ← 包含 PromptInitRequestHandler
  - Senparc.Xncf.AgentsManager       ← 包含 PromptInitResponseHandler
  - Senparc.Areas.Admin
  - ... (其他模块)
EventBus 已注册，共扫描了 XX 个程序集

Senparc NCF EventBus Service is starting with MaxConcurrency=XX, EnableDuplicateDetection=True, MaxEventChainDepth=10, EnableCircularReferenceDetection=True
```**If you don’t see the above output**:
- EventHandler may not be registered correctly
- Please provide complete startup log

### Step 3: Turn on log monitoring

**Run in a new terminal window**:```bash
tail -f tools/NcfSimulatedSite/Senparc.Web/App_Data/SenparcTraceLog/SenparcTrace-20260324.log
```### Step 4: Test initialization

1. **Access page**: `http://localhost:5000` → PromptRange page
2. **Refresh browser**: Cmd+Shift+R (clear JS cache)
3. **Click the "Optimize" button**
4. **Check model list**: 17 models should be displayed, with tooltips for parameter preview
5. **Select the model** and click "Start Initialization"

### Step 5: View detailed logs

**Expected log process**:```
========== EnsureInitializedAsync 开始 ==========
【步骤1/3】检查 PromptCatalyzer Agent 是否已存在...
  Agent 不存在，开始初始化流程...

【步骤2/3】Agent 不存在，开始初始化流程...
  发布 PromptInitRequestEvent: RequestId=xxx, ModelId=2

========== 开始处理 Prompt Init Request ==========
【步骤1/4】检查 PromptRange 'PromptCatalyzer' 是否存在...
  ✅ PromptRange 已存在，ID: XX, Alias: PromptCatalyzer    ← 因为之前已创建

【步骤2/4】确定 AI Model...
  使用用户指定的 Model ID: 2
  ✅ Model 验证通过: NeuChar-gpt-4 (ID: 2)

【步骤3/4】检查 PromptItem 是否存在于 Range XX...
  PromptItem 不存在，开始创建...                           ← 这次应该成功
  Range.Id=XX, Range.RangeName=PromptCatalyzer
  准备创建 PromptItem，Request: {...}
  ✅ PromptItem 创建成功，ID: YY, FullVersion: PromptCatalyzer-T1-A1

【步骤4/4】准备返回 PromptInitResponse...
  ✅ 初始化完成！PromptCode: PromptCatalyzer-T1-A1
========== Prompt Init Request 处理完成 ==========

Prompt Init Response: PromptCatalyzer-T1-A1, RequestId: xxx
  ✅ 收到 PromptInitResponseEvent: Success=True, PromptCode=PromptCatalyzer-T1-A1

【步骤3/3】创建 PromptCatalyzer Agent...
  PromptCode: PromptCatalyzer-T1-A1
  ✅ Agent 创建成功！AgentId: ZZ, PromptCode: PromptCatalyzer-T1-A1
========== EnsureInitializedAsync 完成 ==========
```---

## ❗ Errors that may be encountered

### Error 1: Still failed to save the database

**Log Features**:```
  ❌ 创建 PromptItem 失败！详细错误: An error occurred while saving...
  Inner Exception: [具体的数据库错误]
```**Diagnostic Steps**:
1. View complete inner exception information
2. Check the database table structure: which field in the `PromptItem` table does not allow null
3. Provide specific inner exception message

**Possible reasons**:
- Database table field constraints (NOT NULL, UNIQUE, etc.)
- Foreign key constraints (ModelId, RangeId)
- Field length exceeds limit

### Error 2: PromptCode is empty

**Log Features**:```
Prompt Init Response: , RequestId: xxx
```**Cause**: There is a problem with the generation logic of `item.FullVersion`
**Solution**: Check the `FullVersion` property calculation logic of `PromptItem`

### Error 3: EventHandler not called

**Log Features**:
- Can't see "Start processing Prompt Init Request"
- The request is pending

**Solution**:
1. Check whether the startup log shows that EventBus scanned the module
2. Confirm that `Senparc.Xncf.PromptRange` and `Senparc.Xncf.AgentsManager` are in the scan list

---

## 🔧 If initialization still fails

### Solution A: Clean up existing data and re-initialize```sql
-- 警告：这会删除 PromptCatalyzer 相关的所有数据！
DELETE FROM [Xncf_AgentsManager_AgentTemplate] WHERE Name = 'PromptCatalyzer';
DELETE FROM [Xncf_PromptRange_PromptItem] 
WHERE RangeId IN (SELECT Id FROM [Xncf_PromptRange_PromptRange] WHERE Alias = 'PromptCatalyzer');
DELETE FROM [Xncf_PromptRange_PromptRange] WHERE Alias = 'PromptCatalyzer';
```Then retest the initialization.

### Option B: Manually complete the missing PromptItem

If you don't want to delete an existing Range, you can provide:
1. **Full inner exception log** (will now be recorded in the log)
2. **PromptItem table structure** (which fields are NOT NULL)

I can help you manually construct the correct SQL insert statement.

---

## 📋 Log Checklist

After launching the app, please confirm the following:

- [ ] Startup log shows EventBus scanned `Senparc.Xncf.PromptRange`
- [ ] Startup log shows EventBus scanned `Senparc.Xncf.AgentsManager`
- [ ] The startup log shows `EventBus has been registered and a total of XX assemblies were scanned`
- [ ] After clicking Initialize, the log shows `========== Start processing Prompt Init Request ==========`
- [ ] Log shows that all steps (1/4 to 4/4) were executed successfully
- [ ] The log shows `PromptItem created successfully, FullVersion: xxx`
- [ ] The log shows `Agent created successfully! AgentId: xx`
- [ ] The front end receives a success message

---

## 🎯 Success Criteria

**Signs of successful complete initialization**:

1. **Database records**:
   - `PromptRange` has 1 record
   - `PromptItem` has 1 record (FullVersion format: `PromptCatalyzer-T1-A1`)
   - `AgentTemplate` has 1 record (SystemMessage stores PromptCode)

2. **Front-end display**:
   - The pop-up window displays "Initialization successful! PromptCode: PromptCatalyzer-T1-A1"
   - Automatically refresh page data

3. **Complete log**:
   - All step logs are ✅ marked
   - No ❌ error flag

---

## Next update

Please restart the application:
1. Provide **startup log** (especially the EventBus scanning part)
2. Click Initialize and observe the **complete execution log**
3. If it fails, provide **complete error message** (including inner exception)

Enhanced logging will help us pinpoint the problem!
