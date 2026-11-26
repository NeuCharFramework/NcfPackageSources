/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptStreamController.cs
    File description: Related implementation.

    Created by: Senparc - 20260626

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260729
    Change description: v0.17.1-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.PromptRange.Domain.Services;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.OHS.Local.Controllers;

[ApiController]
[ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
[Route("api/Senparc.Xncf.PromptRange/[controller]/[action]")]
public class PromptStreamController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PromptResultStreamHub _streamHub;

    /// <summary>
    /// Initializes the Server-Sent Events controller over the prompt result stream hub.
    /// </summary>
    /// <param name="streamHub">The hub providing stream events.</param>
    public PromptStreamController(PromptResultStreamHub streamHub)
    {
        _streamHub = streamHub;
    }

    [HttpGet]
    /// <summary>
    /// Streams prompt-result events for one stream identifier using Server-Sent Events.
    /// </summary>
    /// <param name="streamId">The prompt result stream identifier.</param>
    /// <param name="cancellationToken">Token used to close the response stream.</param>
    /// <returns>A task that completes when the stream ends or the client disconnects.</returns>
    public async Task Subscribe(string streamId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("streamId is required", cancellationToken);
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";

        // Send an initial comment so clients know the SSE connection is established.
        await Response.WriteAsync(": connected\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var streamEvent in _streamHub.Subscribe(streamId, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var payload = JsonSerializer.Serialize(streamEvent, JsonOptions);
            await Response.WriteAsync($"event: {streamEvent.EventType}\n", cancellationToken);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
