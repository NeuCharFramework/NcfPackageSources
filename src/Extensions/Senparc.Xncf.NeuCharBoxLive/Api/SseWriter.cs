/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SseWriter.cs
    文件功能描述：把会话事件按 SSE 格式写出（id、event、data，空闲时发 ping）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// A session's event stream (contract: SSE framing): <c>id</c>, <c>event</c> and <c>data</c> lines per event, and a
/// <c>: ping</c> comment after 15 s with nothing new. Each batch is flushed as soon as it is written.
/// </summary>
public sealed class SseWriter(HttpContext ctx)
{
    public static readonly TimeSpan PingEvery = TimeSpan.FromSeconds(15);
    public const string Ping = ": ping\n\n";

    readonly HttpResponse _r = ctx.Response;

    public static string Format(LiveEvent e) =>
        string.Create(CultureInfo.InvariantCulture, $"id: {e.Seq}\nevent: {e.Kind}\ndata: {e.DataJson()}\n\n");

    /// <summary>Sends the headers at once, unbuffered, so the browser sees the stream open before the first event.</summary>
    public async Task StartAsync(CancellationToken ct)
    {
        _r.StatusCode = StatusCodes.Status200OK;
        _r.ContentType = "text/event-stream";
        _r.Headers.CacheControl = "no-cache, no-transform";
        _r.Headers["X-Accel-Buffering"] = "no";
        ctx.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        await _r.StartAsync(ct);
        await _r.Body.FlushAsync(ct);
    }

    /// <summary>
    /// Streams the log's events after <paramref name="after"/>, then follows it live, until <paramref name="stop"/>
    /// fires (the client left, or the session ended) or the connection breaks. Never throws for either.
    /// </summary>
    public async Task RunAsync(EventLog log, long after, CancellationToken stop)
    {
        try
        {
            while (true)
            {
                var batch = await log.WaitAfterAsync(after, PingEvery, stop);
                if (batch.Count == 0)
                {
                    await WriteAsync(Ping, stop);
                    continue;
                }
                var sb = new StringBuilder();
                foreach (var e in batch) sb.Append(Format(e));
                await WriteAsync(sb.ToString(), stop);
                after = batch[^1].Seq;
            }
        }
        catch (Exception e) when (stop.IsCancellationRequested || e is IOException)
        {
            // The visitor closed the tab or lost the connection (a reconnect resumes with ?after=), or the session expired.
        }
    }

    async Task WriteAsync(string text, CancellationToken ct)
    {
        await _r.Body.WriteAsync(Encoding.UTF8.GetBytes(text), ct);
        await _r.Body.FlushAsync(ct);
    }
}
