/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：增强工作流编排、回放、Webhook 与并行执行能力


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Areas.NeuCharWorkflow.Pages;

/// <summary>
/// Workflow 管理页面适配器。所有业务读写均交由 Application 服务，页面仅处理 Admin 会话、HTTP 与视图响应。
/// </summary>
public class IndexModel(
    Lazy<XncfModuleService> xncfModuleService,
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : AdminXncfModulePageModelBase(xncfModuleService)
{
    public Task OnGetAsync() => Task.CompletedTask;

    public async Task<IActionResult> OnGetListAsync() =>
        Ok(await workflowAppService.GetListAsync(CurrentAdminUserId, HttpContext.RequestAborted).ConfigureAwait(false));

    public async Task<IActionResult> OnGetDetailAsync(int id)
    {
        var workflow = await workflowAppService.GetDetailAsync(id, CurrentAdminUserId, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        return workflow == null ? NotFound() : Ok(workflow);
    }

    public async Task<IActionResult> OnGetDesignerDataAsync() =>
        Ok(await workflowAppService.GetDesignerDataAsync(HttpContext.RequestAborted).ConfigureAwait(false));

    public async Task<IActionResult> OnPostSaveAsync([FromBody] SaveWorkflowRequest request)
    {
        try
        {
            var workflow = await workflowAppService.SaveAsync(
                new SaveWorkflowCommand(
                    request?.Id ?? 0,
                    request?.Name,
                    request?.Description,
                    request?.GraphJson,
                    request?.Enabled ?? false,
                    request?.TriggerType,
                    request?.TriggerConfigJson,
                    request?.AutoSaveMinutes ?? 3,
                    request?.ExpectedRevision,
                    request?.SaveSource),
                CurrentAdminUserId,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(workflow);
        }
        catch (WorkflowNotFoundException)
        {
            return NotFound();
        }
        catch (WorkflowModuleUnavailableException ex)
        {
            return StatusCode(409, ex.Message);
        }
        catch (WorkflowConflictException ex)
        {
            return StatusCode(409, ex.Message);
        }
        catch (WorkflowInputException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> OnPostRunAsync([FromBody] RunWorkflowRequest request)
    {
        try
        {
            var result = await workflowAppService.RunImmediatelyAsync(
                request?.Id ?? 0, CurrentAdminUserId, request?.Input ?? string.Empty, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return Ok(result);
        }
        catch (WorkflowNotFoundException)
        {
            return NotFound();
        }
        catch (WorkflowModuleUnavailableException ex)
        {
            return StatusCode(409, ex.Message);
        }
        catch (WorkflowInputException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> OnPostValidateRunAsync([FromBody] RunWorkflowRequest request)
    {
        try
        {
            await workflowAppService.ValidateRunAsync(
                request?.Id ?? 0, CurrentAdminUserId, request?.Input ?? string.Empty, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return Ok(new { success = true });
        }
        catch (WorkflowNotFoundException)
        {
            return NotFound();
        }
        catch (WorkflowModuleUnavailableException ex)
        {
            return StatusCode(409, ex.Message);
        }
        catch (WorkflowInputException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> OnPostStartRunAsync([FromBody] RunWorkflowRequest request)
    {
        try
        {
            var runId = await workflowAppService.StartRunAsync(
                request?.Id ?? 0, CurrentAdminUserId, request?.Input ?? string.Empty, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return Ok(new { runId });
        }
        catch (WorkflowNotFoundException)
        {
            return NotFound();
        }
        catch (WorkflowModuleUnavailableException ex)
        {
            return StatusCode(409, ex.Message);
        }
        catch (WorkflowConflictException ex)
        {
            return StatusCode(409, ex.Message);
        }
        catch (WorkflowInputException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public IActionResult OnGetRunStatus(Guid runId, long afterSequence = 0)
    {
        var snapshot = workflowAppService.GetRunStatus(runId, CurrentAdminUserId, afterSequence);
        return snapshot == null ? NotFound() : Ok(snapshot);
    }

    public IActionResult OnPostAbortRun([FromBody] AbortWorkflowRunRequest request)
    {
        try
        {
            workflowAppService.AbortRun(request?.RunId ?? Guid.Empty, CurrentAdminUserId);
            return Ok(new { success = true });
        }
        catch (WorkflowConflictException ex)
        {
            return StatusCode(409, ex.Message);
        }
        catch (WorkflowInputException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteWorkflowRequest request)
    {
        try
        {
            await workflowAppService.DeleteAsync(request?.Id ?? 0, CurrentAdminUserId, HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return Ok(new { success = true });
        }
        catch (WorkflowNotFoundException)
        {
            return NotFound();
        }
    }

    private int CurrentAdminUserId => adminWorkContextProvider.GetAdminWorkContext().AdminUserId;

    public sealed class SaveWorkflowRequest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? GraphJson { get; set; }
        public bool Enabled { get; set; }
        public string? TriggerType { get; set; }
        public string? TriggerConfigJson { get; set; }
        public int AutoSaveMinutes { get; set; } = 3;
        public int? ExpectedRevision { get; set; }
        public string? SaveSource { get; set; }
    }

    public sealed class RunWorkflowRequest
    {
        public int Id { get; set; }
        public string? Input { get; set; }
    }

    public sealed class DeleteWorkflowRequest
    {
        public int Id { get; set; }
    }

    public sealed class AbortWorkflowRunRequest
    {
        public Guid RunId { get; set; }
    }
}
