# Senparc.Xncf.Swagger

`Senparc.Xncf.Swagger` integrates Swagger/OpenAPI document generation with an NCF ASP.NET Core application.

## Features

- Adds an XNCF module boundary for API documentation configuration.
- Discovers MVC controller actions and their HTTP method, API-version, and route metadata.
- Uses generated XML documentation when the host enables XML documentation output.
- Provides a consistent place for NCF applications to extend document filters and API descriptions.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.Swagger" Version="0.26.0-preview3" />
```

## Key API and requirements

- Mark actions with `[HttpGet]`, `[HttpPost]`, or another HTTP method attribute.
- Add API-version metadata such as `[ApiVersion("1")]` where versioning is enabled.
- Add an explicit `[Route("...")]` so the generated document has a stable path.
- Enable XML documentation generation in the consuming project for method and model comments.

The current integration targets MVC API actions. Razor Pages are not automatically described as API operations. Protect the Swagger endpoint and avoid publishing secrets, internal routes, or unrestricted schemas in production.
