# NeuChar Workflow · Chat 触发器（v0.4.0）

## 📌 概述

Chat Trigger，为 **Senparc.Xncf.NeuCharWorkflow** 模块新增了 **Chat 触发器**：

- 工作流管理员在设计器中选择“Chat 触发”后，得到一个可直接分享的聊天页面 URL。
- 打开该页面的用户可以像聊天一样发送消息，**每条消息都会作为输入启动一次工作流运行**。
- 支持**登录用户**与**未登录访客**（访客开关可在设置中关闭）。
- 工作流仍完全由服务端运行协调器执行，浏览器端不参与运行；会话历史持久化到数据库，主机重启后打开页面自动恢复。

## 🎯 使用方式

### 1. 创建工作流

管理端 `Workflow` 页面 → 新建工作流 → **工作流设置** → 触发方式选择 **Chat 触发**。

设置项（`NeuCharWorkflowChatConfig`）：

| 字段 | 说明 | 限制 |
| --- | --- | --- |
| `title` | 聊天页面标题，留空使用工作流名称 | ≤ 100 字符 |
| `greeting` | 首次打开时的欢迎语，留空使用默认文案 | ≤ 500 字符 |
| `allowGuest` | 是否允许未登录访客访问 | 布尔，默认 `true` |

保存时触发器节点会自动同步为 `chat-trigger`，聊天配置经服务端规范化后写入 `TriggerConfigJson`。

### 2. 打开聊天页面

- 工作流启用（启用开关）且触发方式为 Chat 后，命令栏出现 **聊天页面** 按钮；设置对话框中也可以复制/打开 URL。
- URL 形如：

  ```
  https://<host>/api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/chat/{workflowId}/page
  ```

- 页面为自包含 HTML（内联 CSS/JS，无外部依赖）：消息气泡、输入框（Enter 发送 / Shift+Enter 换行）、运行中的“正在执行某节点…”状态提示、新对话（清空会话）按钮。

### 3. 身份识别

| 访问者 | 参与者标识 | 说明 |
| --- | --- | --- |
| 登录用户 | `user:{登录名}` | 取自 Claims（NameIdentifier 或 Identity.Name），无需额外 Cookie |
| 访客 | `guest:{会话令牌}` | 服务端生成 32 字节随机令牌，写入 HttpOnly Cookie `nxcf_wf_chat_guest`（SameSite=Lax，7 天；HTTPS 时自动 Secure） |

登录/访客身份在同一工作流下互不混用；访客关闭 `allowGuest` 后访问会收到 401 提示。

## 🔌 聊天 API（匿名入口）

控制器：`OHS/Local/Controllers/NeuCharWorkflowChatController.cs`
路由前缀：`api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/chat`

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `{workflowId}/page` | 聊天页面（HTML） |
| GET | `{workflowId}/bootstrap` | 标题、欢迎语、访客标识、历史消息、进行中的运行 |
| POST | `{workflowId}/messages` | 发送消息 `{ "message": "..." }`，返回 `runId`（202） |
| GET | `{workflowId}/runs/{runId}?afterSequence=` | 轮询运行状态：`running` / `finalOutput` / `runError` / `lastNodeMessage` / `lastSequence` |
| POST | `{workflowId}/reset` | 清空当前会话历史（内存 + 数据库，不中止进行中的运行） |

状态码语义与 Webhook 入口一致：404 工作流不存在、409 未启用 Chat 触发、401 访客被禁止、202 已接受。

## ⚙️ 实现要点

### 新增文件

| 文件 | 职责 |
| --- | --- |
| `Domain/Services/NeuCharWorkflowChatConfig.cs` | Chat 配置模型与服务端规范化（标题/欢迎语长度、访客开关） |
| `Domain/Services/NeuCharWorkflowChatSessionService.cs` | 进程内会话存储：消息历史（≤200 条/会话）、运行占位（一会话一运行）、24 小时 TTL、500 会话上限、启动时注入数据库历史 |
| `OHS/Local/Controllers/NeuCharWorkflowChatController.cs` | 匿名聊天 API 与访客 Cookie 管理 |
| `OHS/Local/Controllers/NeuCharWorkflowChatPage.cs` | 自包含聊天页面模板（`__WORKFLOW_ID__` 占位符替换） |

### 修改文件

| 文件 | 变更 |
| --- | --- |
| `Domain/Services/NeuCharWorkflowEngine.cs` | 允许 `chat-trigger` 节点；执行时透传输入；输出描述符按 `string` 处理；宿主服务每日清理超过 30 天保留期的 Chat 消息 |
| `Domain/Services/NeuCharWorkflowRunCoordinator.cs` | 运行来源新增 `chat` |
| `Application/AppServices/NeuCharWorkflowAppService.cs` | 保存时规范化 Chat 配置；新增 `GetChatBootstrapAsync` / `SendChatMessageAsync` / `GetChatRunStatusAsync` / `ResetChatSessionAsync` 与对应结果记录；Chat 消息落库持久化，bootstrap 时内存为空则从数据库恢复历史 |
| `Domain/Models/DatabaseModel/NeuCharWorkflowModels.cs` | 新增 `NeuCharWorkflowChatMessage` 实体（表 `NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage`） |
| `Models/NeuCharWorkflowSenparcEntities.cs` | 新增 `NeuCharWorkflowChatMessages` DbSet |
| `Register.Database.cs` | Chat 消息复合索引（`WorkflowId`, `ParticipantKeyHash`） |
| `ACL/NeuCharWorkflowRepositories.cs` | 新增 Chat 消息仓储 |
| `Domain/Services/NeuCharWorkflowServices.cs` | 新增 `NeuCharWorkflowChatMessageService`：最近 200 条历史查询、按参与者/工作流删除、过期清理 |
| `Models/NeuCharWorkflowMultipleDbContexts.cs` | Oracle/Dm 长文本列映射：Chat 内容使用 NCLOB，回放 JSON 使用 CLOB |
| `Domain/Migrations/<Provider>/…AddChatMessages.cs` | 六种数据库（Sqlite/SqlServer/MySql/PostgreSQL/Oracle/Dm）的 Chat 消息表迁移 |
| `Register.cs` | 注册 `NeuCharWorkflowChatSessionService`（Singleton）与 Chat 消息仓储/服务（Scoped） |
| `Areas/Admin/Pages/NeuCharWorkflow/Index.cshtml` | 触发方式新增 Chat 选项、设置对话框 Chat 配置区、命令栏“聊天页面”按钮 |
| `wwwroot/js/NeuCharWorkflow/Workflow.js` | 表单默认值、触发器节点同步、配置构建/回填、校验、列表标签、`chatUrl` 计算属性 |
| `wwwroot/css/NeuCharWorkflow/Workflow.css` | Chat 配置区样式 |

### 运行与消息流程

1. 用户发送消息 → `SendChatMessageAsync` 校验（模块/工作流/触发方式/访客）→ 追加用户消息并落库 → `RunCoordinator.TryStart(..., source: "chat")` → 会话记录 `PendingRunId`。
2. 页面每 1.2 秒轮询 `GetChatRunStatusAsync` → 运行中显示最新节点事件；结束时把最终输出（或错误）写入会话历史并落库，释放占位。
3. 页面刷新后通过 bootstrap 的 `pendingRunId` 恢复轮询；内存会话为空（如主机重启后）时，bootstrap 先从数据库恢复历史消息。
4. 聊天输入按纯文本字符串传给触发器（与手动触发一致，`parseInputAsJson=false`）。

## 🔒 安全说明

- 入口控制器 `[AllowAnonymous]`，但每次调用都校验工作流启用状态、触发方式与访客开关；工作流未启用时不可聊天。
- 访客令牌仅作为会话标识，不携带任何凭据；数据库与内存字典只保存参与者标识的 SHA256 摘要，原始访客令牌不落库，服务端日志与 API 不返回原始令牌。
- 消息长度上限 8000 字符；会话历史持久化在 `NEUCHAR_WORKFLOW_NeuCharWorkflowChatMessage` 表：主机重启后打开页面自动恢复，保留 30 天（宿主服务每日清理），会话重置时内存与数据库一并清空，工作流删除时其全部聊天历史随之删除。
- 同一会话同一时间只允许一个进行中的运行，防止并发发送造成状态覆盖。

## ✅ 验证

- `dotnet build src/Extensions/Senparc.Xncf.NeuCharWorkflow/Senparc.Xncf.NeuCharWorkflow.csproj` 通过（无新增告警）。
- `node --check wwwroot/js/NeuCharWorkflow/Workflow.js` 语法通过。
- 手动验证步骤：
  1. 管理端新建工作流，触发方式选“Chat 触发”，保存并启用；
  2. 点击“聊天页面”按钮（或复制 URL 在无痕窗口打开）；
  3. 发送消息，观察气泡与节点执行状态，最终输出回复在聊天中显示；
  4. 无痕窗口（访客）与登录窗口各自保持独立会话；关闭“允许访客”后无痕窗口收到 401 提示；
  5. “新对话”按钮清空历史；刷新页面可恢复进行中的运行轮询。
  6. 重启站点进程后重新打开页面，确认历史消息从数据库恢复显示；
  7. 会话重置后重启进程，确认历史不会被再次恢复。
