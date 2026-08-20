# Senparc.Ncf.SMS

`Senparc.Ncf.SMS` defines a small provider-neutral SMS abstraction for NCF applications.

## Features

- Common `ISmsPlatform` and `SmsPlatform` contracts.
- Provider implementations for JunMei and Fissoft.
- Shared settings, result types, reply messages, and remaining-balance queries.
- Factory-based provider selection without coupling callers to a concrete platform.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.SMS" Version="0.26.0-preview3" />
```

## Key API

- `SmsPlatformFactory.GetSmsPlateform(...)` creates the selected platform implementation.
- `ISmsPlatform.Send(string content, string number)` sends a message.
- `ISmsPlatform.GetLastCount()` queries the provider's remaining message count.
- `SenparcSmsSetting` stores provider account and sub-number settings.

The factory method name preserves the existing `Plateform` spelling for binary/source compatibility. Keep credentials in secure configuration and apply provider-specific rate limits, templates, and consent requirements in the host.
