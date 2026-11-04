using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Xncf.PromptRange.Abstractions.References;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public sealed class PromptReferenceService : IPromptReferenceService
{
    private readonly IRepositoryBase<PromptItem> _repository;
    private readonly PromptResultService _results;

    public PromptReferenceService(IRepositoryBase<PromptItem> repository, PromptResultService results)
    {
        _repository = repository;
        _results = results;
    }

    private async Task<PromptItem> GetAsync(int id)
    {
        var db = _repository.BaseDB.BaseDataContext;
        return await (from item in db.Set<PromptItem>()
                      join range in db.Set<Models.DatabaseModel.PromptRange>() on item.RangeId equals range.Id
                      where item.Id == id && range.Kind == SkillKind.Prompt
                      select item).SingleOrDefaultAsync()
            ?? throw new NcfExceptionBase("The referenced PromptCode is not available.");
    }

    private static PromptReferenceInfo Info(PromptItem item) => new(
        item.Id, item.FullVersion, item.RobotScore ?? -1, item.HumanScore ?? -1,
        item.FinalScore ?? item.EvalAvgScore, HasBenchmark(item));

    private static bool HasBenchmark(PromptItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ExpectedResultsJson)) return false;
        try
        {
            return JsonConvert.DeserializeObject<List<string>>(item.ExpectedResultsJson)?
                .Any(value => !string.IsNullOrWhiteSpace(value)) == true;
        }
        catch (JsonException)
        {
            Senparc.CO2NET.Trace.SenparcTrace.SendCustomLog("PromptRange benchmark",
                $"PromptItem {item.Id} has invalid benchmark JSON; AI scoring is unavailable.");
            return false;
        }
    }

    internal static void ApplyScore(DbContext context, PromptItem item, decimal score, bool robot)
    {
        item.SetReferenceScore(score, robot);
        // Repeating a cached value must still win over an intervening save from another request.
        var entry = context.Entry(item);
        entry.Property(value => value.FinalScore).IsModified = true;
        entry.Property(robot ? nameof(PromptItem.RobotScore) : nameof(PromptItem.HumanScore)).IsModified = true;
    }

    public async Task ValidateAsync(string content, IReadOnlyList<PromptReference> references)
    {
        PromptReference.ValidateRanges(content, references);
        if (references == null) return;
        foreach (var reference in references)
        {
            var item = await GetAsync(reference.PromptItemId);
            if (item.FullVersion != reference.PromptCode ||
                !string.Equals(content.Substring(reference.Start, reference.Length), item.Content, StringComparison.Ordinal))
                throw new NcfExceptionBase("Referenced prompt content has changed. Remove the reference or insert the prompt again.");
        }
    }

    public async Task<PromptReferenceInfo> GetInfoAsync(int promptItemId) => Info(await GetAsync(promptItemId));

    public async Task<PromptReferenceInfo> ScoreAsync(int promptItemId, decimal score)
    {
        var item = await GetAsync(promptItemId);
        ApplyScore(_repository.BaseDB.BaseDataContext, item, score, false);
        await _repository.BaseDB.BaseDataContext.SaveChangesAsync();
        await _repository.BaseDB.BaseDataContext.Entry(item).ReloadAsync();
        return Info(item);
    }

    public async Task<PromptReferenceInfo> ScoreWithAIAsync(int promptItemId, string context, string result)
    {
        if (string.IsNullOrWhiteSpace(result) || result.Length > 64000 || (context?.Length ?? 0) > 64000)
            throw new NcfExceptionBase("A non-empty result and context of at most 64000 characters each are required.");
        var item = await GetAsync(promptItemId);
        if (!Info(item).HasBenchmark) throw new NcfExceptionBase("This PromptCode has no benchmark.");
        var score = await _results.EvaluateReferenceAsync(promptItemId, context, result);
        // Reload after the AI call so a concurrent manual score is retained as a source.
        await _repository.BaseDB.BaseDataContext.Entry(item).ReloadAsync();
        item = await GetAsync(promptItemId);
        ApplyScore(_repository.BaseDB.BaseDataContext, item, score, true);
        await _repository.BaseDB.BaseDataContext.SaveChangesAsync();
        await _repository.BaseDB.BaseDataContext.Entry(item).ReloadAsync();
        return Info(item);
    }
}
