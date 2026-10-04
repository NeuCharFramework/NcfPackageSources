# Senparc.Xncf.WeixinManager

`Senparc.Xncf.WeixinManager` is an NCF administration module for managing WeChat public-account configuration, users, tags, message handlers, and reusable notification templates.

## Features

- Persists `MpAccount`, `WeixinUser`, and `UserTag` data with DTOs and service layers.
- Supports account discovery, user synchronization, tag management, and WeChat-facing message handling.
- Provides `WeixinService`, `XncfMpMessageHandler`, and template base types for module integrations.
- Uses Senparc.Weixin APIs and NCF's multi-database context conventions.

## Personal Weixin Bot

The module also supports the personal Weixin Bot channel used by Tencent's
[official openclaw-weixin package](https://www.npmjs.com/package/@tencent-weixin/openclaw-weixin). It implements
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
- Requests a tenant-scoped private-attachment import through the FileManager Abstractions EventBus
  contract. The message record keeps the resulting FileManager file ID; if FileManager is unavailable,
  the original Weixin conversation copy remains usable and the import error is recorded separately.
- Persists the latest inbound user and protected `context_token`, so the Admin page can send a reply after
  the phone sends at least one message. The page reports an actionable error instead of treating a
  context-less send as delivered.
- Persists inbound and outbound text records in `WeixinManager_WeixinClawMessageRecord`; the Admin page
  provides a polling conversation window and shows `sending`, `sent`, and `failed` states. The window
  also supports image, file, and voice-file uploads.

Sends require the inbound `context_token` for the same recipient, preserved without trimming.
Completing a poll updates the cursor/running status without clearing the stored inbound sender or context.
Replying to a stored inbound record uses that record's sender and context; an invalid reply record is
rejected rather than silently sending to a different conversation. Media sends without a reply record
only reuse the latest context when its sender matches the recipient.

An HTTP success with `ret=0` means the API accepted the request, not that the phone displayed it.
The official response's `message_id` is optional: its absence does not make an accepted request fail.
The official `ret` field is also optional; as in Tencent's sender, a supplied nonzero `ret` is a business
failure, while an omitted `ret` is preserved as absent rather than fabricated as zero.
The existing `sent` state means API submission, not a read/delivery receipt. Business errors still
produce a `failed` record. Native voice sends support SILK and MP3 encoding declarations only; other
audio formats must be sent as files. The module does not transcode audio or invent sample rate,
bit depth, or duration metadata.

### Protocol verification (2026-10-03)

The [Weixin Open Documentation ClawBot page](https://developers.weixin.qq.com/doc/aispeech/knowledge/openapi/Clawbotrelated.html)
documents the `/api/v1/wechat/*` login/channel wrapper, not the full direct iLink `sendmessage` contract.
The direct protocol was cross-checked against Tencent's published
[`@tencent-weixin/openclaw-weixin` 2.4.9 source](https://registry.npmjs.org/@tencent-weixin/openclaw-weixin/-/openclaw-weixin-2.4.9.tgz),
particularly `src/api/types.ts`, `src/api/api.ts`, `src/messaging/send.ts`, and `src/cdn/upload.ts`.

- JSON property names are case-sensitive on output: `type`, `text_item.text`, `image_item.media`,
  `voice_item.media`, and `file_item.media` / `file_item.len` must not use C# property casing.
- Text, image, voice, and file messages all use `message_type: 2` (BOT) and `message_state: 2`
  (FINISH). The content type is `item_list[].type`: text=1, image=2, voice=3, file=4.
- Outbound `msg` includes `from_user_id: ""`, the recipient, `client_id`, message type/state, items,
  and context; `run_id` is optional. Server-only inbound fields and null payload branches are omitted.
- JSON POSTs use `AuthorizationType: ilink_bot_token`, `Authorization: Bearer <bot_token>`,
  a random uint32 decimal string encoded as base64 in `X-WECHAT-UIN`, and `Content-Type: application/json`.
  App headers are `iLink-App-Id: bot` and `iLink-App-ClientVersion: 132105`; `base_info.channel_version`
  is `2.4.9`, with NCF identified by `bot_agent`. QR creation is a token-less POST; status polling is GET.
- Upload requests use `rawsize` / `rawfilemd5` for plaintext, `filesize` for PKCS7-padded
  AES-128-ECB ciphertext, and a lowercase hex `aeskey`. Following the official sender, the
  outgoing media `aes_key` is base64 of that hex string; file `len` is plaintext size as a string.
- Contract tests compare complete parsed JSON objects (including field names and value types),
  authenticated request headers, business-error handling, optional response fields, and uint64 IDs.
  Pipeline tests exercise the actual send services, decrypt their uploaded ciphertext to verify
  plaintext/MD5/sizes/key encoding, and confirm that polling preserves a usable reply context.

After deploying, send a fresh message from the phone and reply from NCF to that inbound record.
Verify that the actual phone displays the text and media; unit tests and `ret=0` alone cannot prove delivery.

Concrete Admin Chat, NeuBell, Workflow, and Harness routing is implemented by the upper-layer Admin
integration rather than this channel module.

### Hosted-service tuning

The background WeixinClaw poller can be tuned with the `SenparcXncfWeixinManager:WeixinClaw:HostedService`
configuration section:

- `ScanIntervalSeconds` - account discovery interval, clamped to 1-300 seconds.
- `ErrorRetryDelaySeconds` - delay after `getupdates` returns a business error, clamped to 1-300 seconds.
- `ShutdownWaitSeconds` - host shutdown grace period for in-flight poll tasks, clamped to 1-60 seconds.
- `NotifyStopTimeoutSeconds` - timeout for the best-effort `notifystop` call, clamped to 1-30 seconds.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.WeixinManager" Version="0.24.12" />
```

## Key API

- `MpAccountService` manages account configuration and `MpAccount` records.
- `WeixinService` provides module-level WeChat operations.
- `WeixinUser`/`WeixinUserDto` and `UserTag`/`UserTag_WeixinUserDto` represent synchronized user and tag data.
- `MpMessageHandlerAttribute` and `XncfMpMessageHandler` connect incoming WeChat messages to NCF handlers.
- `WeixinTemplateBase` and the `WeixinTemplate_*` types support reusable template messages.
- `FindWeixinApiController` exposes API discovery/management endpoints.

Store AppSecret, access tokens, and encryption keys in secure configuration. Validate WeChat signatures, scope account access by tenant/administrator, and treat synchronized user data as personal information.
