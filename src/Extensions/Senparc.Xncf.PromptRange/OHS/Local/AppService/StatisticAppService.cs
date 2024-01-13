using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
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
        private readonly LlModelService _llModelService;
        private readonly PromptItemService _promptItemService;
        private readonly IMapper _mapper;

        public StatisticAppService(
            // LlModelService llModelService,
            PromptItemService promptItemService,
            IServiceProvider serviceProvider,
            IMapper mapper) : base(serviceProvider)
        {
            // _llModelService = llModelService;
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
                    int cnt = await _promptItemService.GetCountAsync(
                        p => p.RangeName.StartsWith(today)
                    );

                    return new Statistics_TodayTacticResponse(cnt);
                });
            return response;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptItem_HistoryScoreResponse>> GetHistoryScoreAsync(int promptItemId)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptItem_HistoryScoreResponse>, PromptItem_HistoryScoreResponse>(
                async (response, logger) =>
                {
                    logger.SaveLogs($"传入ID为{promptItemId}");

                    return await _promptItemService.GetHistoryScoreAsync(promptItemId);
                });
        }


        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<Statistic_TodayTacticResponse>> GetLineChartDataAsync(int promptItemId, bool isAvg)
        {
            return await this.GetResponseAsync<AppResponseBase<Statistic_TodayTacticResponse>, Statistic_TodayTacticResponse>(
                async (response, logger) =>
                {
                    logger.SaveLogs($"查询三维折线图，传入ID为{promptItemId}");

                    return await _promptItemService.GetLineChartDataAsync(promptItemId, isAvg);
                });
        }
    }
}