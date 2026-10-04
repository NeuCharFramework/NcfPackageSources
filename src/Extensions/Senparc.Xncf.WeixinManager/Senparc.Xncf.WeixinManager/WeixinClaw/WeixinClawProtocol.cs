/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawProtocol.cs
    文件功能描述：WeixinClawProtocol.cs implementation and project behavior.


    创建标识：Senparc - 20260920

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public static class WeixinClawProtocol
{
    public const string DefaultBaseUrl = "https://ilinkai.weixin.qq.com";
    public const string AppId = "bot";
    // Keep the wire compatibility marker aligned with the current official
    // openclaw-weixin protocol while using bot_agent to identify NCF.
    public const string ChannelVersion = "2.4.8";
    public const int ClientVersion = (2 << 16) | (4 << 8) | 8;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static string BuildWechatUin()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(BitConverter.ToUInt32(bytes).ToString()));
    }
}

public sealed class WeixinClawApi
{
    public const string DefaultCdnBaseUrl = "https://novac2c.cdn.weixin.qq.com/c2c";
    private readonly HttpClient _httpClient;

    public WeixinClawApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeixinClawQrCodeResponse> GetQrCodeAsync(
        string baseUrl = null,
        string botType = "3",
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"ilink/bot/get_bot_qrcode?bot_type={Uri.EscapeDataString(botType)}";
        // QR login is an unauthenticated POST, but the iLink protocol still
        // requires AuthorizationType and X-WECHAT-UIN on JSON POST requests.
        using var request = CreateRequest(HttpMethod.Post, baseUrl, endpoint, includeAuthorization: true);
        request.Content = CreateJsonContent(new { local_token_list = Array.Empty<string>() });
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            return await SendAsync<WeixinClawQrCodeResponse>(request, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("iLink 二维码请求超过 30 秒仍未返回。", ex);
        }
    }

    public async Task<WeixinClawQrCodeStatusResponse> GetQrCodeStatusAsync(
        string qrcode,
        string verifyCode = null,
        string baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"ilink/bot/get_qrcode_status?qrcode={Uri.EscapeDataString(qrcode)}";
        if (!string.IsNullOrWhiteSpace(verifyCode))
        {
            endpoint += $"&verify_code={Uri.EscapeDataString(verifyCode)}";
        }

        using var request = CreateRequest(HttpMethod.Get, baseUrl, endpoint, includeAuthorization: false);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(35));
        return await SendAsync<WeixinClawQrCodeStatusResponse>(request, timeoutSource.Token).ConfigureAwait(false);
    }

    public Task<WeixinClawGetUpdatesResponse> GetUpdatesAsync(
        string baseUrl,
        string botToken,
        string getUpdatesBuf,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<WeixinClawGetUpdatesResponse>(
            baseUrl,
            "ilink/bot/getupdates",
            new
            {
                get_updates_buf = getUpdatesBuf ?? string.Empty,
                base_info = CreateBaseInfo()
            },
            botToken,
            TimeSpan.FromSeconds(40),
            cancellationToken);
    }

    public Task<WeixinClawSendMessageResponse> SendMessageAsync(
        string baseUrl,
        string botToken,
        WeixinClawMessage message,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<WeixinClawSendMessageResponse>(
            baseUrl,
            "ilink/bot/sendmessage",
            new
            {
                msg = message,
                base_info = CreateBaseInfo()
            },
            botToken,
            TimeSpan.FromSeconds(15),
            cancellationToken);
    }

    public Task<WeixinClawGetUploadUrlResponse> GetUploadUrlAsync(
        string baseUrl,
        string botToken,
        WeixinClawGetUploadUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        request.BaseInfo ??= CreateBaseInfo();
        return PostAsync<WeixinClawGetUploadUrlResponse>(
            baseUrl,
            "ilink/bot/getuploadurl",
            request,
            botToken,
            TimeSpan.FromSeconds(15),
            cancellationToken);
    }

    public async Task<string> UploadMediaAsync(
        string uploadFullUrl,
        string uploadParam,
        string fileKey,
        byte[] encryptedBytes,
        CancellationToken cancellationToken = default)
    {
        var url = !string.IsNullOrWhiteSpace(uploadFullUrl)
            ? uploadFullUrl
            : $"{DefaultCdnBaseUrl}/upload?encrypted_query_param={Uri.EscapeDataString(uploadParam ?? string.Empty)}&filekey={Uri.EscapeDataString(fileKey)}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(encryptedBytes)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(60));
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeoutSource.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(timeoutSource.Token).ConfigureAwait(false);
            throw new HttpRequestException(
                $"微信媒体上传失败：{(int)response.StatusCode} {LimitForError(content)}");
        }

        if (!response.Headers.TryGetValues("x-encrypted-param", out var values))
        {
            throw new InvalidOperationException("微信媒体上传响应缺少 x-encrypted-param。");
        }

        return values.FirstOrDefault();
    }

    public async Task<byte[]> DownloadMediaAsync(
        WeixinClawCdnMedia media,
        CancellationToken cancellationToken = default)
    {
        if (media == null || string.IsNullOrWhiteSpace(media.EncryptQueryParam))
        {
            throw new InvalidOperationException("微信媒体缺少 encrypt_query_param。");
        }

        var url =
            $"{DefaultCdnBaseUrl}/download?encrypted_query_param={Uri.EscapeDataString(media.EncryptQueryParam)}";
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(60));
        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            timeoutSource.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(timeoutSource.Token).ConfigureAwait(false);
            throw new HttpRequestException(
                $"微信媒体下载失败：{(int)response.StatusCode} {LimitForError(content)}");
        }

        var encrypted = await response.Content.ReadAsByteArrayAsync(timeoutSource.Token).ConfigureAwait(false);
        if (media.EncryptType == 0 || string.IsNullOrWhiteSpace(media.AesKey))
        {
            return encrypted;
        }

        return DecryptAesEcb(encrypted, ParseAesKey(media.AesKey));
    }

    public Task<WeixinClawGetConfigResponse> GetConfigAsync(
        string baseUrl,
        string botToken,
        string ilinkUserId,
        string contextToken = null,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<WeixinClawGetConfigResponse>(
            baseUrl,
            "ilink/bot/getconfig",
            new
            {
                ilink_user_id = ilinkUserId,
                context_token = contextToken,
                base_info = CreateBaseInfo()
            },
            botToken,
            TimeSpan.FromSeconds(10),
            cancellationToken);
    }

    public async Task SendTypingAsync(
        string baseUrl,
        string botToken,
        string ilinkUserId,
        string typingTicket,
        int status,
        CancellationToken cancellationToken = default)
    {
        await PostAsync<WeixinClawEmptyResponse>(
            baseUrl,
            "ilink/bot/sendtyping",
            new
            {
                ilink_user_id = ilinkUserId,
                typing_ticket = typingTicket,
                status,
                base_info = CreateBaseInfo()
            },
            botToken,
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<WeixinClawEmptyResponse> NotifyStartAsync(
        string baseUrl,
        string botToken,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<WeixinClawEmptyResponse>(
            baseUrl,
            "ilink/bot/msg/notifystart",
            new { base_info = CreateBaseInfo() },
            botToken,
            TimeSpan.FromSeconds(10),
            cancellationToken);
    }

    public Task<WeixinClawEmptyResponse> NotifyStopAsync(
        string baseUrl,
        string botToken,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<WeixinClawEmptyResponse>(
            baseUrl,
            "ilink/bot/msg/notifystop",
            new { base_info = CreateBaseInfo() },
            botToken,
            TimeSpan.FromSeconds(10),
            cancellationToken);
    }

    private async Task<T> PostAsync<T>(
        string baseUrl,
        string endpoint,
        object body,
        string botToken,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            baseUrl,
            endpoint,
            includeAuthorization: true,
            botToken);
        request.Content = CreateJsonContent(body);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        return await SendAsync<T>(request, timeoutSource.Token).ConfigureAwait(false);
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string baseUrl,
        string endpoint,
        bool includeAuthorization,
        string botToken = null)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(NormalizeBaseUrl(baseUrl)), endpoint));
        request.Headers.TryAddWithoutValidation("iLink-App-Id", WeixinClawProtocol.AppId);
        request.Headers.TryAddWithoutValidation(
            "iLink-App-ClientVersion",
            WeixinClawProtocol.ClientVersion.ToString());

        if (includeAuthorization)
        {
            request.Headers.TryAddWithoutValidation("AuthorizationType", "ilink_bot_token");
            request.Headers.TryAddWithoutValidation("X-WECHAT-UIN", WeixinClawProtocol.BuildWechatUin());
            if (!string.IsNullOrWhiteSpace(botToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botToken.Trim());
            }
        }

        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Weixin Claw API returned {(int)response.StatusCode}: {LimitForError(content)}");
        }

        var result = JsonSerializer.Deserialize<T>(content, WeixinClawProtocol.JsonOptions);
        if (result == null)
        {
            throw new InvalidOperationException("Weixin Claw API returned an empty JSON response.");
        }

        return result;
    }

    private static HttpContent CreateJsonContent(object body)
    {
        var content = new ByteArrayContent(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body, WeixinClawProtocol.JsonOptions)));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    public static string NormalizeBaseUrl(string baseUrl)
    {
        return string.IsNullOrWhiteSpace(baseUrl)
            ? WeixinClawProtocol.DefaultBaseUrl + "/"
            : baseUrl.TrimEnd('/') + "/";
    }

    private static WeixinClawBaseInfo CreateBaseInfo() => new()
    {
        ChannelVersion = WeixinClawProtocol.ChannelVersion,
        BotAgent = "NCF-WeixinManager/" + WeixinClawProtocol.ChannelVersion
    };

    private static string LimitForError(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return "(empty)";
        }

        return content.Length <= 500 ? content : content[..500];
    }

    private static byte[] ParseAesKey(string value)
    {
        var decoded = Convert.FromBase64String(value);
        if (decoded.Length == 16)
        {
            return decoded;
        }

        var text = Encoding.ASCII.GetString(decoded);
        if (text.Length == 32 && text.All(IsHex))
        {
            return Convert.FromHexString(text);
        }

        throw new InvalidOperationException("微信媒体 AES key 不是有效的 16 字节密钥。");
    }

    private static bool IsHex(char value) =>
        value is >= '0' and <= '9'
            or >= 'a' and <= 'f'
            or >= 'A' and <= 'F';

    private static byte[] DecryptAesEcb(byte[] encrypted, byte[] key)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.Mode = System.Security.Cryptography.CipherMode.ECB;
        aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
    }
}

public sealed class WeixinClawBaseInfo
{
    [JsonPropertyName("channel_version")]
    public string ChannelVersion { get; set; }

    [JsonPropertyName("bot_agent")]
    public string BotAgent { get; set; }
}

public sealed class WeixinClawQrCodeResponse
{
    public string Qrcode { get; set; }

    [JsonPropertyName("qrcode_img_content")]
    public string QrcodeImageContent { get; set; }
}

public sealed class WeixinClawQrCodeStatusResponse
{
    public string Status { get; set; }

    [JsonPropertyName("bot_token")]
    public string BotToken { get; set; }

    [JsonPropertyName("ilink_bot_id")]
    public string IlinkBotId { get; set; }

    [JsonPropertyName("baseurl")]
    public string BaseUrl { get; set; }

    [JsonPropertyName("ilink_user_id")]
    public string IlinkUserId { get; set; }

    [JsonPropertyName("redirect_host")]
    public string RedirectHost { get; set; }
}

public sealed class WeixinClawGetUpdatesResponse
{
    public int Ret { get; set; }
    public int? Errcode { get; set; }
    public string Errmsg { get; set; }
    public List<WeixinClawMessage> Msgs { get; set; } = new();

    [JsonPropertyName("get_updates_buf")]
    public string GetUpdatesBuf { get; set; }

    [JsonPropertyName("longpolling_timeout_ms")]
    public int? LongPollingTimeoutMs { get; set; }
}

public sealed class WeixinClawSendMessageResponse
{
    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string MessageId { get; set; }

    public int Ret { get; set; }
    public string Errmsg { get; set; }
}

public sealed class WeixinClawGetConfigResponse
{
    public int Ret { get; set; }
    public string Errmsg { get; set; }

    [JsonPropertyName("typing_ticket")]
    public string TypingTicket { get; set; }
}

public sealed class WeixinClawGetUploadUrlRequest
{
    [JsonPropertyName("filekey")]
    public string FileKey { get; set; }

    [JsonPropertyName("media_type")]
    public int MediaType { get; set; }

    [JsonPropertyName("to_user_id")]
    public string ToUserId { get; set; }

    [JsonPropertyName("rawsize")]
    public int RawSize { get; set; }
    [JsonPropertyName("rawfilemd5")]
    public string RawFileMd5 { get; set; }
    [JsonPropertyName("filesize")]
    public int FileSize { get; set; }
    [JsonPropertyName("no_need_thumb")]
    public bool NoNeedThumb { get; set; } = true;
    [JsonPropertyName("aeskey")]
    public string AesKey { get; set; }

    [JsonPropertyName("base_info")]
    public WeixinClawBaseInfo BaseInfo { get; set; }
}

public sealed class WeixinClawGetUploadUrlResponse
{
    [JsonPropertyName("upload_param")]
    public string UploadParam { get; set; }

    [JsonPropertyName("upload_full_url")]
    public string UploadFullUrl { get; set; }

    [JsonPropertyName("thumb_upload_param")]
    public string ThumbUploadParam { get; set; }
}

public sealed class WeixinClawEmptyResponse
{
    public int Ret { get; set; }
    public string Errmsg { get; set; }
}

public sealed class WeixinClawMessage
{
    public long Seq { get; set; }

    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string MessageId { get; set; }

    [JsonPropertyName("from_user_id")]
    public string FromUserId { get; set; }

    [JsonPropertyName("to_user_id")]
    public string ToUserId { get; set; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("create_time_ms")]
    public long CreateTimeMs { get; set; }

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; }

    [JsonPropertyName("group_id")]
    public string GroupId { get; set; }

    [JsonPropertyName("message_type")]
    public int MessageType { get; set; }

    [JsonPropertyName("message_state")]
    public int MessageState { get; set; }

    [JsonPropertyName("item_list")]
    public List<WeixinClawMessageItem> ItemList { get; set; } = new();

    [JsonPropertyName("context_token")]
    public string ContextToken { get; set; }

    [JsonPropertyName("run_id")]
    public string RunId { get; set; }
}

public sealed class WeixinClawMessageItem
{
    public int Type { get; set; }

    [JsonPropertyName("text_item")]
    public WeixinClawTextItem TextItem { get; set; }

    [JsonPropertyName("image_item")]
    public WeixinClawImageItem ImageItem { get; set; }

    [JsonPropertyName("voice_item")]
    public WeixinClawVoiceItem VoiceItem { get; set; }

    [JsonPropertyName("file_item")]
    public WeixinClawFileItem FileItem { get; set; }
}

public sealed class WeixinClawTextItem
{
    public string Text { get; set; }
}

public sealed class WeixinClawCdnMedia
{
    [JsonPropertyName("encrypt_query_param")]
    public string EncryptQueryParam { get; set; }

    [JsonPropertyName("aes_key")]
    public string AesKey { get; set; }

    [JsonPropertyName("encrypt_type")]
    public int EncryptType { get; set; } = 1;
}

public sealed class WeixinClawImageItem
{
    public WeixinClawCdnMedia Media { get; set; }
    [JsonPropertyName("mid_size")]
    public int MidSize { get; set; }
    [JsonPropertyName("hd_size")]
    public int HdSize { get; set; }
}

public sealed class WeixinClawVoiceItem
{
    public WeixinClawCdnMedia Media { get; set; }
    [JsonPropertyName("encode_type")]
    public int EncodeType { get; set; }
    [JsonPropertyName("sample_rate")]
    public int SampleRate { get; set; }
    [JsonPropertyName("bits_per_sample")]
    public int BitsPerSample { get; set; }
    public int Playtime { get; set; }
}

public sealed class WeixinClawFileItem
{
    public WeixinClawCdnMedia Media { get; set; }
    [JsonPropertyName("file_name")]
    public string FileName { get; set; }
    public string Len { get; set; }
}

public sealed class StringOrNumberConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetUInt64().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Expected a string or number, got {reader.TokenType}.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
