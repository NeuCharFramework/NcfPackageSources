# 管理后台AI对话功能改版 - 开发计划

## 📌 项目概览

### 背景和动机
用户需要将 NeuCharFramework 管理后台首页改版，增加 AI 对话入口功能，让用户可以通过自然语言与系统进行交互，提升管理后台的智能化体验和操作便利性。同时支持模块拖拽功能，实现上下文感知的对话。

### 核心目标
1. 保留顶部统计区域，维持现有功能
2. 在统计区域下方增加醒目的 AI 对话入口提示框
3. 创建专门的对话任务页面（左侧历史记录 + 右侧对话窗口）
4. 为原有模块增加拖拽功能，支持模块上下文对话
5. 创建完整的数据模型、服务层和前端交互

### 技术栈
- **前端**: Vue.js 2.x + Element UI 2.13.2 + axios（系统现有，不引入新依赖）
- **后端**: ASP.NET Core (Razor Pages) + NCF 框架
- **数据层**: Entity Framework Core + 多数据库支持
- **样式**: CSS3 + 响应式设计（使用系统现有的 Font Awesome 图标）
- **交互**: HTML5 Drag & Drop API（浏览器原生支持，无需库）

**技术约束**：
- ✅ 只使用系统现有的 UI 组件和库
- ✅ 不引入新的第三方 JavaScript 库
- ✅ 所有资源文件使用本地文件，不使用 CDN
- ✅ DTO 继承 `DtoBase<int>` 自动提供 Id 属性

---

## 🎯 规划者分析

### 需求分析

#### 功能需求
1. **AI 对话入口**
   - 在首页顶部统计区下方添加醒目的对话提示框
   - 提示框需要占据合适的高度，保持适当的 margin
   - 支持用户输入对话内容
   - 点击后跳转到专门的对话页面

2. **对话数据模型**
   - 需要创建聊天会话表（AdminChatSession）
   - 需要创建聊天消息表（AdminChatMessage）
   - 需要创建会话-模块关联表（AdminChatSessionModule）
   - 支持多轮对话历史记录
   - 记录用户、AI 回复、时间戳等信息

3. **对话任务页面**
   - 左侧：对话历史记录列表（会话列表）
   - 右侧：当前会话的对话窗口
   - 符合系统现有风格，重用 Element UI 组件
   - 现代化设计，流畅的交互体验

4. **模块拖拽功能**
   - 原有功能模块卡片支持拖拽
   - 可以拖拽到对话框区域
   - 在对话框下方显示已选模块名称
   - 将选中模块信息传递给 AI 对话上下文

#### 非功能需求
1. **性能**: 对话响应时间 < 3s，页面加载 < 2s
2. **兼容性**: 符合现有系统风格，无破坏性变更
3. **可维护性**: 代码结构清晰，遵循 NCF 框架规范
4. **扩展性**: 支持未来添加更多对话功能（如语音、文件上传等）

### 技术架构

#### 数据库设计

**表结构设计**：

1. **ADMIN_AdminChatSession** (聊天会话表)
   - Id (int, PK) - 由 `EntityBase<int>` 提供
   - Title (string, 150) - 会话标题（从首条消息自动提取）
   - UserId (int) - 用户ID（外键到 AdminUserInfo）
   - Status (enum) - 会话状态（Active, Archived, Deleted）
   - LastMessageTime (DateTime) - 最后消息时间
   - CreatedTime, LastModifiedTime, TenantId, AddTime, Flag - 由 `EntityBase<int>` 提供

2. **ADMIN_AdminChatMessage** (聊天消息表)
   - Id (int, PK) - 由 `EntityBase<int>` 提供
   - SessionId (int, FK) - 关联会话ID
   - RoleType (enum) - 角色类型（User, Assistant, System）
   - Content (string) - 消息内容
   - Sequence (int) - 消息序号
   - UserFeedback (bool?) - 用户反馈（Like/Unlike/null）
   - ModelIdentifier (string, 100) - AI 模型标识
   - CreatedTime (DateTime) - 消息创建时间
   - TenantId, AddTime, Flag - 由 `EntityBase<int>` 提供

3. **ADMIN_AdminChatSessionModule** (会话-模块关联表)
   - Id (int, PK) - 由 `EntityBase<int>` 提供
   - SessionId (int, FK) - 关联会话ID
   - XncfModuleUid (string, 36) - 模块UID
   - ModuleName (string, 100) - 模块名称
   - ModuleVersion (string, 50) - 模块版本
   - AddedTime (DateTime) - 添加时间
   - TenantId, AddTime, Flag - 由 `EntityBase<int>` 提供

**DTO 设计规范**：
- 所有 DTO 必须继承 `DtoBase<int>`，自动获得 int 类型的 Id 属性
- DTO 同时继承 Flag、AddTime、LastUpdateTime、TenantId 等基础字段
- 实体转 DTO 时要复制所有基类属性

#### 项目结构

**新建扩展模块**: `Senparc.Xncf.AdminChat`（在 `Senparc.Areas.Admin` 项目中直接添加相关功能）

**目录结构**：
```
tools/NcfSimulatedSite/Senparc.Areas.Admin/
├── Domain/
│   ├── Models/
│   │   └── DatabaseModel/
│   │       ├── AdminChatSession.cs
│   │       ├── AdminChatMessage.cs
│   │       ├── AdminChatSessionModule.cs
│   │       └── Dto/
│   │           ├── AdminChatSessionDto.cs
│   │           ├── AdminChatMessageDto.cs
│   │           └── AdminChatSessionModuleDto.cs
│   └── Services/
│       ├── AdminChatSessionService.cs
│       ├── AdminChatMessageService.cs
│       └── AdminChatSessionModuleService.cs
├── OHS/Local/AppService/
│   └── AdminChatAppService.cs
├── Areas/Admin/Pages/
│   ├── Index.cshtml (修改)
│   ├── Index.cshtml.cs (修改)
│   └── AdminChat/
│       ├── Chat.cshtml (新建)
│       └── Chat.cshtml.cs (新建)
└── wwwroot/
    ├── js/Admin/Pages/
    │   ├── Index/Index.js (修改)
    │   └── AdminChat/Chat.js (新建)
    └── css/Admin/Pages/
        └── AdminChat/Chat.css (新建)
```

#### 技术实现方案

**方案一：作为独立 XNCF 模块**
- 优点：模块化，可独立管理和升级
- 缺点：需要用户安装，增加复杂度

**方案二：直接集成到 Senparc.Areas.Admin 项目** ✅
- 优点：无需安装，开箱即用，更符合后台核心功能定位
- 缺点：耦合度稍高

**最终决策**: 采用**方案二**，直接集成到 `Senparc.Areas.Admin` 项目中。

**原因**：
1. AI 对话是管理后台的核心功能，不是可选模块
2. 简化用户使用流程，无需额外安装
3. 可以直接访问系统用户信息和权限

### 风险评估

| 风险 | 等级 | 影响 | 应对措施 |
|------|------|------|---------|
| 数据表需要 Migration | 🟢 低 | 用户已表示手动执行 | 只需提供 Model 定义，用户自行执行 Migration |
| 与现有 ChatGroup/ChatTask 功能冲突 | 🟡 中 | 可能造成概念混淆 | 明确区分：AdminChat 用于后台管理对话，ChatTask 用于智能体任务 |
| 拖拽功能的浏览器兼容性 | 🟢 低 | 部分浏览器不支持 | 使用标准 HTML5 Drag API，主流浏览器均支持 |
| AI 对话接口调用权限 | 🟡 中 | 需要配置 AI 模型 | 复用现有 AIKernel 模块的配置和接口 |
| 前端状态管理复杂度 | 🟡 中 | 拖拽状态和对话状态交互 | 使用 Vue 的 data 和 computed 属性管理 |

### 技术选型说明

#### 前端技术栈（使用系统现有）
- **Vue.js 2.x**: `~/lib/vue/vue.js` - 系统已有
- **Element UI 2.13.2**: `~/lib/element-ui_2.13.2/` - 系统已有
- **axios**: `~/lib/axios/axios.min.js` - 系统已有（通过 service 封装）
- **Font Awesome**: `~/lib/font-awesome/` - 系统已有
- **Vuex**: `~/lib/vuex.js` - 系统已有（如需要状态管理）

#### 不引入的库（避免）
- ❌ lodash（防抖等工具函数自己实现）
- ❌ marked.js（Markdown 解析，使用简单正则替换）
- ❌ moment.js（时间格式化自己实现）
- ❌ vue-virtual-scroller（虚拟滚动，当前场景不需要）
- ❌ 任何 CDN 远程资源（必须使用本地文件）

#### 后端技术规范
- **DTO 继承**: 必须使用 `DtoBase<int>`，自动提供 Id 属性
- **Service 继承**: 继承 `ServiceBase<TEntity>`
- **AppService 继承**: 继承 `AppServiceBase`
- **实体继承**: 继承 `EntityBase<int>`

---

## 📋 任务看板

### 🔄 进行中
_待开始第一个任务_

### ⏳ 待开始

#### 阶段一：数据模型层设计（预计 2 小时）
详细实现：[Step 01: 数据模型层设计](./steps/step-01-data-models.md)

- [ ] **[TASK-01]** 创建 AdminChatSession 实体和 DTO (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-01
  
- [ ] **[TASK-02]** 创建 AdminChatMessage 实体和 DTO (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-01

- [ ] **[TASK-03]** 创建 AdminChatSessionModule 实体和 DTO (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-01

- [ ] **[TASK-04]** 更新 DbContext 和 Register.Database.cs (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-01

#### 阶段二：服务层实现（预计 2.5 小时）
详细实现：[Step 02: 服务层实现](./steps/step-02-service-layer.md)

- [ ] **[TASK-05]** 创建 AdminChatSessionService (0.8h)
  - 文件路径映射、代码示例、验收标准详见 step-02

- [ ] **[TASK-06]** 创建 AdminChatMessageService (0.7h)
  - 文件路径映射、代码示例、验收标准详见 step-02

- [ ] **[TASK-07]** 创建 AdminChatSessionModuleService (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-02

- [ ] **[TASK-08]** 创建 AdminChatAppService API 接口 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-02

#### 阶段三：首页UI改版（预计 2 小时）
详细实现：[Step 03: 首页UI改版](./steps/step-03-homepage-ui.md)

- [ ] **[TASK-09]** 修改 Index.cshtml 添加对话入口 (0.8h)
  - 文件路径映射、代码示例、验收标准详见 step-03

- [ ] **[TASK-10]** 修改 Index.js 添加对话入口交互 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-03

- [ ] **[TASK-11]** 修改 Index.cshtml.cs 添加后端逻辑 (0.4h)
  - 文件路径映射、代码示例、验收标准详见 step-03

- [ ] **[TASK-12]** 添加响应式样式和动画效果 (0.3h)
  - 文件路径映射、代码示例、验收标准详见 step-03

#### 阶段四：对话任务页面（预计 3 小时）
详细实现：[Step 04: 对话任务页面](./steps/step-04-chat-page.md)

- [ ] **[TASK-13]** 创建 Chat.cshtml 页面结构 (1h)
  - 文件路径映射、代码示例、验收标准详见 step-04

- [ ] **[TASK-14]** 创建 Chat.cshtml.cs 后端逻辑 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-04

- [ ] **[TASK-15]** 创建 Chat.js 前端交互 (1h)
  - 文件路径映射、代码示例、验收标准详见 step-04

- [ ] **[TASK-16]** 创建 Chat.css 样式文件 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-04

#### 阶段五：模块拖拽功能（预计 1.5 小时）
详细实现：[Step 05: 模块拖拽功能](./steps/step-05-drag-drop.md)

- [ ] **[TASK-17]** 为模块卡片添加拖拽支持 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-05

- [ ] **[TASK-18]** 实现对话框拖放区域 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-05

- [ ] **[TASK-19]** 实现选中模块显示和管理 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-05

#### 阶段六：集成测试和优化（预计 1 小时）
详细实现：[Step 06: 集成测试和优化](./steps/step-06-testing.md)

- [ ] **[TASK-20]** 完整功能测试 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-06

- [ ] **[TASK-21]** 性能优化和代码审查 (0.5h)
  - 文件路径映射、代码示例、验收标准详见 step-06

### ✅ 已完成
_待执行者完成任务后更新_

---

## 💬 执行者反馈

### 当前进度
_待执行者更新_

### 遇到的问题
_待执行者记录_

### 需要的帮助
_待执行者提出_

---

## 📚 经验教训

### 技术难点
_待执行者记录_

### 解决方案
_待执行者记录_

### 避坑指南
_待执行者记录_

---

## 🎉 里程碑记录

_待完成后记录_

---

**创建日期**: 2026-03-25  
**最后更新**: 2026-03-25  
**当前版本**: v0.1.0 (规划阶段)

## 📦 预期交付清单

### 📄 新建的文件

**数据模型层**
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSession.cs`
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatMessage.cs`
3. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSessionModule.cs`
4. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionDto.cs`
5. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatMessageDto.cs`
6. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionModuleDto.cs`

**服务层**
7. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatSessionService.cs`
8. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatMessageService.cs`
9. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatSessionModuleService.cs`
10. `tools/NcfSimulatedSite/Senparc.Areas.Admin/OHS/Local/AppService/AdminChatAppService.cs`

**页面和交互**
11. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml`
12. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml.cs`
13. `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/Chat.js`
14. `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/css/Admin/Pages/AdminChat/Chat.css`

### 🔧 修改的文件
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml`
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml.cs`
3. `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js`
4. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminSenparcEntities.cs`（可能需要）
5. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Register.Database.cs`（可能需要）
