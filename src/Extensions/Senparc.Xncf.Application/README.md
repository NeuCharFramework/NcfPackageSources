# Senparc.Xncf.Application

`Senparc.Xncf.Application` is a small NCF utility module that exposes controlled application-launch functionality through the XNCF function system.

## Features

- Registers an XNCF module with localized metadata.
- Defines the `LaunchApp` function and its validated `FilePath` parameter.
- Returns an NCF `FunctionResult` with operation status and execution log.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.Application" Version="0.1.0" />
```

## Key API

- `LaunchApp` is the function implementation.
- `LaunchApp.LaunchApp_Parameters` carries the required, length-limited `FilePath`.
- `LaunchApp.Run(...)` starts the configured process and returns a `FunctionResult`.
- `ApplicationResource.Get(...)` and `Format(...)` provide localized module/function text.

This module can start a process on the host machine. Restrict it to trusted administrators, use an executable allowlist, avoid passing untrusted arguments, and do not expose it as a general user-controlled command runner.
