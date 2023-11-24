using System;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Repository;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public class PromptResultAppService : AppServiceBase
    {
        // private readonly RepositoryBase<PromptResult> _promptResultRepository;
        private readonly PromptResultService _promptResultService;

        public PromptResultAppService(
            IServiceProvider serviceProvider,
            PromptResultService promptResultService
        ) : base(serviceProvider)
        {
            _promptResultService = promptResultService;
        }


        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> Score(PromptResultScoringRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(
                async (response, logger) =>
                {
                    await _promptResultService.Score(request.PromptResultId, request.HumanScore);
                    
                    return "ok";

                    // var result = await _promptResultService.GetObjectAsync(p => p.Id == request.PromptResultId);
                    //
                    // result.Scoring(request.HumanScore);
                    //
                    // await _promptResultService.SaveObjectAsync(result);
                    //
                    // return "ok";
                });
        }
    }
}