# Senparc.Ncf.Repository

`Senparc.Ncf.Repository` provides the generic repository layer used by NCF services and XNCF modules on top of Entity Framework Core.

## Features

- Generic `IRepositoryBase<TEntity>` and `RepositoryBase<TEntity>` implementations.
- `IClientRepositoryBase<TEntity>` and `ClientRepositoryBase<TEntity>` for NCF `EntityBase` models.
- Query, paging, include, batch-save, and transaction helpers.
- Ready-to-use repositories for common system entities and XNCF modules.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Repository" Version="0.26.0-preview3" />
```

## Key API

- `GetFirstOrDefaultObjectAsync(...)` retrieves one entity with optional includes.
- `SaveObjectListAsync(...)` persists a collection using the repository's change-tracking rules.
- `BeginTransaction()`, `BeginTransactionAsync()`, `CommitTransaction()`, and `RollbackTransaction()` control explicit transactions.
- `ISysButtonRespository.DeleteButtonsByMenuId(...)` and `ISysRolePermissionRepository.GetAllResouceCodesByAccountIdAsync(...)` expose common system operations.

Repositories are normally injected into `ServiceBase<TEntity>` or a derived service. Keep query predicates server-translatable and scope repositories to the current tenant where multi-tenancy is enabled.
