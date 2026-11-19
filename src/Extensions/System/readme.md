# System 文件夹说明

此文件夹中都是系统指定的 XNCF 顶级模块。

## 须知

`Senparc.Xncf.SystemCore` 是所有模块启动所必须的前置模块，所以每个项目必须添加（特别是数据库项目）。

> 后期会考虑集成到一个专门的 DatabaseCore 项目中。

## 数字前缀

命名前缀中的数字（如 `[5999]`）表示该模块在启动过程中的执行优先级，数字越大，执行越靠前。

## 系统模块一览（节选）

| 顺序前缀 | 模块 | 系统 UID（末段） |
|---------|------|------------------|
| `[5930]` | `Senparc.Xncf.Installer` | `…0001-000000000007` |
| `[5940]` | `Senparc.Xncf.Menu` | `…0001-000000000005` |
| `[5950]` | `Senparc.Xncf.XncfModuleManager` | `…0001-000000000004` |
| `[5955]` | `Senparc.Xncf.AreasBase` | `…0001-000000000006` |
| `[5960]` | `Senparc.Xncf.SystemPermission` | `…0001-000000000003` |
| `[5970]` | `Senparc.Xncf.SystemManager` | `…0001-000000000002` |
| `[5980]` | `Senparc.Xncf.SystemCore` | `…0001-000000000001` |
| `[5990]` | `Senparc.Xncf.Tenant` | 见 `SiteConfig.SYSTEM_XNCF_TANENT_UID` |

完整 UID 常量定义见 `SiteConfig.Core.cs` 中的 `SYSTEM_XNCF_*`。