/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptShottingService.cs
    File description: Save and execute AdminChat prompt shots through PromptRange

    Created by: Senparc - 20261122

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Abstractions.Shotting;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public sealed class PromptShottingService : IPromptShottingService
{
    private readonly PromptRangeService _ranges;
    private readonly PromptItemService _items;
    private readonly PromptResultService _results;

    public PromptShottingService(
        PromptRangeService ranges,
        PromptItemService items,
        PromptResultService results)
    {
        _ranges = ranges;
        _items = items;
        _results = results;
    }

    public async Task<IReadOnlyList<PromptShottingRange>> GetRangesAsync()
    {
        var ranges = await _ranges.GetListAsync().ConfigureAwait(false);
        var items = await _items.GetFullListAsync(item => true).ConfigureAwait(false);
        var promptCounts = items
            .GroupBy(item => item.RangeId)
            .ToDictionary(group => group.Key, group => group.Count());

        return ranges
            .Select(range => new PromptShottingRange(
                range.Id,
                string.IsNullOrWhiteSpace(range.Alias) ? range.RangeName : range.Alias,
                promptCounts.GetValueOrDefault(range.Id)))
            .OrderBy(range => range.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<PromptShottingResult> SaveAsync(PromptShottingRequest request)
    {
        if (request == null)
        {
            throw new NcfExceptionBase("PromptShotting request is required.");
        }

        var prompt = request.Prompt?.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new NcfExceptionBase("Prompt cannot be empty.");
        }

        PromptRangeDto range;
        var newRangeName = request.NewRangeName?.Trim();
        if (!string.IsNullOrWhiteSpace(newRangeName))
        {
            range = await _ranges.AddAsync(newRangeName).ConfigureAwait(false);
        }
        else if (request.RangeId.HasValue && request.RangeId.Value > 0)
        {
            range = await _ranges.GetAsync(request.RangeId.Value).ConfigureAwait(false);
        }
        else
        {
            throw new NcfExceptionBase("Select a range or enter a new range name.");
        }

        var existingItems = await _items
            .GetFullListAsync(item => item.RangeId == range.Id, item => item.Id, OrderingType.Descending)
            .ConfigureAwait(false);
        var baseItem = existingItems.FirstOrDefault();
        var item = await _items.AddPromptItemAsync(new PromptItem_AddRequest
        {
            Id = baseItem?.Id,
            RangeId = range.Id,
            ModelId = request.ModelId,
            Content = prompt,
            Note = "AdminChat.Shot",
            MaxToken = 2000,
            Temperature = 0.5f,
            TopP = 0.5f,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            StopSequences = null,
            NumsOfResults = request.Run ? 1 : 0,
            IsTopTactic = baseItem != null,
            IsDraft = !request.Run
        }).ConfigureAwait(false);

        string output = null;
        if (request.Run)
        {
            var result = await _results
                .SenparcGenerateResultAsync(item, null, null, null, null)
                .ConfigureAwait(false);
            output = result.ResultString;
            await _results.UpdateEvalScoreAsync(item.Id).ConfigureAwait(false);
        }

        return new PromptShottingResult(range.Id, range.Alias, item.Id, item.FullVersion, output);
    }
}
