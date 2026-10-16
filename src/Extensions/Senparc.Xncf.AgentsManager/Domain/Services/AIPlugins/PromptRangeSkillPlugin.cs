using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

    public PromptRangeSkillPlugin(ISkillCatalog catalog, ISkillExecutor executor)
    {
        _catalog = catalog;
        _executor = executor;
    }

    [KernelFunction]
    [Description("List published PromptRange Skills that this agent may use.")]
    public async Task<string> ListSkillsAsync()
    {
        var skills = await _catalog.ListAsync(new SkillQuery(
            PublishedOnly: true,
            CallerModule: "AgentsManager"));
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
        var result = await _executor.ExecuteAsync(new SkillExecutionRequest(
            skillId,
            version,
            input,
            new SkillExecutionContext("AgentsManager")));
        return result.Output;
    }
}
