using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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

public class IndexModel : BaseAdminWeixinManagerModel
{
    public List<WeixinClawAccountDto> Accounts { get; private set; } = new();

    private readonly WeixinClawAccountService _accountService;
    private readonly WeixinClawLoginService _loginService;
    private readonly WeixinClawMessageService _messageService;
    private readonly WeixinClawMessageRecordService _recordService;
    private readonly WeixinClawBindingProfileService _bindingProfileService;
    private readonly WeixinClawMediaService _mediaService;
    private readonly WeixinClawMediaStorageService _mediaStorage;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        Lazy<XncfModuleService> xncfModuleService,
        WeixinClawAccountService accountService,
        WeixinClawLoginService loginService,
        WeixinClawMessageService messageService,
        WeixinClawMessageRecordService recordService,
        WeixinClawBindingProfileService bindingProfileService,
        WeixinClawMediaService mediaService,
        WeixinClawMediaStorageService mediaStorage,
        ILogger<IndexModel> logger) : base(xncfModuleService)
    {
        _accountService = accountService;
        _loginService = loginService;
        _messageService = messageService;
        _recordService = recordService;
        _bindingProfileService = bindingProfileService;
        _mediaService = mediaService;
        _mediaStorage = mediaStorage;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        Accounts = await _accountService.GetDtosAsync().ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetAjaxAsync()
    {
        return Ok(new { list = await _accountService.GetDtosAsync().ConfigureAwait(false) });
    }

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
            }
        }

        return Ok(new
        {
            accountId,
            list
        });
    }

    public async Task<IActionResult> OnGetMediaAsync(int recordId, int index = 0)
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

        return PhysicalFile(
            path,
            string.IsNullOrWhiteSpace(media.ContentType)
                ? "application/octet-stream"
                : media.ContentType);
    }

    public async Task<IActionResult> OnGetConversationsAsync(int accountId)
    {
        return Ok(new
        {
            accountId,
            list = await _recordService.GetConversationsAsync(accountId).ConfigureAwait(false)
        });
    }

    public async Task<IActionResult> OnGetBindingProfilesAsync(int accountId)
    {
        return Ok(new
        {
            accountId,
            list = await _bindingProfileService.GetDtosAsync(accountId).ConfigureAwait(false)
        });
    }

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

    public async Task<IActionResult> OnPostDeleteBindingProfileAsync([FromBody] int id)
    {
        var profile = await _bindingProfileService.GetObjectAsync(item => item.Id == id).ConfigureAwait(false);
        if (profile != null)
        {
            await _bindingProfileService.DeleteObjectAsync(profile).ConfigureAwait(false);
        }

        return Ok(new { deleted = true });
    }

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

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
    {
        foreach (var id in ids ?? Array.Empty<int>())
        {
            var account = await _accountService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);
            if (account != null)
            {
                await _accountService.DeleteObjectAsync(account).ConfigureAwait(false);
            }
        }
        return Ok(new { uid = Uid });
    }

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

    public async Task<IActionResult> OnGetLoginStatusAsync(string sessionId)
    {
        try
        {
            return Ok(await _loginService.PollAsync(sessionId).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            return BadRequest(new { msg = ex.Message });
        }
    }

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
            var kind = Enum.IsDefined(typeof(WeixinClawMediaKind), mediaKind)
                ? (WeixinClawMediaKind)mediaKind
                : WeixinClawMediaKind.File;
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

    public sealed class StartLoginRequest
    {
        public string Name { get; set; }
        public string PromptRangeCode { get; set; }
    }

    public sealed class SendMessageRequest
    {
        public int AccountId { get; set; }
        public string ToUserId { get; set; }
        public string Text { get; set; }
        public string ContextToken { get; set; }
        public int? ReplyToRecordId { get; set; }
    }
}
