/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PromptItemService.Selector.cs
    文件功能描述：PromptItemService.Selector 相关定义与实现


    创建标识：Senparc - 20261102

    修改标识：Senparc - 20261103
    修改描述：v0.20.8 add PromptRange selector service, UI components, and tests

    修改标识：Senparc - 20261105
    修改描述：v0.20.14 选择器返回评分并支持按评分排序

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.21.0 Prompt 选择器服务增强
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Abstractions.Skills;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public partial class PromptItemService
{
    private IQueryable<PromptSelectorItem> SelectorQuery()
    {
        var db = RepositoryBase.BaseDB.BaseDataContext;
        // Keep the host's tenant and soft-delete query filters on both tables.
        return from item in db.Set<PromptItem>().AsNoTracking()
               join range in db.Set<Models.DatabaseModel.PromptRange>().AsNoTracking()
                   on item.RangeId equals range.Id
               where range.Kind == SkillKind.Prompt
               select new PromptSelectorItem
               {
                   Id = item.Id,
                   RangeId = range.Id,
                   RangeName = range.RangeName,
                   RangeAlias = range.Alias,
                   Tactic = item.Tactic,
                   NickName = item.NickName,
                   FullVersion = item.FullVersion,
                   IsDraft = item.IsDraft,
                   EvalAvgScore = item.EvalAvgScore,
                   EvalMaxScore = item.EvalMaxScore,
                   CurrentScore = item.FinalScore ?? item.EvalAvgScore,
                   Content = item.Content
               };
    }

    public Task<List<PromptSelectorOption>> GetSelectorOptionsAsync()
    {
        return SelectorQuery().Select(item => new PromptSelectorOption
        {
            RangeId = item.RangeId,
            RangeName = item.RangeName,
            RangeAlias = item.RangeAlias,
            Tactic = item.Tactic
        }).Distinct().OrderBy(item => item.RangeName).ThenBy(item => item.Tactic).ToListAsync();
    }

    public async Task<PromptSelectorSearchResponse> SearchSelectorAsync(
        int rangeId, string tactic, string keyword, int pageIndex, int pageSize, string sortBy = "score")
    {
        if (rangeId < 0 || pageIndex < 1 || pageSize < 1 || pageSize > 50 ||
            (long)(pageIndex - 1) * pageSize > int.MaxValue ||
            (keyword?.Length ?? 0) > 200 || (tactic?.Length ?? 0) > 50 ||
            (rangeId == 0 && !string.IsNullOrWhiteSpace(tactic)) ||
            (sortBy != "score" && sortBy != "promptCode"))
        {
            throw new NcfExceptionBase("Invalid PromptRange selector filters or pagination.");
        }

        var query = SelectorQuery();
        if (rangeId > 0)
        {
            query = query.Where(item => item.RangeId == rangeId);
        }
        if (!string.IsNullOrWhiteSpace(tactic))
        {
            query = query.Where(item => item.Tactic == tactic);
        }
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim().ToLowerInvariant();
            query = query.Where(item =>
                (item.NickName != null && item.NickName.ToLower().Contains(term)) ||
                (item.Content != null && item.Content.ToLower().Contains(term)) ||
                (item.RangeAlias != null && item.RangeAlias.ToLower().Contains(term)) ||
                item.FullVersion.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();
        // SQLite cannot order decimal columns directly; scores use a numeric cast, not text ordering.
        var orderedQuery = sortBy == "promptCode"
            ? query.OrderBy(item => item.FullVersion).ThenByDescending(item => item.Id)
            : query.OrderByDescending(item => (double)item.CurrentScore).ThenByDescending(item => item.Id);
        var items = await orderedQuery
            .Skip((pageIndex - 1) * pageSize).Take(pageSize)
            .Select(item => new PromptSelectorItem
            {
                Id = item.Id,
                RangeId = item.RangeId,
                RangeName = item.RangeName,
                RangeAlias = item.RangeAlias,
                Tactic = item.Tactic,
                NickName = item.NickName,
                FullVersion = item.FullVersion,
                IsDraft = item.IsDraft,
                EvalAvgScore = item.EvalAvgScore,
                EvalMaxScore = item.EvalMaxScore,
                CurrentScore = item.CurrentScore,
                Content = item.Content == null ? "" :
                    item.Content.Length > 240 ? item.Content.Substring(0, 240) : item.Content
            }).ToListAsync();

        return new PromptSelectorSearchResponse { Items = items, TotalCount = totalCount };
    }

    public async Task<PromptSelectorItem> GetSelectorPromptAsync(int id)
    {
        return await SelectorQuery().SingleOrDefaultAsync(item => item.Id == id)
            ?? throw new NcfExceptionBase("The selected prompt is no longer available.");
    }

    public async Task<List<PromptSelectorScoreItem>> GetSelectorRangeScoresAsync(int id)
    {
        var rangeId = await SelectorQuery().Where(item => item.Id == id)
            .Select(item => (int?)item.RangeId).SingleOrDefaultAsync()
            ?? throw new NcfExceptionBase("The selected prompt is no longer available.");
        return await SelectorQuery().Where(item => item.RangeId == rangeId)
            .OrderBy(item => item.FullVersion).ThenBy(item => item.Id)
            .Select(item => new PromptSelectorScoreItem
            {
                Id = item.Id,
                RangeId = item.RangeId,
                RangeName = item.RangeName,
                RangeAlias = item.RangeAlias,
                Tactic = item.Tactic,
                FullVersion = item.FullVersion,
                CurrentScore = item.CurrentScore
            }).ToListAsync();
    }
}
