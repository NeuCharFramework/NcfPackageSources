# Senparc.Ncf.Log

`Senparc.Ncf.Log` provides lightweight logging helpers used across NCF packages, with integration points for the configured logging implementation.

## Features

- Central logger lookup by name.
- NLog-oriented extension methods for formatted error and diagnostic messages.
- A small dependency surface suitable for shared NCF libraries.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Log" Version="0.26.0-preview3" />
```

## Key API

- `LogUtility.GetLogger(string)` obtains an `ILog` instance for a component.
- NLog extensions such as `ErrorFormat(...)` provide structured formatting convenience.

Configure the concrete logging provider and sinks in the host. This package does not replace application-level log filtering, redaction, or retention policies.
