# Senparc.Xncf.FirmwareUpdate

`Senparc.Xncf.FirmwareUpdate` mirrors NCF Host runtime packages and NcfDesktop application packages from GitHub Releases into an NCF site's `wwwroot/NcfPackages` area and exposes controlled fallback metadata.

## Features

- Stores mirror configuration and synchronization status in an XNCF database context.
- Supports scheduled or explicitly requested package synchronization.
- Provides a package mirror service for release metadata and downloaded assets.
- Stores NCF Host packages under `NcfPackages/host/{tag}` and NcfDesktop packages under `NcfPackages/desktop/{tag}`.
- Keeps the existing GitHub-compatible `latest-release.json` schema for NCF Host packages and maintains a separate `latest-desktop-release.json` for the public NcfDesktop download page.
- Publishes each feed independently only after all six ZIP assets pass size and GitHub SHA-256 verification.
- Supports the NCF multi-database context model and localized module resources.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.FirmwareUpdate" Version="0.26.0-preview3" />
```

## Key API

- `FirmwareUpdateAppService` exposes configuration and synchronization operations.
- `FirmwareUpdate_ConfigureRequest` describes mirror settings.
- `FirmwareUpdate_SyncNowRequest` requests an immediate synchronization.
- `NcfPackageMirrorService` performs release/package mirror work.
- `FirmwareUpdateConfig` stores the module's persisted configuration.

Configure repository allowlists, release channels, download timeouts, destination permissions, and cleanup policy in the host. Verify downloaded assets before serving them, and never allow arbitrary URLs to become mirror targets.
