using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public class PromptRangeService : ServiceBase<PromptRange>
{
    public PromptRangeService(IRepositoryBase<PromptRange> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
    {
    }

    public async Task<PromptRangeDto> UpdateExpectedResultsAsync(int promptRangeId, string expectedResults)
    {
        var promptRange = await this.GetObjectAsync(p => p.Id == promptRangeId) ??
                          throw new Exception($"未找到{promptRangeId}对应的靶场");

        promptRange.UpdateExpectedResultsJson(expectedResults);

        await this.SaveObjectAsync(promptRange);

        return this.TransEntityToDto(promptRange);
    }


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

    private PromptRangeDto TransEntityToDto(PromptRange promptRange)
    {
        return this.Mapper.Map<PromptRangeDto>(promptRange);
    }
}