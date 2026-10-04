/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawMediaService.cs
    文件功能描述：WeixinClawMediaService.cs implementation and project behavior.


    创建标识：Senparc - 20260930

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public enum WeixinClawMediaKind
{
    Image = 1,
    File = 3,
    Voice = 4
}

public sealed class WeixinClawMediaService
{
    private readonly WeixinClawAccountService _accountService;
    private readonly WeixinClawMessageRecordService _recordService;
    private readonly WeixinClawApi _api;
    private readonly WeixinClawMediaStorageService _storage;
    private readonly WeixinClawFileManagerBridge _fileManagerBridge;

    public WeixinClawMediaService(
        WeixinClawAccountService accountService,
        WeixinClawMessageRecordService recordService,
        WeixinClawApi api,
        WeixinClawMediaStorageService storage,
        WeixinClawFileManagerBridge fileManagerBridge)
    {
        _accountService = accountService;
        _recordService = recordService;
        _api = api;
        _storage = storage;
        _fileManagerBridge = fileManagerBridge;
    }

    public async Task<List<WeixinClawStoredMedia>> DownloadInboundAsync(
        int tenantId,
        int accountId,
        string identity,
        IReadOnlyList<WeixinClawMessageItem> items,
        CancellationToken cancellationToken = default)
    {
        var storedItems = new List<WeixinClawStoredMedia>();
        var index = 0;
        foreach (var item in items ?? Array.Empty<WeixinClawMessageItem>())
        {
            var media = GetInboundMedia(item, out var kind, out var name);
            if (media == null)
            {
                continue;
            }

            var displayName = string.IsNullOrWhiteSpace(name)
                ? DefaultFileName(kind, item)
                : Path.GetFileName(name);
            var stored = new WeixinClawStoredMedia
            {
                Kind = kind,
                Name = displayName,
                ContentType = GuessContentType(kind, displayName, null),
                Size = 0
            };
            try
            {
                var bytes = await _api.DownloadMediaAsync(media, cancellationToken).ConfigureAwait(false);
                stored.Size = bytes.LongLength;
                stored.ContentType = GuessContentType(kind, displayName, bytes);
                stored.StorageKey = await _storage.SaveAsync(
                    accountId,
                    identity,
                    index,
                    displayName,
                    bytes,
                    cancellationToken).ConfigureAwait(false);
                await _fileManagerBridge.ImportAsync(
                    tenantId,
                    accountId,
                    identity,
                    stored,
                    bytes,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                stored.Error = ex.Message;
            }

            storedItems.Add(stored);
            index++;
        }

        return storedItems;
    }

    public async Task<WeixinClawSendTextResult> SendAsync(
        int accountId,
        string fileName,
        string contentType,
        byte[] bytes,
        WeixinClawMediaKind kind,
        string toUserId = null,
        int? replyToRecordId = null,
        CancellationToken cancellationToken = default)
    {
        if (bytes == null || bytes.Length == 0)
        {
            throw new ArgumentException("媒体内容不能为空。", nameof(bytes));
        }
        if (bytes.Length > 50 * 1024 * 1024)
        {
            throw new InvalidOperationException("媒体文件不能超过 50 MB。");
        }
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), "不支持的微信媒体类型。");
        }
        var displayName = string.IsNullOrWhiteSpace(fileName) ? "微信媒体" : Path.GetFileName(fileName);
        var voiceEncodeType = kind == WeixinClawMediaKind.Voice
            ? GetVoiceEncodeType(displayName, contentType)
            : 0;

        var account = await _accountService.GetObjectAsync(item => item.Id == accountId)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("个人微信账号不存在或已删除。");
        var botToken = _accountService.UnprotectToken(account);
        if (string.IsNullOrWhiteSpace(botToken))
        {
            throw new InvalidOperationException("个人微信账号未连接或 token 无法解密。");
        }

        var replyRecord = replyToRecordId.HasValue
            ? await _recordService.GetInboundRecordAsync(accountId, replyToRecordId.Value).ConfigureAwait(false)
            : null;
        if (replyToRecordId.HasValue && replyRecord == null)
        {
            throw new InvalidOperationException("指定的入站消息不存在或不属于当前账号。");
        }
        var targetUserId = replyRecord?.FromUserId
            ?? (string.IsNullOrWhiteSpace(toUserId)
                ? account.LastMessageFromUserId ?? account.IlinkUserId
                : toUserId.Trim());
        var contextToken = replyRecord == null
            ? (string.Equals(targetUserId, account.LastMessageFromUserId, StringComparison.Ordinal)
                ? _accountService.UnprotectContextToken(account)
                : null)
            : _accountService.UnprotectContextToken(replyRecord.ContextTokenProtected);
        if (string.IsNullOrWhiteSpace(targetUserId) || string.IsNullOrWhiteSpace(contextToken))
        {
            throw new InvalidOperationException("没有可用的最近微信会话。请先让手机向微信 ClawBot 发送一条消息。");
        }

        var plainMd5 = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();
        var aesKey = RandomNumberGenerator.GetBytes(16);
        var aesKeyHex = Convert.ToHexString(aesKey).ToLowerInvariant();
        var encrypted = EncryptAesEcb(bytes, aesKey);
        var fileKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var uploadResponse = await _api.GetUploadUrlAsync(
            account.BaseUrl,
            botToken,
            new WeixinClawGetUploadUrlRequest
            {
                FileKey = fileKey,
                MediaType = (int)kind,
                ToUserId = targetUserId,
                RawSize = bytes.Length,
                RawFileMd5 = plainMd5,
                FileSize = encrypted.Length,
                NoNeedThumb = true,
                AesKey = aesKeyHex
            },
            cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(uploadResponse.UploadFullUrl)
            && string.IsNullOrWhiteSpace(uploadResponse.UploadParam))
        {
            throw new InvalidOperationException("getuploadurl 没有返回 upload_full_url 或 upload_param。");
        }

        var encryptedQueryParam = await _api.UploadMediaAsync(
            uploadResponse.UploadFullUrl,
            uploadResponse.UploadParam,
            fileKey,
            encrypted,
            cancellationToken).ConfigureAwait(false);
        var media = new WeixinClawCdnMedia
        {
            EncryptQueryParam = encryptedQueryParam,
            // Official plugin sends the base64 representation of the hex AES key.
            AesKey = Convert.ToBase64String(Encoding.ASCII.GetBytes(aesKeyHex)),
            EncryptType = 1
        };

        var item = new WeixinClawMessageItem { Type = (int)GetMessageItemType(kind) };
        switch (kind)
        {
            case WeixinClawMediaKind.Image:
                item.ImageItem = new WeixinClawImageItem
                {
                    Media = media,
                    MidSize = encrypted.Length
                };
                break;
            case WeixinClawMediaKind.Voice:
                item.VoiceItem = new WeixinClawVoiceItem
                {
                    Media = media,
                    EncodeType = voiceEncodeType
                };
                break;
            default:
                item.FileItem = new WeixinClawFileItem
                {
                    Media = media,
                    FileName = displayName,
                    Len = bytes.Length.ToString(CultureInfo.InvariantCulture)
                };
                break;
        }

        var message = new WeixinClawMessage
        {
            ToUserId = targetUserId,
            ClientId = Guid.NewGuid().ToString("N"),
            MessageType = 2,
            MessageState = 2,
            ContextToken = contextToken,
            RunId = replyRecord?.RunId,
            ItemList = [item]
        };
        var record = await _recordService.AddOutboundPendingAsync(
            accountId,
            message.ClientId,
            account.IlinkUserId,
            targetUserId,
            message.MessageType,
            message.MessageState,
            $"[{kind}] {displayName}").ConfigureAwait(false);
        try
        {
            var storedMedia = new WeixinClawStoredMedia
            {
                Kind = kind.ToString().ToLowerInvariant(),
                Name = displayName,
                ContentType = GuessContentType(kind.ToString(), displayName, bytes),
                Size = bytes.LongLength,
                StorageKey = await _storage.SaveAsync(
                    accountId,
                    record.Id.ToString(),
                    0,
                    displayName,
                    bytes,
                    cancellationToken).ConfigureAwait(false)
            };
            record.SetText(WeixinClawMessageContent.Serialize(null, [storedMedia]));
            await _recordService.SaveObjectAsync(record).ConfigureAwait(false);

            var response = await _api.SendMessageAsync(
                account.BaseUrl,
                botToken,
                message,
                cancellationToken).ConfigureAwait(false);
            await _recordService.MarkSentAsync(record.Id, response.MessageId).ConfigureAwait(false);
            return new WeixinClawSendTextResult(record.Id, response.MessageId, targetUserId, true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _recordService.MarkFailedAsync(record.Id, ex.Message).ConfigureAwait(false);
            throw;
        }
    }

    private static WeixinClawCdnMedia GetInboundMedia(
        WeixinClawMessageItem item,
        out string kind,
        out string name)
    {
        kind = null;
        name = null;
        if (item == null)
        {
            return null;
        }

        switch (item.Type)
        {
            case 2 when item.ImageItem?.Media != null:
                kind = "image";
                return item.ImageItem.Media;
            case 3 when item.VoiceItem?.Media != null:
                kind = "voice";
                return item.VoiceItem.Media;
            case 4 when item.FileItem?.Media != null:
                kind = "file";
                name = item.FileItem.FileName;
                return item.FileItem.Media;
            case 5 when item.VideoItem?.Media != null:
                kind = "video";
                return item.VideoItem.Media;
            default:
                return null;
        }
    }

    private static string DefaultFileName(string kind, WeixinClawMessageItem item)
    {
        return kind switch
        {
            "image" => "微信图片.jpg",
            "voice" => item?.VoiceItem?.EncodeType == 6 ? "微信语音.silk" : "微信语音.bin",
            "video" => "微信视频.mp4",
            _ => "微信文件.bin"
        };
    }

    private static string GuessContentType(string kind, string fileName, byte[] bytes)
        => WeixinClawMediaFormat.GetContentType(kind, fileName, bytes);

    private static byte[] EncryptAesEcb(byte[] plain, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plain, 0, plain.Length);
    }

    private static int GetVoiceEncodeType(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".silk") return 6;
        if (extension == ".mp3") return 7;
        var mimeType = contentType?.Split(';')[0].Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            if (mimeType is "audio/silk" or "audio/x-silk") return 6;
            if (mimeType is "audio/mpeg" or "audio/mp3") return 7;
        }
        throw new InvalidOperationException(
            "语音消息仅支持 SILK 或 MP3；其他音频格式请使用文件发送，不能标记为 OGG-Speex。");
    }

    private static WeixinClawMessageItemType GetMessageItemType(WeixinClawMediaKind kind) =>
        kind switch
        {
            WeixinClawMediaKind.Image => WeixinClawMessageItemType.Image,
            WeixinClawMediaKind.Voice => WeixinClawMessageItemType.Voice,
            _ => WeixinClawMessageItemType.File
        };
}

public enum WeixinClawMessageItemType
{
    Image = 2,
    Voice = 3,
    File = 4
}
