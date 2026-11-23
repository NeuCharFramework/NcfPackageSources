# Local branch

The Local branch handles communication between modules or bounded contexts inside the same system. It exposes stable interfaces so collaborators stay decoupled and easy to extend. Local OHS runs inside one process or service and does not cross network boundaries.

AppServices live under [`Application/AppServices`](../../Application/AppServices); request and response models live under [`Application/DTOs`](../../Application/DTOs). NCF dynamic WebApi can expose AppService methods as HTTP APIs automatically. Add controllers under OHS `Local` or `Remote` only when you need explicit HTTP contract control.