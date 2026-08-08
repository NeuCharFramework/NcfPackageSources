using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuCharPivot;

[IgnoreAuth]
[AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
public class WorkflowModel(
    IServiceProvider serviceProvider,
    NeuCharWorkflowService workflowService,
    NeuCharWorkflowEngine workflowEngine,
    NeuCharPivotService pivotService,
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
        var objects = await workflowEngine.GetWorkflowObjectsAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(new
        {
            functions = snapshots.SelectMany(snapshot => snapshot.Functions
                .Where(function => function.Visible)
                .Select(function => new
                {
                    function.Id,
                    function.FunctionName,
                    function.Description,
                    function.ModuleUid,
                    moduleName = snapshot.Configuration.Name,
                    moduleAvailable = snapshot.FunctionAvailability.TryGetValue(function.Id, out var available) && available,
                    snapshot.ModuleState,
                    parameterSchemaJson = function.UiSchemaJson,
                    function.DefaultParametersJson
                })),
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

        var triggerType = string.Equals(request.TriggerType, "interval", StringComparison.OrdinalIgnoreCase)
            ? "interval"
            : "manual";
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
        var nextRun = request.Enabled
            ? NeuCharWorkflowEngine.CalculateNextRun(triggerType, request.TriggerConfigJson, DateTime.UtcNow)
            : null;
        workflow.Update(
            request.Name,
            request.Description,
            normalizedGraphJson,
            request.Enabled,
            triggerType,
            request.TriggerConfigJson,
            nextRun);
        await workflowService.SaveObjectAsync(workflow).ConfigureAwait(false);
        return Ok(ToResponse(workflow));
    }

    public async Task<IActionResult> OnPostRunAsync([FromBody] RunWorkflowRequest request)
    {
        if (request == null || request.Input?.Length > 100_000)
        {
            return BadRequest("工作流输入不能超过 100000 个字符。");
        }
        var workflow = await workflowService.GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false);
        if (workflow == null)
        {
            return NotFound();
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
        await workflowService.DeleteObjectAsync(workflow).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    private static object ToResponse(NeuCharWorkflow workflow, string graphJson = null) => new
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
