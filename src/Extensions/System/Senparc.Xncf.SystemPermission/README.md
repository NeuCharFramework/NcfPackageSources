# Senparc.Xncf.SystemPermission

`Senparc.Xncf.SystemPermission` is the NCF system module for role, permission, resource, and authorization persistence.

## Features

- Defines the system-permission `DbContext` and provider-specific variants.
- Carries permission schema migrations and configuration mapping.
- Integrates with NCF authorization checks and system administration modules.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.SystemPermission" Version="0.26.0-preview3" />
```

## Key API

- `Register` activates the permission module.
- `SystemPermissionSenparcEntities` is the module database context.
- `SenparcDbContextFactory_*` types support design-time migration operations.
- `SystemPermissionResource.Get(...)` and `Format(...)` provide localized module text.

Permission records are security-critical data. Run migrations deliberately, protect administration endpoints, and verify cache invalidation and tenant scope after permission changes.
