# Senparc.Ncf.Utility

`Senparc.Ncf.Utility` is the shared utility layer for NCF applications and modules. It groups DI, HTTP context, localization, reflection, expression, stream, file, and compatibility helpers.

## Features

- NCF DI and application-pipeline extensions.
- Culture normalization and scoped language helpers.
- Reflection and expression/predicate composition utilities.
- HTTP request metadata, path mapping, stream wrappers, file names, ID-card validation, and legacy encryption helpers.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Utility" Version="0.26.0-preview3" />
```

## Key API

- `UseSenparcMvcDI(...)` and the `DI` helpers connect common services to the host container.
- `NcfLocalizationOptions.TryNormalizeCulture(...)` and `GetSystemLanguage(...)` normalize request culture.
- `GlobalCulture.Create(...)` provides scoped language callbacks such as `SetEnglish(...)` and `SetChinese(...)`.
- `PredicateBuilder`, `SenparcExpressionHelper<TEntity>`, and `SenparcIQueryableExtension` compose query expressions.
- `SenparcHttpContext.MapPath(...)`, `MapWebPath(...)`, and request extensions expose safe host-aware paths and request metadata.
- `ReflectionHelper.CreateInstance(...)`/`GetTypeFromName(...)` support controlled plug-in discovery.

Review the security implications of path, reflection, DES, and request helpers before exposing them to untrusted input. Prefer modern authenticated encryption and allowlisted types for new code.
