# AIKernel local fine-tuning worker / 本地微调 Worker

This companion service runs **one offline training child at a time**, independently of the NCF web process. CPU/CUDA use Transformers + PEFT; Apple Silicon uses native MLX. NCF provides the AdminOnly management UI and does not execute arbitrary scripts. Training exports adapters, not a deployed inference endpoint.

本服务独立于 NCF Web 进程，同时仅运行一个离线训练子进程。CPU/CUDA 使用 Transformers + PEFT；Apple Silicon 使用原生 MLX。NCF 提供管理员界面，不执行任意脚本。训练导出 adapter，不会自动部署推理服务。

Worker **1.1** adds bounded catalog paging and a persistent SQLite `storeId`. `/jobs/page` and `/datasets/page` return `items`, `total`, `offset`, `limit` (1..200 per page); `/datasets/<id>` returns selected dataset metadata. The old array endpoints retain their latest-100 behavior for compatibility. The console uses paging for complete history and clears cached resource IDs when storage identity changes. Normal restarts retain the identity.

Worker **1.1** 新增有边界的目录分页与 SQLite 持久化 `storeId`。`/jobs/page`、`/datasets/page` 返回 `items`、`total`、`offset`、`limit`（每页 1..200 条），`/datasets/<id>` 返回已选数据集元数据。旧数组接口保留最近 100 条的兼容行为。管理界面以分页浏览完整历史，在存储身份变化时清空旧资源 ID；正常重启保留身份。

## Requirements / 环境要求

- CPU: Docker Desktop (Mac/Windows) or Docker Engine (Linux), at least 6 GiB assigned for the supplied profile. Tiny models only for practical CPU verification.
- CUDA: Linux NVIDIA host, compatible driver for CUDA 12.8/PyTorch 2.8, NVIDIA Container Toolkit and sufficient GPU memory. The supplied profile grants one GPU, never privileged mode.
- MLX: native macOS Apple Silicon, supported macOS/Metal and Python 3.12/3.13. **Linux containers cannot access Apple Metal.** Use a restricted macOS service account; Python virtual environments are dependency isolation, not a security sandbox.
- Approved, fully provisioned local model/tokenizer directory: `models/<model-id>/config.json`, `tokenizer_config.json`, tokenizer files and `model*.safetensors` (including all shards/index when applicable). No symlinks, pickle/bin weights, remote code or job-time downloads.
- Architecture allowlist: CPU/CUDA `gpt2`, `llama`, `mistral`, `qwen2`, `qwen3`, `phi3`, `gemma`, `gemma2`, `gemma3_text`, `opt`; MLX the supported subset in [models.py](worker/models.py). Preflight is authoritative.

CPU 需 Docker 与足够内存，适合小模型验证；CUDA 需 Linux/NVIDIA 驱动及 Container Toolkit；MLX 需原生 Apple Silicon 与 Metal。模型需经过批准并完整准备为本地 safetensors；拒绝符号链接、pickle 权重、remote code 与训练期间下载。模型目录名是 UI 使用的 model ID，预检决定实际兼容性。

Provision weights on a separate, controlled download machine after reviewing the license and pinning a model revision. For example, using the Hugging Face CLI in that machine's own environment:

在独立受控下载机审核许可证、固定模型 revision 后准备权重，例如使用该机器环境中的 Hugging Face CLI：

```bash
hf download APPROVED_ORG/MODEL --revision PINNED_COMMIT \
  --local-dir ./approved-models/model-id \
  --include '*.safetensors' '*.json' '*.model' '*.txt' '*.jinja' '*.tiktoken'
```

Replace placeholders with approved values, verify hashes and completeness, then transfer the directory into the read-only model root. Do not copy a symlink-based cache or provider tokens into the training volume. Custom-code models are intentionally unsupported; preflight can reject an approved download if its architecture/tokenizer still requires remote code.

占位符需替换为批准值，检查 hash 和完整性后再转入只读模型根目录。不要复制符号链接缓存或 provider token 到训练卷。需要自定义代码的模型不受支持，已审核下载仍可能因架构/tokenizer 需 remote code 而被预检拒绝。

## CPU/CUDA containers / CPU 与 CUDA 容器初始化

From this directory / 在本目录执行：

```bash
export NCF_WORKER_KEY="$(python3 -c 'import secrets; print(secrets.token_hex(32))')"
# Supply the same secret to NCF securely; do not print or commit it.
export NCF_MODELS_DIR="/absolute/path/to/approved-models"
docker compose --profile cpu up -d --build cpu gateway
```

For NVIDIA Linux / NVIDIA Linux 使用：

```bash
docker compose --profile cuda up -d --build cuda gateway
```

**Choose one profile only.** The profiles use separate persistent data volumes. The gateway publishes `127.0.0.1:8091` and forwards only to the fixed worker API. Worker containers use an **internal network without Internet egress**. Directly publishing an internal-only container's port does not provide reliable Docker Desktop access; the restricted API gateway bridges the ingress/internal networks without granting the worker an external route or Docker socket.

只启用一个 profile；两者使用独立数据卷。入口容器将 `127.0.0.1:8091` 转发到固定 Worker API。Worker 仅加入无外网的内部网络。不能假定内部网络容器直接映射端口在 Docker Desktop 上可用；入口转发容器解决可达性，但不会把外网路由或 Docker socket 交给 Worker。

```bash
curl --fail -H "X-NCF-Worker-Key: $NCF_WORKER_KEY" http://127.0.0.1:8091/health
docker compose --profile cpu logs --tail 100 cpu gateway
```

The worker runs as UID 10001, with read-only root/model mounts, dropped capabilities, no-new-privileges, PID/CPU/RAM limits and bounded tmpfs. Do not add privileged mode, a Docker socket, general host mounts or an outbound training network. Build-time package downloads occur before isolation; job execution is offline.

Worker 以 UID 10001 运行，根文件系统及模型只读，去除 capabilities，设置 no-new-privileges、PID/CPU/内存限制及 tmpfs 上限。不要增加特权、Docker socket、宽泛宿主挂载或训练外网。构建时下载依赖，运行时离线。

Build and review images in your controlled environment, scan them, lock transitive dependencies and deploy approved immutable digests. Provided Dockerfiles pin PyTorch/backend release ranges but are not a fully reproducible supply-chain lock. Do not mistake the locally verified image for a published production image.

请在受控环境构建、审核、扫描镜像，锁定传递依赖，并使用批准的不可变 digest 部署。当前依赖范围并非完整供应链锁文件，本地已验证镜像不等于已发布的生产镜像。

## Native Apple Silicon / 原生苹果芯片初始化

```bash
python3 -m venv .venv
.venv/bin/python -m pip install -r requirements-mlx.txt
export NCF_WORKER_KEY="$(python3 -c 'import secrets; print(secrets.token_hex(32))')"
export NCF_MODELS_ROOT="/absolute/path/to/approved-models"
export NCF_DATA_ROOT="/absolute/path/to/private-training-data"
.venv/bin/python -m worker
```

Native workers bind **127.0.0.1** by default. Never run as root; keep model mounts/read permissions and writable data limited to the service account. Enforce native host network/resource policies separately. For native CPU use `requirements-cpu.txt` plus `torch==2.8.0`. MLX does not require PyTorch for training; the optional fixture generator uses it only to generate test weights.

原生 Worker 默认仅监听 **127.0.0.1**。不要以 root 运行；限制模型读取及数据写入权限，独立落实宿主网络与资源策略。原生 CPU 安装 `requirements-cpu.txt` 及 `torch==2.8.0`。MLX 训练不需要 PyTorch，测试模型生成器才需要。

## Connect NCF / 连接 NCF

Set these on the NCF host, sharing the secret without exposing it to the browser:

在 NCF 宿主配置下列环境变量，密钥与 Worker 一致，不暴露给浏览器：

```bash
export SenparcXncfAIKernel__FineTuning__Enabled=true
export SenparcXncfAIKernel__FineTuning__AllowedHosts__0=127.0.0.1
export SenparcXncfAIKernel__FineTuning__WorkerApiKeys__cpu_lab="$NCF_WORKER_KEY"
```

Set these before starting NCF (restart it after environment changes). In the UI, add a Worker profile with alias `cpu_lab`, endpoint `http://127.0.0.1:8091`, timeout 180 seconds and enabled state. The alias must match `WorkerApiKeys`. Endpoint, timeout and note are database settings; the global switch, host allowlist and secrets are infrastructure settings. The deprecated single-worker `WorkerEndpoint`/`WorkerApiKey` options are not used by database profiles. Upgrade an existing AIKernel installation through module management to apply its Worker-table migrations first.

这些环境变量需在 NCF 启动前设置（修改后重启 NCF）。在 UI 中添加 Worker 配置：别名 `cpu_lab`、端点 `http://127.0.0.1:8091`、超时 180 秒、启用状态，别名必须与 `WorkerApiKeys` 对应。端点、超时和备注保存在数据库；全局开关、主机白名单和密钥属于基础设施配置。废弃的单 Worker `WorkerEndpoint`/`WorkerApiKey` 不用于数据库配置。已有 AIKernel 安装应先通过模块管理升级，应用 Worker 表迁移。

Use a private HTTPS endpoint for cross-host connections. `127.0.0.1` works only in the same network namespace. If NCF is containerized, attach its administration service to the gateway's ingress network and use `http://gateway:8080`, not the NCF container's own loopback. Keep the training worker off the external network.

跨主机使用私网 HTTPS；同一命名空间才使用 loopback。NCF 容器可加入入口网络并访问 `http://gateway:8080`，此时将 `gateway` 加入 NCF 的 `AllowedHosts`，不要误用自己的 loopback；训练 Worker 不加入外部网络。

Use the default-expanded in-page tutorial and the dataset example download to prepare the first smoke test. Each example has four distinct rows and only demonstrates format; it is not a production dataset. The browser checks line structure, 2..10000 rows and the 2 MiB UTF-8 limit, while the Worker performs authoritative validation, including duplicate properties and held-out overlap.

使用默认展开的页内教程及数据样例下载准备首次冒烟测试。每种样例含四条不同记录，仅演示格式，不是生产数据集。浏览器检查行结构、2..10000 行及 2 MiB UTF-8 上限；Worker 仍进行最终校验，包括重复字段与验证集重叠检查。

Open **AIKernel → Local fine-tuning / 本地模型微调**. Upload UTF-8 JSONL (2..10000 rows, <=2 MiB) of `messages` or `prompt`/`completion`. Messages require the approved tokenizer's chat template. Explicit validation must not overlap training rows. If omitted, the worker deterministically holds out at least one distinct row (approximately 20%); it rejects fewer than two distinct rows.

打开管理页，上传 2..10000 行、最多 2 MiB 的 UTF-8 JSONL（messages 或 prompt/completion）。messages 要求批准的 tokenizer chat template；显式验证集不能与训练记录重叠。不选验证集时，自动按 seed 留出约 20%、至少一条不同记录，少于两条不同记录则拒绝。

The implemented objective is **causal next-token loss on all non-padding tokens**, including prompt/user tokens, not assistant-only masked SFT. Both backends use AdamW, linear warmup/decay and gradient clipping. Right truncation is warned; inspect data length so answers are not lost. A positive `maxSteps` overrides epochs; zero uses epochs.

训练目标是**所有非 padding token 的因果下一 token loss**，包括 prompt/user，不是仅 assistant 监督的 mask。两种后端使用 AdamW、线性预热/衰减与梯度裁剪。右截断会发出警告，需检查长度避免丢失答案。正数 maxSteps 优先于 epochs，0 才按 epochs。

## Budgets, persistence and artifacts / 预算、持久化及产物

| Worker environment | Default | Meaning / 含义 |
| --- | --- | --- |
| `NCF_QUEUE_LIMIT` | 8 | Queued jobs / 排队任务上限 |
| `NCF_MAX_DURATION_MINUTES` | 1440 | Operator ceiling, distinct from request budget / 运维时长上限 |
| `NCF_MAX_JOB_BYTES` | 21474836480 | Output budget, checked periodically / 输出预算，定期检查 |
| `NCF_MIN_FREE_BYTES` | 1073741824 | Disk reserve; archive requires extra space / 磁盘预留，归档需额外空间 |
| `NCF_EVENT_LIMIT` | 10000 | Retained events per job / 每任务保留事件数 |
| `NCF_MAX_DATASETS`, `NCF_MAX_JOBS` | 1000 each | Store retention admission caps / 存储记录接收上限 |
| `NCF_CANCEL_GRACE_SECONDS` | 30 | Graceful checkpoint stop before tree kill / 优雅停止后进程树强杀 |
| `NCF_TELEMETRY_SECONDS` | 2 | Resource sampling interval / 资源采样间隔 |
| `NCF_PREFLIGHT_SECONDS` | 120 | Offline model preflight timeout / 离线模型预检超时 |
| `NCF_TORCH_THREADS` | 2 | CPU training threads / CPU 训练线程 |

A filesystem quota remains necessary: disk checks are periodic, not a hard synchronous quota. Configure environment overrides in Compose for your deployment. CPU/RAM/PID budgets are container/host settings; no per-job RAM guarantee is promised.

磁盘定期检查不是硬实时配额，生产仍需文件系统 quota。Compose 中需显式传入环境覆盖。CPU/内存/PID 由容器/宿主约束，不承诺每任务内存一定足够。

SQLite WAL, canonical datasets and outputs persist together in `/data`. One process owns the store via an OS lock. Restarted active jobs become `Interrupted`; there is **no resume/pause API**. Cancellation saves at safe boundaries when possible; forced termination retains only completed checkpoints. Last two checkpoints are kept; older events are pruned with an explicit truncated-history indicator.

SQLite WAL、数据与输出共同持久化；OS 锁限制一个 Worker。重启后活跃任务标为 Interrupted，**不提供 resume/pause API**。取消尽可能在安全边界保存；强杀仅保留已完成 checkpoint。保留最后两个 checkpoint，历史事件裁剪会明确提示。

Downloads include `training-export.zip`, `manifest.json` and `metrics.jsonl`. Archives contain adapter/tokenizer files, completed checkpoints, optimizer/RNG state and provenance. Manifest records base safetensors hashes, data hashes, parameters, dependency versions, evaluation strategy and loss convention. Optimizer state is worker-produced diagnostic data, not accepted as an input model. Adapters require the same base/format; MLX QLoRA from unquantized input additionally requires recreating the exported 4-bit affine quantization (group size 64) before loading the adapter. Evaluate and deploy separately; never register this training API as an AIModel inference endpoint.

下载包包含 adapter/tokenizer、已完成 checkpoint、优化器/RNG 与追溯信息。Manifest 记录权重/数据 hash、参数、依赖版本、验证策略及 loss 约定。优化器状态仅用于诊断，不接收为模型输入。MLX 从未量化基座进行 QLoRA 时，加载 adapter 前应按导出的 4-bit affine/group size 64 重新量化基座。评估与部署独立完成，训练 API 不能注册为推理 API。

Resources are global trusted-operator assets, not tenant-isolated. Back up the stopped store/volume consistently, retain according to policy, and rotate to a new store when admission caps are reached. No online deletion or automated model promotion is provided.

资源属于可信全局运维人员，不提供租户隔离。停止 Worker 后一致备份，按策略保留，到达接收上限时归档并切换存储；不提供在线删除或自动发布。

## Verification / 验证

```bash
python3 -m venv .venv-test
.venv-test/bin/python -m pip install -r requirements-test.txt
.venv-test/bin/python -m unittest discover -s tests -v
node --test tests/test_ui.cjs
```

Generate a random tiny model **outside production models**, using the CPU image or a test environment with PyTorch. This downloads no model/data:

在非生产模型目录生成随机小模型，不下载模型或数据：

```bash
python -m tests.make_tiny_model /absolute/path/to/test-models
# Start the test worker against that directory, then:
python -m tests.smoke --endpoint http://127.0.0.1:8091 --backend cpu --model tiny-gpt2 \
  --models-root /absolute/path/to/test-models
```

An isolated native CPU run can also exercise the named-profile .NET client against the same temporary authenticated Worker:

隔离的原生 CPU 测试也可同时验证命名配置的 .NET 客户端：

```bash
python -m tests.run_native_smoke --backend cpu \
  --dotnet-project ../../src/Extensions/Senparc.Xncf.AIKernel.Tests/Senparc.Xncf.AIKernel.Tests.csproj
```

Install `requirements-cpu.txt` in the test environment and restore the .NET project first. The runner verifies actual B-matrix updates, base hashes and adapter inference reload, not merely exported file existence. If `--models-root` is omitted for a standalone smoke run, `adapterReloadVerified` explicitly reports false.

测试环境需安装 `requirements-cpu.txt`，首次运行先恢复 .NET 项目依赖。测试核对真实 B 矩阵更新、基座 hash 和 adapter 重载推理，不只检查导出文件是否存在。独立 smoke 命令省略 `--models-root` 时，`adapterReloadVerified` 会明确为 false。

Native Apple smoke requires `requirements-mlx.txt` plus `torch==2.8.0` for fixture generation; temporary worker/model/data are cleaned automatically:

苹果原生测试需安装 MLX 依赖及仅用于生成权重的 PyTorch；测试自动清理临时 Worker/模型/数据：

```bash
python -m tests.run_native_smoke --backend mlx
```

Verified on 2026-10-07: actual native CPU LoRA and native Apple Metal LoRA/quantized LoRA, each with six optimizer steps, held-out loss, artifacts, running/queued cancellation and successful adapter reload with changed inference output. MLX quantized reload recreates the exported quantization configuration. Named-profile .NET live integration and SQLite profile persistence were also exercised. These tiny random-model tests verify infrastructure, **not model quality, large-model capacity or production container deployment**. CUDA must be validated on an NVIDIA Linux host.

2026-10-07 已真实验证原生 CPU LoRA 与原生 Apple Metal LoRA/量化 LoRA：各完成六步优化器更新、独立验证、产物下载、运行/排队取消，以及 adapter 重载后的推理输出变化。MLX 量化重载按导出配置重建基座；同时执行了命名配置的 .NET 实时集成及 SQLite 配置持久化。这不代表业务质量、大模型容量或正式容器部署已验收；CUDA 需在 NVIDIA Linux 宿主验证。
