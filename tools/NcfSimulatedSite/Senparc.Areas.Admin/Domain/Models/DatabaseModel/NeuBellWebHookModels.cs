/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellWebHookModels.cs
    File description: Related implementation.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260914
    Change description: v0.7.1 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuBellWebHook))]
[Serializable]
public class NeuBellWebHook : EntityBase<int>
{
    /// <summary>
    /// displayname
    /// </summary>
    [Required, MaxLength(200)]
    public string Name { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Required, MaxLength(1000)]
    public string WebHookUrl { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(200)]
    public string ProviderFilter { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(300)]
    public string Secret { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    [Required, MaxLength(10)]
    public string HttpMethod { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public string BodyTemplate { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool NotifyOnAdd { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool NotifyOnRemove { get; private set; }

    /// <summary>
    /// whether enabled
    /// </summary>
    public bool IsEnabled { get; private set; }

    public int AdminUserId { get; private set; }

    private NeuBellWebHook() { }

    public NeuBellWebHook(string name, string webHookUrl, int adminUserId)
    {
        Name = name?.Trim() ?? string.Empty;
        WebHookUrl = webHookUrl?.Trim() ?? string.Empty;
        ProviderFilter = string.Empty;
        Secret = string.Empty;
        HttpMethod = MethodPost;
        BodyTemplate = string.Empty;
        NotifyOnAdd = true;
        NotifyOnRemove = true;
        IsEnabled = true;
        AdminUserId = adminUserId;
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public const string MethodGet = "GET";
    public const string MethodPost = "POST";
    public const string MethodPut = "PUT";

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public const int MaxBodyTemplateLength = 20000;

    public void UpdateInfo(
        string name,
        string webHookUrl,
        string providerFilter,
        string secret,
        bool notifyOnAdd,
        bool notifyOnRemove,
        bool isEnabled,
        string httpMethod = null,
        string bodyTemplate = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }
        if (!string.IsNullOrWhiteSpace(webHookUrl))
        {
            WebHookUrl = webHookUrl.Trim();
        }
        ProviderFilter = (providerFilter ?? string.Empty).Trim();
        Secret = (secret ?? string.Empty).Trim();
        NotifyOnAdd = notifyOnAdd;
        NotifyOnRemove = notifyOnRemove;
        IsEnabled = isEnabled;
        if (httpMethod != null)
        {
            HttpMethod = NormalizeHttpMethod(httpMethod);
        }
        if (bodyTemplate != null)
        {
            BodyTemplate = bodyTemplate;
        }
        SetUpdateTime();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static string NormalizeHttpMethod(string input)
    {
        var value = (input ?? string.Empty).Trim().ToUpperInvariant();
        return value is MethodGet or MethodPut ? value : MethodPost;
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static bool TryValidateHttpMethod(string input, out string normalized, out string error)
    {
        var value = (input ?? string.Empty).Trim().ToUpperInvariant();
        normalized = value;
        if (value.Length == 0 || value is MethodGet or MethodPost or MethodPut)
        {
            normalized = NormalizeHttpMethod(input);
            error = null;
            return true;
        }
        error = $"请求方式必须是 GET、POST 或 PUT（当前为“{input?.Trim()}”）";
        return false;
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static bool TryValidateUrl(string url, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "WebHook 地址不能为空";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "WebHook 地址必须是合法的 http/https 绝对地址";
            return false;
        }

        return true;
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public static bool TryValidateUrlTemplate(string url, out string error)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "WebHook 地址不能为空";
            return false;
        }

        var masked = NeuBellWebHookTemplate.MaskTokens(url.Trim());
        return TryValidateUrl(masked, out error);
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool MatchesProvider(string providerId)
    {
        return string.IsNullOrWhiteSpace(ProviderFilter)
               || string.Equals(ProviderFilter, providerId, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuBellWebHookLog))]
[Serializable]
public class NeuBellWebHookLog : EntityBase<int>
{
    public const string StatusSending = "sending";
    public const string StatusSuccess = "success";
    public const string StatusFailed = "failed";

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Required, MaxLength(50)]
    public string EventKind { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(10)]
    public string HttpMethod { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Required, MaxLength(1000)]
    public string WebHookUrl { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(200)]
    public string ProviderId { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(500)]
    public string Title { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public string Payload { get; private set; }

    /// <summary>
    /// executestatus：sending / success / failed
    /// </summary>
    [Required, MaxLength(20)]
    public string Status { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MaxLength(2000)]
    public string Result { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int? StatusCode { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public long ElapsedMilliseconds { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public DateTime? FinishTime { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int AdminUserId { get; private set; }

    private NeuBellWebHookLog() { }

    public NeuBellWebHookLog(
        string eventKind,
        string httpMethod,
        string webHookUrl,
        string providerId,
        string title,
        string payload,
        int adminUserId)
    {
        EventKind = (eventKind ?? string.Empty).Trim();
        HttpMethod = httpMethod ?? string.Empty;
        WebHookUrl = (webHookUrl ?? string.Empty).Trim();
        ProviderId = (providerId ?? string.Empty).Trim();
        Title = (title ?? string.Empty).Trim();
        Payload = payload ?? string.Empty;
        AdminUserId = adminUserId;
        Status = StatusSending;
        Result = string.Empty;
    }

    /// <summary>
    /// recordrequestresult
    /// </summary>
    public void MarkFinished(bool success, string result, int? statusCode, long elapsedMilliseconds)
    {
        Status = success ? StatusSuccess : StatusFailed;
        Result = (result ?? string.Empty).Trim();
        if (Result.Length > 2000)
        {
            Result = Result[..2000];
        }
        StatusCode = statusCode;
        ElapsedMilliseconds = elapsedMilliseconds;
        FinishTime = DateTime.Now;
        SetUpdateTime();
    }
}
