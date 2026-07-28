# Senparc.Xncf.AIAgentsHub

`Senparc.Xncf.AIAgentsHub` is an NCF/XNCF sample module that provides a starting point for AI-agent hub features, module registration, and tenant-aware persistence.

## Features

- Demonstrates the standard XNCF registration and module metadata pattern.
- Includes an EF Core module context with multi-database context variants.
- Provides sample application-service and custom-function endpoints that can be replaced by a production agent orchestration layer.
- Includes a module resource class for localized display metadata.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.AIAgentsHub" Version="0.26.0-preview3" />
```

## Key API

- `Register` is the module entry point discovered by `Senparc.Ncf.XncfBase`.
- `AIAgentsHubSenparcEntities` is the module database context; provider-specific variants support the configured database engine.
- `ApiAppService`, `ColorAppService`, and `MyFuctionAppService` show the module's application-service/function-render shape.
- `AIAgentsHubResource` contains localizable module resources.

This package is a module sample and integration point; it does not select an AI provider or ship model credentials. Configure agent services, authorization, and data retention in the host application.
