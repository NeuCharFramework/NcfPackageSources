/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowHumanInputController.cs
    File description: Controlled external resume endpoint for Human Input nodes

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support pausing and externally resuming Human Input nodes

    Modified by: Senparc - 20260822
    Change description: v0.2.0 improve workflow function calls, task control, and replay management

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

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

    /// <summary>
    /// Initializes the external Human Input resume controller.
    /// </summary>
    /// <param name="humanInputService">The service that validates and resolves pending requests.</param>
    public NeuCharWorkflowHumanInputController(NeuCharWorkflowHumanInputService humanInputService)
    {
        _humanInputService = humanInputService;
    }

    /// <summary>
    /// Returns pending external Human Input requests for a workflow after resume-key validation.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <returns>An HTTP 200 response containing pending requests.</returns>
    [HttpGet("pending/{workflowId:int}")]
    public IActionResult GetPending(int workflowId)
    {
        var resumeKey = ResolveResumeKey(null);
        var pending = _humanInputService.GetExternalPending(workflowId, resumeKey);
        return Ok(new { success = true, items = pending });
    }

    /// <summary>
    /// Resolves one pending Human Input request using the supplied approval, input, and reason.
    /// </summary>
    /// <param name="requestId">The pending request identifier.</param>
    /// <param name="request">The optional resume key and resolution values.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>An HTTP 200 result when resolved, otherwise an HTTP 409 conflict.</returns>
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

    /// <summary>
    /// Resolves a resume key from the request body first, then the configured header.
    /// </summary>
    /// <param name="bodyKey">The optional body-provided key.</param>
    /// <returns>The trimmed resume key, or <see langword="null"/> when absent.</returns>
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
