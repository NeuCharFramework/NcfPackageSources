using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuCharPivot;

[IgnoreAuth]
[AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
public class WorkflowModel(
    IServiceProvider serviceProvider,
    NeuCharWorkflowService workflowService,
    NeuCharWorkflowVersionService workflowVersionService,
    NeuCharWorkflowEngine workflowEngine,
    NeuCharPivotService pivotService,
    NeuCharFunctionService functionService,
    NeuCharWorkflowRunCoordinator runCoordinator,
    IAdminWorkContextProvider adminWorkContextProvider) : BaseAdminPageModel(serviceProvider)
{
    public Task OnGetAsync() => Task.CompletedTask;

    public async Task<IActionResult> OnGetListAsync()
    {
        var workflows = await workflowService.GetFullListAsync(
            z => true,
            z => z.LastUpdateTime,
            OrderingType.Descending).ConfigureAwait(false);
        return Ok(workflows.Select(ToListResponse));
    }

    public async Task<IActionResult> OnGetDetailAsync(int id)
    {
        var workflow = await workflowService.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);
        if (workflow == null)
        {
            return NotFound();
        }
        var editableGraphJson = await workflowEngine.BuildEditableGraphJsonAsync(
            workflow.GraphJson,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ToResponse(workflow, editableGraphJson));
    }

    public async Task<IActionResult> OnGetDesignerDataAsync()
    {
        var snapshots = await pivotService.GetAllSnapshotsAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        var catalog = await functionService.GetCatalogAsync(
            null,
            true,
            HttpContext.RequestAborted).ConfigureAwait(false);
        var objects = await workflowEngine.GetWorkflowObjectsAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        var storedFunctions = snapshots
            .SelectMany(snapshot => snapshot.Functions.Where(z => z.Visible))
            .GroupBy(z => $"{z.ModuleUid}|{z.FunctionKey}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(z => z.Key, z => z.First(), StringComparer.OrdinalIgnoreCase);
        var functions = catalog.Select(descriptor =>
        {
            storedFunctions.TryGetValue(
                $"{descriptor.ModuleUid}|{descriptor.FunctionKey}",
                out var storedFunction);
            var parameterSchema = NeuCharPivotService.BuildParameterSchema(
                descriptor,
                descriptor.Parameters.Select(z => z.Name).ToArray());
            var defaults = NeuCharPivotService.BuildDefaultParameters(parameterSchema);
            return new
            {
                id = storedFunction?.Id ?? 0,
                descriptor.FunctionKey,
                functionName = descriptor.Name,
                descriptor.Description,
                descriptor.ModuleUid,
                descriptor.ModuleName,
                descriptor.ModuleVersion,
                descriptor.ModuleAvailable,
                moduleState = descriptor.ModuleAvailable ? "open" : "disabled",
                parameterSchemaJson = JsonSerializer.Serialize(parameterSchema),
                defaultParametersJson = storedFunction?.DefaultParametersJson ?? JsonSerializer.Serialize(defaults),
                descriptor.Output,
                descriptor.CatalogError
            };
        }).ToList();
        return Ok(new
        {
            functions,
            objects
        });
    }

    public async Task<IActionResult> OnPostSaveAsync([FromBody] SaveWorkflowRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("工作流名称不能为空。");
        }
        if (request.Name.Trim().Length > 200)
        {
            return BadRequest("工作流名称不能超过 200 个字符。");
        }
        if (request.Description?.Length > 10_000 || request.TriggerConfigJson?.Length > 100_000)
        {
            return BadRequest("工作流描述或触发器配置超过允许长度。");
        }
        if (request.AutoSaveMinutes is < 0 or > 1440)
        {
            return BadRequest("自动保存间隔必须为 0 到 1440 分钟，0 表示关闭。");
        }

        NeuCharWorkflowGraph graph;
        try
        {
            graph = workflowEngine.ParseAndValidateGraph(request.GraphJson);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        var workflow = request.Id > 0
            ? await workflowService.GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false)
            : null;
        if (request.Id > 0 && workflow == null)
        {
            return NotFound();
        }
        if (workflow != null && request.ExpectedRevision.HasValue &&
            request.ExpectedRevision.Value != workflow.Revision)
        {
            return StatusCode(409, "工作流已被其他页面更新，请刷新后再保存。");
        }
        try
        {
            await workflowEngine.MergeExistingSecretsAsync(
                graph,
                workflow?.GraphJson,
                HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        if (request.Enabled)
        {
            var referenceError = await workflowEngine.ValidateReferencesAsync(
                graph,
                HttpContext.RequestAborted).ConfigureAwait(false);
            if (referenceError != null)
            {
                return BadRequest(referenceError);
            }
        }

        var triggerType = request.TriggerType?.Trim().ToLowerInvariant() switch
        {
            "interval" => "interval",
            "webhook" => "webhook",
            "manual" or null or "" => "manual",
            _ => null
        };
        if (triggerType == null)
        {
            return BadRequest("工作流触发方式无效。");
        }
        var expectedTriggerNodeType = $"{triggerType}-trigger";
        if (!graph.Nodes.Any(z => string.Equals(
                z.Type,
                expectedTriggerNodeType,
                StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("工作流触发器节点与触发方式不一致。");
        }
        try
        {
            await workflowEngine.ProtectSecretsAsync(
                graph,
                HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        var normalizedGraphJson = System.Text.Json.JsonSerializer.Serialize(
            graph,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        workflow ??= new NeuCharWorkflow(
            request.Name.Trim(),
            adminWorkContextProvider.GetAdminWorkContext().AdminUserId);
        string triggerConfigJson;
        try
        {
            triggerConfigJson = string.Equals(triggerType, "webhook", StringComparison.Ordinal)
                ? NeuCharWorkflowWebhookConfig.Normalize(
                    request.TriggerConfigJson,
                    workflow?.TriggerConfigJson).ToJson()
                : string.Equals(triggerType, "interval", StringComparison.Ordinal)
                    ? request.TriggerConfigJson ?? "{}"
                    : "{}";
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        var nextRun = request.Enabled
            ? NeuCharWorkflowEngine.CalculateNextRun(triggerType, triggerConfigJson, DateTime.UtcNow)
            : null;
        var autoSaveMinutes = request.AutoSaveMinutes <= 0
            ? 0
            : Math.Clamp(request.AutoSaveMinutes, 1, 1440);
        if (workflow.Id > 0 && IsUnchanged(
                workflow,
                request.Name,
                request.Description,
                normalizedGraphJson,
                request.Enabled,
                triggerType,
                triggerConfigJson,
                autoSaveMinutes))
        {
            var editableUnchangedGraph = await workflowEngine.BuildEditableGraphJsonAsync(
                workflow.GraphJson,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(ToResponse(workflow, editableUnchangedGraph, unchanged: true));
        }
        workflow.Update(
            request.Name,
            request.Description,
            normalizedGraphJson,
            request.Enabled,
            triggerType,
            triggerConfigJson,
            nextRun,
            autoSaveMinutes);
        await workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
        var adminUserId = adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        await SaveVersionAsync(workflow, adminUserId, request.SaveSource).ConfigureAwait(false);
        var editableGraphJson = await workflowEngine.BuildEditableGraphJsonAsync(
            workflow.GraphJson,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ToResponse(workflow, editableGraphJson));
    }

    public async Task<IActionResult> OnPostRunAsync([FromBody] RunWorkflowRequest request)
    {
        if (request == null || request.Id <= 0 || request.Input?.Length > 100_000)
        {
            return BadRequest("工作流输入不能超过 100000 个字符。");
        }
        var workflow = await workflowService.GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        if (workflow == null)
        {
            return NotFound();
        }
        try
        {
            var graph = workflowEngine.ParseAndValidateGraph(workflow.GraphJson);
            var validationError = await workflowEngine.ValidateReferencesAsync(
                graph,
                HttpContext.RequestAborted).ConfigureAwait(false);
            if (validationError != null)
            {
                return BadRequest(validationError);
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        workflow.MarkStarted(workflow.NextRunAt);
        await workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
        var result = await workflowEngine.RunAsync(
            workflow,
            request.Input,
            HttpContext.RequestAborted).ConfigureAwait(false);
        workflow.MarkCompleted(result.Success, result.ErrorMessage);
        await workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
        return Ok(result);
    }

    public async Task<IActionResult> OnPostValidateRunAsync([FromBody] RunWorkflowRequest request)
    {
        if (request == null || request.Id <= 0 || request.Input?.Length > 100_000)
        {
            return BadRequest("工作流测试请求无效，输入不能超过 100000 个字符。");
        }
        var workflow = await workflowService.GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        if (workflow == null)
        {
            return NotFound();
        }
        try
        {
            var graph = workflowEngine.ParseAndValidateGraph(workflow.GraphJson);
            var validationError = await workflowEngine.ValidateReferencesAsync(
                graph,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return validationError == null
                ? Ok(new { success = true })
                : BadRequest(validationError);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<IActionResult> OnPostStartRunAsync([FromBody] RunWorkflowRequest request)
    {
        if (request == null || request.Id <= 0 || request.Input?.Length > 100_000)
        {
            return BadRequest("工作流测试请求无效，输入不能超过 100000 个字符。");
        }
        var workflow = await workflowService.GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        if (workflow == null)
        {
            return NotFound();
        }
        try
        {
            var graph = workflowEngine.ParseAndValidateGraph(workflow.GraphJson);
            var validationError = await workflowEngine.ValidateReferencesAsync(
                graph,
                HttpContext.RequestAborted).ConfigureAwait(false);
            if (validationError != null)
            {
                return BadRequest(validationError);
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        var adminUserId = adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        if (!runCoordinator.TryStart(
                workflow.Id,
                adminUserId,
                request.Input,
                out var runId,
                out var error))
        {
            return StatusCode(409, error);
        }
        return Ok(new { runId });
    }

    public IActionResult OnGetRunStatus(Guid runId, long afterSequence = 0)
    {
        if (runId == Guid.Empty)
        {
            return BadRequest("运行标识无效。");
        }
        var adminUserId = adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var snapshot = runCoordinator.GetSnapshot(runId, adminUserId, afterSequence);
        return snapshot == null ? NotFound() : Ok(snapshot);
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteWorkflowRequest request)
    {
        if (request == null || request.Id <= 0)
        {
            return BadRequest("工作流请求无效。");
        }
        var workflow = await workflowService.GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        if (workflow == null)
        {
            return NotFound();
        }
        var versions = await workflowVersionService.GetFullListAsync(
            z => z.WorkflowId == workflow.Id).ConfigureAwait(false);
        foreach (var version in versions)
        {
            await workflowVersionService.DeleteObjectAsync(version).ConfigureAwait(false);
        }
        await workflowService.DeleteObjectAsync(workflow).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    private async Task SaveVersionAsync(NeuCharWorkflow workflow, int adminUserId, string saveSource)
    {
        await workflowVersionService.SaveObjectAsync(
            new NeuCharWorkflowVersion(workflow, adminUserId, saveSource)).ConfigureAwait(false);
        var versions = await workflowVersionService.GetFullListAsync(
            z => z.WorkflowId == workflow.Id,
            z => z.Revision,
            OrderingType.Descending).ConfigureAwait(false);
        foreach (var obsolete in versions.Skip(5))
        {
            await workflowVersionService.DeleteObjectAsync(obsolete).ConfigureAwait(false);
        }
    }

    private static bool IsUnchanged(
        NeuCharWorkflow workflow,
        string name,
        string description,
        string graphJson,
        bool enabled,
        string triggerType,
        string triggerConfigJson,
        int autoSaveMinutes) =>
        string.Equals(workflow.Name, name?.Trim(), StringComparison.Ordinal) &&
        string.Equals(workflow.Description, description?.Trim(), StringComparison.Ordinal) &&
        string.Equals(workflow.GraphJson, graphJson, StringComparison.Ordinal) &&
        workflow.Enabled == enabled &&
        string.Equals(workflow.TriggerType, triggerType, StringComparison.Ordinal) &&
        string.Equals(workflow.TriggerConfigJson, triggerConfigJson, StringComparison.Ordinal) &&
        workflow.AutoSaveMinutes == autoSaveMinutes;

    private static object ToResponse(
        NeuCharWorkflow workflow,
        string graphJson = null,
        bool unchanged = false) => new
    {
        workflow.Id,
        workflow.Name,
        workflow.Description,
        GraphJson = graphJson ?? workflow.GraphJson,
        workflow.Enabled,
        workflow.TriggerType,
        workflow.TriggerConfigJson,
        workflow.NextRunAt,
        workflow.LastRunAt,
        workflow.LastSucceeded,
        workflow.LastError,
        workflow.Revision,
        workflow.AutoSaveMinutes,
        unchanged,
        workflow.LastUpdateTime
    };

    private static object ToListResponse(NeuCharWorkflow workflow) => new
    {
        workflow.Id,
        workflow.Name,
        workflow.Description,
        workflow.Enabled,
        workflow.TriggerType,
        workflow.NextRunAt,
        workflow.LastRunAt,
        workflow.LastSucceeded,
        workflow.LastError,
        workflow.Revision,
        workflow.AutoSaveMinutes,
        workflow.LastUpdateTime
    };

    public sealed class SaveWorkflowRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string GraphJson { get; set; }
        public bool Enabled { get; set; }
        public string TriggerType { get; set; }
        public string TriggerConfigJson { get; set; }
        public int AutoSaveMinutes { get; set; } = 3;
        public int? ExpectedRevision { get; set; }
        public string SaveSource { get; set; }
    }

    public sealed class RunWorkflowRequest
    {
        public int Id { get; set; }
        public string Input { get; set; }
    }

    public sealed class DeleteWorkflowRequest
    {
        public int Id { get; set; }
    }
}
