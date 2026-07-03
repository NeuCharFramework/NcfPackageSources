/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatTaskStreamController.cs
    文件功能描述：ChatTaskStreamController 控制器逻辑
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

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

    public ChatTaskStreamController(ChatTaskStreamHub chatTaskStreamHub)
    {
        _chatTaskStreamHub = chatTaskStreamHub;
    }

    [HttpGet]
    public async Task Subscribe(int chatTaskId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        // 先发送连接确认，确保 EventSource 尽快进入已连接状态并减少代理缓冲影响。
        await Response.WriteAsync(": connected\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        await foreach (var streamEvent in _chatTaskStreamHub.Subscribe(chatTaskId, cancellationToken))
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
