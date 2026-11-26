/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.Containers;
using Senparc.Weixin.MP.Entities;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager;

namespace Senparc.Xncf.WeixinManager.Areas.Admin.WeixinManager
{
    public class IndexModel : BaseAdminWeixinManagerModel
    {
        public class AccessTokenData
        {
            public int Id { get; set; }
            public string AppId { get; set; }
            public int TotalSeconds { get; set; }
            public double LeftPercent { get; set; }
            public string Status { get; set; }
        }

        public List<MpAccountDto> MpAccountDtos { get; set; }
        public int RegisteredMpAccountCount { get; set; }
        public int WeixinUserCount { get; set; }
        public int TodayWeixinUserCount { get; set; }

        public List<AccessTokenBag> AccessTokenBags { get; set; }

        private readonly ServiceBase<Domain.Models.DatabaseModel.MpAccount> _mpAccountService;
        private readonly ServiceBase<Domain.Models.DatabaseModel.WeixinUser> _weixinUserService;
        public IndexModel(Lazy<XncfModuleService> xncfModuleService,
            ServiceBase<Domain.Models.DatabaseModel.MpAccount> mpAccountService,
            ServiceBase<Domain.Models.DatabaseModel.WeixinUser> weixinUserService
            ) : base(xncfModuleService)
        {
            _mpAccountService = mpAccountService;
            _weixinUserService = weixinUserService;
        }

        /// <summary>
        /// Loads all public-account configurations, access-token state, and user counters
        /// needed by the Weixin-manager dashboard.
        /// </summary>
        /// <returns>A task that completes after the dashboard properties are populated.</returns>
        public async Task OnGetAsync()
        {
            var allMpAccounts = await _mpAccountService.GetObjectListAsync(0, 0, z => true, z => z.Id, OrderingType.Descending);

            MpAccountDtos = allMpAccounts.Select(z => _mpAccountService.Mapper.Map<MpAccountDto>(z)).ToList();

            AccessTokenBags = new List<AccessTokenBag>();
            RegisteredMpAccountCount = 0;
            foreach (var mpAccount in allMpAccounts)
            {
                if (await AccessTokenContainer.CheckRegisteredAsync(mpAccount.AppId))
                {
                    var bag = await AccessTokenContainer.TryGetItemAsync(mpAccount.AppId);
                    if (bag.AccessTokenResult != null && !bag.AccessTokenResult.access_token.IsNullOrEmpty())
                    {
                        RegisteredMpAccountCount++;
                    }
                    AccessTokenBags.Add(bag);
                }
            }

            WeixinUserCount = await _weixinUserService.GetCountAsync(z => true);
            TodayWeixinUserCount = await _weixinUserService.GetCountAsync(z => z.AddTime >= DateTime.Today);
        }

        /// <summary>
        /// Returns the current public-account dashboard data, including access-token bags
        /// and total/today user counts, for asynchronous refresh.
        /// </summary>
        /// <returns>An <see cref="OkObjectResult"/> containing the dashboard snapshot.</returns>
        public async Task<IActionResult> OnGetAjaxAsync()
        {
            var allMpAccounts = await _mpAccountService.GetObjectListAsync(0, 0, z => true, z => z.Id, OrderingType.Descending);

            var mpAccountDtos = allMpAccounts.Select(z => _mpAccountService.Mapper.Map<MpAccountDto>(z)).ToList();

            var accessTokenBags = new List<AccessTokenBag>();
            var registeredMpAccountCount = 0;
            foreach (var mpAccount in allMpAccounts)
            {
                if (await AccessTokenContainer.CheckRegisteredAsync(mpAccount.AppId))
                {
                    var bag = await AccessTokenContainer.TryGetItemAsync(mpAccount.AppId);
                    if (bag.AccessTokenResult != null && !bag.AccessTokenResult.access_token.IsNullOrEmpty())
                    {
                        registeredMpAccountCount++;
                    }
                    accessTokenBags.Add(bag);
                }
            }

            var weixinUserCount = await _weixinUserService.GetCountAsync(z => true);
            var todayWeixinUserCount = await _weixinUserService.GetCountAsync(z => z.AddTime >= DateTime.Today);
            return Ok(new 
            {
                mpAccountDtos,
                accessTokenBags,
                registeredMpAccountCount,
                weixinUserCount,
                todayWeixinUserCount
            });
        }

        /// <summary>
        /// Calculates access-token status and remaining-lifetime percentages for the requested accounts.
        /// </summary>
        /// <param name="ids">The public-account identifiers whose token state is requested.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing one status record per known identifier.</returns>
        /// <exception cref="NullReferenceException">Thrown when the request body does not bind an identifier array.</exception>
        public async Task<IActionResult> OnPostAccessTokenStatusAsync([FromBody]int[] ids)
        {
            var data = new List<AccessTokenData>();
            var allMpAccounts = await _mpAccountService.GetFullListAsync(z => true);
            foreach (var id in ids)
            {
                var mpAccount = allMpAccounts.FirstOrDefault(z => z.Id == id);
                if (mpAccount == null)
                {
                    continue;
                }
                var appId = mpAccount.AppId;
                string status = null;
                double leftSeconds = 0;
                AccessTokenBag bag = null;
                if (!appId.IsNullOrEmpty())
                {
                    if (await AccessTokenContainer.CheckRegisteredAsync(appId))
                    {
                        bag = await AccessTokenContainer.TryGetItemAsync(appId);
                        if (bag.AccessTokenResult != null && !bag.AccessTokenResult.access_token.IsNullOrEmpty())
                        {
                            leftSeconds = (bag.AccessTokenExpireTime - SystemTime.Now).TotalSeconds;
                            if (leftSeconds > 9999)
                            {
                                leftSeconds = 0;
                                status = "δ";
                            }
                            else if (leftSeconds > 0)
                            {
                                status = "Ч";
                            }
                            else //leftSeconds <= 0
                            {
                                leftSeconds = 0;
                                status = "ѹ";
                            }
                        }
                        else
                        {
                            status = "δ";
                        }
                    }
                    else
                    {
                        status = "δע";
                    }
                }
                else
                {
                    status = "AppIdЧ";
                }

                var totalSeconds = bag?.AccessTokenResult.expires_in ?? 0;
                var leftPercent = bag?.AccessTokenResult != null && totalSeconds != 0 ? Math.Round(leftSeconds / bag.AccessTokenResult.expires_in * 100, 1) : 0;
                data.Add(new AccessTokenData()
                {
                    Id = id,
                    AppId = appId,
                    Status = status,
                    LeftPercent = leftPercent,
                    TotalSeconds = totalSeconds,
                });
            }
            return Ok(data);
        }
    }
}
