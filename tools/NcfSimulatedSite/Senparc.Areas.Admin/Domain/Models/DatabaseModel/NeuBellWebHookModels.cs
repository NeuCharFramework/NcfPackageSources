/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookModels.cs
    文件功能描述：NeuBell WebHook（WebAPI）设置实体：
    为纽铃（NeuBell）提供外部 WebHook 通知能力，当纽铃条目新增（added）
    或移除（removed）时，按配置以异步方式请求对应的 WebHook 地址

    创建标识：Senparc - 20260906

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// 纽铃 WebHook 设置：一条记录对应一个外部 WebAPI（WebHook）通知端点。
/// 当某个纽铃 Provider 的条目发生新增或移除时，Dispatcher 会按
/// <see cref="ProviderFilter"/> 匹配并异步 POST 通知。
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuBellWebHook))]
[Serializable]
public class NeuBellWebHook : EntityBase<int>
{
    /// <summary>
    /// 显示名称
    /// </summary>
    [Required, MaxLength(200)]
    public string Name { get; private set; }

    /// <summary>
    /// WebHook（WebAPI）请求地址，仅允许 http/https
    /// </summary>
    [Required, MaxLength(1000)]
    public string WebHookUrl { get; private set; }

    /// <summary>
    /// 只通知指定纽铃 Provider（ProviderId）；留空表示全部 Provider
    /// </summary>
    [MaxLength(200)]
    public string ProviderFilter { get; private set; }

    /// <summary>
    /// 可选签名密钥：配置后请求会携带 X-NeuBell-Signature（HMAC-SHA256）请求头
    /// </summary>
    [MaxLength(300)]
    public string Secret { get; private set; }

    /// <summary>
    /// 条目新增（added）时是否通知
    /// </summary>
    public bool NotifyOnAdd { get; private set; }

    /// <summary>
    /// 条目移除（removed）时是否通知
    /// </summary>
    public bool NotifyOnRemove { get; private set; }

    /// <summary>
    /// 是否启用
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
        NotifyOnAdd = true;
        NotifyOnRemove = true;
        IsEnabled = true;
        AdminUserId = adminUserId;
    }

    public void UpdateInfo(
        string name,
        string webHookUrl,
        string providerFilter,
        string secret,
        bool notifyOnAdd,
        bool notifyOnRemove,
        bool isEnabled)
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
        SetUpdateTime();
    }

    /// <summary>
    /// 校验 WebHook 地址是否合法（仅允许 http/https，且必须可解析为绝对 URI）
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
    /// 该 WebHook 是否关注指定 Provider（ProviderFilter 留空表示全部）
    /// </summary>
    public bool MatchesProvider(string providerId)
    {
        return string.IsNullOrWhiteSpace(ProviderFilter)
               || string.Equals(ProviderFilter, providerId, StringComparison.OrdinalIgnoreCase);
    }
}
