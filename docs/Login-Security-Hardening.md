# 管理员登录安全加强：滑块验证码 + IP 滑动窗口限流

> 分支：`Developer-MAF-V3-Spark-FineTuning-LoginHardening`（基于 `Developer-MAF-V3-Spark-FineTuning`）
> 日期：2026-11-05

## 1. 背景与威胁模型

加固前的登录防护（已在线上）：

| 层 | 机制 | 弱点 |
| --- | --- | --- |
| 账号级 | 阶梯锁定（5 次→5 分钟、10 次→2 小时、20 次→倍增） | 只约束**单一用户名**，对撞库（用户名列表）无效 |
| 人机 | 2 次失败后要求 SVG 文本验证码（6 位、一次性、2 分钟） | 文本验证码可被 OCR 攻破；`/Captcha` 端点匿名且**无速率限制**，攻击者可无限刷取验证码逐张 OCR |
| 枚举 | 不存在用户与密码错误行为一致 | 已完成（`2a464dbae`） |

核心问题：**“重试”本身没有客户端维度的成本**。锁定只能拖慢对同一账号的暴力破解，无法限制单 IP 的高频请求风暴与验证码刷取。

## 2. 加固方案（三层防御）

```
攻击请求
   │
   ▼
┌────────────────────────────┐
│ L3 账号阶梯锁定（已有）       │  单账号暴力破解 → 锁账号
├────────────────────────────┤
│ L2 人机校验（本次新增/升级）   │  机器人批量重试 → 滑块+行为校验
│   滑块验证码（MIT 组件）       │     └ 位置校验 + 轨迹时长/点数/瞬移检测
│   文本验证码（兜底，已有）      │
├────────────────────────────┤
│ L1 IP 滑动窗口限流（本次新增） │  请求风暴 → 429 + Retry-After
│   login  20 次 / 10 分钟/IP  │
│   captcha 30 次 / 10 分钟/IP │
└────────────────────────────┘
```

### 2.1 L1：IP 滑动窗口限流

- 新增 `Domain/Services/LoginIpThrottleService.cs`
  - `OnPostLoginAsync` 与验证码下发端点（`OnGetCaptcha` 走既有逻辑、`OnGetSlideCaptcha` 新增）先按 IP 计数，超阈值返回 **429 + `Retry-After`**
  - 窗口状态存 CO2NET 对象缓存，缓存锁内读-改-写，避免并发漂移
  - 存储抽象为 `ILoginRateLimitStore`（默认 `Co2NetLoginRateLimitStore`），单元测试可注入内存实现
- 配置（`appsettings`，均有默认值）：
  - `AdminLogin:Throttle:LoginMaxPerWindow`（默认 20）
  - `AdminLogin:Throttle:LoginWindowMinutes`（默认 10）
  - `AdminLogin:Throttle:CaptchaMaxPerWindow`（默认 30）
  - `AdminLogin:Throttle:CaptchaWindowMinutes`（默认 10）
- IP 取 `X-Forwarded-For` 首段（反向代理部署），否则 `RemoteIpAddress`。注意：限流只是**提高重试成本**，不是安全边界。

### 2.2 L2：滑块验证码（替换文本验证码为主力，文本保留兜底）

选型（用户要求 **MIT / Apache-2.0** 许可）：

| 候选 | 许可 | 结论 |
| --- | --- | --- |
| **LazySlideCaptcha**（`Lazy.SlideCaptcha.Core`，.NET Standard 2.1，SkiaSharp 渲染） | **MIT** | ✅ 采用：滑块+轨迹校验，可替换验证器/资源提供器 |
| Kaptcha.NET | MIT | ❌ System.Drawing.Common，Linux 受限，仍是文本 OCR 型 |
| IconCaptcha | MIT | 备选（图标对比型，无行为维度） |
| YMJake.FusionCaptcha | 未明确 | ❌ 许可不符合要求 |
| Cloudflare Turnstile / reCAPTCHA / hCaptcha | 服务 ToS（非 OSS 许可） | 备选（需外部服务，不符合自托管许可要求） |

实现要点：

- **程序化背景**：`ProceduralSlideBackgroundHandler`（SkiaSharp 实时绘制 552×344 渐变+几何+噪点），仓库不内置图片资源，每张验证码背景都不同，防止模板预采集
- **行为校验器**：`LoginBehaviorSlideValidator : BaseValidator`（替换默认 SimpleValidator）：
  1. 轨迹点数 ≥ 10（脚本一次性位移伪造通常只有 1~3 个点）
  2. 拖拽时长 ≥ 200ms
  3. 单步 X 位移 ≤ 45px（禁止瞬移）
  4. 位置校验：`|轨迹终点 Percent − 目标 Percent| ≤ Tolerant`（0~1 背景宽度比例口径，`Tolerant=0.04`≈22px）
  - 阈值均为保守值：拦截明显自动化，不误伤真人
- **降级开关**：`AdminLogin:SlideCaptcha:Enabled`（默认 true）；滑块生成异常（如 SkiaSharp 原生库缺失）时自动退化为既有 SVG 文本验证码，**登录永远可用**
- 前端：登录页（Vue 2 + Element UI）自绘滑块条（鼠标/触摸），松手即携带 `SlideCaptchaId + SlideTrack`（轨迹点 `{X,Y,T}` + 起止时间 + 尺寸）自动提交登录；429 显示“请求过于频繁”提示
- 多语言：7 个 resx 新增 4 条文案（zh-CN/en/ja/fr/es/ru）

### 2.3 L3：账号阶梯锁定（已有，未改动）

## 3. 改动文件

| 文件 | 说明 |
| --- | --- |
| `Senparc.Areas.Admin/Senparc.Areas.Admin.csproj` | + `Lazy.SlideCaptcha.Core` 2.0.1（MIT） |
| `Senparc.Areas.Admin/Register.cs` | 注册滑块验证码（程序化背景 + 行为校验器 + IDistributedCache 兜底）、限流服务 |
| `Senparc.Areas.Admin/Domain/Services/LoginIpThrottleService.cs` | 新增：IP 滑动窗口限流 |
| `Senparc.Areas.Admin/Domain/Services/LoginSlideCaptchaSupport.cs` | 新增：程序化背景 + 行为校验器 |
| `Senparc.Areas.Admin/Areas/Admin/Pages/Login.cshtml.cs` | 限流接入、滑块校验、`OnGetSlideCaptcha`、DTO 扩展（`slideCaptcha`/`retryAfterSeconds`） |
| `Senparc.Areas.Admin/Areas/Admin/Pages/Login.cshtml` | 滑块 UI（slide/text 双模式） |
| `Senparc.Areas.Admin/wwwroot/js/Admin/Pages/LogIn/Index.js` | 滑块拖拽/轨迹采集/429 处理 |
| `Senparc.Areas.Admin/wwwroot/css/Admin/login/index.css` | 滑块样式 |
| `Senparc.Areas.Admin/Resources/AdminResource*.resx` ×7 | 新增 4 条多语言文案 |
| `Tests/.../LoginBehaviorSlideValidatorTests.cs` | 新增：行为校验器 5 用例 |
| `Tests/.../LoginIpThrottleTests.cs` | 新增：限流 5 用例 |

## 4. 测试

```bash
dotnet test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests \
  --filter "FullyQualifiedName~LoginBehaviorSlideValidatorTests|FullyQualifiedName~LoginIpThrottleTests"
```

- 行为校验器：正常轨迹通过；位置错误 / 点数不足 / 过快 / 瞬移 均拒绝
- 限流：默认 20 次窗口、不同 IP 独立、配置覆盖生效、captcha 与 login 独立窗口

手动验证（`tools/NcfSimulatedSite`）：
1. 同一账号连错 2 次密码 → 第 3 次出现滑块（拖动到缺口松手自动提交）
2. 用脚本 21 次/10 分钟打 `/Admin/Login?handler=Login` → 第 21 次返回 429 + `Retry-After`
3. 脚本一次性提交伪造轨迹（3 个点、瞬移、0ms 时长）→ 全部拒绝
4. `AdminLogin:SlideCaptcha:Enabled=false` → 回退文本验证码流程

## 5. 已知限制

- 限流基于单进程对象缓存：多实例部署时窗口不共享（如需全局窗口，将 `ILoginRateLimitStore` 实现切换到 Redis 即可，接口已预留）
- `X-Forwarded-For` 可被伪造（直连场景）；反向代理部署时由代理覆盖该头即安全
- 滑块位置仍可被高级图像分析定位（缺口检测），但此时攻击者必须同时伪造出通过行为校验的轨迹 + 承受 IP 限流成本，综合成本显著提高
- 后续可选增强（未实施）：登录成功后按风险触发 TOTP/邮箱 OTP 二次验证（`OtpNet`，MIT）；设备指纹（`fingerprintjs`，MIT）
