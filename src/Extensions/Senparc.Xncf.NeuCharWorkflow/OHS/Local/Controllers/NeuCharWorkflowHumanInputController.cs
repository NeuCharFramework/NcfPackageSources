/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowHumanInputController.cs
    File description: Controlled external resume endpoint for Human Input nodes

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support pausing and externally resuming Human Input nodes

    Modified by: Senparc - 20260822
    Change description: v0.2.0 improve workflow function calls, task control, and replay management

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
/// External callers use the resume key configured on a node to read the pending
/// input request and submit a one-time response. Anonymous access is allowed,
/// but resume-key validation is still required; requests and resume handles are
/// valid only within the current Host process.
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
