using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

/// <summary>
/// NeuChar Workflow 的匿名 Webhook 入口。真正的工作流仍由服务端协调器执行，浏览器端不会参与运行。
/// </summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/Senparc.Areas.Admin/neuchar-workflow/webhook")]
public sealed class NeuCharWorkflowWebhookController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxRequestBytes = 1_048_576;

    private readonly NeuCharWorkflowService _workflowService;
    private readonly NeuCharWorkflowRunCoordinator _runCoordinator;

    public NeuCharWorkflowWebhookController(
        NeuCharWorkflowService workflowService,
        NeuCharWorkflowRunCoordinator runCoordinator)
    {
        _workflowService = workflowService;
        _runCoordinator = runCoordinator;
    }

    // 这里不声明 HTTP 动词约束；当配置为 any 时，路由可以接收任意动词，配置为 GET/POST 时由下方校验返回 405。
    [Route("{workflowId:int}")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> TriggerAsync(int workflowId, CancellationToken cancellationToken)
    {
        var workflow = await _workflowService.GetObjectAsync(z => z.Id == workflowId).ConfigureAwait(false);
        if (workflow == null)
        {
            return NotFound(new { success = false, errorMessage = "工作流不存在。" });
        }
        if (!workflow.Enabled || !string.Equals(workflow.TriggerType, "webhook", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { success = false, errorMessage = "工作流未启用 Webhook 触发。" });
        }

        NeuCharWorkflowWebhookConfig config;
        try
        {
            config = NeuCharWorkflowWebhookConfig.ParseStored(workflow.TriggerConfigJson);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, errorMessage = "Webhook 配置无效，请在 Workflow 页面重新保存。" });
        }

        if (!config.IsMethodAllowed(Request.Method))
        {
            Response.Headers.Allow = config.Method == "get" ? "GET" : "POST";
            return StatusCode(StatusCodes.Status405MethodNotAllowed,
                new { success = false, errorMessage = $"Webhook 只接受 {config.Method.ToUpperInvariant()} 请求。" });
        }

        var suppliedToken = Request.Headers["X-NeuChar-Webhook-Token"].FirstOrDefault();
        suppliedToken ??= Request.Query["token"].FirstOrDefault();
        if (!TokensEqual(config.Token, suppliedToken))
        {
            return Unauthorized(new { success = false, errorMessage = "Webhook 访问密钥无效。" });
        }

        Dictionary<string, object> values;
        try
        {
            values = await ReadValuesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { success = false, errorMessage = ex.Message });
        }

        var selectedValues = config.Parameters.Count == 0
            ? values
            : values.Where(pair => config.Parameters.Any(parameter =>
                    string.Equals(parameter.Name, pair.Key, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var missing = config.Parameters
            .Where(parameter => parameter.Required &&
                                (!selectedValues.TryGetValue(parameter.Name, out var value) || IsEmpty(value)))
            .Select(parameter => parameter.Name)
            .ToArray();
        if (missing.Length > 0)
        {
            return BadRequest(new
            {
                success = false,
                errorMessage = $"缺少必填 Webhook 参数：{string.Join("、", missing)}。",
                missingParameters = missing
            });
        }

        var input = JsonSerializer.Serialize(selectedValues, JsonOptions);
        if (!_runCoordinator.TryStart(
                workflow.Id,
                workflow.AdminUserId,
                input,
                out var runId,
                out var error))
        {
            return Conflict(new { success = false, errorMessage = error });
        }

        return Accepted(new
        {
            success = true,
            workflowId = workflow.Id,
            runId,
            acceptedAt = DateTimeOffset.UtcNow
        });
    }

    private async Task<Dictionary<string, object>> ReadValuesAsync(CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Request.Query)
        {
            if (!string.Equals(pair.Key, "token", StringComparison.OrdinalIgnoreCase))
            {
                SetValue(values, pair.Key, pair.Value.Count == 1 ? pair.Value[0] : pair.Value.ToArray());
            }
        }

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            foreach (var pair in form)
            {
                SetValue(values, pair.Key, pair.Value.Count == 1 ? pair.Value[0] : pair.Value.ToArray());
            }
            return values;
        }

        if (Request.ContentLength is null or 0)
        {
            return values;
        }
        if (Request.ContentLength > MaxRequestBytes)
        {
            throw new InvalidDataException("Webhook 请求体不能超过 1 MB。");
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return values;
        }
        if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        SetValue(values, property.Name, property.Value.Clone());
                    }
                }
                else
                {
                    SetValue(values, "_body", document.RootElement.Clone());
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Webhook JSON 请求体无效。", ex);
            }
        }
        else
        {
            SetValue(values, "_body", body);
        }
        return values;
    }

    private static void SetValue(Dictionary<string, object> values, string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            values[key] = value;
        }
    }

    private static bool IsEmpty(object value) => value switch
    {
        null => true,
        string text => string.IsNullOrWhiteSpace(text),
        string[] values => values.Length == 0 || values.All(string.IsNullOrWhiteSpace),
        JsonElement element => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ||
                                element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString()),
        _ => false
    };

    private static bool TokensEqual(string expected, string actual)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
        {
            return false;
        }
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

}
