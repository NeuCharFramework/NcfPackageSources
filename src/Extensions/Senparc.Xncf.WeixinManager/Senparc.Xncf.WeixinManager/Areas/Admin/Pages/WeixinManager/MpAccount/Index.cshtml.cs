using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Weixin.MP.Containers;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Senparc.Xncf.WeixinManager.Areas.Admin.WeixinManager
{
    public class MpAccount_IndexModel : BaseAdminWeixinManagerModel
    {
        public PagedList<MpAccountDto> MpAccountDtos { get; set; }

        private readonly ServiceBase<Domain.Models.DatabaseModel.MpAccount> _mpAccountService;
        private int pageCount = 20;


        public MpAccount_IndexModel(Lazy<XncfModuleService> xncfModuleService, ServiceBase<Domain.Models.DatabaseModel.MpAccount> mpAccountService) : base(xncfModuleService)
        {
            _mpAccountService = mpAccountService;
        }

        public async Task OnGetAsync(int pageIndex = 1)
        {
            var result = await _mpAccountService.GetObjectListAsync(pageIndex, pageCount, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);
            MpAccountDtos = new PagedList<MpAccountDto>(result.Select(z => _mpAccountService.Mapper.Map<MpAccountDto>(z)).ToList(), result.PageIndex, result.PageCount, result.TotalCount);

            //测试，将用户加入某个组
            //await Senparc.Weixin.MP.AdvancedAPIs.UserTagApi.BatchTaggingAsync(MpAccountDtos[0].AppId, 2, new System.Collections.Generic.List<string> { "oxRg0uLsnpHjb8o93uVnwMK_WAVw" });
        }

        /// <summary>
        /// Handler=Ajax
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAjaxAsync(int pageIndex = 1, int pageSize = 10)
        {
            var result = await _mpAccountService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);
            var mpAccountDtos = new PagedList<MpAccountDto>(result.Select(z => _mpAccountService.Mapper.Map<MpAccountDto>(z)).ToList(), result.PageIndex, result.PageCount, result.TotalCount);
            return Ok(new { mpAccountDtos.TotalCount, pageIndex, pageSize, list = mpAccountDtos.AsEnumerable() });
            //测试，将用户加入某个组
            //await Senparc.Weixin.MP.AdvancedAPIs.UserTagApi.BatchTaggingAsync(MpAccountDtos[0].AppId, 2, new System.Collections.Generic.List<string> { "oxRg0uLsnpHjb8o93uVnwMK_WAVw" });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == id);
                if (mpAccount != null)
                {
                    await _mpAccountService.DeleteObjectAsync(mpAccount);
                    await AccessTokenContainer.RemoveFromCacheAsync(mpAccount.AppId);//清除注册状态
                }
            }
            return Ok(new { Uid });
            //return RedirectToPage("./Index", new { Uid });
        }
    }
}

