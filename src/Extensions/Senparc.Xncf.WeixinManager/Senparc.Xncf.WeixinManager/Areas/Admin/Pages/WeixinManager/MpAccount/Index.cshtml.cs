/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Weixin.MP.Containers;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Areas.Admin.WeixinManager
{
    public class MpAccount_IndexModel : BaseAdminWeixinManagerModel
    {
        public PagedList<MpAccountDto> MpAccountDtos { get; set; }

        private readonly ServiceBase<Domain.Models.DatabaseModel.MpAccount> _mpAccountService;
        private int pageCount = 20;


        /// <summary>
        /// Initializes the public-account administration page model.
        /// </summary>
        /// <param name="xncfModuleService">The module service used by the base administration model.</param>
        /// <param name="mpAccountService">The service used to query and delete public accounts.</param>
        public MpAccount_IndexModel(Lazy<XncfModuleService> xncfModuleService, ServiceBase<Domain.Models.DatabaseModel.MpAccount> mpAccountService) : base(xncfModuleService)
        {
            _mpAccountService = mpAccountService;
        }

        /// <summary>
        /// Loads a paged list of public Weixin accounts for the administration page.
        /// </summary>
        /// <param name="pageIndex">The one-based page number.</param>
        public async Task OnGetAsync(int pageIndex = 1)
        {
            var result = await _mpAccountService.GetObjectListAsync(pageIndex, pageCount, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);
            MpAccountDtos = new PagedList<MpAccountDto>(result.Select(z => _mpAccountService.Mapper.Map<MpAccountDto>(z)).ToList(), result.PageIndex, result.PageCount, result.TotalCount);

            //���ԣ����û�����ĳ����
            //await Senparc.Weixin.MP.AdvancedAPIs.UserTagApi.BatchTaggingAsync(MpAccountDtos[0].AppId, 2, new System.Collections.Generic.List<string> { "oxRg0uLsnpHjb8o93uVnwMK_WAVw" });
        }

        /// <summary>
        /// Returns a paged public-account list for the AJAX administration view.
        /// </summary>
        /// <param name="pageIndex">The one-based page number.</param>
        /// <param name="pageSize">The maximum number of records per page.</param>
        /// <returns>An HTTP 200 JSON result containing pagination metadata and DTOs.</returns>
        public async Task<IActionResult> OnGetAjaxAsync(int pageIndex = 1, int pageSize = 10)
        {
            var result = await _mpAccountService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);
            var mpAccountDtos = new PagedList<MpAccountDto>(result.Select(z => _mpAccountService.Mapper.Map<MpAccountDto>(z)).ToList(), result.PageIndex, result.PageCount, result.TotalCount);
            return Ok(new { mpAccountDtos.TotalCount, pageIndex, pageSize, list = mpAccountDtos.AsEnumerable() });
            //���ԣ����û�����ĳ����
            //await Senparc.Weixin.MP.AdvancedAPIs.UserTagApi.BatchTaggingAsync(MpAccountDtos[0].AppId, 2, new System.Collections.Generic.List<string> { "oxRg0uLsnpHjb8o93uVnwMK_WAVw" });
        }

        /// <summary>
        /// Deletes the selected public-account records and removes their access-token cache entries.
        /// </summary>
        /// <param name="ids">The account identifiers selected for deletion.</param>
        /// <returns>An HTTP 200 response containing the current module identifier.</returns>
        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            foreach (var id in ids)
            {
                var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == id);
                if (mpAccount != null)
                {
                    await _mpAccountService.DeleteObjectAsync(mpAccount);
                    await AccessTokenContainer.RemoveFromCacheAsync(mpAccount.AppId);//���ע��״̬
                }
            }
            return Ok(new { Uid });
            //return RedirectToPage("./Index", new { Uid });
        }
    }
}

