# Senparc.Xncf.AreasBase

`Senparc.Xncf.AreasBase` supplies the shared Area registration and UI boundary required by NCF's built-in system modules.

## Features

- Registers the system Area base with the NCF module engine.
- Provides the `AreaRegister` integration point and localized module resources.
- Establishes the common Area foundation consumed by system and administration modules.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.AreasBase" Version="0.26.0-preview3" />
```

## Key API

- `Register` is the XNCF module registration entry point.
- `AreaRegister` contains Area setup behavior.
- `AreasBaseResource.Get(...)` and `Format(...)` provide localized resource lookup.

This is a system foundation package. Install it together with the NCF system module set and keep its version aligned with the host's NCF core and AreaBase packages.
