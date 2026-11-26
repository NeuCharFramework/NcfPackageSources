/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Edit.cshtml.cs
    File description: Edit.cshtml related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260731
    Change description: v0.24.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Service;
using Senparc.Weixin.MP.Containers;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Areas.Admin.Pages.WeixinManager
{
    public class MpAccount_EditModel : BaseAdminWeixinManagerModel
    {
        [BindProperty]
        public MpAccountDto MpAccountDto { get; set; }

        private readonly ServiceBase<MpAccount> _mpAccountService;
        public bool IsEdit { get; set; }

        /// <summary>
        /// Initializes the public-account editor page model.
        /// </summary>
        /// <param name="xncfModuleService">The lazily resolved WeixinManager module service.</param>
        /// <param name="mpAccountService">The public-account persistence service.</param>
        public MpAccount_EditModel(Lazy<XncfModuleService> xncfModuleService,
                                   ServiceBase<MpAccount> mpAccountService) : base(xncfModuleService)
        {
            _mpAccountService = mpAccountService;
        }


        /// <summary>
        /// Loads an existing public-account configuration or initializes a new editor model.
        /// </summary>
        /// <param name="id">The optional public-account identifier.</param>
        /// <returns>The editor page or a localized not-found error.</returns>
        public async Task<IActionResult> OnGetAsync(int id = 0)
        {
            IsEdit = id > 0;
            if (IsEdit)
            {
                var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == id);
                if (mpAccount == null)
                {
                    return RenderError(WeixinManagerResource.Get("Account.NotFound", "Account information was not found."));
                }

                MpAccountDto = _mpAccountService.Mapper.Map<MpAccountDto>(mpAccount);
            }
            else
            {
                MpAccountDto = new MpAccountDto();
            }
            return Page();
        }

        /// <summary>
        /// Returns a public-account DTO for asynchronous editor loading.
        /// </summary>
        /// <param name="id">The optional public-account identifier.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the DTO.</returns>
        public async Task<IActionResult> OnGetAjaxAsync(int id = 0)
        {
            var mpAccountDto = new MpAccountDto();
            if (id > 0)
            {
                var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == id);
                if (mpAccount == null)
                {
                    return RenderError(WeixinManagerResource.Get("Account.NotFound", "Account information was not found."));
                }

                mpAccountDto = _mpAccountService.Mapper.Map<MpAccountDto>(mpAccount);
            }
            return Ok(mpAccountDto);
        }

        /// <summary>
        /// Creates or updates a public-account configuration, registers its access token,
        /// and reports whether credential verification succeeded.
        /// </summary>
        /// <param name="id">The optional persisted account identifier.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the persisted ID and module UID.</returns>
        public async Task<IActionResult> OnPostAsync(int id = 0)
        {
            IsEdit = id > 0;
            MpAccount mpAccount = null;
            if (IsEdit)
            {
                mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == id);
                if (mpAccount == null)
                {
                    return RenderError(WeixinManagerResource.Get("Account.NotFound", "Account information was not found."));
                }
                _mpAccountService.Mapper.Map(MpAccountDto, mpAccount);
            }
            else
            {
                mpAccount = new MpAccount(MpAccountDto);
            }
            await _mpAccountService.SaveObjectAsync(mpAccount);

            //½йںע
            await AccessTokenContainer.RegisterAsync(mpAccount.AppId, mpAccount.AppSecret, $"{mpAccount.Name}-{mpAccount.Id}");

            try
            {
                //ȡ AccessToken
                await AccessTokenContainer.GetAccessTokenAsync(mpAccount.AppId, true);
            }
            catch (Exception ex)
            {
                return Ok(new { id = mpAccount.Id, uid = Uid, msg = WeixinManagerResource.Get("Account.CredentialsUnverified", "Saved, but AppId and AppSecret could not be verified.") });
            }


            //return RedirectToPage("./Edit", new { id = mpAccount.Id, uid = Uid });
            return Ok(new { id = mpAccount.Id, uid = Uid });
        }

        /// <summary>
        /// Creates or updates a public-account configuration through a JSON request,
        /// then registers and verifies its access token.
        /// </summary>
        /// <param name="mpAccountDto">The public-account values submitted by the client.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the persisted ID and module UID.</returns>
        public async Task<IActionResult> OnPostAjaxAsync([FromBody] MpAccountDto mpAccountDto)
        {
            MpAccount mpAccount = null;
            if (mpAccountDto.Id > 0)
            {
                mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == mpAccountDto.Id);
                if (mpAccount == null)
                {
                    return RenderError(WeixinManagerResource.Get("Account.NotFound", "Account information was not found."));
                }

                mpAccountDto.AddTime = mpAccount.AddTime;
                _mpAccountService.Mapper.Map(mpAccountDto, mpAccount);
                mpAccount.LastUpdateTime = DateTime.Now;
            }
            else
            {
                mpAccount = new MpAccount(mpAccountDto);
            }
            await _mpAccountService.SaveObjectAsync(mpAccount);

            //½йںע
            await AccessTokenContainer.RegisterAsync(mpAccount.AppId, mpAccount.AppSecret, $"{mpAccount.Name}-{mpAccount.Id}");

            try
            {
                //ȡ AccessToken
                await AccessTokenContainer.GetAccessTokenAsync(mpAccount.AppId, true);
            }
            catch (Exception ex)
            {
                return Ok(new { id = mpAccount.Id, uid = Uid, msg = WeixinManagerResource.Get("Account.CredentialsUnverified", "Saved, but AppId and AppSecret could not be verified.") });
            }


            //return RedirectToPage("./Edit", new { id = mpAccount.Id, uid = Uid });
            return Ok(new { id = mpAccount.Id, uid = Uid });
        }
    }
}
