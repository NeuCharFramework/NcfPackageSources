# System folder

Built-in top-level XNCF modules required by the platform.

## Important

`Senparc.Xncf.SystemCore` is required for every module to start. Every host must reference it (including database tooling hosts).

> A dedicated DatabaseCore project may be introduced later.

## Numeric prefix

The number in the name (for example `[5999]`) is the startup priority. Higher numbers run earlier.

## System modules (excerpt)

| Prefix | Module | System UID (suffix) |
|--------|--------|---------------------|
| `[5930]` | `Senparc.Xncf.Installer` | `бн0001-000000000007` |
| `[5940]` | `Senparc.Xncf.Menu` | `бн0001-000000000005` |
| `[5950]` | `Senparc.Xncf.XncfModuleManager` | `бн0001-000000000004` |
| `[5955]` | `Senparc.Xncf.AreasBase` | `бн0001-000000000006` |
| `[5960]` | `Senparc.Xncf.SystemPermission` | `бн0001-000000000003` |
| `[5970]` | `Senparc.Xncf.SystemManager` | `бн0001-000000000002` |
| `[5980]` | `Senparc.Xncf.SystemCore` | `бн0001-000000000001` |
| `[5990]` | `Senparc.Xncf.Tenant` | See `SiteConfig.SYSTEM_XNCF_TANENT_UID` |

Full UID constants live in `SiteConfig.Core.cs` under `SYSTEM_XNCF_*`.