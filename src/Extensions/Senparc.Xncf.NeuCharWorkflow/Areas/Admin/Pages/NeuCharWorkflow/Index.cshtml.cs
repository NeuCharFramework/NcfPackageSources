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

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

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
/// Provides the administration page handlers for workflow design, interchange,
/// validation, execution control, replay status, and human interaction.
/// </summary>
public class IndexModel(
    Lazy<XncfModuleService> xncfModuleService,
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : AdminXncfModulePageModelBase(xncfModuleService)
{
    /// <summary>
    /// Completes the initial page request; all initial data is loaded by asynchronous handlers.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnGetAsync() => Task.CompletedTask;

    /// <summary>
    /// Returns workflows owned by the current administrator.
    /// </summary>
    /// <returns>An HTTP 200 result containing the workflow list.</returns>
    public async Task<IActionResult> OnGetListAsync() =>
        Ok(await workflowAppService.GetListAsync(CurrentAdminUserId, HttpContext.RequestAborted).ConfigureAwait(false));

    /// <summary>
    /// Returns one owner-scoped workflow detail record.
    /// </summary>
    /// <param name="id">The workflow identifier.</param>
    /// <returns>An HTTP 200 detail result or HTTP 404 when no owned workflow exists.</returns>
    public async Task<IActionResult> OnGetDetailAsync(int id)
    {
        var workflow = await workflowAppService.GetDetailAsync(id, CurrentAdminUserId, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        return workflow == null ? NotFound() : Ok(workflow);
    }

    /// <summary>
    /// Returns observed output schemas collected for one owner-scoped workflow.
    /// </summary>
    /// <param name="id">The workflow identifier.</param>
    /// <returns>An HTTP 200 result containing observed schemas.</returns>
    public async Task<IActionResult> OnGetObservedOutputSchemasAsync(int id) =>
        Ok(await workflowAppService.GetObservedOutputSchemasAsync(
            id,
            CurrentAdminUserId,
            HttpContext.RequestAborted).ConfigureAwait(false));

    /// <summary>
    /// Returns the module and function metadata required by the workflow designer.
    /// </summary>
    /// <returns>An HTTP 200 designer-data result.</returns>
    public async Task<IActionResult> OnGetDesignerDataAsync() =>
        Ok(await workflowAppService.GetDesignerDataAsync(HttpContext.RequestAborted).ConfigureAwait(false));

    /// <summary>
    /// Exports an owner-scoped workflow as JSON or YAML.
    /// </summary>
    /// <param name="id">The workflow identifier.</param>
    /// <param name="format">The requested interchange format, defaulting to JSON.</param>
    /// <returns>A downloadable file result or an HTTP error for invalid format/missing workflow.</returns>
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

    /// <summary>
    /// Validates an import document without persisting it.
    /// </summary>
    /// <param name="request">The document content and optional format.</param>
    /// <returns>An HTTP 200 preview result or HTTP 400 for invalid interchange content.</returns>
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

    /// <summary>
    /// Imports and persists a workflow definition for the current administrator.
    /// </summary>
    /// <param name="request">The document content and optional format.</param>
    /// <returns>An HTTP 200 import result or a mapped HTTP error.</returns>
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

    /// <summary>
    /// Creates or updates a workflow while enforcing revision and module-availability checks.
    /// </summary>
    /// <param name="request">The workflow definition and optional expected revision.</param>
    /// <returns>An HTTP 200 workflow result or a mapped validation/conflict response.</returns>
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

    /// <summary>
    /// Executes a workflow immediately using the supplied input.
    /// </summary>
    /// <param name="request">The workflow identifier and runtime input.</param>
    /// <returns>An HTTP 200 run result or a mapped workflow error.</returns>
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

    /// <summary>
    /// Validates a workflow run without starting execution.
    /// </summary>
    /// <param name="request">The workflow identifier and candidate input.</param>
    /// <returns>An HTTP 200 success result or a mapped workflow error.</returns>
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

    /// <summary>
    /// Starts a background run and returns its run identifier.
    /// </summary>
    /// <param name="request">The workflow identifier and runtime input.</param>
    /// <returns>An HTTP 200 result containing the run identifier or a mapped error.</returns>
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

    /// <summary>
    /// Returns incremental status events for an owner-scoped workflow run.
    /// </summary>
    /// <param name="runId">The runtime run identifier.</param>
    /// <param name="afterSequence">The last event sequence already consumed by the client.</param>
    /// <returns>An HTTP 200 snapshot or HTTP 404 when the run is unavailable.</returns>
    public async Task<IActionResult> OnGetRunStatusAsync(Guid runId, long afterSequence = 0)
    {
        var snapshot = await workflowAppService.GetRunStatusAsync(
            runId,
            CurrentAdminUserId,
            afterSequence,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return snapshot == null ? NotFound() : Ok(snapshot);
    }

    /// <summary>
    /// Resolves a pending human interaction for the current administrator.
    /// </summary>
    /// <param name="request">The run, request, decision, input, and reason values.</param>
    /// <returns>An HTTP 200 result when handled, otherwise HTTP 409.</returns>
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

    /// <summary>
    /// Requests cancellation of a live run or persisted execution log.
    /// </summary>
    /// <param name="request">The optional live run and execution-log identifiers.</param>
    /// <returns>An HTTP 200 result or a mapped conflict/input error.</returns>
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

    /// <summary>
    /// Deletes an owner-scoped workflow.
    /// </summary>
    /// <param name="request">The workflow identifier to delete.</param>
    /// <returns>An HTTP 200 result or HTTP 404 when the workflow is absent.</returns>
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

    /// <summary>
    /// Parses a required JSON/YAML format label without throwing for invalid input.
    /// </summary>
    /// <param name="value">The format label supplied by the caller.</param>
    /// <param name="format">Receives the parsed format when valid.</param>
    /// <returns><see langword="true"/> when the label is JSON or YAML.</returns>
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

    /// <summary>
    /// Parses an optional interchange format and converts invalid non-empty values to a workflow error.
    /// </summary>
    /// <param name="value">The optional format label.</param>
    /// <returns>The parsed format, or <see langword="null"/> when omitted.</returns>
    /// <exception cref="WorkflowInterchangeException">Thrown when a non-empty value is not JSON or YAML.</exception>
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
