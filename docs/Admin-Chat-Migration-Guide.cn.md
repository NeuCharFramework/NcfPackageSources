[English](Admin-Chat-Migration-Guide.md)

# 管理后台 AI 对话功能 - Migration 指南

## 📋 执行清单

### ✅ 已完成的工作

1. **数据模型创建**
   - ✅ 3 个实体类（AdminChatSession, AdminChatMessage, AdminChatSessionModule）
   - ✅ 3 个 DTO 类（继承 DtoBase<int>）
   - ✅ 3 个 Repository 接口和实现类

2. **服务层实现**
   - ✅ 3 个 Domain Service 类
   - ✅ 1 个 AppService（9 个 API 接口）
   - ✅ 所有服务已在 Register.cs 中注册

3. **前端页面开发**
   - ✅ 首页 AI 对话入口（Index.cshtml + Index.js）
   - ✅ 对话任务页面（Chat.cshtml + Chat.js + Chat.css）
   - ✅ 模块拖拽功能

4. **代码质量**
   - ✅ 编译成功（0 个错误）
   - ✅ 应用程序可正常启动
   - ✅ 所有依赖已正确注册

---

## 🚀 待执行操作（醒来后操作）

### 步骤 1: 创建数据库 Migration

```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# 创建 Migration
dotnet ef migrations add AddAdminChatTables \
  --project ../Senparc.Areas.Admin/Senparc.Areas.Admin.csproj \
  --context AdminSenparcEntities
```

**预期结果**：在 `Senparc.Areas.Admin/Migrations/` 文件夹中生成新的 Migration 文件。

---

### 步骤 2: 检查 Migration 文件

打开生成的 Migration 文件（如 `20260325_AddAdminChatTables.cs`），确认包含以下 3 张表：

1. **ADMIN_AdminChatSession**
   - 字段：Id, Title, UserId, Status, LastMessageTime, AddTime, LastUpdateTime, TenantId, Flag

2. **ADMIN_AdminChatMessage**
   - 字段：Id, SessionId, RoleType, Content, Sequence, UserFeedback, ModelIdentifier, AddTime, LastUpdateTime, TenantId, Flag

3. **ADMIN_AdminChatSessionModule**
   - 字段：Id, SessionId, XncfModuleUid, ModuleName, ModuleVersion, AddedTime, AddTime, LastUpdateTime, TenantId, Flag

---

### 步骤 3: 执行 Migration

```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# 应用 Migration 到数据库
dotnet ef database update \
  --project ../Senparc.Areas.Admin/Senparc.Areas.Admin.csproj \
  --context AdminSenparcEntities
```

**预期结果**：数据库中创建 3 张新表，并显示 "Done." 消息。

---

### 步骤 4: 验证数据库表

使用数据库管理工具（如 Navicat, DBeaver）连接数据库，检查：

```sql
-- 检查表是否存在
SELECT * FROM ADMIN_AdminChatSession LIMIT 0;
SELECT * FROM ADMIN_AdminChatMessage LIMIT 0;
SELECT * FROM ADMIN_AdminChatSessionModule LIMIT 0;

-- 查看表结构
DESCRIBE ADMIN_AdminChatSession;
DESCRIBE ADMIN_AdminChatMessage;
DESCRIBE ADMIN_AdminChatSessionModule;
```

---

### 步骤 5: 启动应用程序

```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# 启动应用
dotnet run
```

**预期结果**：
- 应用程序成功启动
- 显示 "Now listening on: http://localhost:5000"
- 没有数据库相关错误

---

### 步骤 6: 功能测试

#### 6.1 首页测试

1. 访问 `http://localhost:5000/Admin`
2. 登录管理后台
3. 检查首页改版效果：
   - ✅ 顶部统计区域正常显示
   - ✅ AI 对话入口提示框显示（醒目、有 margin）
   - ✅ "开始对话"按钮可点击
   - ✅ 模块卡片保留

#### 6.2 模块拖拽测试

1. 在首页，拖拽任一模块卡片
2. 拖拽到 AI 对话入口提示框上方
3. 检查效果：
   - ✅ 拖拽时提示框高亮/变色
   - ✅ 释放后模块名称显示在提示框下方
   - ✅ 可以删除已选模块（点击 x 按钮）

#### 6.3 对话页面测试

1. 在对话入口输入文字，点击"开始对话"
2. 跳转到对话页面 `/Admin/AdminChat/Chat`
3. 检查页面布局：
   - ✅ 左侧会话列表显示
   - ✅ 右侧对话窗口显示
   - ✅ 初始消息已发送
   - ✅ AI 回复显示（占位文本）

#### 6.4 对话交互测试

1. 在右侧对话窗口输入消息，按 Enter 发送
2. 检查效果：
   - ✅ 用户消息显示在右侧（蓝色头像）
   - ✅ AI 回复显示在左侧（机器人头像）
   - ✅ 消息时间戳正确
   - ✅ 滚动到最新消息

3. 测试点赞/点踩功能：
   - ✅ 点击 AI 消息的 👍 或 👎 图标
   - ✅ 状态切换正常（高亮/取消高亮）

4. 测试会话切换：
   - ✅ 点击左侧不同会话
   - ✅ 右侧内容切换正确
   - ✅ 消息历史加载正确

5. 测试新建对话：
   - ✅ 点击"新建对话"按钮
   - ✅ 创建新会话，切换到新对话

---

## ⚠️ 已知限制和待扩展功能

### 当前限制

1. **AI 回复为占位文本**
   - `AdminChatAppService.GenerateAIResponseAsync()` 方法返回固定文本
   - **原因**: 需要配置真实的 AI 模型接口
   - **扩展方向**: 集成 AIKernel 模块，调用配置的 AI 模型

2. **模块上下文暂未传递给 AI**
   - 选中的模块信息会保存到数据库，但暂未在 AI 生成时使用
   - **扩展方向**: 在 `GenerateAIResponseAsync` 中读取关联模块，加入 prompt

3. **会话管理功能简化**
   - 暂不支持会话归档、删除、重命名等操作
   - **扩展方向**: 在会话列表添加更多操作按钮（归档、删除、导出等）

### 未来扩展方向

1. **文件上传支持**
   - 支持上传文档、图片到对话中
   - 使用 FileManager 模块存储文件

2. **语音对话支持**
   - 集成语音识别和 TTS
   - 支持语音消息

3. **对话导出功能**
   - 导出对话为 Markdown / PDF
   - 分享对话链接

4. **多人协作对话**
   - 支持多个管理员在同一会话中对话
   - 实时消息推送（WebSocket）

5. **智能建议和快捷指令**
   - 根据对话历史提供智能建议
   - 支持 `/` 快捷指令（如 `/help`, `/clear` 等）

---

## 📊 功能清单总结

| 功能模块 | 状态 | 说明 |
|---------|------|------|
| 首页统计区域保留 | ✅ 已完成 | 原有功能完全保留 |
| AI 对话入口提示框 | ✅ 已完成 | 醒目设计，支持输入和拖拽 |
| 模块拖拽功能 | ✅ 已完成 | HTML5 Drag API，无需库 |
| 对话任务页面 | ✅ 已完成 | 左右布局，现代化设计 |
| 会话历史管理 | ✅ 已完成 | 列表显示，切换，新建 |
| 消息发送接收 | ✅ 已完成 | 用户/AI 消息区分 |
| 消息反馈功能 | ✅ 已完成 | 点赞/点踩 |
| 模块上下文关联 | ✅ 已完成 | 保存到数据库 |
| API 接口完整 | ✅ 已完成 | 9 个接口，CRUD 完整 |
| 编译通过 | ✅ 已完成 | 0 个错误 |
| AI 真实回复 | ⏳ 待扩展 | 需要集成 AI 模型 |

---

## 🎯 技术亮点

1. **零新依赖**: 完全使用系统现有的 Vue.js、Element UI、axios，无新增库
2. **DDD 架构**: 严格遵循 Domain 实体、Service、AppService 分层
3. **Repository 模式**: 为每个实体创建对应的 Repository 接口和实现
4. **DTO 规范化**: 所有 DTO 继承 `DtoBase<int>`，统一基类属性管理
5. **拖拽交互**: 原生 HTML5 Drag API，无需第三方库
6. **现代化 UI**: 符合系统风格，响应式设计，流畅动画
7. **API 安全**: 使用 `[BackendJwtAuthorize]` 保护所有接口

---

**文档创建日期**: 2026-03-25  
**预计执行时间**: 10-15 分钟  
**难度等级**: ⭐⭐ (中等)
