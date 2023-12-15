using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    /// <summary>
    /// 用于传送统计数据的接口服务
    /// </summary>
    public class StatisticAppService : AppServiceBase
    {
        private readonly LlmModelService _llmModelService;
        private readonly PromptItemService _promptItemService;
        private readonly IMapper _mapper;

        public StatisticAppService(
            LlmModelService llmModelService,
            PromptItemService promptItemService,
            IServiceProvider serviceProvider,
            IMapper mapper) : base(serviceProvider)
        {
            _llmModelService = llmModelService;
            _promptItemService = promptItemService;
            _mapper = mapper;
        }


        [ApiBind]
        public async Task<StringAppResponse> TestAsync()
        {
            var response = await this.GetResponseAsync<StringAppResponse, string>(
                delegate { return Task.FromResult("Service is Running"); }
            );
            return response;
        }

        [ApiBind]
        public async Task<AppResponseBase<Statistics_TodayTacticResponse>> TodayTacticStatisticAsync([FromQuery] string today)
        {
            var response = await this.GetResponseAsync<AppResponseBase<Statistics_TodayTacticResponse>, Statistics_TodayTacticResponse>(
                async (resp, logger) =>
                {
                    int cnt = await _promptItemService.GetCountAsync(p => p.Name.StartsWith(today));

                    return new Statistics_TodayTacticResponse(cnt);
                });
            return response;
        }
    }
}