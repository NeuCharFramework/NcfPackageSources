# Senparc.Xncf.Tenant

`Senparc.Xncf.Tenant` provides NCF multi-tenant persistence, request resolution, caching, and service integration.

## Features

- Persists `TenantInfo` records through a tenant database context and repository.
- Resolves the current request tenant through middleware.
- Provides `TenantInfoService`, tenant DTO mapping, and cached full-tenant information.
- Supports provider-specific contexts and NCF multi-database migrations.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.Tenant" Version="0.26.0-preview3" />
```

## Key API

- `Register.AddMultiTenant(...)` registers tenant services and middleware dependencies.
- `TenantMiddleware` resolves tenant context for an HTTP request.
- `TenantInfoService.GetRequestTenantInfo(...)` and `SetTenantInfo(...)` read/update tenant information.
- `TenantInfoRepository` and `FullTenantInfoCache` provide persistence and cached lookup.
- `SenparcEntitiesMultiTenant` is the multi-tenant database context.

Choose and document the tenant-resolution source, fail closed when a tenant cannot be resolved, and ensure every business query applies the resolved tenant boundary.

When using `TenantRule.RequestHeader`, anonymous requests can select a tenant by sending
`TenantKey`. Requests authenticated with the NCF admin cookie must present the same
tenant key as their authenticated `TenantKey` claim; accounts without a tenant claim
cannot switch tenants. When the header is absent, a signed-in tenant user retains
the tenant from their cookie claim rather than falling back to the default tenant.
Requests with an `Authorization` header and `TenantKey` are
rejected until their authentication scheme is explicitly bound to a tenant. A trusted
proxy should strip client-supplied tenant headers if it injects its own.
