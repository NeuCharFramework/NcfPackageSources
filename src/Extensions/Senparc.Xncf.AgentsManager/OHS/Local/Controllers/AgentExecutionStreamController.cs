/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutionStreamController.cs
    文件功能描述：独立 Agent 执行任务 SSE 控制器

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 提供独立 Agent 过程事件流

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务持久化、管理页和 SSE 过程回放


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
public sealed class AgentExecutionStreamController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AgentExecutionStreamHub _streamHub;

    public AgentExecutionStreamController(AgentExecutionStreamHub streamHub)
    {
        _streamHub = streamHub;
    }

    [HttpGet]
    public async Task Subscribe(
        int agentExecutionTaskId,
        bool replayBuffered = true,
        CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        await Response.WriteAsync(": connected\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var item in _streamHub.Subscribe(
            agentExecutionTaskId,
            replayBuffered,
            cancellationToken))
        {
            var payload = JsonSerializer.Serialize(item, JsonOptions);
            await Response.WriteAsync($"event: {item.EventType}\n", cancellationToken);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
