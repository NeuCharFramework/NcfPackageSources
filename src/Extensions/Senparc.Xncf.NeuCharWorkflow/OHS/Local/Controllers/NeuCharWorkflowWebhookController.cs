/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowWebhookController.cs
    文件功能描述：HTTP 控制器与远程接口


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

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
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

namespace Senparc.Xncf.NeuCharWorkflow.OHS.Local.Controllers;

/// <summary>
/// NeuChar Workflow 的匿名 Webhook 入口。真正的工作流仍由服务端协调器执行，浏览器端不会参与运行。
/// </summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/webhook")]
public sealed class NeuCharWorkflowWebhookController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxRequestBytes = 1_048_576;

    private readonly NeuCharWorkflowAppService _workflowAppService;

    public NeuCharWorkflowWebhookController(
        NeuCharWorkflowAppService workflowAppService)
    {
        _workflowAppService = workflowAppService;
    }

    // 这里不声明 HTTP 动词约束；当配置为 any 时，路由可以接收任意动词，配置为 GET/POST 时由下方校验返回 405。
    [Route("{workflowId:int}")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> TriggerAsync(int workflowId, CancellationToken cancellationToken)
    {
        var suppliedToken = Request.Headers["X-NeuChar-Webhook-Token"].FirstOrDefault();
        suppliedToken ??= Request.Query["token"].FirstOrDefault();

        Dictionary<string, object> values;
        try
        {
            values = await ReadValuesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { success = false, errorMessage = ex.Message });
        }

        var result = await _workflowAppService.TriggerWebhookAsync(
            workflowId,
            Request.Method,
            suppliedToken,
            values,
            cancellationToken).ConfigureAwait(false);
        if (result.StatusCode == StatusCodes.Status405MethodNotAllowed)
        {
            Response.Headers.Allow = result.AllowedMethod == "get" ? "GET" : "POST";
        }
        return StatusCode(result.StatusCode, result.StatusCode == StatusCodes.Status202Accepted
            ? new { success = true, workflowId = result.WorkflowId, runId = result.RunId, acceptedAt = DateTimeOffset.UtcNow }
            : new { success = false, errorMessage = result.ErrorMessage, missingParameters = result.MissingParameters });
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

}
