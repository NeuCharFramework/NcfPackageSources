# Senparc.Xncf.DynamicData

`Senparc.Xncf.DynamicData` is an NCF module for describing dynamic tables, columns, layouts, and records without compiling a new entity type for every business shape.

## Features

- Defines dynamic table and column metadata through `TableMetadata`, `ColumnMetadata`, `DataTemplate`, and `ColumnTemplate`.
- Stores and queries dynamic records with `TableData`, `TableDataDto`, and `TableDataService`.
- Provides configurable page, layout, data-sheet, and render models for the management UI.
- Uses NCF's localized module/function metadata and multi-database context conventions.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.DynamicData" Version="0.26.0-preview3" />
```

## Key API

- `Register` registers the module with the NCF engine.
- `TableMetadataService` and `ColumnMetadataService` manage dynamic schema definitions.
- `TableDataService` manages dynamic rows and maps them to `TableDataDto`.
- `DataTemplate` and `ColumnTemplate` describe reusable field behavior.
- `LayoutSetModel`, `PageSetModel`, and `renderLayoutPageModel` support the module's administrative UI.

Dynamic schemas are application data, not a substitute for authorization or validation. Restrict who can create/alter tables, validate field types and query conditions, and review database-provider limitations before enabling the module in production.
