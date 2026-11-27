/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatTaskStreamController.cs
    File description: Related implementation.

    Created by: Senparc - 20260626

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260704
    Change description: v0.11.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.11.1-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.11.2-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.Controllers;

[ApiController]
[ApiAuthorize]
[Route("api/Senparc.Xncf.AgentsManager/[controller]/[action]")]
public class ChatTaskStreamController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ChatTaskStreamHub _chatTaskStreamHub;

    /// <summary>
    /// Initializes the server-sent-event controller with the task stream hub.
    /// </summary>
    /// <param name="chatTaskStreamHub">The hub that buffers and publishes task events.</param>
    public ChatTaskStreamController(ChatTaskStreamHub chatTaskStreamHub)
    {
        _chatTaskStreamHub = chatTaskStreamHub;
    }

    [HttpGet]
    /// <summary>
    /// Streams buffered and newly published task events as server-sent events until the
    /// client disconnects, the task stream ends, or cancellation is requested.
    /// </summary>
    /// <param name="chatTaskId">The chat-task identifier whose events should be streamed.</param>
    /// <param name="replayBuffered">Whether events already buffered for the task should be sent first.</param>
    /// <param name="cancellationToken">The request-abort token used to stop the stream.</param>
    /// <exception cref="OperationCanceledException">Thrown when the request is cancelled while writing the response.</exception>
    public async Task Subscribe(int chatTaskId, bool replayBuffered = true, CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        // send the connection confirmation first so EventSource reaches the connected state quickly and proxy buffering is reduced.
        await Response.WriteAsync(": connected\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var streamEvent in _chatTaskStreamHub.Subscribe(chatTaskId, replayBuffered, cancellationToken))
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
