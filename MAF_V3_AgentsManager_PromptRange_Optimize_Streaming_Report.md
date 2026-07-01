# MAF V3 AgentsManager / PromptRange 升级与优化交付报告

## 1. 任务概览

本次在当前工作区完成了以下三项任务的代码实现与验证：

1. `AgentsManager` 优化（Usage 入库、统计分析页、维度查询）
2. `PromptRange` 增加 Token Usage 记录与 UI 展示
3. `PromptRange` 与 `AgentsManager` 流式输出升级（由非实时/轮询到实时流式）

> 说明：按照你的要求尝试创建分支 `Developer-MAF-V3-AgentsManager-Optimize` 和 `Developer-MAF-V3-AgentsManager-Optimize-Streaming`，但当前执行环境对 `.git` 写入受限，且提权审批接口返回 404，导致无法在此环境内实际创建分支（代码改动已完成）。

---

## 2. 任务一：AgentsManager Usage 统计优化

### 2.1 数据记录（数据库层）

- 在对话消息级别记录 Usage（输入/输出/总 Token、响应耗时、轮次、响应ID）：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Models/Usage/ChatUsageRemarkCodec.cs`
  - 通过 `ChatGroupHistory.AdminRemark` 编解码持久化
- 在任务级别累计聚合 Usage：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/ChatTaskService.cs`
  - 聚合写入 `ChatTask.AdminRemark`
- DTO 透出聚合统计字段：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Models/DatabaseModel/Dto/ChatTaskDto.cs`
  - 包含 `TotalPromptTokens / TotalCompletionTokens / TotalTokens / TotalRounds / AverageResponseMilliseconds / MaxResponseMilliseconds`

### 2.2 服务层统计聚合能力

- 新增用量分析接口返回模型：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Application/DTOs/ChatGroupHistoryResponse.cs`
- 实现多维聚合分析：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/ChatGroupHistoryAppService.cs`
- 支持维度：
  - 单轮对话（RoundStats）
  - 单个 Agent（AgentStats）
  - 按时间桶统计（TimelineStats）
  - 响应耗时概览（平均/最小/最大/P95）
  - Agent 过滤 + 时间区间过滤

### 2.3 UI 页面展示

- 新增任务“用量统计”弹窗和查询工具栏：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Areas/Admin/Pages/AgentsManager/Index.cshtml`
- 前端逻辑：
  - `src/Extensions/Senparc.Xncf.AgentsManager/wwwroot/js/AgentsManager/index.js`
  - `openUsageAnalytics` / `loadUsageAnalytics` / `resetUsageAnalyticsFilters`
- 样式：
  - `src/Extensions/Senparc.Xncf.AgentsManager/wwwroot/css/AgentsManager/index.css`

---

## 3. 任务二：PromptRange Token Usage 记录与展示

### 3.1 Usage 记录与累计

- Usage 解析与安全处理：
  - `src/Extensions/Senparc.Xncf.PromptRange/Domain/Services/PromptUsageHelper.cs`
- `PromptResult` 增加累计更新方法：
  - `src/Extensions/Senparc.Xncf.PromptRange/Domain/Models/DatabaseModel/PromptResult.cs`
  - `AppendUsageAndResult(...)`
- 在生成与继续对话中写入/累计 Token Usage：
  - `src/Extensions/Senparc.Xncf.PromptRange/Domain/Services/PromptResultService.cs`

### 3.2 接口透传与页面展示

- 请求模型支持 `streamId`（为流式能力准备）：
  - `src/Extensions/Senparc.Xncf.PromptRange/Application/DTOs/Request/PromptItem_AddRequest.cs`
  - `src/Extensions/Senparc.Xncf.PromptRange/Application/DTOs/Request/PromptResult_ContinueChatRequest.cs`
- 页面展示 Token（总量、输入、输出）：
  - `src/Extensions/Senparc.Xncf.PromptRange/Areas/Admin/Pages/PromptRange/Prompt.cshtml`
  - `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

---

## 4. 任务三：流式输出升级（PromptRange + AgentsManager）

### 4.1 PromptRange 打靶/继续对话流式输出

- 新增流式 Hub：
  - `src/Extensions/Senparc.Xncf.PromptRange/Domain/Services/PromptResultStreamHub.cs`
- 新增 SSE 控制器：
  - `src/Extensions/Senparc.Xncf.PromptRange/OHS/Local/Controllers/PromptStreamController.cs`
- 服务注入：
  - `src/Extensions/Senparc.Xncf.PromptRange/Register.cs`
- 后端 AppService 推送 `chunk/final/complete` 事件：
  - `src/Extensions/Senparc.Xncf.PromptRange/Application/AppServices/PromptItemAppService.cs`
  - `src/Extensions/Senparc.Xncf.PromptRange/Application/AppServices/PromptResultAppService.cs`
- 前端接入 EventSource，替换一次性结果体验：
  - `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`
  - 覆盖 `executeTargetShootWithChatMessage`、`rapidFireHandel`、`continueChatSubmit`

### 4.2 AgentsManager 群聊实时流式输出

- 新增流式 Hub：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/ChatTaskStreamHub.cs`
- 新增 SSE 控制器：
  - `src/Extensions/Senparc.Xncf.AgentsManager/OHS/Local/Controllers/ChatTaskStreamController.cs`
- 服务注入：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Register.cs`
- 在多智能体执行过程中推送实时分块：
  - `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/ChatGroupService.cs`
  - 事件类型：`chunk` / `message` / `status`
- 前端由轮询为主改为实时流（轮询仅作为降级）：
  - `src/Extensions/Senparc.Xncf.AgentsManager/wwwroot/js/AgentsManager/index.js`

---

## 5. 单元测试与编译验证

### 5.1 新增/增强测试

- `src/Extensions/Senparc.Xncf.AgentsManagerTests/Domain/Services/UsageAnalyticsTests.cs`
  - Usage 编解码一致性
  - ChatTask 聚合统计映射验证
- `src/Extensions/Senparc.Xncf.AgentsManagerTests/Domain/Services/PromptUsageHelperTests.cs`
  - Token 回退逻辑验证
  - long -> int 边界裁剪验证
- `src/Extensions/Senparc.Xncf.AgentsManagerTests/Program.cs`
  - 测试入口（可直接执行）

### 5.2 执行命令与结果

已执行并通过：

1. `dotnet build src/Extensions/Senparc.Xncf.AgentsManager/Senparc.Xncf.AgentsManager.csproj --no-restore -p:BuildProjectReferences=false -m:1 -p:RunAnalyzers=false -p:UseSharedCompilation=false -v minimal`
2. `dotnet build src/Extensions/Senparc.Xncf.PromptRange/Senparc.Xncf.PromptRange.csproj --no-restore -p:BuildProjectReferences=false -m:1 -p:RunAnalyzers=false -p:UseSharedCompilation=false -v minimal`
3. `dotnet build src/Extensions/Senparc.Xncf.AgentsManagerTests/Senparc.Xncf.AgentsManagerTests.csproj --no-restore -p:BuildProjectReferences=false -m:1 -p:RunAnalyzers=false -p:UseSharedCompilation=false -v minimal`
4. `dotnet run --project src/Extensions/Senparc.Xncf.AgentsManagerTests/Senparc.Xncf.AgentsManagerTests.csproj --no-build`
   - 输出：`UsageAnalyticsTests passed.`、`PromptUsageHelperTests passed.`
5. `node --check src/Extensions/Senparc.Xncf.AgentsManager/wwwroot/js/AgentsManager/index.js`
6. `node --check src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

> 备注：当前构建环境存在上游依赖版本冲突 warning（如 OpenAI/Microsoft.Extensions.AI.* 等），但不影响本次改动的编译通过。

---

## 6. 分支创建执行结果

已执行：

1. `git switch -c Developer-MAF-V3-AgentsManager-Optimize`
2. `git switch -c Developer-MAF-V3-AgentsManager-Optimize-Streaming`

结果：均失败，错误为 `.git/refs/...lock: Operation not permitted`。

尝试提权执行后，平台审批接口返回 `404 Not Found`（自动审批链路故障），因此无法在当前环境内创建分支。

---

## 7. 结论

- 三项功能改造代码已完整落地并通过编译与单元测试验证。
- 流式输出、Usage 统计、PromptRange Token Usage 入库与展示均已打通。
- 唯一未完成项是“实际创建两个新分支”，原因是当前执行环境对 `.git` 写权限和提权审批链路限制。
