/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSkillPlugin.cs
    File description: Related implementation.

    Created by: Senparc - 20261016

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 update AdminChat with PromptRange Skill integration

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Areas.Admin.Domain.Services.AIPlugins;

/// <summary>
/// Provides the current AdminChat with the published Skill catalog through abstractions.
/// </summary>
public sealed class AdminChatSkillPlugin
{
    private readonly ISkillCatalog _catalog;
    private readonly ISkillExecutor _executor;
    private readonly ISkillPackageService _packageService;
    private readonly HashSet<string> _loadedSkillIds = new(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the Skill catalog, execution, and package-management dependencies used by AdminChat.
    /// </summary>
    /// <param name="catalog">The catalog used to list and load published Skills.</param>
    /// <param name="executor">The executor used to run loaded Skills.</param>
    /// <param name="packageService">The package service used for explicit administrator-requested installs.</param>
    public AdminChatSkillPlugin(
        ISkillCatalog catalog,
        ISkillExecutor executor,
        ISkillPackageService packageService)
    {
        _catalog = catalog;
        _executor = executor;
        _packageService = packageService;
    }

    /// <summary>
    /// Lists Skills published for the AdminChat caller and serializes their current versions.
    /// </summary>
    /// <returns>A JSON array describing the published Skills and available prompt versions.</returns>
    [KernelFunction]
    [Description("List published Skills available to AdminChat.")]
    public async Task<string> ListSkillsAsync()
    {
        var skills = await _catalog.ListAsync(new SkillQuery(
            PublishedOnly: true,
            CallerModule: "AdminChat"));
        return JsonSerializer.Serialize(skills.Select(skill => new
        {
            skill.SkillId,
            skill.Name,
            skill.Description,
            skill.CurrentVersion,
            Versions = skill.Versions.Select(version => version.PromptCode)
        }));
    }

    /// <summary>
    /// Loads one published Skill into the current plugin instance so a later execution call may use it.
    /// </summary>
    /// <param name="skillId">The stable Skill identifier or SkillCode.</param>
    /// <returns>A JSON object describing the loaded Skill.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the catalog cannot resolve the requested Skill.</exception>
    [KernelFunction]
    [Description("Load one published Skill into the current AdminChat turn so it can be used. Always call this before ExecuteSkill.")]
    public async Task<string> LoadSkillAsync(
        [Description("Stable SkillId or SkillCode")] string skillId)
    {
        var skill = await _catalog.GetAsync(
            skillId,
            context: new SkillExecutionContext("AdminChat", Mode: SkillExecutionMode.Production));
        _loadedSkillIds.Add(skill.SkillId);
        return JsonSerializer.Serialize(new
        {
            skill.SkillId,
            skill.Name,
            skill.CurrentVersion,
            Loaded = true
        });
    }

    /// <summary>
    /// Executes a Skill previously loaded into this plugin instance.
    /// </summary>
    /// <param name="skillId">The stable Skill identifier or SkillCode.</param>
    /// <param name="input">The user input passed to the Skill.</param>
    /// <param name="version">An optional concrete PromptCode version.</param>
    /// <returns>The Skill execution output, or a prompt instructing the caller to load the Skill first.</returns>
    [KernelFunction]
    [Description("Execute a published Skill by SkillId with user input.")]
    public async Task<string> ExecuteSkillAsync(
        [Description("Stable SkillId or SkillCode")] string skillId,
        [Description("User input passed to the Skill")] string input,
        [Description("Optional concrete PromptCode version")] string version = null)
    {
        if (!_loadedSkillIds.Contains(skillId))
        {
            return "该 Skill 尚未加载。请先调用 LoadSkillAsync，并确认使用的 SkillId。";
        }

        var result = await _executor.ExecuteAsync(new SkillExecutionRequest(
            skillId,
            version,
            input,
            new SkillExecutionContext("AdminChat")));
        return result.Output;
    }

    /// <summary>
    /// Installs a Skill package from an HTTPS URL after explicit administrator request.
    /// Installation creates a draft requiring approval; it does not publish or execute the Skill.
    /// </summary>
    /// <param name="packageUrl">The HTTPS URL of the Skill ZIP package.</param>
    /// <returns>A JSON object describing the installed draft and approval requirement.</returns>
    /// <exception cref="ArgumentException">Thrown when the package service rejects the URL.</exception>
    [KernelFunction]
    [Description("Install a Skill package from a HTTPS URL after the administrator explicitly requests installation. It installs as Draft and never publishes or executes it automatically.")]
    public async Task<string> InstallSkillFromUrlAsync(
        [Description("HTTPS URL of the Skill ZIP package")] string packageUrl)
    {
        var result = await _packageService.InstallFromUrlAsync(packageUrl);
        return JsonSerializer.Serialize(new
        {
            result.Skill.SkillId,
            result.Skill.Name,
            result.Skill.CurrentVersion,
            result.RequiresApproval,
            result.Message
        });
    }
}
