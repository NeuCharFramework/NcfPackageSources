using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public class PromptRangeService : ServiceBase<PromptRange>
{
    public PromptRangeService(IRepositoryBase<PromptRange> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
    {
    }

    // public async Task<PromptRangeDto> UpdateExpectedResultsAsync(int promptRangeId, string expectedResults)
    // {
    //     var promptRange = await this.GetObjectAsync(p => p.Id == promptRangeId) ??
    //                       throw new Exception($"未找到{promptRangeId}对应的靶场");
    //
    //     promptRange.UpdateExpectedResultsJson(expectedResults);
    //
    //     await this.SaveObjectAsync(promptRange);
    //
    //     return this.TransEntityToDto(promptRange);
    // }


    public async Task<PromptRangeDto> GetAsync(int Id)
    {
        var promptRange = await this.GetObjectAsync(p => p.Id == Id) ??
                          throw new Exception($"未找到{Id}对应的靶场");

        return this.TransEntityToDto(promptRange);
    }

    public async Task<List<PromptRangeDto>> GetListAsync()
    {
        var promptRange = await this.GetFullListAsync(p => true);

        return promptRange.Select(TransEntityToDto).ToList();
    }

    public async Task<PromptRangeDto> AddAsync(string alias)
    {
        var today = SystemTime.Now;
        var todayStr = today.ToString("yyyy.MM.dd");

        List<PromptRange> todayRangeList = await this.GetFullListAsync(
            p => p.RangeName.StartsWith($"{todayStr}."),
            p => p.Id,
            OrderingType.Descending
        );

        var promptRange = new PromptRange($"{todayStr}.{todayRangeList.Count + 1}");

        promptRange.ChangeAlias(alias);

        await this.SaveObjectAsync(promptRange);

        return this.TransEntityToDto(promptRange);
    }

    public async Task<PromptRangeDto> ChangeAliasAsync(int rangeId, string alias)
    {
        var promptRange = await this.GetObjectAsync(r => r.Id == rangeId)
                          ?? throw new NcfExceptionBase($"没有找到{rangeId}对应的靶场");

        promptRange.ChangeAlias(alias);

        await this.SaveObjectAsync(promptRange);

        return this.TransEntityToDto(promptRange);
    }

    private PromptRangeDto TransEntityToDto(PromptRange promptRange)
    {
        return this.Mapper.Map<PromptRangeDto>(promptRange);
    }
}