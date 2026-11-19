/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowWebhookController.cs
    File description: HTTP controller and remote endpoints

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance Workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

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
/// Anonymous Webhook entry point for NeuChar Workflow. The server coordinator executes
/// the workflow; the browser does not participate in execution.
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

    // Do not declare an HTTP verb constraint here. The route accepts any verb when
    // configured as any; GET/POST restrictions return 405 from the validation below.
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
            throw new InvalidDataException("The Webhook request body must not exceed 1 MB.");
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
                throw new InvalidDataException("The Webhook JSON request body is invalid.", ex);
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
