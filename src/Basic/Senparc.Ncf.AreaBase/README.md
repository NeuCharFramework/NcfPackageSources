# Senparc.Ncf.AreaBase

`Senparc.Ncf.AreaBase` contains the ASP.NET Core MVC and Razor Page foundations used by NCF administration areas and XNCF modules.

## Features

- Base page models for regular and administrator-facing module pages.
- Administrator authorization policies and API/page authorization attributes.
- Authentication result filters, resource markers, and automatic anti-forgery conventions.
- Shared hooks for building an NCF Area without duplicating security and page plumbing.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.AreaBase" Version="0.26.0-preview3" />
```

## Key API

- `AdminPageModelBase` and `AdminXncfModulePageModelBase` provide reusable page-model behavior.
- `NcfAdminAuthorizationExtensions.AddNcfAdminAuthorizationPolicies(...)` registers the standard administrator policies.
- `AdminAuthorizeAttribute`, `ApiAuthorizeAttribute`, `AuthenticationAsyncPageFilterAttribute`, and `AuthenticationResultFilterAttribute` apply the matching authorization and authentication behavior.
- `AutoValidateAntiForgeryTokenModelConvention` enables the default anti-forgery convention for eligible page handlers.

The package is an ASP.NET Core integration layer. Register it inside the host application's MVC/Razor pipeline and keep the host's authentication scheme and authorization policy configuration consistent with NCF.
