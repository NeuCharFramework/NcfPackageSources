# Senparc.Xncf.WeixinManager

`Senparc.Xncf.WeixinManager` is an NCF administration module for managing WeChat public-account configuration, users, tags, message handlers, and reusable notification templates.

## Features

- Persists `MpAccount`, `WeixinUser`, and `UserTag` data with DTOs and service layers.
- Supports account discovery, user synchronization, tag management, and WeChat-facing message handling.
- Provides `WeixinService`, `XncfMpMessageHandler`, and template base types for module integrations.
- Uses Senparc.Weixin APIs and NCF's multi-database context conventions.

## Personal Weixin Bot

The module also supports the personal Weixin Bot channel used by Tencent's
[openclaw-weixin protocol](https://github.com/Tencent/openclaw-weixin). It implements
the iLink HTTP JSON protocol directly and does not require the OpenClaw runtime.

- Open `/Admin/WeixinManager/WeixinClaw` to scan a QR code and connect an account.
- Polls inbound text messages with a persisted `get_updates_buf` cursor.
- Stores `message_id + seq` receipts for idempotent processing.
- Protects bot tokens with ASP.NET Data Protection.
- Exposes `IWeixinClawMessageHandler` for Admin Chat, NeuBell, or custom routing.
- Exposes `WeixinClawMessageService.SendTextAsync()` for outbound text.

The first phase handles text messages and QR login. Media transfer and concrete
Admin Chat routing should be implemented by an upper-layer handler.

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
