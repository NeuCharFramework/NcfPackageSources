# 管理员登录安全全面评估报告

> 日期：2026-11-08
> 评估对象：`Senparc.Areas.Admin` 管理员登录体系（分支 `Developer-MAF-V3-Spark-FineTuning-LoginHardening`）
> 评估方法：源码走查（登录全链路）+ 单元测试 + 攻击面分析
> 配套文档：[Login-Security-Hardening.md](./Login-Security-Hardening.md)（加固实施记录）

---

## 1. 评估范围

登录全链路上所有可被未认证访问者触达的端点与机制：

| 端点/机制 | 位置 | 认证要求 |
| --- | --- | --- |
| `POST /Admin/Login?handler=Login` | `Login.cshtml.cs OnPostLoginAsync` | 匿名 |
| `GET /Admin/Login?handler=SlideCaptcha` | `OnGetSlideCaptcha` | 匿名 |
| `GET /Admin/Login?handler=Captcha` | `OnGetCaptcha`（文本验证码） | 匿名 |
| `GET /Admin/Login?handler=CaptchaRequired` | `OnGetCaptchaRequired`（探测端点） | 匿名 |
| 账号阶梯锁定 | `AdminUserInfoService.TryLoginAsync`（缓存锁内计数） | — |
| 多租户解析 | `TenantMiddleware`（`Senparc.Xncf.Tenant`） | 匿名可触发 |
| JWT 签发 | `AdminUserInfoService.GenerateToken*` | 登录成功后 |

## 2. 现有防线盘点（五层）

```
匿名请求
  │
  ▼
[L5 租户解析 fail-closed]   TenantKey 无法匹配 → 403，禁止静默降级默认租户
  │
  ▼
[L1 IP 滑动窗口限流]        login 20 次/10min/IP；captcha 30 次/10min/IP（两个验证码端点均已接入）
  │                          超限 → 429 + Retry-After
  ▼
[L4 防枚举]                 不存在用户 ≡ 密码错误：同文案、同响应结构；
  │                          CaptchaRequired 探测端点统一返回 false
  ▼
[L2 人机校验]               连续 2 次失败后强制人机验证：
  │   首选：自研滑块（纯 C# + SVG，零第三方依赖）
  │     · 程序化随机场景（渐变+几何+噪点+拼图缺口），每张不同，防模板预采集
  │     · 答案一次性（CO2NET 缓存，5 分钟，校验即失效）
  │     · 行为轨迹校验：点数≥10、时长≥200ms、单步≤45px、
  │       上报 Percent 与轨迹反推漂移≤2、位置容差 4%
  │   兜底：纯 SVG 文本验证码（一次性、2 分钟），滑块生成异常自动降级
  ▼
[L3 账号阶梯锁定]           5 次→5 分钟；7 次→30 分钟；10 次→2 小时；20+ 次→失败数×2 小时
                             失败计数 24 小时有效；缓存锁（LoginLock-Admin）防并发漂移
  ▼
 密码校验（SHA512+盐）→ JWT 签发（有效期 AdminAuthConfig 可配置）
```

## 3. 强度评估（按攻击者视角）

### 3.1 单账号暴力破解 — 已有效遏制
- 2 次失败即进入人机验证；5 次即锁定 5 分钟，之后指数级加重；
- 滑块需同时破解"缺口定位 + 伪造符合 5 项行为约束的轨迹"，且 Token 一次性；
- 综合成本：单账号每分钟至多约 4 次有效尝试（限流+锁定），且每次需过人机验证。
- **结论：离线字典/在线爆破均不经济。** ✅

### 3.2 撞库（用户名列表 × 弱密码）— 成本显著抬高，仍有残余风险
- 锁定按用户名，撞库换用户名可绕过单账号锁定；
- 但每个新用户名前 2 次可裸试（无限流内）、之后需过滑块 + 受 IP 窗口约束（20 次/10min/IP）；
- **残余风险**：慢速撞库（每 IP 每 10 分钟 <20 次、每账号 ≤2 次裸试）理论上可长期滚动。缓解依赖滑块成本与限流。⚠️（见建议 R4）

### 3.3 验证码批量刷取 + OCR — 已封堵
- 加固前：`OnGetCaptcha` 无速率限制，可无限刷取逐张 OCR；
- **本次评估发现并修复**：`OnGetSlideCaptcha` 与 `OnGetCaptcha` 统一接入 `ActionCaptcha` 窗口（commit `1a4f65fd8`）。✅

### 3.4 用户名枚举 — 已消除
- 不存在用户与密码错误：同文案（`Admin.Login.Error.InvalidCredentials` 语义）、同响应结构、同 `CaptchaRequired` 表现；
- 探测端点 `CaptchaRequired` 统一返回 false，不再泄露"该用户是否已有失败计数"。✅

### 3.5 多租户侧信道 — 已封堵
- `TenantKey` 无法匹配 fail-closed 403，不再静默降级到默认租户（避免跨租户探测/越权）。✅

### 3.6 重放与缓存 — 已控制
- 滑块/文本验证码均一次性、短时效；
- 验证码端点响应头 `Cache-Control: no-store, no-cache, must-revalidate`；
- 滑块 Token 校验后无论成败即失效，无法重放。✅

### 3.7 会话（JWT）— 基本合理
- 有效期由 `AdminAuthConfig` 配置（`GetBackendJwtExpireMinutes`），支持 rememberMe 差异化；
- 桌面换票 JWT 受源 Cookie 寿命与配置寿命双重上限约束。✅
- 未见刷新令牌滥用面（无 refresh endpoint 暴露）。

## 4. 弱点与残余风险清单

| # | 风险 | 等级 | 说明 | 状态/对策 |
| --- | --- | --- | --- | --- |
| W1 | 文本验证码端点无限刷取 | 高 | `OnGetCaptcha` 原无限流 | ✅ 已修复（`1a4f65fd8`） |
| W2 | 锁定/限流状态为进程内缓存 | 中 | 多实例部署时各实例独立计数，攻击者可分散到多实例绕过；进程重启清空 | `ILoginRateLimitStore` 抽象已预留，生产多实例时应切换 Redis 实现 |
| W3 | 密码哈希为 SHA512+盐（无关键拉伸） | 中 | 无 PBKDF2/bcrypt/Argon2 迭代，数据库泄露后 GPU 离线爆破成本低于现代方案 | 建议双写迁移：新密码/改密时升级为 Argon2id，读取时兼容旧格式（见 R3） |
| W4 | `X-Forwarded-For` 可伪造 | 中 | 直连（无反向代理）时攻击者可轮换伪造 IP 头绕过限流 | 反向代理部署时由代理强制覆盖该头；可增信源配置（仅信任代理 IP 段） |
| W5 | 慢速撞库残余风险 | 中低 | 见 3.2 | 建议 R4：连续人机验证失败的 IP 进入"冷却升级"（窗口阈值动态收紧） |
| W6 | 滑块缺口可被图像分析定位 | 低 | 缺口有描边、场景为程序化生成，模板匹配仍可能被 CNN 攻破 | 已有行为校验 + 一次性 + 限流三层叠加；可再增加缺口描边随机化/干扰缺口 |
| W7 | 密码比较非恒定时间 | 低 | `CheckPassword` 使用 `string.Equals`，存在理论时序侧信道；且发生在人机验证之后 | 实际利用难度极高；可改 `FixedTimeEquals`（.NET 内置） |
| W8 | 无第二因素（2FA） | 信息 | 高价值管理员账号可考虑 TOTP | 可选增强（`OtpNet` MIT），接口可挂 `AdminAuthConfig` |
| W9 | 锁定提示返回精确解锁时间 | 信息 | `LoginLockException(lockEndTime)` 可能向攻击者泄露锁定窗口细节 | 对外文案建议模糊化（"请 N 分钟后再试"） |

## 5. 测试证据

- 登录相关单元测试 13/13 通过：
  - `LoginSlideTrackValidatorTests`（8）：正常轨迹、容差内通过；位置错误/点数不足/过快/瞬移/Percent 漂移/空轨迹全部拒绝
  - `LoginIpThrottleTests`（5）：窗口计数、多 IP 隔离、配置覆盖、login/captcha 独立窗口
- 全量套件：`Senparc.Areas.Admin.Tests` 357 通过 / 2 失败（`NeuCharWorkflowEngineTests` 的 RunCoordinator 存量问题，与登录无关）
- 构建：0 警告 0 错误

## 6. 后续加固建议（按优先级）

| # | 建议 | 优先级 | 工作量 |
| --- | --- | --- | --- |
| R1 | 多实例部署：`ILoginRateLimitStore` 与锁定计数切换 Redis（接口已预留） | 高（多实例时） | 小 |
| R2 | 生产部署基线：反向代理强制覆盖 `X-Forwarded-For` + HSTS；限流阈值按出口 IP 池规模复核 | 高 | 配置级 |
| R3 | 密码哈希迁移 Argon2id/bcrypt：改密与新账号直接新算法，登录成功时透明升级旧哈希（双读单写） | 中 | 中 |
| R4 | 人机验证失败驱动的 IP 冷却升级：同 IP 短时间多次验证码失败 → 临时收紧该 IP 窗口阈值 | 中 | 小 |
| R5 | `FixedTimeEquals` 替换密码比较（一行改动） | 低 | 极小 |
| R6 | 管理员 TOTP 二次验证（`OtpNet`，MIT）：对改密/改权等高危操作强制 | 低（按需求） | 中 |
| R7 | 锁定对外文案模糊化（不暴露精确解锁时间） | 低 | 极小 |

## 7. 结论

当前登录体系已形成 **限流（网络）→ 人机验证（滑块+行为）→ 锁定（账号）→ 防枚举（行为一致）→ 租户 fail-closed** 的纵深防御，单账号爆破与验证码刷取两条主要攻击路径已被有效封堵；本次评估额外发现并修复了文本验证码端点无限流的缺口（W1）。

主要残余风险集中在 **部署形态**（多实例状态共享 W2、代理头信任 W4）与 **密码哈希强度**（W3），均为有明确对策的工程项，不影响当前单实例 + 反向代理部署下的安全水位。
