[中文版](EventBus-Handler-Registration-Fix.cn.md)

# EventBus Handler registration problem fixed

## Problem description

### Problem 1: The initialization request is always Pending
- **Phenomena**: After clicking the "Start Initialization" button, the request remains in the pending state until it times out.
- **Backend log**: Shows that 17 AI Models of Chat type were found.
- **Root cause**: `PromptInitResponseHandler` is not registered to the DI container, causing EventBus to be unable to call it to complete the initialization request

### Problem 2: The front end cannot correctly parse the back end response
- **Phenomenon**: The front end prompts "No available AI Model found", but the back end has returned 17 models
- **Root cause**: The front-end code directly accesses `response.data.models`, but NCF's `AppResponseBase` return format is nested `{ success: true, data: { models: [...] } }`

### Problem 3: UI parameter preview lacks description
- **Phenomenon**: The default parameter preview only displays numbers, and users do not know the meaning of these parameters.
- **Requirement**: Add tooltips and description text

---

## Repair plan

### Fix 1: Register EventBus and Handler

**Modify file**: `tools/NcfSimulatedSite/Senparc.Web/Register.cs`

**Problem Analysis**:
The previous code only registered the EventBus itself and the HostedService, but did not call the `AddSenparcEventBus()` method to scan and register the EventHandler.

**Fixed content**:
```csharp
public static void AddNcf(this WebApplicationBuilder builder)
{
    StartTime = SystemTime.Now.DateTime;

    //激活 Xncf 扩展引擎（必须）
    var logMsg = builder.StartWebEngine(new[] { "Senparc.Areas.Admin"});
    
    // 注册 EventBus 并自动扫描所有模块的 EventHandler
    // 必须在 StartWebEngine 之后，确保所有模块程序集已加载
    var assembliesToScan = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && 
                   (a.FullName.Contains("Senparc.Xncf.") || 
                    a.FullName.Contains("Senparc.Areas.")))
        .ToArray();
    
    builder.Services.AddSenparcEventBus(
        options =>
        {
            options.MaxConcurrency = Math.Max(4, Environment.ProcessorCount * 2);
            options.EnableDuplicateDetection = true;
            options.RetryOnFailure = true;
            options.MaxRetryAttempts = 3;
            options.MaxEventChainDepth = 10;
            options.EnableCircularReferenceDetection = true;
        },
        assembliesToScan);
    
    Console.WriteLine($"EventBus 已注册，共扫描了 {assembliesToScan.Length} 个程序集");
}
```
**Key Improvements**:
1. Use the `AddSenparcEventBus()` method instead of manual registration
2. Automatically scan all `Senparc.Xncf.*` and `Senparc.Areas.*` assemblies
3. Configure EventBus options (concurrency, loop detection, retry, etc.)
4. Called **after** `StartWebEngine` to ensure all module assemblies are loaded

**Automatically registered Handler**:
- `PromptInitRequestHandler` (PromptRange module)
- `PromptInitResponseHandler` (AgentsManager module)
- `PromptOptimizationRequestHandler` (PromptRange module)
- `PromptOptimizationResponseHandler` (AgentsManager module)

---

### Fix 2: Correct front-end response data access path

**Modify file**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

**NCF AppResponseBase return format**:
```json
{
  "success": true,
  "data": {
    "models": [...],
    "recommendedModelId": 1
  },
  "errorMessage": null
}
```
**Modification 1: `loadAvailableModels` method**
```javascript
// 修改前：
this.availableModelsForInit = response.data.models || [];

// 修改后：
if (response.data && response.data.success) {
    this.availableModelsForInit = response.data.data.models || [];
    // ...
}
```
**Modification 2: `checkPromptCatalyzerStatus` method**
```javascript
// 修改前：
return response.data.isInitialized;

// 修改后：
if (response.data && response.data.success && response.data.data) {
    return response.data.data.isInitialized;
}
```
**Modification 3: `executeInitialization` method**
```javascript
// 修改前：
if (response.data.success) {
    this.$message({
        message: `✅ 初始化成功！PromptCode: ${response.data.promptCode}`,
        // ...
    });
}

// 修改后：
if (response.data && response.data.success) {
    const initData = response.data.data || {};
    this.$message({
        message: `✅ 初始化成功！PromptCode: ${initData.promptCode || '已创建'}`,
        // ...
    });
}
```
**Improvement points**:
- Added `console.log` to output complete response for easy debugging
- Correct access to nested `data.data` structures
- Enhanced error handling logic

---

### Fix 3: Improved UI parameter preview

**Modify file**: `src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml`

**Improvements**:
```html
<el-descriptions-item label-class-name="desc-label">
    <template slot="label">
        <el-tooltip content="控制输出的随机性：0.0-1.0，越低越精确，越高越创意" placement="top">
            <span>Temperature <i class="el-icon-question"></i></span>
        </el-tooltip>
    </template>
    <el-tag size="mini" type="success">0.7</el-tag>
    <span style="margin-left: 8px; color: #909399; font-size: 12px;">(平衡模式)</span>
</el-descriptions-item>
```
**Added** for each parameter:
1. **Tooltip**: Display detailed instructions when the mouse is hovered
2. **Question mark icon**: Prompts the user to view more information
3. **Explanation text**: Visually display the purpose of parameters (balanced mode, high quality, standard length, etc.)

**Parameter Description**:
- **Temperature (0.7)** - Balanced mode: controls the randomness of the output, 0.0-1.0
- **TopP (0.9)** - High quality: kernel sampling parameters, control vocabulary selection range
- **MaxTokens (4000)** - standard length: maximum number of tokens generated
- **FrequencyPenalty (0)** - No penalty: -2.0 to 2.0, reduce repeated words

---

## Review of how EventBus works

### Initialization process:
```
用户点击"开始初始化"
    ↓
前端调用 /api/.../Initialize (POST)
    ↓
PromptCatalyzerInitController.Initialize()
    ↓
PromptOptimizationService.EnsureInitializedAsync()
    ↓
发布 PromptInitRequestEvent (EventBus)
    ↓
PromptInitRequestHandler.Handle() (PromptRange 模块)
    ↓
创建 PromptRange、PromptItem
    ↓
发布 PromptInitResponseEvent (EventBus)
    ↓
PromptInitResponseHandler.Handle() (AgentsManager 模块) ← 这里之前没有被注册！
    ↓
调用 PromptOptimizationService.CompleteInitRequest()
    ↓
完成 TaskCompletionSource，返回结果
    ↓
Controller 返回给前端
```
### Why Pending?
1. Controller uses `Task.WhenAny(tcs.Task, timeoutTask)` on lines 86-87 to wait for the response
2. The response requires `PromptInitResponseHandler` to call `CompleteInitRequest()` to complete
3. If `PromptInitResponseHandler` is not registered, `PromptInitResponseEvent` will not be processed
4. `tcs.Task` never completes, and the request remains pending until it times out in 2 minutes.

---

## Test steps

### 1. Restart the application
```bash
# 停止当前运行的应用（如果有）
# 然后启动应用
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources
dotnet run --project tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj
```
### 2. Check the startup log
You should see:
```
EventBus 扫描程序集:
  - Senparc.Xncf.PromptRange
  - Senparc.Xncf.AgentsManager
  - Senparc.Areas.Admin
  - ... (其他模块)
EventBus 已注册，共扫描了 XX 个程序集
```
### 3. Test initialization function
1. Visit `http://localhost:5000`
2. Enter the PromptRange page
3. Click the "Optimize" button
4. You should see a pop-up window showing 17 available models.
5. The parameter preview area should display tooltips and description text (mouseover the parameter name)
6. Select a model and click "Start Initialization"
7. **Expected results**: Initialization should be completed within a few seconds (no longer pending)

### 4. View logs
```bash
tail -f tools/NcfSimulatedSite/Senparc.Web/App_Data/SenparcTraceLog/SenparcTrace-20260324.log
```
**Logs you should be able to see**:
```
Initializing PromptCatalyzer with ModelId: 1
Publishing PromptInitRequestEvent with RequestId: xxx
Received Prompt Init Request: RequestId=xxx, ModelId=1
Creating default PromptItem with Model ID: 1...
Prompt Init Response: [PromptCode], RequestId: xxx
Received PromptInitResponse with PromptCode: [PromptCode]
Successfully created PromptCatalyzer Agent with ID: xxx
```
---

## Technical points

### EventBus Handler registration mechanism
1. **Automatic Scanning**: `AddSenparcEventBus()` uses reflection to scan the specified assembly
2. **Interface matching**: Find classes that implement `IIntegrationEventHandler<T>`
3. **DI Registration**: Register Handler as Scoped service
4. **Dynamic call**: HostedService obtains the Handler instance through `GetServices(handlerType)`

### Why register after StartWebEngine?
- `StartWebEngine` will trigger the scanning and loading of the assembly
- Ensure that all Xncf module assemblies are in the AppDomain
- If registered before, modules loaded later may not be scanned.

### Front-end response format standardization
All NCF API return formats follow the `AppResponseBase<T>` structure:
```typescript
{
  success: boolean,
  data: T,                  // 实际数据在这里
  errorMessage?: string
}
```
The front-end must use `response.data.data.xxx` (two-layer data) when accessing data.

---

## Next step

1. **Restart the application** and test the initialization function
2. **Check the console log**: Confirm that EventBus scanned all modules
3. **Test optimization function**: After successful initialization, test the complete optimization process
4. **Restore authority verification**: After the functional test passes, uncomment all `[ApiAuthorize("AdminOnly")]` attributes

---

## Related documents

- `tools/NcfSimulatedSite/Senparc.Web/Register.cs` - EventBus registration configuration
- `src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptInitResponseHandler.cs` - Initialize response handler
- `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptInitRequestHandler.cs` - Initialize request handler
- `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js` - Front-end API calling logic
- `src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml` - UI parameter preview

---

## Repair date
2026-03-24
