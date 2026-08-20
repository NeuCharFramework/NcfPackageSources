# Senparc.Xncf.ReloadPage

`Senparc.Xncf.ReloadPage` is an unpublished development-oriented XNCF extension that notifies connected browsers when server-side or static web files change.

## Features

- Provides a SignalR `ReloadPageHub` at `/reloadHub`.
- Watches `wwwroot` JavaScript/CSS files and Razor files through a physical file provider.
- Sends a `ReloadPage` client message when a watched file changes.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.ReloadPage" Version="0.26.0-preview3" />
```

## Key API

- `ReloadPageHub.Route` identifies the hub route.
- `PhysicalFileAppBuilderExtensions.UsePhysicalFile(...)` starts file watching for the application.
- `RegisterPhysical(...)` registers the watcher against an `IHubContext<ReloadPageHub>`.

This extension is intended for development feedback, not production deployment. It broadcasts file-change notifications to all connected clients and should be disabled or isolated in environments containing sensitive pages.
