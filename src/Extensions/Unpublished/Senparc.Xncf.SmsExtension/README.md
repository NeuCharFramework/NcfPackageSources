# Senparc.Xncf.SmsExtension

`Senparc.Xncf.SmsExtension` is an unpublished, legacy XNCF extension for SMS verification flows and SMS usage records.

## Features

- Creates and reads send tokens through `SmsRecordService`.
- Sends phone verification and general SMS messages.
- Tracks usage counts and estimates message consumption.
- Provides placeholder replacement helpers for SMS content.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.SmsExtension" Version="0.26.0-preview3" />
```

## Key API

- `SmsRecordService.SetSendSmsToken()` and `GetSendSmsToken()` manage the send token.
- `SendPhoneCheck(...)` sends a phone verification message.
- `Send(...)` sends a general SMS payload.
- `GetLastCount()` and `GetSmsUseCount(...)` expose usage information.
- `ReplacePlaceHolder(...)` expands message placeholders.

This module is unpublished and legacy. Protect provider credentials and verification tokens, rate-limit by account/IP/phone, prevent replay, and do not treat a client-provided phone number as proof of identity.
