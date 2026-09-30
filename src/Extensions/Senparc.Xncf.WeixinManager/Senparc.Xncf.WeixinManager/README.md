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
- Exposes `IWeixinClawMessageSender` / `WeixinClawMessageService.SendTextAsync()` for outbound text.
- Supports outbound images, files, and voice files through `getuploadurl`, the iLink CDN upload
  flow, and `sendmessage`.
- Downloads inbound image, voice, and file media from the iLink CDN, decrypts it, and stores it
  under the host `App_Data/WeixinClawMedia` directory for authenticated conversation display.
- Persists the latest inbound user and protected `context_token`, so the Admin page can send a reply after
  the phone sends at least one message. The page reports an actionable error instead of treating a
  context-less send as delivered.
- Persists inbound and outbound text records in `WeixinManager_WeixinClawMessageRecord`; the Admin page
  provides a polling conversation window and shows `sending`, `sent`, and `failed` states. The window
  also supports image, file, and voice-file uploads.

Media sends use the most recent inbound conversation context automatically. If no phone message has
been received yet, the UI asks the operator to send one first. A successful HTTP response is only
treated as submitted when iLink returns a `message_id`; `ret=0` without `message_id` is recorded as
failed because it does not confirm that the message entered the downlink queue. Native voice bubbles
depend on the receiving Weixin client accepting the supplied encoding metadata; file upload remains
the fallback for unsupported voice formats.

Concrete Admin Chat, NeuBell, Workflow, and Harness routing is implemented by the upper-layer Admin
integration rather than this channel module.

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
