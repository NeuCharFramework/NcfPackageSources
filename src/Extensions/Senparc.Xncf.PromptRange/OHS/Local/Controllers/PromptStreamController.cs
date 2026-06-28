using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Xncf.PromptRange.Domain.Services;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.OHS.Local.Controllers;

[ApiController]
[AdminAuthorize]
[Route("api/Senparc.Xncf.PromptRange/[controller]/[action]")]
public class PromptStreamController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PromptResultStreamHub _streamHub;

    public PromptStreamController(PromptResultStreamHub streamHub)
    {
        _streamHub = streamHub;
    }

    [HttpGet]
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

        // 立即 flush，让 EventSource 收到 200 并触发 onopen
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
