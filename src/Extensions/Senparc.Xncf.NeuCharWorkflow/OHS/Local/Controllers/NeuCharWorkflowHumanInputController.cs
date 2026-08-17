/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowHumanInputController.cs
    文件功能描述：等待人工输入节点的受控外部恢复接口

    创建标识：Senparc - 20260815

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 支持 Human Input 人工节点暂停与外部恢复

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.OHS.Local.Controllers;

/// <summary>
/// 外部调用方通过节点设置的恢复密钥读取当前待输入请求并提交一次性输入。
/// 该接口允许匿名访问，但不会绕过恢复密钥校验；请求和恢复句柄只在当前 Host 进程中有效。
/// </summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/human-input")]
public sealed class NeuCharWorkflowHumanInputController : ControllerBase
{
    private const string ResumeKeyHeader = "X-NeuChar-Workflow-Resume-Key";
    private readonly NeuCharWorkflowHumanInputService _humanInputService;

    public NeuCharWorkflowHumanInputController(NeuCharWorkflowHumanInputService humanInputService)
    {
        _humanInputService = humanInputService;
    }

    [HttpGet("pending/{workflowId:int}")]
    public IActionResult GetPending(int workflowId)
    {
        var resumeKey = ResolveResumeKey(null);
        var pending = _humanInputService.GetExternalPending(workflowId, resumeKey);
        return Ok(new { success = true, items = pending });
    }

    [HttpPost("{requestId}")]
    public async Task<IActionResult> ResumeAsync(
        string requestId,
        [FromBody] ResumeWorkflowHumanInputRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _humanInputService.ResolveFromExternalAsync(
            requestId,
            ResolveResumeKey(request?.ResumeKey),
            request?.Approved ?? true,
            request?.Input,
            request?.Reason,
            cancellationToken).ConfigureAwait(false);
        return result.Success
            ? Ok(new
            {
                success = true,
                approved = result.Approved,
                message = result.Message
            })
            : Conflict(new { success = false, errorMessage = result.Message });
    }

    private string? ResolveResumeKey(string? bodyKey)
    {
        if (!string.IsNullOrWhiteSpace(bodyKey))
        {
            return bodyKey.Trim();
        }
        var headerKey = Request.Headers[ResumeKeyHeader].FirstOrDefault();
        return string.IsNullOrWhiteSpace(headerKey) ? null : headerKey.Trim();
    }

    public sealed class ResumeWorkflowHumanInputRequest
    {
        public string? ResumeKey { get; set; }
        public bool? Approved { get; set; }
        public string? Input { get; set; }
        public string? Reason { get; set; }
    }
}
