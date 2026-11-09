/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SettingsView.cs
    文件功能描述：设置页的纯逻辑（状态、试用链接、模型选项、保存校验）


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增 SettingsView 与 ModelChoice（设置页的纯逻辑，页面模型只做转发）

----------------------------------------------------------------*/

using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>One entry of a model picker: an AI Kernel model Id, its label, and whether live can call its platform. Never the key.</summary>
public sealed record ModelChoice(int Id, string Label, bool Supported);

/// <summary>The Settings page's logic, kept out of the PageModel so tests reach it without an admin login.</summary>
public static class SettingsView
{
    public const string DemoUrl = "https://leozheng265.github.io/neucharbox-demo/";

    /// <summary>
    /// The demo page's link that points it at this site, for <c>http://localhost:*</c> and <c>http://127.0.0.1:*</c> only
    /// (the hosted demo is https and a browser lets it reach plain-http loopback; any other origin needs the visitor's
    /// own API base). Null otherwise.
    /// </summary>
    public static string? TryItLink(string siteOrigin)
    {
        if (!Uri.TryCreate(siteOrigin, UriKind.Absolute, out var u) || u.Scheme != Uri.UriSchemeHttp) return null;
        if (!(u.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || u.Host == "127.0.0.1")) return null;
        return DemoUrl + "?liveApi=" + Uri.EscapeDataString(siteOrigin);
    }

    /// <summary>The resx key of the status line: Status.On, Status.Off.budget, or Status.Off.&lt;LiveOff code&gt;. An off reason wins over a spent budget.</summary>
    public static string StatusKey(LiveSnapshot s, bool budgetSpent)
    {
        if (!s.On) return "Status.Off." + (s.OffReason ?? LiveOff.Unavailable);
        return budgetSpent ? "Status.Off.budget" : "Status.On";
    }

    /// <summary>The Chat models, with Supported = live can call their platform. Label = Alias, else ModelId.</summary>
    public static IReadOnlyList<ModelChoice> Choices(IEnumerable<AIModelDto> models) => models
        .Where(m => m.ConfigModelType == ConfigModelType.Chat)
        .Select(m => new ModelChoice(m.Id, string.IsNullOrWhiteSpace(m.Alias) ? m.ModelId ?? "" : m.Alias, AiKernelChatClients.IsSupported(m.AiPlatform)))
        .ToList();

    /// <summary>"PlanModelId" and/or "RunModelId" when that id is set but is not a choice, or is one live can't call.</summary>
    public static IReadOnlyList<string> ModelProblems(LiveSettings s, IReadOnlyList<ModelChoice> choices)
    {
        var problems = new List<string>();
        if (!Usable(s.PlanModelId, choices)) problems.Add(nameof(LiveSettings.PlanModelId));
        if (!Usable(s.RunModelId, choices)) problems.Add(nameof(LiveSettings.RunModelId));
        return problems;
    }

    static bool Usable(int? id, IReadOnlyList<ModelChoice> choices) =>
        id is not { } i || choices.Any(c => c.Id == i && c.Supported);

    /// <summary>Saves only when both <see cref="ModelProblems"/> and <see cref="LiveSettingsService.Validate"/> are empty; otherwise returns all the problems and changes nothing.</summary>
    public static async Task<IReadOnlyList<string>> SaveAsync(LiveSettingsService service, LiveSettings s, IReadOnlyList<ModelChoice> choices,
        CancellationToken ct = default)
    {
        var problems = ModelProblems(s, choices).Concat(LiveSettingsService.Validate(s)).ToList();
        if (problems.Count > 0) return problems;
        return await service.SaveAsync(s, ct);
    }

    /// <summary>The extra-origins textarea: one origin per line, blank lines dropped.</summary>
    public static IReadOnlyList<string> ParseOrigins(string? text) =>
        (text ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
