# Senparc.Xncf.AIKernel.Abstractions

`Senparc.Xncf.AIKernel.Abstractions` is the compatibility and dependency anchor for AI-kernel integrations shared by NCF AI modules.

## Features

- Keeps AI-kernel-facing module dependencies separate from the full AI management module.
- References NCF shared event abstractions without imposing a concrete model provider.
- Supports applications that need the AI-kernel abstraction package as a stable package reference.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.AIKernel.Abstractions" Version="0.2.2-preview2" />
```

## Key API

This package intentionally exposes the shared abstraction surface supplied by its referenced contracts rather than a provider implementation. Use it when a library needs the AI-kernel dependency boundary without loading the administrative `Senparc.Xncf.AIKernel` module.

The package does not configure model credentials, providers, vector stores, or authorization. Those decisions belong to the consuming host.
