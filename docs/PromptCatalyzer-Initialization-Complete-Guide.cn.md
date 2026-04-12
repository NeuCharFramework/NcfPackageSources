[English](PromptCatalyzer-Initialization-Complete-Guide.md)

# PromptCatalyzer 初始化完整性检查与修复指南

## 当前状态分析

**已知情况**：
- ✅ PromptCatalyzer Range 已创建
- ❌ PromptItem（靶道）为空
- ❌ 之前的初始化因数据库错误中断

---

## 🔍 数据完整性检查

### 检查数据库当前状态

```sql
-- 1. 检查 PromptRange 表
SELECT * FROM [Xncf_PromptRange_PromptRange] WHERE Alias = 'PromptCatalyzer';

-- 2. 检查 PromptItem 表（靶道）
SELECT * FROM [Xncf_PromptRange_PromptItem] 
WHERE RangeId IN (SELECT Id FROM [Xncf_PromptRange_PromptRange] WHERE Alias = 'PromptCatalyzer');

-- 3. 检查 AgentTemplate 表
SELECT * FROM [Xncf_AgentsManager_AgentTemplate] WHERE Name = 'PromptCatalyzer';
```

### 预期结果

**正常初始化完成后应该有**：
1. **PromptRange 记录**：1条，Alias = "PromptCatalyzer"
2. **PromptItem 记录**：至少1条，关联到上述 Range
3. **AgentTemplate 记录**：1条，Name = "PromptCatalyzer"

---

## 🛠️ 修复内容总结

### 修复1：EventHandler 注册（核心问题）
**文件**：`tools/NcfSimulatedSite/Senparc.Web/Register.cs`
- 使用 `AddSenparcEventBus()` 自动扫描和注册 Handler
- 在 `StartWebEngine()` 之后调用，确保所有模块已加载

### 修复2：PromptItem 字段初始化
**文件**：`src/Extensions/Senparc.Xncf.PromptRange/Domain/Models/DatabaseModel/PromptItem.cs`
- 在构造函数中添加 `NickName = string.Empty;`

### 修复3：PromptInitRequestHandler 增强容错
**文件**：`src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptInitRequestHandler.cs`
- 为 request 对象设置所有字符串字段的默认值
- 增加详细的步骤日志（步骤1/4、2/4 等）
- 增加 try-catch 容错处理
- 捕获完整的 inner exception

### 修复4：PromptOptimizationService 增强日志
**文件**：`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`
- 增加详细的步骤日志
- 验证返回的 PromptCode 不为空

---

## 🚀 完整测试流程

### 第1步：重启应用

```bash
# 停止当前运行的应用（Ctrl+C）
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources
dotnet run --project tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj
```

### 第2步：检查启动日志（关键！）

**必须看到以下输出**：

```
EventBus 扫描程序集:
  - Senparc.Xncf.PromptRange        ← 包含 PromptInitRequestHandler
  - Senparc.Xncf.AgentsManager       ← 包含 PromptInitResponseHandler
  - Senparc.Areas.Admin
  - ... (其他模块)
EventBus 已注册，共扫描了 XX 个程序集

Senparc NCF EventBus Service is starting with MaxConcurrency=XX, EnableDuplicateDetection=True, MaxEventChainDepth=10, EnableCircularReferenceDetection=True
```

**如果没有看到上述输出**：
- EventHandler 可能没有正确注册
- 请提供完整的启动日志

### 第3步：打开日志监控

**在新终端窗口运行**：

```bash
tail -f tools/NcfSimulatedSite/Senparc.Web/App_Data/SenparcTraceLog/SenparcTrace-20260324.log
```

### 第4步：测试初始化

1. **访问页面**：`http://localhost:5000` → PromptRange 页面
2. **刷新浏览器**：Cmd+Shift+R（清除 JS 缓存）
3. **点击"优化"按钮**
4. **检查模型列表**：应显示17个模型，参数预览有工具提示
5. **选择模型**，点击"开始初始化"

### 第5步：查看详细日志

**预期日志流程**：

```
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
```

---

## ❗ 可能遇到的错误

### 错误1：仍然是数据库保存失败

**日志特征**：
```
  ❌ 创建 PromptItem 失败！详细错误: An error occurred while saving...
  Inner Exception: [具体的数据库错误]
```

**诊断步骤**：
1. 查看完整的 inner exception 信息
2. 检查数据库表结构：`PromptItem` 表的哪个字段不允许 null
3. 提供具体的 inner exception 消息

**可能的原因**：
- 数据库表字段约束（NOT NULL、UNIQUE等）
- 外键约束（ModelId、RangeId）
- 字段长度超限

### 错误2：PromptCode 为空

**日志特征**：
```
Prompt Init Response: , RequestId: xxx
```

**原因**：`item.FullVersion` 生成逻辑有问题
**解决**：检查 `PromptItem` 的 `FullVersion` 属性计算逻辑

### 错误3：EventHandler 未被调用

**日志特征**：
- 看不到 "开始处理 Prompt Init Request"
- 请求一直 pending

**解决**：
1. 检查启动日志是否显示 EventBus 扫描到了模块
2. 确认 `Senparc.Xncf.PromptRange` 和 `Senparc.Xncf.AgentsManager` 在扫描列表中

---

## 🔧 如果初始化仍然失败

### 方案A：清理已有数据重新初始化

```sql
-- 警告：这会删除 PromptCatalyzer 相关的所有数据！
DELETE FROM [Xncf_AgentsManager_AgentTemplate] WHERE Name = 'PromptCatalyzer';
DELETE FROM [Xncf_PromptRange_PromptItem] 
WHERE RangeId IN (SELECT Id FROM [Xncf_PromptRange_PromptRange] WHERE Alias = 'PromptCatalyzer');
DELETE FROM [Xncf_PromptRange_PromptRange] WHERE Alias = 'PromptCatalyzer';
```

然后重新测试初始化。

### 方案B：手动补全缺失的 PromptItem

如果您不想删除已有的 Range，可以提供：
1. **完整的 inner exception 日志**（现在会记录在日志中）
2. **PromptItem 表的结构**（哪些字段是 NOT NULL）

我可以帮您手动构造正确的 SQL 插入语句。

---

## 📋 日志检查清单

启动应用后，请确认以下内容：

- [ ] 启动日志显示 EventBus 扫描了 `Senparc.Xncf.PromptRange`
- [ ] 启动日志显示 EventBus 扫描了 `Senparc.Xncf.AgentsManager`
- [ ] 启动日志显示 `EventBus 已注册，共扫描了 XX 个程序集`
- [ ] 点击初始化后，日志显示 `========== 开始处理 Prompt Init Request ==========`
- [ ] 日志显示所有步骤（1/4 到 4/4）都成功执行
- [ ] 日志显示 `PromptItem 创建成功，FullVersion: xxx`
- [ ] 日志显示 `Agent 创建成功！AgentId: xx`
- [ ] 前端收到成功消息

---

## 🎯 成功标准

**完整初始化成功的标志**：

1. **数据库记录**：
   - `PromptRange` 有1条记录
   - `PromptItem` 有1条记录（FullVersion 格式：`PromptCatalyzer-T1-A1`）
   - `AgentTemplate` 有1条记录（SystemMessage 存储 PromptCode）

2. **前端显示**：
   - 弹窗显示"初始化成功！PromptCode: PromptCatalyzer-T1-A1"
   - 页面数据自动刷新

3. **日志完整**：
   - 所有步骤日志都有 ✅ 标记
   - 没有 ❌ 错误标记

---

## 下次更新

请重启应用后：
1. 提供**启动日志**（特别是 EventBus 扫描部分）
2. 点击初始化，观察**完整的执行日志**
3. 如果失败，提供**完整的错误信息**（包括 inner exception）

增强的日志会帮助我们精确定位问题！
