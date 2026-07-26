# NCF Agent 角色规范

正式实现位于 `Views/Controls/NcfMascotView.cs`：使用 Avalonia 原生矢量绘制，无图片和额外 NuGet 依赖，可跨平台缩放并按状态播放轻量动作。PNG 规则保留为以后制作宣传级素材时的可选规范。

## 角色

| ID | 名称 | 定位 | 主色 | 标志物 |
|---|---|---|---|---|
| `nono` | Nono | NCF 核心向导，负责启动和总体状态 | `#2563EB` | 胸前 NCF 菱形核心 |
| `cici` | Cici | Admin Chat 对话助手 | `#7C3AED` | 对话气泡耳机 |
| `qiao` | Qiao | DesktopBridge 信使，负责连接与同步 | `#06B6D4` | 双向发光连接环 |
| `opsi` | Opsi | 构建、发布和故障提示助手 | `#F59E0B` | 小工具包与状态灯 |

角色应有明显差异，但共享相同的眼睛比例、材质、光照方向和菱形 NCF 元素，组合出现时像同一动画世界中的团队。

## 动作

每个角色生成以下透明背景姿势：

- `idle`：自然站立或轻微悬浮；
- `wave`：欢迎、首次连接；
- `thinking`：等待模型回复；
- `working`：启动、构建或同步中；
- `success`：完成；
- `warning`：断线、认证过期或任务失败，不使用恐慌表情。

控件使用 `NcfMascotKind` 选择角色，使用 `NcfMascotPose` 切换六种动作。如未来补充渲染图，输出路径为 `Assets/Characters/{character-id}/{pose}.png`。

## 生成要求

- 1024×1024 PNG，透明背景，角色完整，四周保留约 8% 安全边距；
- 在 48×48 与 96×96 下仍可辨认；
- 同一角色各动作保持服装、配色、面部和比例一致；
- 不使用 OpenAI、Codex、GitHub 等第三方商标或受保护角色造型；
- 不包含文字、背景板、裁切阴影或超出画布的肢体；
- 保留原图，并按需另导出 256×256 优化版本。

## 基础提示词

> Create an original friendly 3D soft-vinyl mascot for the NCF developer platform. Rounded compact body, expressive simple eyes, subtle glowing diamond-shaped NCF core, clean premium product illustration, transparent background, full body, centered, consistent soft studio lighting, no text, no logo from other brands. Character: {角色描述}. Pose: {动作描述}. Keep the exact same character identity and proportions across the full pose set.

生成后，在快捷聊天禁用态、登录态、等待回复、同步成功和错误提示中分别使用相应动作；素材加载失败时继续使用当前占位，不阻断 GUI。
