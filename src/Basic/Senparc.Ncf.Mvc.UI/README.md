# Senparc.Ncf.Mvc.UI

`Senparc.Ncf.Mvc.UI` contains reusable ASP.NET Core MVC/Razor UI helpers used by NCF administration pages and XNCF modules.

## Features

- Enum-backed dropdowns and description rendering.
- Current-menu helpers, pagination bars, repeaters, grid views, action links, and click spans.
- Backend content/message boxes and HTML attribute/JavaScript parameter formatting.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Mvc.UI" Version="0.26.0-preview3" />
```

## Key API

- `DropDownListFormEnum(...)` and `GetDescriptionForEnum(...)` render enum metadata.
- `PagerBar(...)`, `GridViewExtension`, `RepeaterExtension`, and `CurrentMenu(...)` build common administrative UI patterns.
- `ContentBox(...)`, `ShowMessageBox(...)`, and `RenderMessageBox(...)` provide backend feedback containers.
- `ToAttributeList(...)`, `ToJsParams(...)`, and `GetPropertyHash(...)` convert view metadata for HTML/JavaScript output.

These helpers render server-side HTML and assume the host supplies the corresponding CSS/JavaScript and HTML encoding conventions.
