# Senparc.Xncf.EmailExtension

`Senparc.Xncf.EmailExtension` is an unpublished, legacy XNCF extension for email sending, queued automatic email records, and reusable email parameter types.

## Features

- Sends common message types through `SendEmail` and `SendEmailFactory`.
- Defines live-code, password-reset, invitation, order, and custom email parameter models.
- Tracks queued sends with `SendEmailCache` and `AutoSendEmailThreadUtility`.
- Retains XML/configuration data helpers for older NCF integrations.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.EmailExtension" Version="0.26.0-preview3" />
```

## Key API

- `SendEmail.DoSendEmail(...)` performs the core send operation.
- `SendEmailFactory` creates the configured sender.
- `ISendEmailCache.GetAllList()`, `InsertEmail(...)`, `SendSuccess(...)`, and `SendFail(...)` manage queued records.
- `AutoSendEmailThreadUtility.SetSleep(...)` and `SendEventHandler(...)` control the legacy worker behavior.

This module is unpublished and legacy. Configure SMTP/provider credentials securely, prevent header injection, obtain recipient consent, and test delivery behavior before using it in a production host.
