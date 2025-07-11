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
        /// Handler=Ajax
        /// </summary>
        /// <returns></returns>
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
                                status = "未启动";
                            }
                            else if (leftSeconds > 0)
                            {
                                status = "有效";
                            }
                            else //leftSeconds <= 0
                            {
                                leftSeconds = 0;
                                status = "已过期";
                            }
                        }
                        else
                        {
                            status = "未启动";
                        }
                    }
                    else
                    {
                        status = "未注册";
                    }
                }
                else
                {
                    status = "AppId无效";
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
