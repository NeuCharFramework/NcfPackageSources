# Senparc.Xncf.Menu

`Senparc.Xncf.Menu` provides the system menu module and persistence model used to describe NCF navigation entries.

## Features

- Stores menu data in `MenuSenparcEntities`.
- Provides multi-database context variants for NCF's supported providers.
- Registers menu resources and database migration metadata with the XNCF engine.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.Menu" Version="0.26.0-preview3" />
```

## Key API

- `Register` activates the menu module.
- `MenuSenparcEntities` is the module database context.
- `SenparcDbContextFactory_*` types support design-time migration context creation.
- `MenuResource.Get(...)` and `Format(...)` resolve localized module text.

Keep menu UIDs and parent relationships stable across releases. Authorization belongs to the NCF permission system; a visible menu item must not be treated as an authorization grant.
