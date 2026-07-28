# Senparc.Ncf.Core

`Senparc.Ncf.Core` is the shared application foundation for NeuCharFramework (NCF). It contains the request/response model, application-service, event, authorization, localization, entity, and web API primitives used by NCF and XNCF packages.

## Features

- `AppServiceBase`, `AppRequestBase`, and `AppResponseBase<T>` for consistent application-service contracts.
- In-process event bus and request/response event support with cancellation, timeout, and correlation handling.
- Permission requirements, permission attributes, administrator context, and authorization handlers.
- Function-render metadata and localized descriptions for module functions and parameters.
- Core entities, DTO support, paging models, validation, cache markers, and common web helpers.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Core" Version="0.26.0-preview3" />
```

## Key API

- Derive application services from `AppServiceBase` and use `AppServiceHelper` for common response and request handling.
- Use `AppRequestBase`/`AppResponseBase<T>` and `StringAppResponse` as transport-friendly function contracts.
- Register or consume `IEventBus`, `IEventBusRequestClient`, `InMemoryEventBus`, and `EventBusExtensions` for integration events and request clients.
- Apply `PermissionAttribute`, `PermissionFilterAttribute`, `PermissionRequirement`, and `PermissionHandler` for NCF authorization.
- Use `FunctionRenderAttribute`, `LocalizedDescriptionAttribute`, `LocalizedRequiredAttribute`, and `ResourceStringLocalizer` to expose localized module metadata.

NCF hosts normally load this package through the standard NCF startup pipeline. When composing a smaller host, register the required DI, authentication, data, and localization services explicitly and do not assume that a package reference alone enables authorization.
