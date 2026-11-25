# GitHub NuGet Trusted Publishing 配置与切换指南

## 目的

本仓库通过 [GitHub Actions 工作流](../.github/workflows/publish-nuget.yml) 使用 NuGet Trusted Publishing（OIDC）发布 `Senparc.Ncf.*` 和 `Senparc.Xncf.*` 包。

现有 [Azure DevOps 配置](../azure-pipelines.yml) 暂不修改和删除。首次验证期间，GitHub 自动发布默认关闭；验证通过并完成切换后，才应启用 GitHub 的 `master` 自动发布。

新流程不保存 nuget.org 长期 API key：

1. GitHub Actions 申请短期 OIDC token。
2. `NuGet/login` 将 OIDC token 换取有效期约 1 小时的临时 NuGet API key。
3. 工作流使用临时 key 推送包。
4. 临时 key 由 NuGet 官方 action 作为步骤输出传递，不写入仓库。

## 工作流行为

工作流文件名是：

```text
.github/workflows/publish-nuget.yml
```

触发和执行逻辑：

| 事件 | 构建/打包 | 发布到 nuget.org |
| --- | --- | --- |
| Pull Request 到 `master` | 是 | 否 |
| Push 到 `master` | 是 | 仅当仓库变量 `ENABLE_GITHUB_NUGET_PUBLISH=true` |
| 手动 `workflow_dispatch` | 是 | 仅当运行时勾选 `publish=true` |

发布作业使用 GitHub Environment `nuget-production`。建议为该环境配置 `master` 部署分支限制和 Required reviewers。发布作业之间使用同一个并发组，避免多个版本同时发布。

打包行为与现有 ADO 发布保持一致：

- 构建 `src/NcfPackageSources.sln` 的 Release 配置。
- 统一收集构建生成的 `.nupkg`。
- 提交前置校验 5 个必须生成的抽象包。
- 只推送文件名以 `Senparc.Ncf.` 或 `Senparc.Xncf.` 开头的 `.nupkg`。
- 使用 `--skip-duplicate`，已存在的相同 PackageId + Version 不会导致流水线失败。
- 与现有 ADO 一样，以 `.nupkg` 作为推送入口；`dotnet nuget push` 会按 CLI 默认规则处理同目录匹配的 symbol package。

## 必须完成的仓库与 NuGet 设置

以下操作需要 GitHub 仓库管理员和 nuget.org 包所有者权限，无法仅通过提交代码完成。

### 1. 在 GitHub 创建受保护环境

进入 GitHub 仓库：

1. 打开 `Settings` → `Environments`。
2. 点击 `New environment`。
3. 环境名称严格填写 `nuget-production`。
4. 建议启用 `Required reviewers`，至少指定负责发布的维护者。
5. 将 `Deployment branches and tags` 限制为 `master`。如果希望允许从其他受控分支手动验证，请只加入明确的发布分支，不要使用全部分支。

环境名称必须同时与工作流和 NuGet Trusted Publishing policy 完全一致。

### 2. 保存 NuGet 用户名

在 GitHub 中进入：

```text
Settings → Environments → nuget-production → Add environment secret
```

新增 secret：

| 名称 | 值 |
| --- | --- |
| `NUGET_USER` | nuget.org 账户的 profile name / username |

注意：

- 使用 nuget.org 页面显示的用户名，不是登录邮箱。
- 该用户需要是即将创建的 Trusted Publishing policy 的所有者，或有权选择对应的 NuGet organization owner。
- `NUGET_USER` 是标识，不是 API key。不要把旧的 `NUGET_API_KEY` 或任何长期 NuGet key 添加到 GitHub。

### 3. 在 nuget.org 创建 Trusted Publishing policy

登录 <https://www.nuget.org/>：

1. 点击当前用户名，打开 Trusted Publishing 页面。
2. 选择 `Add new policy` 或对应的新建入口。
3. Policy owner 选择实际拥有这些 `Senparc.*` 包的用户或 organization。
4. 填写 GitHub policy：

| 字段 | 值 |
| --- | --- |
| Repository Owner | `NeuCharFramework` |
| Repository | `NcfPackageSources` |
| Workflow File | `publish-nuget.yml` |
| Environment | `nuget-production` |

Workflow File 只填写文件名，不包含 `.github/workflows/` 路径。Repository Owner、Repository、Environment 必须和 GitHub 的实际标识一致。

5. 在 policy scopes 中至少覆盖：

```text
Senparc.Ncf.*
Senparc.Xncf.*
```

如果 NuGet 页面使用具体 package ID glob，配置和项目实际发布范围一致，并至少允许“发布新版本”。首次发布新 PackageId 时还需要允许“发布新包”。不要把 scope 扩大到无关包。

6. 保存 policy。

如果仓库为 private，NuGet 可能将 policy 临时标记为活跃 7 天。首次成功发布会把 GitHub repository/owner ID 固化到 policy；因此应在 7 天有效期内完成一次实际发布。如果只测试已经存在的版本，仍应检查 NuGet 页面确认 policy 已完成永久激活。

## 首次验证

首次验证时不要设置 `ENABLE_GITHUB_NUGET_PUBLISH=true`，以免 `master` push 同时触发 GitHub 和 ADO 发布。

1. 合并或推送本次工作流变更。
2. 在 GitHub `Actions` 中打开 `Build and publish NuGet packages`。
3. 先确认 `pull_request` 或 `push` 触发的构建作业通过：
   - .NET 10 SDK 安装成功。
   - Release 构建完成。
   - 5 个必需抽象包校验通过。
   - `nuget-packages-*` artifact 可以下载。
4. 为了确认 OIDC 真正具备发布权限，最好准备一个尚未存在于 nuget.org 的正式或预发布版本。验证前暂时将 ADO pipeline 暂停；保留 `azure-pipelines.yml` 文件和所有 ADO 配置，不做删除。
5. 点击 `Run workflow`，选择目标分支 `master`，勾选 `publish=true`。
6. 如果启用了 Required reviewers，批准 `nuget-production` deployment。
7. 检查 `NuGet login with GitHub OIDC` 和 `Push NuGet packages`：
   - 登录步骤成功代表 OIDC policy、用户名、仓库、工作流文件和环境均匹配。
   - 推送步骤成功后，在 nuget.org 打开新版本并确认内容。
   - `--skip-duplicate` 的旧版本只会被识别为已存在。
8. 回到 nuget.org Trusted Publishing 页面，确认 policy 处于 active 状态，且不存在 pending/expired/ownership warning。

如果需要先做不发布验证，可手动运行工作流但不勾选 `publish`。这种方式只能验证打包，不能验证 NuGet policy。

## 切换为 GitHub 自动发布

只有完成上述首次真实发布并核验 package owner 后，再执行切换：

1. 在 Azure DevOps 中暂停或禁用旧 pipeline 的自动运行。先不要删除 `azure-pipelines.yml`，也不要删除 ADO service connection / variable group，确保可以快速回滚。
2. 在 GitHub 仓库进入：

```text
Settings → Secrets and variables → Actions → Variables
```

3. 新增 repository variable：

| 名称 | 值 |
| --- | --- |
| `ENABLE_GITHUB_NUGET_PUBLISH` | `true` |

4. 之后每次 push 到 `master` 都会通过 GitHub Trusted Publishing 发布。
5. 至少观察一次真实 `master` push 的完整流水线，包括 environment approval、OIDC login 和 package push。

不要在 GitHub 自动发布启用时保留 ADO 自动发布。两者都使用 `--skip-duplicate`，虽然通常不会重复写入同一版本，但会造成双重构建、竞态和排查困难。

## 验证完成后的后续清理

建议连续至少一个发布周期稳定运行后，再清理 ADO：

- 从仓库删除 `azure-pipelines.yml`。
- 删除 ADO variable group `Nuget_Key` 中不再使用的长期 `NUGET_API_KEY`。
- 在 nuget.org 撤销已经不再需要的旧 API key。
- 如 ADO pipeline 还有其他用途，拆分其任务后再删除，不要误删非发布 CI。

完成清理时应确认整个仓库不再引用 `NUGET_API_KEY` 作为持久化密钥；GitHub 工作流中同名变量只来自 `NuGet/login` 的短期步骤输出。

## 回滚

如果 GitHub 自动发布出现故障：

1. 将 `ENABLE_GITHUB_NUGET_PUBLISH` 改为 `false` 或删除该变量。
2. 在 Azure DevOps 重新启用原 pipeline。
3. 修复 GitHub OIDC、environment 或 package scope。
4. 修复后重新手动运行 GitHub 工作流，并再次完成真实发布验证。
5. 验证通过后重复切换步骤。

已存在的 nuget.org 包版本不可覆盖；修复后通常需要提高 PackageId 对应的版本号。

## 常见故障

### `NuGet/login` 报 policy 不匹配

逐项核对以下大小写和名称：

- GitHub owner 是否为 `NeuCharFramework`。
- repository 是否为 `NcfPackageSources`。
- policy Workflow File 是否仅为 `publish-nuget.yml`。
- policy Environment 是否为 `nuget-production`。
- NuGet 工作流是否确实运行在 `nuget-production` job。
- `NUGET_USER` 是否是 profile name，而非 email。

### Environment secret 不可用

`NUGET_USER` 必须存放在 `nuget-production` environment 或仓库级允许该 environment 访问的位置。若 secret 为空，NuGet login 会失败。Environment 审批通过前，作业不会获得 environment secret。

### package scope 拒绝推送

在 Trusted Publishing policy scopes 中同时覆盖 `Senparc.Ncf.*` 和 `Senparc.Xncf.*`，并允许发布新包和新版本。Policy owner 必须实际拥有目标 package。

### 发布作业没有运行

- 手动运行时必须勾选 `publish=true`。
- 自动运行时 `ENABLE_GITHUB_NUGET_PUBLISH` 必须是 repository variable，且值为字符串 `true`，不是 GitHub Secret。
- 自动发布只响应 `master` push。

### 包已存在

nuget.org 不允许覆盖同一 PackageId + Version。修改对应 `.csproj` 的版本，并更新必要的 `PackageReleaseNotes` 后重新运行。

## 官方参考

- [NuGet Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
- [NuGet/login](https://github.com/NuGet/login)
- [GitHub Actions environments](https://docs.github.com/actions/concepts/security/deployment-environments)
- [GitHub Actions concurrency](https://docs.github.com/actions/writing-workflows/choosing-when-your-workflow-runs/control-the-concurrency-of-workflows-and-jobs)
