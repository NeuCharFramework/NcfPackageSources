# Senparc.Xncf.AgentsManager 升级报告（AutoGen -> Senparc.AI.AgentKernel / MAF）

## 任务目标
- 将 `Senparc.Xncf.AgentsManager` 从 AutoGen 多智能体实现迁移到 `Senparc.AI.AgentKernel`（基于 Microsoft Agent Framework）。
- 修复升级过程中的依赖冲突，确保项目可编译通过。

## 关键代码改造

### 1. 多智能体主流程迁移到 MAF
- 文件：`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/ChatGroupService.cs`
- 核心变化：
  - 使用 `MagenticWorkflowBuilder` 构建多智能体工作流。
  - 使用 `InProcessExecution.RunStreamingAsync(...)` 进行流式执行与事件处理。
  - 引入统一消息落库与 webhook 发送收口（`SaveAgentMessageAsync`）。
  - 增强单智能体回退路径（`RunSingleAgentAsync`）。
  - 保留并完善 Function Calling + MCP 工具注入（`BuildAgentToolsAsync`、`BuildMcpToolsAsync`）。
  - 增强任务状态管理和异常收尾逻辑（缓存清理、任务状态回收）。

### 2. 旧 AutoGen 中间件适配清理
- 文件：`src/Extensions/Senparc.Xncf.AgentsManager/ACL/AgentTemplatePrintMessageMiddleware.cs`
  - 从旧 `Action<IAgent, IMessage, ...>` 回调改为 `SendWechatMessageAsync(...)`。
  - 去除 AutoGen 类型依赖，保留消息推送能力。
- 删除文件：
  - `src/Extensions/Senparc.Xncf.AgentsManager/ACL/PrintWechatMessageMiddlewareExtension.cs`
  - `src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/MyRolePlayOrchestrator.cs`

### 3. AgentsManager 依赖项升级
- 文件：`src/Extensions/Senparc.Xncf.AgentsManager/Senparc.Xncf.AgentsManager.csproj`
  - 新增：
    - `Senparc.AI.AgentKernel`（本地构建输出引用）
    - `Microsoft.Agents.AI.Workflows`（1.8.0）
  - `XncfBuilder` 项目引用追加：
    - `AdditionalProperties="UseCheckedInXncfBuilderGeneratedCode=true"`
  - 目的：避免升级期内 source generator 触发导致的无关构建阻塞。

## 关联项目依赖对齐（解决 1.8.0 版本链要求）
- 文件：
  - `src/Extensions/Senparc.Xncf.AIKernel/Senparc.Xncf.AIKernel.csproj`
  - `src/Extensions/Senparc.Xncf.PromptRange/Senparc.Xncf.PromptRange.csproj`
  - `src/Extensions/Senparc.Xncf.MCP/Senparc.Xncf.MCP.csproj`
  - `src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder.csproj`
- 统一新增引用：
  - `Senparc.AI.AgentKernel`
  - `Microsoft.Agents.AI` 1.8.0
  - `Microsoft.Agents.AI.Abstractions` 1.8.0

## 构建验证
- 执行命令（均为 `--no-restore -p:BuildInParallel=false`）：
  - `dotnet build src/Extensions/Senparc.Xncf.AIKernel/Senparc.Xncf.AIKernel.csproj`
  - `dotnet build src/Extensions/Senparc.Xncf.PromptRange/Senparc.Xncf.PromptRange.csproj`
  - `dotnet build src/Extensions/Senparc.Xncf.MCP/Senparc.Xncf.MCP.csproj`
  - `dotnet build src/Extensions/Senparc.Xncf.AgentsManager/Senparc.Xncf.AgentsManager.csproj`
- 结果：全部构建成功，`0 error`。
- 备注：当前仍有若干 `MSB3277` 版本冲突 warning（OpenAI / OllamaSharp / System.ClientModel / Microsoft.Extensions.AI.OpenAI），不影响本次编译通过。

## 分支创建说明
- 目标分支：`Developer-MAF-V3-AgentsManager`
- 当前受限：工作环境对 `.git` 写操作受限，且提权审批接口异常（404），导致无法在本次自动流程中完成分支创建。
- 不影响：代码升级与编译验证已在当前工作树完成。

