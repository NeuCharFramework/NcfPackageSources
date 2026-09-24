using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Services;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawLoginService
{
    private readonly WeixinClawApi _api;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, LoginSession> _sessions = new();

    public WeixinClawLoginService(
        WeixinClawApi api,
        IServiceScopeFactory scopeFactory)
    {
        _api = api;
        _scopeFactory = scopeFactory;
    }

    public async Task<WeixinClawLoginStatus> StartAsync(
        string name,
        string promptRangeCode,
        CancellationToken cancellationToken = default)
    {
        var qr = await _api.GetQrCodeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(qr.Qrcode) || string.IsNullOrWhiteSpace(qr.QrcodeImageContent))
        {
            throw new InvalidOperationException("微信 Claw 服务没有返回有效二维码。");
        }

        var sessionId = Guid.NewGuid().ToString("N");
        _sessions[sessionId] = new LoginSession
        {
            Id = sessionId,
            Name = string.IsNullOrWhiteSpace(name) ? "个人微信" : name.Trim(),
            PromptRangeCode = promptRangeCode?.Trim(),
            Qrcode = qr.Qrcode,
            QrcodeImageContent = qr.QrcodeImageContent,
            ApiBaseUrl = WeixinClawProtocol.DefaultBaseUrl,
            StartedAt = DateTimeOffset.UtcNow,
            Status = "wait"
        };
        return ToStatus(_sessions[sessionId]);
    }

    public async Task<WeixinClawLoginStatus> PollAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return new WeixinClawLoginStatus(sessionId, "expired", null, null, null, null, null, "登录会话不存在或已过期。");
        }
        if (DateTimeOffset.UtcNow - session.StartedAt > TimeSpan.FromMinutes(10))
        {
            session.Status = "expired";
            session.Error = "二维码已过期，请重新获取。";
            return ToStatus(session);
        }
        if (session.Status == "confirmed")
        {
            return ToStatus(session);
        }

        await session.PollLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (DateTimeOffset.UtcNow - session.StartedAt > TimeSpan.FromMinutes(10))
            {
                session.Status = "expired";
                session.Error = "二维码已过期，请重新获取。";
                return ToStatus(session);
            }
            if (session.Status == "confirmed")
            {
                return ToStatus(session);
            }

            var response = await _api.GetQrCodeStatusAsync(
                session.Qrcode,
                baseUrl: session.ApiBaseUrl,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            session.Status = response.Status ?? "wait";

            if (string.Equals(response.Status, "scanned_but_redirect", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(response.RedirectHost))
            {
                session.ApiBaseUrl = response.RedirectHost.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? response.RedirectHost
                    : "https://" + response.RedirectHost;
            }

            if (string.Equals(response.Status, "confirmed", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(response.BotToken) ||
                    string.IsNullOrWhiteSpace(response.IlinkBotId) ||
                    string.IsNullOrWhiteSpace(response.IlinkUserId))
                {
                    session.Status = "error";
                    session.Error = "登录成功响应缺少 bot token 或账号标识。";
                    return ToStatus(session);
                }

                var effectiveBaseUrl = string.IsNullOrWhiteSpace(response.BaseUrl)
                    ? session.ApiBaseUrl
                    : response.BaseUrl;

                var account = new WeixinClawAccountDto
                {
                    Name = session.Name,
                    BaseUrl = effectiveBaseUrl,
                    BotToken = response.BotToken,
                    IlinkBotId = response.IlinkBotId,
                    IlinkUserId = response.IlinkUserId,
                    PromptRangeCode = session.PromptRangeCode,
                    Enabled = true
                };
                using var scope = _scopeFactory.CreateScope();
                var accountService = scope.ServiceProvider.GetRequiredService<WeixinClawAccountService>();
                var saved = await accountService.SaveSettingsAsync(account).ConfigureAwait(false);
                saved.SetAuthenticated(
                    accountService.ProtectToken(response.BotToken),
                    response.IlinkBotId,
                    response.IlinkUserId,
                    WeixinClawApi.NormalizeBaseUrl(effectiveBaseUrl));
                await accountService.SaveObjectAsync(saved).ConfigureAwait(false);

                session.Status = "confirmed";
                session.AccountId = saved.Id;
                session.AccountBotId = response.IlinkBotId;
                session.AccountUserId = response.IlinkUserId;
                session.Error = null;
            }
            else if (session.Status is "expired" or "verify_code_blocked" or "scanned_but_redirect" or "binded_redirect")
            {
                session.Error = "当前二维码状态：" + session.Status;
            }

            return ToStatus(session);
        }
        finally
        {
            session.PollLock.Release();
        }
    }

    private static WeixinClawLoginStatus ToStatus(LoginSession session)
    {
        return new WeixinClawLoginStatus(
            session.Id,
            session.Status,
            session.QrcodeImageContent,
            session.AccountId,
            session.AccountBotId,
            session.AccountUserId,
            session.StartedAt,
            session.Error);
    }

    private sealed class LoginSession
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PromptRangeCode { get; set; }
        public string Qrcode { get; set; }
        public string QrcodeImageContent { get; set; }
        public string ApiBaseUrl { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
        public int? AccountId { get; set; }
        public string AccountBotId { get; set; }
        public string AccountUserId { get; set; }
        public SemaphoreSlim PollLock { get; } = new(1, 1);
    }
}

public sealed record WeixinClawLoginStatus(
    string SessionId,
    string Status,
    string QrcodeImageContent,
    int? AccountId,
    string AccountBotId,
    string AccountUserId,
    DateTimeOffset? StartedAt,
    string Error);
