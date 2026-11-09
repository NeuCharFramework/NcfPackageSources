/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveEvent.cs
    文件功能描述：NeuCharBox Live 引擎（Events）：LiveEvent


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Events;

public record PlanStep(string Id, string Text, IReadOnlyList<string> Devices);

public record Usage(int PromptTokens, int CompletionTokens, int Calls);

public record LiveEvent(long Seq, string Kind, string? Rid, JsonObject Data)
{
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>The SSE data line: Data plus "rid" when the event belongs to a request. UTF-8 text, not \u-escaped.</summary>
    public string DataJson()
    {
        var o = (JsonObject)Data.DeepClone();
        if (Rid != null) o["rid"] = Rid;
        return o.ToJsonString(Options);
    }
}
