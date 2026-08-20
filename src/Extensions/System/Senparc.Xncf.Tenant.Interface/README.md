# Senparc.Xncf.Tenant.Interface

`Senparc.Xncf.Tenant.Interface` contains the DTO contract shared by NCF tenant-aware modules without pulling in tenant persistence or middleware implementation.

## Features

- Defines `TenantInfoDto` for read models.
- Defines `CreateOrUpdate_TenantInfoDto` for tenant management input.
- Keeps cross-module references lightweight and implementation-independent.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.Tenant.Interface" Version="0.26.0-preview3" />
```

## Key API

- `TenantInfoDto` represents tenant identity and display data shared with consumers.
- `CreateOrUpdate_TenantInfoDto` carries create/update values for tenant administration.

This package is a contract boundary. Validate tenant IDs and ownership in the service that processes the DTO; a DTO alone does not establish tenant isolation.
