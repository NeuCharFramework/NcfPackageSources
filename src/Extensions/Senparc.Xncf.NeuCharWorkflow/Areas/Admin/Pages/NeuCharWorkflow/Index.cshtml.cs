/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: enhance workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260817
    Change description: v0.2.0 support pausing and external resumption of Human Input nodes

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance workflow function calls, task control, and replay management

    Modified by: Senparc - 20261021
    Change description: v0.4.4 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.4.4 enhance fine-tuning functionality with artifact tracking and import/export capabilities

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Areas.NeuCharWorkflow.Pages;

/// <summary>
///Legacy comment translated to English.
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

    public async Task<IActionResult> OnGetObservedOutputSchemasAsync(int id) =>
        Ok(await workflowAppService.GetObservedOutputSchemasAsync(
            id,
            CurrentAdminUserId,
            HttpContext.RequestAborted).ConfigureAwait(false));

    public async Task<IActionResult> OnGetDesignerDataAsync() =>
        Ok(await workflowAppService.GetDesignerDataAsync(HttpContext.RequestAborted).ConfigureAwait(false));

    public async Task<IActionResult> OnGetExportAsync(int id, string format = "json")
    {
        if (!TryParseInterchangeFormat(format, out var interchangeFormat))
        {
            return BadRequest("导出格式只能是 json 或 yaml。");
        }

        try
        {
            var export = await workflowAppService.ExportAsync(
                id,
                CurrentAdminUserId,
                interchangeFormat,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return File(Encoding.UTF8.GetBytes(export.Content), export.ContentType, export.FileName);
        }
        catch (WorkflowNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostValidateImportAsync([FromBody] ImportWorkflowRequest request)
    {
        try
        {
            var format = ParseOptionalInterchangeFormat(request?.Format);
            return Ok(await workflowAppService.ValidateImportAsync(
                request?.Content ?? string.Empty,
                format,
                HttpContext.RequestAborted).ConfigureAwait(false));
        }
        catch (WorkflowInterchangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> OnPostImportAsync([FromBody] ImportWorkflowRequest request)
    {
        try
        {
            var format = ParseOptionalInterchangeFormat(request?.Format);
            return Ok(await workflowAppService.ImportAsync(
                request?.Content ?? string.Empty,
                CurrentAdminUserId,
                format,
                HttpContext.RequestAborted).ConfigureAwait(false));
        }
        catch (WorkflowNotFoundException)
        {
            return NotFound();
        }
        catch (WorkflowInputException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (WorkflowInterchangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

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

    public async Task<IActionResult> OnGetRunStatusAsync(Guid runId, long afterSequence = 0)
    {
        var snapshot = await workflowAppService.GetRunStatusAsync(
            runId,
            CurrentAdminUserId,
            afterSequence,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return snapshot == null ? NotFound() : Ok(snapshot);
    }

    public async Task<IActionResult> OnPostResolveHumanAsync([FromBody] ResolveHumanWorkflowRequest request)
    {
        var result = await workflowAppService.ResolveHumanInteractionAsync(
            request?.RunId ?? Guid.Empty,
            CurrentAdminUserId,
            request?.RequestId,
            request?.Approved ?? false,
            request?.Input,
            request?.Reason,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return result.Success ? Ok(result) : StatusCode(409, result.Message);
    }

    public async Task<IActionResult> OnPostAbortRunAsync([FromBody] AbortWorkflowRunRequest request)
    {
        try
        {
            await workflowAppService.AbortRunAsync(
                request?.RunId,
                request?.ExecutionLogId,
                CurrentAdminUserId,
                HttpContext.RequestAborted).ConfigureAwait(false);
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

    private static bool TryParseInterchangeFormat(string? value, out WorkflowInterchangeFormat format)
    {
        if (string.Equals(value, "yaml", StringComparison.OrdinalIgnoreCase))
        {
            format = WorkflowInterchangeFormat.Yaml;
            return true;
        }

        if (string.Equals(value, "json", StringComparison.OrdinalIgnoreCase))
        {
            format = WorkflowInterchangeFormat.Json;
            return true;
        }

        format = default;
        return false;
    }

    private static WorkflowInterchangeFormat? ParseOptionalInterchangeFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TryParseInterchangeFormat(value, out var format)
            ? format
            : throw new WorkflowInterchangeException("格式只能是 json 或 yaml。");
    }

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

    public sealed class ImportWorkflowRequest
    {
        public string? Content { get; set; }
        public string? Format { get; set; }
    }

    public sealed class RunWorkflowRequest
    {
        public int Id { get; set; }
        public string? Input { get; set; }
    }

    public sealed class ResolveHumanWorkflowRequest
    {
        public Guid RunId { get; set; }
        public string? RequestId { get; set; }
        public bool Approved { get; set; }
        public string? Input { get; set; }
        public string? Reason { get; set; }
    }

    public sealed class DeleteWorkflowRequest
    {
        public int Id { get; set; }
    }

    public sealed class AbortWorkflowRunRequest
    {
        public Guid? RunId { get; set; }
        public int? ExecutionLogId { get; set; }
    }
}
