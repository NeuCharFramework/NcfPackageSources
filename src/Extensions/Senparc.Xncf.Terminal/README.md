# Senparc.Xncf.Terminal

`Senparc.Xncf.Terminal` is an NCF function module that exposes terminal command execution for trusted development and administration scenarios.

## Features

- Registers the terminal XNCF module and localized function metadata.
- Defines a `Terminal_RunRequest` function request.
- Returns execution status and output through the NCF function result pipeline.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.Terminal" Version="0.26.0-preview3" />
```

## Key API

- `TerminalAppService` is the module application-service entry point.
- `Terminal_RunRequest` describes a terminal execution request.
- `Register` integrates the module with `Senparc.Ncf.XncfBase`.
- `TerminalResource` supplies localized text.

This package can execute operating-system commands. Keep it disabled in public deployments unless there is a narrow, audited allowlist and a trusted administrator boundary. Never pass raw user input to a shell.
