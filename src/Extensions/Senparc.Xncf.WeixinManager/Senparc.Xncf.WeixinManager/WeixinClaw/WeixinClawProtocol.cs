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

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public static class WeixinClawProtocol
{
    public const string DefaultBaseUrl = "https://ilinkai.weixin.qq.com";
    public const string AppId = "bot";
    public const string ChannelVersion = "0.1.0";
    public const int ClientVersion = (2 << 16) | (4 << 8) | 9;

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
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(15));
        return await SendAsync<WeixinClawQrCodeResponse>(request, timeoutSource.Token).ConfigureAwait(false);
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
}

public sealed class WeixinClawMessageItem
{
    public int Type { get; set; }

    [JsonPropertyName("text_item")]
    public WeixinClawTextItem TextItem { get; set; }
}

public sealed class WeixinClawTextItem
{
    public string Text { get; set; }
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
