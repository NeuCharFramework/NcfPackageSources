/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261028
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager;
using Senparc.Xncf.WeixinManager.Domain.Services;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Areas.Admin.Pages.WeixinManager.WeixinClaw;

/// <summary>
/// Serves the administration page and AJAX handlers for personal Weixin Claw accounts,
/// conversations, bindings, login sessions, text messages, and media.
/// </summary>
public class IndexModel : BaseAdminWeixinManagerModel
{
    public List<WeixinClawAccountDto> Accounts { get; private set; } = new();

    private readonly WeixinClawAccountService _accountService;
    private readonly WeixinClawMessageReceiptService _receiptService;
    private readonly WeixinClawLoginService _loginService;
    private readonly WeixinClawMessageService _messageService;
    private readonly WeixinClawMessageRecordService _recordService;
    private readonly WeixinClawBindingProfileService _bindingProfileService;
    private readonly WeixinClawMediaService _mediaService;
    private readonly WeixinClawMediaStorageService _mediaStorage;
    private readonly WeixinClawMediaPlaybackService _mediaPlayback;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes the page model with account, message, login, binding, and media services.
    /// </summary>
    /// <param name="xncfModuleService">The module service required by the base administration model.</param>
    /// <param name="accountService">The service for account configuration and persistence.</param>
    /// <param name="receiptService">The service for deleting account message receipts.</param>
    /// <param name="loginService">The service for starting and polling login sessions.</param>
    /// <param name="messageService">The service for sending text messages.</param>
    /// <param name="recordService">The service for loading message records and conversations.</param>
    /// <param name="bindingProfileService">The service for managing personal binding profiles.</param>
    /// <param name="mediaService">The service for sending media messages.</param>
    /// <param name="mediaStorage">The service for resolving stored media files.</param>
    /// <param name="mediaPlayback">The service for preparing media in browser-playable formats.</param>
    /// <param name="logger">The logger used for media and account operation failures.</param>
    public IndexModel(
        Lazy<XncfModuleService> xncfModuleService,
        WeixinClawAccountService accountService,
        WeixinClawMessageReceiptService receiptService,
        WeixinClawLoginService loginService,
        WeixinClawMessageService messageService,
        WeixinClawMessageRecordService recordService,
        WeixinClawBindingProfileService bindingProfileService,
        WeixinClawMediaService mediaService,
        WeixinClawMediaStorageService mediaStorage,
        WeixinClawMediaPlaybackService mediaPlayback,
        ILogger<IndexModel> logger) : base(xncfModuleService)
    {
        _accountService = accountService;
        _receiptService = receiptService;
        _loginService = loginService;
        _messageService = messageService;
        _recordService = recordService;
        _bindingProfileService = bindingProfileService;
        _mediaService = mediaService;
        _mediaStorage = mediaStorage;
        _mediaPlayback = mediaPlayback;
        _logger = logger;
    }

    /// <summary>
    /// Loads the account list used by the initial administration page.
    /// </summary>
    /// <returns>A task that completes after the account list has been loaded.</returns>
    public async Task OnGetAsync()
    {
        Accounts = await _accountService.GetDtosAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the current account list for asynchronous page refreshes.
    /// </summary>
    /// <returns>An HTTP 200 result containing the account DTO list.</returns>
    public async Task<IActionResult> OnGetAjaxAsync()
    {
        return Ok(new { list = await _accountService.GetDtosAsync().ConfigureAwait(false) });
    }

    /// <summary>
    /// Returns recent message records and converts stored media references into handler URLs.
    /// </summary>
    /// <param name="accountId">The Weixin Claw account identifier.</param>
    /// <param name="peerUserId">Optional conversation peer identifier.</param>
    /// <param name="take">The maximum number of records requested.</param>
    /// <returns>An HTTP 200 result containing the account identifier and recent records.</returns>
    public async Task<IActionResult> OnGetMessagesAsync(int accountId, string peerUserId = null, int take = 100)
    {
        var list = await _recordService.GetRecentDtosAsync(accountId, peerUserId, take)
            .ConfigureAwait(false);
        foreach (var record in list)
        {
            for (var index = 0; index < record.MediaItems.Count; index++)
            {
                record.MediaItems[index].Url =
                    $"/Admin/WeixinManager/WeixinClaw?handler=Media&recordId={record.Id}&index={index}";
                if (record.MediaItems[index].Kind is "voice" or "video")
                {
                    record.MediaItems[index].PlaybackUrl = record.MediaItems[index].Url + "&playback=true";
                }
            }
        }

        return Ok(new
        {
            accountId,
            list
        });
    }

    /// <summary>
    /// Streams one stored media item from a message record, optionally after playback preparation.
    /// </summary>
    /// <param name="recordId">The message record identifier containing the media metadata.</param>
    /// <param name="index">The zero-based media-item index within the record.</param>
    /// <param name="playback">
    /// Whether audio or video should be passed through the playback preparation service before streaming.
    /// </param>
    /// <returns>
    /// A range-enabled physical file result, or an HTTP error when the index, record, or stored file is invalid.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// May be handled and converted to an HTTP 400 result when playback preparation rejects the media.
    /// </exception>
    /// <exception cref="IOException">
    /// May be handled and converted to an HTTP 400 result when playback preparation cannot access the media.
    /// </exception>
    public async Task<IActionResult> OnGetMediaAsync(int recordId, int index = 0, bool playback = false)
    {
        if (index < 0)
        {
            return BadRequest(new { msg = "媒体索引无效。" });
        }

        var record = await _recordService.GetObjectAsync(item => item.Id == recordId)
            .ConfigureAwait(false);
        if (record == null)
        {
            return NotFound();
        }

        var content = WeixinClawMessageContent.Parse(record.Text);
        if (index >= content.MediaItems.Count)
        {
            return NotFound();
        }

        var media = content.MediaItems[index];
        if (!_mediaStorage.TryGetFile(media.StorageKey, out var path))
        {
            return NotFound();
        }

        var contentType = string.IsNullOrWhiteSpace(media.ContentType)
            ? "application/octet-stream"
            : media.ContentType;
        if (playback)
        {
            try
            {
                var playable = await _mediaPlayback.GetPlayableFileAsync(
                    path, media.Kind, contentType, HttpContext.RequestAborted).ConfigureAwait(false);
                path = playable.Path;
                contentType = playable.ContentType;
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException)
            {
                _logger.LogError(ex, "准备微信媒体播放失败：RecordId={RecordId}, Index={Index}", recordId, index);
                return BadRequest(new { msg = "微信媒体无法准备播放，请下载原始文件或查看服务日志。" });
            }
        }

        return new PhysicalFileResult(path, contentType) { EnableRangeProcessing = true };
    }

    /// <summary>
    /// Returns conversation summaries for one Weixin Claw account.
    /// </summary>
    /// <param name="accountId">The Weixin Claw account identifier.</param>
    /// <returns>An HTTP 200 result containing the account identifier and conversation list.</returns>
    public async Task<IActionResult> OnGetConversationsAsync(int accountId)
    {
        return Ok(new
        {
            accountId,
            list = await _recordService.GetConversationsAsync(accountId).ConfigureAwait(false)
        });
    }

    /// <summary>
    /// Returns personal binding profiles associated with an account.
    /// </summary>
    /// <param name="accountId">The Weixin Claw account identifier.</param>
    /// <returns>An HTTP 200 result containing the binding profile DTO list.</returns>
    public async Task<IActionResult> OnGetBindingProfilesAsync(int accountId)
    {
        return Ok(new
        {
            accountId,
            list = await _bindingProfileService.GetDtosAsync(accountId).ConfigureAwait(false)
        });
    }

    /// <summary>
    /// Creates or updates one personal Weixin Claw binding profile.
    /// </summary>
    /// <param name="dto">The binding profile values supplied by the administrator.</param>
    /// <returns>An HTTP 200 result containing the persisted profile, or HTTP 400 on validation/service failure.</returns>
    public async Task<IActionResult> OnPostSaveBindingProfileAsync(
        [FromBody] WeixinClawBindingProfileDto dto)
    {
        try
        {
            return Ok(new
            {
                profile = await _bindingProfileService.SaveAsync(dto).ConfigureAwait(false)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存个人微信 Claw 绑定配置失败。");
            return BadRequest(new { msg = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a personal binding profile when it exists.
    /// </summary>
    /// <param name="id">The binding profile identifier.</param>
    /// <returns>An HTTP 200 result indicating that the delete request has completed.</returns>
    public async Task<IActionResult> OnPostDeleteBindingProfileAsync([FromBody] int id)
    {
        var profile = await _bindingProfileService.GetObjectAsync(item => item.Id == id).ConfigureAwait(false);
        if (profile != null)
        {
            await _bindingProfileService.DeleteObjectAsync(profile).ConfigureAwait(false);
        }

        return Ok(new { deleted = true });
    }

    /// <summary>
    /// Saves account settings and returns the normalized account DTO.
    /// </summary>
    /// <param name="dto">The account settings submitted by the administrator.</param>
    /// <returns>An HTTP 200 result with the saved account, or HTTP 400 when saving fails.</returns>
    public async Task<IActionResult> OnPostSaveAsync([FromBody] WeixinClawAccountDto dto)
    {
        try
        {
            var saved = await _accountService.SaveSettingsAsync(dto).ConfigureAwait(false);
            return Ok(new { account = _accountService.ToDto(saved) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动个人微信 Claw 二维码登录失败。");
            return BadRequest(new { msg = ex.Message });
        }
    }

    /// <summary>
    /// Deletes the selected accounts and their persisted receipt records.
    /// </summary>
    /// <param name="ids">The account identifiers to delete; null is treated as an empty selection.</param>
    /// <returns>An HTTP 200 result containing the current page user identifier.</returns>
    public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
    {
        foreach (var id in ids ?? Array.Empty<int>())
        {
            var account = await _accountService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);
            if (account != null)
            {
                await _receiptService.DeleteObjectAsync(z => z.WeixinClawAccountId == id).ConfigureAwait(false);
                await _accountService.DeleteObjectAsync(account).ConfigureAwait(false);
            }
        }
        return Ok(new { uid = Uid });
    }

    /// <summary>
    /// Starts a personal Weixin login session and returns its initial status.
    /// </summary>
    /// <param name="request">Optional display name and prompt-range code for the login session.</param>
    /// <returns>An HTTP 200 result with login status, or HTTP 400 when session creation fails.</returns>
    public async Task<IActionResult> OnPostStartLoginAsync([FromBody] StartLoginRequest request)
    {
        try
        {
            var status = await _loginService.StartAsync(
                request?.Name,
                request?.PromptRangeCode).ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            return BadRequest(new { msg = ex.Message });
        }
    }

    /// <summary>
    /// Polls a personal Weixin login session using the current tenant snapshot.
    /// </summary>
    /// <param name="sessionId">The login session identifier returned by the start handler.</param>
    /// <param name="verifyCode">Optional verification code supplied during login.</param>
    /// <returns>An HTTP 200 result with the current login status, or HTTP 400 on service failure.</returns>
    public async Task<IActionResult> OnGetLoginStatusAsync(string sessionId, string verifyCode = null)
    {
        try
        {
            var tenantSnapshot = RequestTenantSnapshot.Capture(HttpContext.RequestServices);
            return Ok(await _loginService.PollAsync(sessionId, verifyCode, tenant: tenantSnapshot).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            return BadRequest(new { msg = ex.Message });
        }
    }

    /// <summary>
    /// Sends a text message through a configured Weixin Claw account.
    /// </summary>
    /// <param name="request">The account, recipient, text, context, and optional reply linkage.</param>
    /// <returns>An HTTP 200 result with send identifiers, or HTTP 400 when sending fails.</returns>
    public async Task<IActionResult> OnPostSendAsync([FromBody] SendMessageRequest request)
    {
        try
        {
            var result = await _messageService.SendTextWithResultAsync(
                request.AccountId,
                request.ToUserId,
                request.Text,
                request.ContextToken,
                request.ReplyToRecordId).ConfigureAwait(false);
            return Ok(new
            {
                sent = true,
                recordId = result.RecordId,
                messageId = result.MessageId,
                targetUserId = result.TargetUserId,
                contextTokenUsed = result.ContextTokenUsed
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { msg = ex.Message });
        }
    }

    /// <summary>
    /// Reads an uploaded media file and sends it through a configured Weixin Claw account.
    /// </summary>
    /// <param name="accountId">The sending account identifier.</param>
    /// <param name="file">The uploaded media file.</param>
    /// <param name="replyToRecordId">Optional record identifier to which the media replies.</param>
    /// <param name="mediaKind">The numeric <see cref="WeixinClawMediaKind"/> value to send.</param>
    /// <returns>An HTTP 200 result with send identifiers, or HTTP 400 when validation or sending fails.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Converted to an HTTP 400 result when <paramref name="mediaKind"/> is not a defined media kind.
    /// </exception>
    public async Task<IActionResult> OnPostSendMediaAsync(
        int accountId,
        IFormFile file,
        int? replyToRecordId,
        int mediaKind = 3)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { msg = "请选择要发送的媒体文件。" });
            }

            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, HttpContext.RequestAborted).ConfigureAwait(false);
            if (!Enum.IsDefined(typeof(WeixinClawMediaKind), mediaKind))
            {
                throw new ArgumentOutOfRangeException(nameof(mediaKind), "不支持的微信媒体类型。");
            }
            var kind = (WeixinClawMediaKind)mediaKind;
            var result = await _mediaService.SendAsync(
                accountId,
                file.FileName,
                file.ContentType,
                memory.ToArray(),
                kind,
                replyToRecordId: replyToRecordId,
                cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(new
            {
                sent = true,
                recordId = result.RecordId,
                messageId = result.MessageId,
                targetUserId = result.TargetUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送个人微信 Claw 媒体失败。");
            return BadRequest(new { msg = ex.Message });
        }
    }

    /// <summary>
    /// Carries optional values used when starting a personal Weixin login session.
    /// </summary>
    public sealed class StartLoginRequest
    {
        public string Name { get; set; }
        public string PromptRangeCode { get; set; }
    }

    /// <summary>
    /// Carries the fields required to send a text message through Weixin Claw.
    /// </summary>
    public sealed class SendMessageRequest
    {
        public int AccountId { get; set; }
        public string ToUserId { get; set; }
        public string Text { get; set; }
        public string ContextToken { get; set; }
        public int? ReplyToRecordId { get; set; }
    }
}
