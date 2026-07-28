# Senparc.Xncf.XncfBuilder

`Senparc.Xncf.XncfBuilder` is an NCF module and service layer for generating the starter code and metadata of an XNCF module.

## Features

- Generates module files, application-service interfaces, database entities, and migration scaffolding.
- Reads the XNCF template package and writes multiple generated files with placeholder replacement.
- Provides database migration and template inspection workflows through NCF function requests.
- Includes module-inventory request/response events for cross-module discovery.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.XncfBuilder" Version="0.26.0-preview3" />
```

For the `dotnet new` workflow, install the companion template package:

```bash
dotnet new install Senparc.Xncf.XncfBuilder.Template
dotnet new xncf -n MyProject --force --UseDatabase true
```

## Key API

- `BuildXncfAppService` handles module-generation operations.
- `BuildXncf_BuildRequest`, `BuildXncf_CreateAppServiceRequest`, and `BuildXncf_CreateDatabaseEntityRequest` describe generation tasks.
- `GenerateAppServiceInterface` and `DatabaseMigrationsAppService` expose focused code-generation workflows.
- `MultiFileCodeGenerator` writes template-driven file sets; `BuildXncfRequestHelper` normalizes generation requests.
- `XncfModulesInventoryRequestHandler` and `XncfModulesInventoryResponseHandler` support module inventory exchange.

Review generated files before committing them. Generated source can include database and authorization decisions that must be adapted to the host application's domain and security model.
