# Senparc.Xncf.Sandbox

NCF 独立沙箱编排模块：为用户快速创建/销毁隔离实验环境（学习 Python/C#、短任务执行、可选 JupyterLab）。

> 与 `XncfBuilder` Preview Host **解耦**：Preview 用于模块开发预览（进程级）；Sandbox 用于可丢弃实验环境（容器/Wasm）。

## 官方文档（环境准备以 Docs 为准）

- 中文：https://doc.ncf.pub/zh/NcfPackageSources/xncf/sandbox-environment.html  
- English：https://doc.ncf.pub/NcfPackageSources/xncf/sandbox-environment.html  

**镜像 tag、内部仓库示例、安装命令只维护在 NcfDocs**，模块 UI 仅做检测摘要 + 文档链接，避免版本漂移。  
源码文档路径：`NcfDocs/docs/zh/NcfPackageSources/xncf/sandbox-environment.md`（需发布到 doc.ncf.pub 后线上可见）。

## 状态（2026-08-08）

| 项 | 状态 |
|---|---|
| 模块脚手架 + UID/DB 前缀 | ✅ `BDF12490-AA0B-41B4-ADB3-63155ED95A93` / `Senparc_Sandbox_` |
| Domain Session + 配额/TTL | ✅ |
| Docker Runtime（CLI） | ✅ 一期 |
| Wasm Runtime | ⏳ Stub |
| Function + Admin 面板 | ✅ |
| **环境准备引导页** | ✅ `/Admin/Sandbox/Setup`（检测 Docker + 链到 Docs） |
| NcfDocs 环境准备文档 | ✅ [线上文档](https://doc.ncf.pub/zh/NcfPackageSources/xncf/sandbox-environment.html) |
| 镜像仓库映射配置 | ✅ `SenparcXncfSandbox:Images`（RegistryPrefix / Overrides） |
| 单元测试（ImageResolver） | ✅ |
| Jupyter 访问 / 反向代理 | ✅ 列表使用容器本机映射端口；仍支持 `/sandbox-jupyter/{sessionId}/` 代理 |
| csharp-exec .NET 10 file-based | ✅ `sdk:10.0` + `dotnet run --file main.cs` |
| 持久化 Lab FunctionRender 控制 | ✅ 命令、工作区文件上传/下载/列举 |
| Wasmtime 实装 | ⏳ |

## 架构

```
Areas / Function (SandboxAppService)
        ↓
SandboxOrchestrator（配额、TTL、孤儿回收）
        ↓
ISandboxRuntime
  ├─ DockerSandboxRuntime
  └─ WasmSandboxRuntime (Stub)
```

### 模板键（协议值，勿本地化）

| Key | 说明 | Create 后是否常驻容器 |
|---|---|---|
| `python-exec` | 短任务 Python | 否（登记会话；Exec 时 `docker run --rm`） |
| `csharp-exec` | 短任务 C#（**.NET 10** file-based：`dotnet run --file main.cs`） | 否（同上） |
| `jupyter-python` | JupyterLab 交互（Python；更耗内存） | **是**（Destroy / TTL 停容器） |
| `jupyter-csharp` | JupyterLab 交互（C#；需配置独立镜像） | **是**（Destroy / TTL 停容器） |

「销毁」停止并清理运行环境，保留会话记录用于状态追踪；若存在真实容器（Jupyter 或将来的常驻 Exec worker）则一并删除。
「删除会话记录」仅允许用于已停止、已过期或已清理完成的失败会话，并会永久删除数据库记录。

默认镜像（与 Docs 对齐）：`mcr.microsoft.com/dotnet/sdk:10.0`。更新 tag 时优先改 Docs，再同步代码模板。  
C# 代码可用顶层语句，例如：`Console.WriteLine("hello");`（无需手写完整 Program/csproj）。  
Exec 容器无外网：自动注入离线 `nuget.config` + `PublishAot=false`（避免默认 AOT 去拉 NuGet）。

`jupyter-csharp` 使用 `tools/SandboxImages/JupyterDotnet` 构建的独立镜像，内含 .NET SDK、.NET Interactive
Jupyter Kernel 和构建时预热的常用 NuGet 包；镜像构建完成后需通过 `Images:Overrides:jupyter-csharp` 配置。

### 安全与资源默认

- Docker 标签：`ncf.sandbox=1`，启动扫描孤儿容器
- CPU / memory / pids 限额；Exec 默认无外网
- TTL 强制回收
- **无 Docker 时不降级裸进程**
- JupyterLab：BSD-3-Clause（勿用商标背书）
- Jupyter 列表链接使用 Docker 分配的本机映射端口和 token 直达容器；容器只绑定 `127.0.0.1`
- Jupyter 反向代理仍可通过 `/sandbox-jupyter/{sessionId}/lab` 访问（需管理员登录）；该入口由服务端注入 token

### 持久化 Lab 操作

`jupyter-python` 和 `jupyter-csharp` 是持久化交互式会话。Sandbox 的 FunctionRender
提供以下稳定方法，可由 Admin Function、AI Function Calling 和 NeuCharWorkflow 复用：

- `LabExec`：在容器工作区内执行 `/bin/sh -lc` 命令，默认 30 秒，最长 120 秒，输出有长度上限；
- `LabUploadFile`：使用 Base64 写入工作区内的相对路径，单文件默认最多约 3 MB；
- `LabDownloadFile`：读取工作区文件并返回 Base64，读取大小受上限约束；
- `LabListFiles`：列举工作区文件，可选择递归和返回数量上限。

所有文件操作都拒绝绝对路径、路径穿越和符号链接/重解析点，并且只允许运行中的
Jupyter Lab 会话。`python-exec` / `csharp-exec` 仍然是一次性 `docker run --rm`
任务；`ncf-preview` 仍然只支持固定的 NCF 预览流程，不开放通用 Shell 控制。

AdminChat 仍需开启 Function Invocation，并将 Sandbox 模块关联到会话；Workflow
则通过已有 Function 目录和统一执行服务调用这些方法。命令执行属于高权限容器内操作，
生产部署应继续结合管理员权限、会话关联和 Docker 运行时隔离策略。

## 后台入口

1. **环境准备** `/Admin/Sandbox/Setup`：Docker 检测 + 文档链接  
2. **沙箱面板** `/Admin/Sandbox/Index`：会话列表 / 打开 Notebook（本机映射端口）/ 销毁运行环境 / 删除会话记录
3. Function：创建沙箱 / 列表 / Exec / 销毁运行环境 / 删除会话记录

### Jupyter 代理调试

- 中间件：`SandboxJupyterProxyMiddleware`（HTTP + WebSocket）
- 容器启动参数：`ServerApp.base_url=/sandbox-jupyter/{sessionId}/`
- 未登录访问代理路径会跳转 `/Admin/Login?returnUrl=...`  
- 应用关闭时不会主动删除交互式容器；应用启动会按 Docker 完整容器 ID 校准运行中、已停止和已删除的会话，并清理无对应会话的孤儿容器


## 调试信息

- 工作目录：`%TEMP%/Senparc.Ncf/Sandbox/{sessionId}`
- Orchestrator 每 30s 扫 TTL

## 故障排查：SQL Server 升级 Init 失败（nvarchar(max) 索引）

若出现：

`Column 'SessionId' ... is of a type that is invalid for use as a key column in an index`

原因：早期手工改写的 SqlServer `Init` 把字符串列写成了 `nvarchar(max)`，无法建唯一索引；升级会在建索引时失败，并留下**半成品表**（`Init` 通常尚未写入 MigrationsHistory）。

**处理（当前库）：**

1. 已修复 `Domain/Migrations/SqlServer/20240423143154_Init.cs` 为 `nvarchar(n)`  
2. 在 SQL Server 执行（无业务数据可直接删）：

```sql
IF OBJECT_ID(N'dbo.Senparc_Sandbox_SandboxSession', N'U') IS NOT NULL
    DROP TABLE dbo.Senparc_Sandbox_SandboxSession;
```

3. 重新编译后执行：

```bash
dotnet run -- --database-upgrade
```

> 约定：模块已对外发布后，schema 变更请**新增 migration**，不要再改已成功应用的 Init。本次属于 Init 从未成功应用，故直接修正 Init。

## 镜像配置（appsettings）

```json
"SenparcXncfSandbox": {
  "Docker": {
    // docker run 在本地没有镜像时会同步下载；默认 900 秒，可按需调整为 60-3600
    "InteractiveCreateTimeoutSeconds": 900
  },
  "Images": {
    "RegistryPrefix": "",
    "Overrides": {
      // 国内网络临时代理示例（第三方地址，稳定性不保证；不是清华 TUNA 官方镜像）
      // "jupyter-python": "quay.dockerproxy.net/jupyter/minimal-notebook:latest",
      // "jupyter-csharp": "ncf-jupyter-dotnet:10.0"
    }
  }
}
```

细节与推荐镜像清单：[环境准备指南](https://doc.ncf.pub/zh/NcfPackageSources/xncf/sandbox-environment.html)

## 你需要继续做的事

1. 若尚未处理：清理半成品表并 `--database-upgrade`（见上一节）  
2. **重启站点**（已改 appsettings / 模块代码）  
3. 打开 **环境准备**：确认 Docker 检测通过，文档链接可打开  
4. ~~Function 创建 `python-exec` 并 Exec 冒烟~~ ✅（ExitCode 0）  
5. 下一步可选：A) Wasmtime  B) Jupyter 代理/鉴权  



## 版本

- `0.1.0-preview1`：创世骨架  
- 同日补充：环境准备页 + Docs 链接 + 镜像仓库映射配置  

