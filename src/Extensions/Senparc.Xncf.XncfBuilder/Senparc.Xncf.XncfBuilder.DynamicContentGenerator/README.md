# Senparc.Xncf.XncfBuilder.DynamicContentGenerator

`Senparc.Xncf.XncfBuilder.DynamicContentGenerator` is a Roslyn incremental source generator used by XNCF Builder-related projects to generate strongly typed template content and helper APIs.

## Features

- Generates code from a structured `FileGenerationConfig`.
- Supports `FileItem`, `GroupingOptions`, and `GenerationOptions` for multi-file template sets.
- Produces template type/content lookup helpers for generated XNCF code.
- Targets the compiler as a development dependency and does not provide runtime services.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.XncfBuilder.DynamicContentGenerator" Version="0.0.2" PrivateAssets="all" />
```

## Key API

- `MultiFileCodeGenerator` implements `IIncrementalGenerator`.
- `FileGenerationConfig` describes the source file set.
- `GroupingOptions` and `GenerationOptions` control grouping and generated output.
- Generated helpers include `GetTemplatesByType(...)`, `GetTemplateContent(...)`, and `GetAllTemplateTypes()`.

Keep the package private to the build unless a consumer explicitly needs the analyzer. Generated output should be reviewed as source code and compiled against the intended target framework.
