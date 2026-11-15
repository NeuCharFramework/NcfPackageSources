/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AdminChatSystemMessagePromptProvider.cs
    File description: PromptRange provider for the default AdminChat system prompt

    Created by: Senparc - 20261115

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Abstractions.SystemMessages;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public sealed class AdminChatSystemMessagePromptProvider : IAdminChatSystemMessageProvider
{
    public const string RangeAlias = "AdminChat.SystemMessage";

    private static readonly SemaphoreSlim Initialization = new(1, 1);
    private readonly PromptRangeService _ranges;
    private readonly PromptItemService _items;

    public static string DefaultPrompt =>
        PromptRangeResource.Get("AdminChat.SystemPrompt.Default");

    public AdminChatSystemMessagePromptProvider(PromptRangeService ranges, PromptItemService items)
    {
        _ranges = ranges;
        _items = items;
    }

    public async Task<AdminChatSystemMessagePrompt> GetAsync(CancellationToken cancellationToken = default)
    {
        await Initialization.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var range = await _ranges.GetObjectAsync(item => item.Alias == RangeAlias).ConfigureAwait(false);
            if (range == null)
            {
                var created = await _ranges.AddAsync(RangeAlias).ConfigureAwait(false);
                range = await _ranges.GetObjectAsync(item => item.Id == created.Id).ConfigureAwait(false);
            }

            if (range == null)
            {
                throw new NcfExceptionBase($"Unable to initialize PromptRange: {RangeAlias}");
            }

            var items = await _items.GetFullListAsync(item => item.RangeId == range.Id).ConfigureAwait(false);
            var published = items.Where(item => !item.IsDraft).OrderBy(item => item.Id).ToList();
            if (published.Count == 0)
            {
                var item = CreateDefaultPrompt(range.Id, range.RangeName);
                await _items.SaveObjectAsync(item).ConfigureAwait(false);
                published.Add(item);
            }

            var selected = SelectPublishedPrompt(published);
            return new AdminChatSystemMessagePrompt(selected.Content, selected.FullVersion);
        }
        finally
        {
            Initialization.Release();
        }
    }

    public static PromptItem CreateDefaultPrompt(int rangeId, string rangeName) =>
        new(rangeId, rangeName, "1", 1, string.Empty, new PromptItem_AddRequest
        {
            RangeId = rangeId,
            ModelId = 0,
            Content = DefaultPrompt,
            IsTopTactic = true,
            IsDraft = false,
            NumsOfResults = 0,
            MaxToken = 2000,
            Temperature = 0.6f,
            TopP = 0.9f,
            Note = "AdminChat.System"
        });

    public static PromptItem SelectPublishedPrompt(IEnumerable<PromptItem> items)
    {
        var published = (items ?? Array.Empty<PromptItem>())
            .Where(item => !item.IsDraft)
            .OrderBy(item => item.Id)
            .ToList();
        if (published.Count == 0)
        {
            throw new NcfExceptionBase("AdminChat.SystemMessage has no published prompt.");
        }

        var scored = published
            .Select(item => new { Item = item, Score = CurrentScore(item) })
            .Where(value => value.Score.HasValue)
            .OrderByDescending(value => value.Score)
            .ThenByDescending(value => value.Item.EvalMaxScore)
            .ThenBy(value => value.Item.Id)
            .FirstOrDefault();
        var selected = scored?.Item ?? published[0];
        if (string.IsNullOrWhiteSpace(selected.Content) || selected.Content.Length > 12000)
        {
            throw new NcfExceptionBase("AdminChat.SystemMessage prompt must contain 1-12000 characters.");
        }

        return selected;
    }

    private static decimal? CurrentScore(PromptItem item)
    {
        if (item.FinalScore.HasValue)
        {
            return item.FinalScore.Value;
        }

        return item.EvalAvgScore >= 0 ? item.EvalAvgScore : null;
    }
}
