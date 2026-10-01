using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Services;
using System;
using System.Collections.Generic;
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
        var targetUserId = replyRecord?.FromUserId
            ?? (string.IsNullOrWhiteSpace(toUserId)
                ? account.LastMessageFromUserId ?? account.IlinkUserId
                : toUserId.Trim());
        var contextToken = replyRecord == null
            ? _accountService.UnprotectContextToken(account)
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
            AesKey = Convert.ToBase64String(aesKey),
            EncryptType = 1
        };

        var item = new WeixinClawMessageItem { Type = (int)GetMessageItemType(kind) };
        var displayName = string.IsNullOrWhiteSpace(fileName) ? "微信媒体" : Path.GetFileName(fileName);
        switch (kind)
        {
            case WeixinClawMediaKind.Image:
                item.ImageItem = new WeixinClawImageItem
                {
                    Media = media,
                    MidSize = encrypted.Length,
                    HdSize = encrypted.Length
                };
                break;
            case WeixinClawMediaKind.Voice:
                item.VoiceItem = new WeixinClawVoiceItem
                {
                    Media = media,
                    EncodeType = GetVoiceEncodeType(displayName, contentType),
                    SampleRate = 16000,
                    BitsPerSample = 16,
                    Playtime = 0
                };
                break;
            default:
                item.FileItem = new WeixinClawFileItem
                {
                    Media = media,
                    FileName = displayName,
                    Len = bytes.Length.ToString()
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
            if (response.Ret != 0 || string.IsNullOrWhiteSpace(response.MessageId))
            {
                var error = response.Ret != 0
                    ? $"媒体发送失败：{response.Ret} {response.Errmsg}"
                    : "iLink 接受媒体请求但没有返回 message_id，未确认进入下行队列。";
                await _recordService.MarkFailedAsync(record.Id, error).ConfigureAwait(false);
                throw new InvalidOperationException(error);
            }

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
            _ => "微信文件.bin"
        };
    }

    private static string GuessContentType(string kind, string fileName, byte[] bytes)
    {
        if (bytes?.Length >= 12)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return "image/gif";
            if (Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF"
                && Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") return "image/webp";
        }

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => kind == "voice" ? "audio/silk" : "application/octet-stream"
        };
    }

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
        var value = (fileName + " " + contentType).ToLowerInvariant();
        if (value.Contains("silk")) return 6;
        if (value.Contains("mp3") || value.Contains("mpeg")) return 7;
        return 8;
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
