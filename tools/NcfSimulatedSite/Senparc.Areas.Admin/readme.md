## Senparc.Areas.Admin 项目说明

此项目是管理员后台模块，同样属于 XNCF 模块。不过这是一个特殊的模块，在系统安装的时候就被装载，并且可用于管理其他所有模块。

如果你狠狠心，也可以将此模块删除，这不会影响系统的正常运行，只不过你无法再进入管理后台。当然对黑客来说也一样。

## Admin Chat：Harness 模式（MAF 长任务）

管理后台 Admin Chat 支持两种运行模式，可在输入框下方切换：

- **普通对话（Simple）**：单轮问答，保持既有体验（默认）。
- **长任务（Harness）**：基于 Microsoft Agent Framework（MAF）的多步骤自主执行。在同一 `Agent`/`AgentSession` 上反复运行“计划 → 调用工具 → 观察 → 继续”，直到模型输出完成标记 `[[DONE]]`、达到最大步数或超时，适合较复杂的长任务。

关键实现：

- `AdminChatAiService.GenerateHarnessResponseAsync(...)`：构建与 Simple 模式一致的 MAF Agent（复用共享的工具构建 `BuildAdminChatFunctionsAsync`，含模块 FunctionRender 与工作流工具），并在 `AdminChatHarnessExecutor` 中按步驱动执行。
- `AdminChatHarnessExecutor`：与具体模型无关的可测试执行器，负责步数预算、超时控制、控制标记（`[[CONTINUE]]` / `[[DONE]]`）识别、步骤记录与标记清洗。
- `ChatMessageInputDto.Mode`（`AdminChatMode.Simple/Harness`）：请求级模式开关；`SendMessageResponse.HarnessSteps` 返回执行步骤供前端展示。

前端在 `Areas/Admin/Pages/AdminChat/Chat.cshtml` 提供模式切换与“执行步骤”折叠面板。相关单元测试见 `Tests/Senparc.Areas.Admin.Tests/Domain/Services/AdminChatHarnessExecutorTests.cs`。

## NeuBell WebHook（WebAPI）通知设置

为纽铃（NeuBell）增加外部 WebHook（WebAPI）通知能力：管理员可添加一个或多个通知端点，当任一纽铃 Provider 的条目发生**新增（added）**或**移除（removed）**时，系统以**异步方式** POST 通知到匹配的地址，不阻塞业务请求与 Footer 刷新。

此外还支持**按调用触发**：在 FunctionRender「纽铃可见提醒测试」（`SystemInfoAppService.TriggerNeuBellTest`，action=`send`）中提供可选参数 **`WebHookUrl`（WebHook 地址，可选）**——填写后，创建该条纽铃时会立即向该地址异步 POST 一条 `kind=item-created` 事件（fire-and-forget，不阻塞 Function 响应）；留空则只走常规监测分发。

所有 WebHook 出站请求（`item-created` / `items-changed` / `test` 三类）的**完整请求报文与结果**都会记录到「请求日志」列表（管理页 NeuBell → 请求日志），支持查看报文、按条删除与批量清空（保留最近 50 条）。

管理入口：页脚“纽铃”抽屉 → “WebHook 设置”（页面 `Areas/Admin/Pages/NeuBell/Index`，仅超级管理员可访问）。

每条设置支持：

- **名称 / WebHook 地址**：仅允许 http/https 绝对地址（`NeuBellWebHook.TryValidateUrl` 校验）。
- **Provider 过滤**：留空通知全部 Provider；填写 ProviderId 只通知对应模块（如 `admin-neubell-test`）。
- **签名密钥（可选）**：配置后请求携带 `X-NeuBell-Signature: t=<unix>,v1=<hex>` 请求头，`v1 = HMAC-SHA256(secret, t + "." + body)`，便于接收端验签。
- **新增时通知 / 移除时通知**：分别控制 added / removed 两类事件。
- **启用开关**：停用的设置不参与分发；管理页“测试”按钮会向该端点发送一条 `kind=test` 事件用于连通性验证。

通知报文（`application/json`）示例：

```json
{
  "source": "NeuBell",
  "kind": "items-changed",
  "providerId": "admin-neubell-test",
  "moduleUid": "Senparc.Areas.Admin",
  "displayName": "Admin 测试纽铃",
  "occurredAt": "2026-09-06T12:00:00+00:00",
  "added": [ { "id": "task-1", "title": "任务一", "summary": "摘要", "count": 1, "severity": "info", "detailUrl": "/Admin/...", "updatedAt": "2026-09-06T12:00:00+00:00" } ],
  "removed": []
}
```

`item-created`（创建时按参数触发）报文示例：

```json
{
  "source": "NeuBell",
  "kind": "item-created",
  "providerId": "admin-neubell-test",
  "occurredAt": "2026-09-11T12:00:00+00:00",
  "item": { "title": "任务一", "summary": "摘要" }
}
```

关键实现：

- `Domain/Models/DatabaseModel/NeuBellWebHookModels.cs`：实体 `NeuBellWebHook`（表 `ADMIN_NeuBellWebHook`），含 URL 校验与 Provider 匹配逻辑。
- `Domain/Services/NeuBellWebHookService.cs` / `ACL/Repository/NeuBellWebHookRepository.cs`：设置项 CRUD 与启用列表查询（契约 `INeuBellWebHookService` 便于测试替换）。
- `Domain/Services/NeuBellWebHookDispatcher.cs`：按 Provider 维护条目基线，diff 出 added/removed 后 fire-and-forget 分发（`IHttpClientFactory` 命名客户端 `NeuBellWebHook`，`SemaphoreSlim(4)` 限制并发出站）。
  - 首次观测只建基线，避免进程重启把存量条目误报为“新增”；
  - Provider 本轮缺席且仍在开放列表中（快照获取失败）时保留基线等待下轮，避免误报移除；模块被关闭时才将存量条目按“移除”通知。
- `Domain/Services/NeuBellWebHookLogService.cs` / `ACL/Repository/NeuBellWebHookRepository.cs`：请求日志实体 `NeuBellWebHookLog`（表 `ADMIN_NeuBellWebHookLog`）的「发送中 → 成功/失败」两段式记录；日志写入失败只记警告，绝不影响通知本身。
- 按调用触发：`OHS/Local/PL/NeuBellTest_Request.cs` 的 `WebHookUrl` 可选参数 → `SystemInfoAppService` 以 fire-and-forget 方式调用 `NeuBellWebHookDispatcher.NotifyItemCreatedAsync`（无效地址不发起 HTTP，但落一条失败日志便于排查）。
- `Domain/Services/NeuBellWebHookMonitorService.cs`：`IHostedService` 监测循环，轮询间隔由 `appsettings.json` 的 `NeuBellWebHook:PollingIntervalSeconds` 控制（默认 30 秒，最小 5 秒），并订阅 `NeuBellChangeNotifier` 变更事件提前唤醒一轮观测。
- 数据库迁移已同步生成至 Sqlite / SqlServer / MySql / Dm / Oracle / PostgreSQL 六个提供方（`Domain/Migrations/*`）。

相关单元测试见 `Tests/Senparc.Areas.Admin.Tests/Domain/Services/NeuBellWebHookDispatcherTests.cs`（基线建立、新增/移除检测、过滤匹配、缺席判定、HMAC 签名、测试事件，以及 `item-created` 按调用通知与请求日志的成功/失败记录）。
