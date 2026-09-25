using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager;
using Senparc.Xncf.WeixinManager.Domain.Services;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Areas.Admin.Pages.WeixinManager.WeixinClaw;

public class IndexModel : BaseAdminWeixinManagerModel
{
    public List<WeixinClawAccountDto> Accounts { get; private set; } = new();

    private readonly WeixinClawAccountService _accountService;
    private readonly WeixinClawMessageReceiptService _receiptService;
    private readonly WeixinClawLoginService _loginService;
    private readonly WeixinClawMessageService _messageService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        Lazy<XncfModuleService> xncfModuleService,
        WeixinClawAccountService accountService,
        WeixinClawMessageReceiptService receiptService,
        WeixinClawLoginService loginService,
        WeixinClawMessageService messageService,
        ILogger<IndexModel> logger) : base(xncfModuleService)
    {
        _accountService = accountService;
        _receiptService = receiptService;
        _loginService = loginService;
        _messageService = messageService;
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
                await _receiptService.DeleteObjectAsync(z => z.WeixinClawAccountId == id).ConfigureAwait(false);
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
            await _messageService.SendTextAsync(
                request.AccountId,
                request.ToUserId,
                request.Text,
                request.ContextToken).ConfigureAwait(false);
            return Ok(new { sent = true });
        }
        catch (Exception ex)
        {
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
    }
}
