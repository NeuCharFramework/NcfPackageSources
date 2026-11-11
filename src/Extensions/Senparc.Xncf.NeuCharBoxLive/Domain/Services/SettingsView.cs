/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SettingsView.cs
    文件功能描述：设置页的纯逻辑（状态、试用链接、模型选项、保存校验）


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增 SettingsView 与 ModelChoice（设置页的纯逻辑，页面模型只做转发）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增 LoadChoicesAsync（AI Kernel 读不到时返回空列表并标记，页面不再抛错）；SaveAsync 保存成功后等待运行时刷新

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增 WakeIfStartingAsync：打开设置页时唤醒仍在 starting 的运行时（本次运行中新安装的站点）

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 “试用”链接改指向本站自带的演示页（/live/demo/），不再限于 http 回环地址

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>One entry of a model picker: an AI Kernel model Id, its label, and whether live can call its platform. Never the key.</summary>
public sealed record ModelChoice(int Id, string Label, bool Supported);

/// <summary>The Settings page's logic, kept out of the PageModel so tests reach it without an admin login.</summary>
public static class SettingsView
{
    /// <summary>
    /// The link to the demo pages this site serves itself (<see cref="Api.LiveDemoFiles"/>), under the site's path base.
    /// Same origin as /live/api, so it works on any address the site has.
    /// </summary>
    public static string TryItLink(string? pathBase) => (pathBase ?? "").TrimEnd('/') + Api.LiveDemoFiles.Prefix + "/";

    /// <summary>The resx key of the status line: Status.On, Status.Off.budget, or Status.Off.&lt;LiveOff code&gt;. An off reason wins over a spent budget.</summary>
    public static string StatusKey(LiveSnapshot s, bool budgetSpent)
    {
        if (!s.On) return "Status.Off." + (s.OffReason ?? LiveOff.Unavailable);
        return budgetSpent ? "Status.Off.budget" : "Status.On";
    }

    /// <summary>
    /// The pickers' choices from AI Kernel, or none with Unreadable true when AI Kernel can't be read (not installed or not
    /// enabled, its table missing, its database down): the page then shows an empty picker and says so instead of failing.
    /// Only the exception's type is logged. A cancelled request still throws.
    /// </summary>
    public static async Task<(IReadOnlyList<ModelChoice> Choices, bool Unreadable)> LoadChoicesAsync(IAiModelSource models,
        ILogger? log = null, CancellationToken ct = default)
    {
        try
        {
            return (Choices(await models.ListAsync(ct)), false);
        }
        catch (Exception e) when (!ct.IsCancellationRequested)
        {
            log?.LogWarning("NeuCharBox Live's Settings page can't read AI Kernel's models ({ExceptionType}); the model pickers are empty",
                e.GetType().FullName);
            return ([], true);
        }
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

    /// <summary>
    /// Saves only when both <see cref="ModelProblems"/> and <see cref="LiveSettingsService.Validate"/> are empty; otherwise
    /// returns all the problems and changes nothing. After a save it awaits <paramref name="refresh"/> (the module runtime's
    /// RefreshAsync), so the page the admin is sent back to shows the new status, not the one from before the save.
    /// </summary>
    public static async Task<IReadOnlyList<string>> SaveAsync(LiveSettingsService service, LiveSettings s, IReadOnlyList<ModelChoice> choices,
        CancellationToken ct = default, Func<CancellationToken, Task>? refresh = null)
    {
        var problems = ModelProblems(s, choices).Concat(LiveSettingsService.Validate(s)).ToList();
        if (problems.Count > 0) return problems;
        problems = (await service.SaveAsync(s, ct)).ToList();
        if (problems.Count == 0 && refresh is not null) await refresh(ct);
        return problems;
    }

    /// <summary>
    /// Opening the Settings page wakes a runtime that is still <see cref="LiveOff.Starting"/>: on a site installed in this
    /// run NCF never reports ready until a restart, and a successful refresh here (the admin page itself just read the
    /// database) lets the runtime poll as usual. Does nothing for a runtime that is already awake.
    /// </summary>
    public static Task WakeIfStartingAsync(LiveSnapshot current, Func<CancellationToken, Task>? refresh, CancellationToken ct = default) =>
        current.OffReason == LiveOff.Starting && refresh is not null ? refresh(ct) : Task.CompletedTask;

    /// <summary>The extra-origins textarea: one origin per line, blank lines dropped.</summary>
    public static IReadOnlyList<string> ParseOrigins(string? text) =>
        (text ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
