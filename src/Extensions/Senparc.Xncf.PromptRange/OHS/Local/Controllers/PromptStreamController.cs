using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.PromptRange.Domain.Services;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.OHS.Local.Controllers;

[ApiController]
[ApiAuthorize("AdminOnly")]
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
            Response.StatusCode = 400;
            await Response.WriteAsync("streamId is required", cancellationToken);
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";

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
