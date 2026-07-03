/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Edit.cshtml.cs
    文件功能描述：Edit.cshtml 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

        public MpAccount_EditModel(Lazy<XncfModuleService> xncfModuleService,
                                   ServiceBase<MpAccount> mpAccountService) : base(xncfModuleService)
        {
            _mpAccountService = mpAccountService;
        }


        public async Task<IActionResult> OnGetAsync(int id = 0)
        {
            IsEdit = id > 0;
            if (IsEdit)
            {
                var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == id);
                if (mpAccount == null)
                {
                    return RenderError("ںϢڣ");
                }

                MpAccountDto = _mpAccountService.Mapper.Map<MpAccountDto>(mpAccount);
            }
            else
            {
                MpAccountDto = new MpAccountDto();
            }
            return Page();
        }

        public async Task<IActionResult> OnGetAjaxAsync(int id = 0)
        {
            var mpAccountDto = new MpAccountDto();
            if (id > 0)
            {
                var mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == id);
                if (mpAccount == null)
                {
                    return RenderError("ںϢڣ");
                }

                mpAccountDto = _mpAccountService.Mapper.Map<MpAccountDto>(mpAccount);
            }
            return Ok(mpAccountDto);
        }

        public async Task<IActionResult> OnPostAsync(int id = 0)
        {
            IsEdit = id > 0;
            MpAccount mpAccount = null;
            if (IsEdit)
            {
                mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == id);
                if (mpAccount == null)
                {
                    return RenderError("ںϢڣ");
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
                return Ok(new { id = mpAccount.Id, uid = Uid, msg = "˺Ѿӣ AppId  Secret ޷飡" });
            }


            //return RedirectToPage("./Edit", new { id = mpAccount.Id, uid = Uid });
            return Ok(new { id = mpAccount.Id, uid = Uid });
        }

        public async Task<IActionResult> OnPostAjaxAsync([FromBody] MpAccountDto mpAccountDto)
        {
            MpAccount mpAccount = null;
            if (mpAccountDto.Id > 0)
            {
                mpAccount = await _mpAccountService.GetObjectAsync(z => z.Id == mpAccountDto.Id);
                if (mpAccount == null)
                {
                    return RenderError("ںϢڣ");
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
                return Ok(new { id = mpAccount.Id, uid = Uid, msg = "˺Ѿӣ AppId  Secret ޷飡" });
            }


            //return RedirectToPage("./Edit", new { id = mpAccount.Id, uid = Uid });
            return Ok(new { id = mpAccount.Id, uid = Uid });
        }
    }
}
