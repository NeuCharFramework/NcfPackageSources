# EventBus Handler 注册问题修复

## 问题描述

### 问题1：初始化请求一直 Pending
- **现象**：点击"开始初始化"按钮后，请求一直处于 pending 状态，直到超时
- **后端日志**：显示找到了17个 Chat 类型的 AI Model
- **根本原因**：`PromptInitResponseHandler` 没有被注册到 DI 容器，导致 EventBus 无法调用它来完成初始化请求

### 问题2：前端无法正确解析后端响应
- **现象**：前端提示"没有找到可用的 AI Model"，但后端已返回17个模型
- **根本原因**：前端代码直接访问 `response.data.models`，但 NCF 的 `AppResponseBase` 返回格式是嵌套的 `{ success: true, data: { models: [...] } }`

### 问题3：UI 参数预览缺少说明
- **现象**：默认参数预览只显示数字，用户不清楚这些参数的含义
- **需求**：添加工具提示和说明文字

---

## 修复方案

### 修复1：注册 EventBus 和 Handler

**修改文件**：`tools/NcfSimulatedSite/Senparc.Web/Register.cs`

**问题分析**：
之前的代码只注册了 EventBus 本身和 HostedService，但没有调用 `AddSenparcEventBus()` 方法来扫描和注册 EventHandler。

**修复内容**：
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

**关键改进**：
1. 使用 `AddSenparcEventBus()` 方法替代手动注册
2. 自动扫描所有 `Senparc.Xncf.*` 和 `Senparc.Areas.*` 程序集
3. 配置 EventBus 选项（并发度、循环检测、重试等）
4. 在 `StartWebEngine` **之后**调用，确保所有模块程序集已加载

**自动注册的 Handler**：
- `PromptInitRequestHandler` (PromptRange 模块)
- `PromptInitResponseHandler` (AgentsManager 模块)
- `PromptOptimizationRequestHandler` (PromptRange 模块)
- `PromptOptimizationResponseHandler` (AgentsManager 模块)

---

### 修复2：修正前端响应数据访问路径

**修改文件**：`src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

**NCF AppResponseBase 返回格式**：
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

**修改1：`loadAvailableModels` 方法**
```javascript
// 修改前：
this.availableModelsForInit = response.data.models || [];

// 修改后：
if (response.data && response.data.success) {
    this.availableModelsForInit = response.data.data.models || [];
    // ...
}
```

**修改2：`checkPromptCatalyzerStatus` 方法**
```javascript
// 修改前：
return response.data.isInitialized;

// 修改后：
if (response.data && response.data.success && response.data.data) {
    return response.data.data.isInitialized;
}
```

**修改3：`executeInitialization` 方法**
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

**改进点**：
- 添加了 `console.log` 输出完整响应，便于调试
- 正确访问嵌套的 `data.data` 结构
- 增强了错误处理逻辑

---

### 修复3：改进 UI 参数预览

**修改文件**：`src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml`

**改进内容**：
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

**为每个参数添加了**：
1. **工具提示 (Tooltip)**：鼠标悬停时显示详细说明
2. **问号图标**：提示用户可以查看更多信息
3. **说明文字**：直观显示参数的用途（平衡模式、高质量、标准长度等）

**参数说明**：
- **Temperature (0.7)** - 平衡模式：控制输出的随机性，0.0-1.0
- **TopP (0.9)** - 高质量：核采样参数，控制词汇选择范围
- **MaxTokens (4000)** - 标准长度：生成的最大 token 数量
- **FrequencyPenalty (0)** - 不惩罚：-2.0 到 2.0，减少重复词汇

---

## EventBus 工作原理回顾

### 初始化流程：
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

### 为什么会 Pending？
1. Controller 在第86-87行使用 `Task.WhenAny(tcs.Task, timeoutTask)` 等待响应
2. 响应需要 `PromptInitResponseHandler` 调用 `CompleteInitRequest()` 来完成
3. 如果 `PromptInitResponseHandler` 没有被注册，`PromptInitResponseEvent` 不会被处理
4. `tcs.Task` 永远不会完成，请求一直 pending 直到2分钟超时

---

## 测试步骤

### 1. 重启应用
```bash
# 停止当前运行的应用（如果有）
# 然后启动应用
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources
dotnet run --project tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj
```

### 2. 检查启动日志
应该能看到：
```
EventBus 扫描程序集:
  - Senparc.Xncf.PromptRange
  - Senparc.Xncf.AgentsManager
  - Senparc.Areas.Admin
  - ... (其他模块)
EventBus 已注册，共扫描了 XX 个程序集
```

### 3. 测试初始化功能
1. 访问 `http://localhost:5000`
2. 进入 PromptRange 页面
3. 点击"优化"按钮
4. 应该能看到弹窗，显示17个可用模型
5. 参数预览区域应该显示工具提示和说明文字（鼠标悬停在参数名称上）
6. 选择一个模型，点击"开始初始化"
7. **预期结果**：应该在几秒内完成初始化（不再 pending）

### 4. 查看日志
```bash
tail -f tools/NcfSimulatedSite/Senparc.Web/App_Data/SenparcTraceLog/SenparcTrace-20260324.log
```

**应该能看到的日志**：
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

## 技术要点

### EventBus Handler 注册机制
1. **自动扫描**：`AddSenparcEventBus()` 使用反射扫描指定程序集
2. **接口匹配**：查找实现了 `IIntegrationEventHandler<T>` 的类
3. **DI 注册**：将 Handler 注册为 Scoped 服务
4. **动态调用**：HostedService 通过 `GetServices(handlerType)` 获取 Handler 实例

### 为什么要在 StartWebEngine 之后注册？
- `StartWebEngine` 会触发程序集的扫描和加载
- 确保所有 Xncf 模块的程序集都在 AppDomain 中
- 如果在之前注册，可能扫描不到后加载的模块

### 前端响应格式标准化
所有 NCF API 返回格式都遵循 `AppResponseBase<T>` 结构：
```typescript
{
  success: boolean,
  data: T,                  // 实际数据在这里
  errorMessage?: string
}
```

前端访问数据时必须使用 `response.data.data.xxx`（两层 data）。

---

## 下一步

1. **重启应用**并测试初始化功能
2. **检查控制台日志**：确认 EventBus 扫描到了所有模块
3. **测试优化功能**：初始化成功后，测试完整的优化流程
4. **恢复权限验证**：功能测试通过后，取消注释所有 `[ApiAuthorize("AdminOnly")]` 属性

---

## 相关文件

- `tools/NcfSimulatedSite/Senparc.Web/Register.cs` - EventBus 注册配置
- `src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptInitResponseHandler.cs` - 初始化响应处理器
- `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptInitRequestHandler.cs` - 初始化请求处理器
- `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js` - 前端 API 调用逻辑
- `src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml` - UI 参数预览

---

## 修复日期
2026-03-24
