/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptRangeSkillPlugin.cs
    文件功能描述：把 PromptRange Skill 暴露给 Agent 调用
    
    
    创建标识：Senparc - 20261016
    
    修改标识：Senparc - 20261021
    修改描述：v0.19.0 按绑定策略调用 PromptRange Skill

----------------------------------------------------------------*/

using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.SemanticKernel;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins;

/// <summary>
/// Exposes published PromptRange Skills to AgentsManager without coupling agents to PromptRange entities.
/// </summary>
public sealed class PromptRangeSkillPlugin
{
    private readonly ISkillCatalog _catalog;
    private readonly ISkillExecutor _executor;
    private readonly IReadOnlyDictionary<string, AgentSkillBinding> _bindings;

    public PromptRangeSkillPlugin(
        ISkillCatalog catalog,
        ISkillExecutor executor,
        IReadOnlyDictionary<string, AgentSkillBinding> bindings = null)
    {
        _catalog = catalog;
        _executor = executor;
        _bindings = bindings;
    }

    [KernelFunction]
    [Description("List published PromptRange Skills that this agent may use.")]
    public async Task<string> ListSkillsAsync()
    {
        var skills = await _catalog.ListAsync(new SkillQuery(
            PublishedOnly: true,
            CallerModule: "AgentsManager",
            AllowedSkillIds: _bindings?.Keys.ToArray()));
        return JsonSerializer.Serialize(skills.Select(skill => new
        {
            skill.SkillId,
            skill.Name,
            skill.Description,
            skill.CurrentVersion,
            Versions = skill.Versions.Select(version => version.PromptCode)
        }));
    }

    [KernelFunction]
    [Description("Execute one published PromptRange Skill by SkillId and optional PromptCode version.")]
    public async Task<string> ExecuteSkillAsync(
        [Description("Stable SkillId or SkillCode")] string skillId,
        [Description("User input passed to the Skill")] string input,
        [Description("Optional concrete PromptCode such as Range-T1-A1")] string version = null)
    {
        if (_bindings == null ||
            !_bindings.TryGetValue(skillId, out var binding) ||
            !binding.Enabled)
        {
            return "该 Agent 未显式绑定此 Skill，无法执行。";
        }

        var result = await _executor.ExecuteAsync(new SkillExecutionRequest(
            skillId,
            version ?? binding.Version,
            input,
            new SkillExecutionContext(
                "AgentsManager",
                Mode: SkillExecutionMode.Production,
                Arguments: null,
                Policy: binding.Policy)));
        return result.Output;
    }
}
