# Senparc.Xncf.WeixinManager

`Senparc.Xncf.WeixinManager` is an NCF administration module for managing WeChat public-account configuration, users, tags, message handlers, and reusable notification templates.

## Features

- Persists `MpAccount`, `WeixinUser`, and `UserTag` data with DTOs and service layers.
- Supports account discovery, user synchronization, tag management, and WeChat-facing message handling.
- Provides `WeixinService`, `XncfMpMessageHandler`, and template base types for module integrations.
- Uses Senparc.Weixin APIs and NCF's multi-database context conventions.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.WeixinManager" Version="0.26.0-preview3" />
```

## Key API

- `MpAccountService` manages account configuration and `MpAccount` records.
- `WeixinService` provides module-level WeChat operations.
- `WeixinUser`/`WeixinUserDto` and `UserTag`/`UserTag_WeixinUserDto` represent synchronized user and tag data.
- `MpMessageHandlerAttribute` and `XncfMpMessageHandler` connect incoming WeChat messages to NCF handlers.
- `WeixinTemplateBase` and the `WeixinTemplate_*` types support reusable template messages.
- `FindWeixinApiController` exposes API discovery/management endpoints.

Store AppSecret, access tokens, and encryption keys in secure configuration. Validate WeChat signatures, scope account access by tenant/administrator, and treat synchronized user data as personal information.
