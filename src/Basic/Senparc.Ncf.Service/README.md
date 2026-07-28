# Senparc.Ncf.Service

`Senparc.Ncf.Service` supplies the service layer that connects NCF entities, repositories, DTOs, AutoMapper, and application workflows.

## Features

- Generic `ServiceBase<TEntity>`, `ClientServiceBase<TEntity>`, and `DtoServiceBase<...>` foundations.
- Entity lookup, paging, mapping, change detection, and transaction helpers.
- Resilient transaction execution and common system services for menus, roles, permissions, and XNCF modules.
- `PagedList<TEntity>.ToDtoPagedList(...)` mapping convenience.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Service" Version="0.26.0-preview3" />
```

## Key API

- `ServiceBase<TEntity>.GetObject(...)` and `GetObjectAsync(...)` retrieve entities with optional ordering and includes.
- `GetObjectList(...)`/`GetObjectListAsync(...)` provide paged queries.
- `TryDetectChange(...)` and `IsInsert(...)` coordinate EF Core entity state.
- `ResilientTransaction.New(DbContext).ExecuteAsync(...)` executes an operation with the configured transaction strategy.
- `ServiceExtension.ToDtoPagedList(...)` maps a paged entity list through a service's `Mapping<TDto>(...)` method.

Register AutoMapper, repositories, and the relevant database context before resolving services. Service methods do not replace authorization or tenant-boundary checks.
