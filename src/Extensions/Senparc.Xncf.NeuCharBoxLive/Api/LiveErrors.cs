/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveErrors.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：LiveErrors


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>A contract error kind, its HTTP status, and the plain sentence the demo falls back to for an unknown kind.</summary>
public record LiveError(string Kind, int Status, string Message);

/// <summary>Every error kind in the contract (Transport table), with its status and fallback message.</summary>
public static class LiveErrors
{
    public static IReadOnlyDictionary<string, LiveError> All { get; } = new LiveError[]
    {
        new("bad_request", 400, "That request couldn't be read."),
        new("bad_room", 400, "That room can't be set up. Check its devices and try again."),
        new("too_long", 400, "Keep it under 400 characters."),
        new("bad_value", 400, "That value isn't allowed for this device."),
        new("bad_code", 401, "That invite code isn't valid."),
        new("no_session", 404, "This live session has ended."),
        new("no_request", 404, "That request isn't in this session."),
        new("no_automation", 404, "That automation isn't in this room any more."),
        new("busy", 409, "Wait for the current request to finish."),
        new("not_waiting", 409, "Nothing is waiting for an answer."),
        new("device_unavailable", 409, "That device can't be changed right now."),
        new("code_limit", 429, "That's today's live requests for this code. Try again tomorrow."),
        new("global_limit", 429, "Live AI has hit today's limit. Try again tomorrow."),
        new("budget", 429, "Live AI is resting for today."),
        new("rate", 429, "Too many tries from here. Try again later."), // apply, access/check, set, advance and automations
        new("live_off", 503, "Live AI is offline right now."),
        new("internal", 500, "Something went wrong on our side. Try again in a moment."),
    }.ToDictionary(e => e.Kind, StringComparer.Ordinal);

    /// <summary>The contract's error body <c>{ "error", "message" }</c> with the kind's status. An unknown kind is a bug.</summary>
    public static ContentResult Result(string kind)
    {
        var e = Of(kind);
        return LiveJson.Result(e.Status, Body(e));
    }

    /// <summary>
    /// The same body written straight to a response that hasn't started, for middleware outside MVC. The status is the
    /// kind's, unless <paramref name="status"/> is given (a 405 carries the bad_request body).
    /// </summary>
    public static Task WriteAsync(HttpContext ctx, string kind, int? status = null)
    {
        var e = Of(kind);
        ctx.Response.StatusCode = status ?? e.Status;
        ctx.Response.ContentLength = null;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        return ctx.Response.WriteAsync(Body(e).ToJsonString(LiveJson.Options));
    }

    static LiveError Of(string kind) =>
        All.TryGetValue(kind, out var e) ? e : throw new ArgumentOutOfRangeException(nameof(kind), kind, "not a contract error kind");

    static JsonObject Body(LiveError e) => new() { ["error"] = e.Kind, ["message"] = e.Message };
}

/// <summary>
/// JSON bodies written here rather than through the host's MVC formatters, so the wire stays camelCase UTF-8 whatever
/// JSON settings the NCF host uses.
/// </summary>
internal static class LiveJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static ContentResult Result(int status, JsonNode body) => new()
    {
        StatusCode = status,
        ContentType = "application/json; charset=utf-8",
        Content = body.ToJsonString(Options),
    };
}
