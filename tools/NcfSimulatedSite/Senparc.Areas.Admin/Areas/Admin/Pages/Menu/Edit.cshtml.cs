using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class MenuEditModel : BaseAdminPageModel
    {
        private readonly SysMenuService _sysMenuService;
        private readonly SysButtonService _sysButtonService;
        private readonly IStringLocalizer<AdminResource> _localizer;

        public MenuEditModel(IServiceProvider serviceProvider, SysMenuService _sysMenuService, SysButtonService _sysButtonService, IStringLocalizer<AdminResource> localizer)
            : base(serviceProvider)
        {
            CurrentMenu = "Menu";
            this._sysMenuService = _sysMenuService;
            this._sysButtonService = _sysButtonService;
            this._localizer = localizer;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        public SysMenuDto SysMenuDto { get; set; }

        public IEnumerable<SysButton> SysButtons { get; set; }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                var entity = await _sysMenuService.GetObjectAsync(_ => _.Id == Id);
                SysButtons = await _sysButtonService.GetFullListAsync(_ => _.MenuId == Id);

                SysMenuDto = _sysMenuService.Mapper.Map<SysMenuDto>(entity);
            }
            else
            {
                SysMenuDto = new SysMenuDto() { Visible = true };
                SysButtons = new List<SysButton>() { new SysButton() };
            }
        }

        public async Task<IActionResult> OnGetDetailAsync(string id)
        {
            var entity = await _sysMenuService.GetObjectAsync(_ => _.Id == Id);
            var sysMenuDto = _sysMenuService.Mapper.Map<SysMenuDto>(entity);
            var sysButtons = await _sysButtonService.GetFullListAsync(_ => _.MenuId == Id);
            return Ok(new { sysMenuDto, sysButtons });
        }

        /// <summary>
        /// ??????
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMenuAsync()
        {
            var menus = await _sysMenuService.GetMenuDtoByDbAsync();
            return Ok(menus.Select(m => new
            {
                m.Id,
                m.MenuName,
                localizedMenuName = LocalizeMenuDb(m.MenuName),
                m.ParentId,
                m.Url,
                m.Icon,
                m.Sort,
                m.Visible,
                m.ResourceCode,
                m.IsLocked,
                m.MenuType,
                m.IsMenu,
                m.Flag,
                m.AdminRemark,
                m.Remark,
                m.AddTime,
                m.LastUpdateTime,
                m.TenantId
            }));
        }

        private string LocalizeMenuDb(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var localized = _localizer[$"Menu.Db.{text}"];
            return localized.ResourceNotFound ? text : localized.Value;
        }

        /// <summary>
        /// ???????????
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetButtonsAsync(string menuId)
        {
            return Ok(await _sysButtonService.GetSysButtonDtosAsync(menuId));
        }

        public async Task<IActionResult> OnPostDeleteButtonAsync(string buttonId)
        {
            if (string.IsNullOrEmpty(buttonId))
            {
                return Ok(false);
            }
            await _sysButtonService.DeleteObjectAsync(_ => _.Id == buttonId);
            return Ok(true);
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] string[] ids)
        {
            var entity = await _sysMenuService.GetFullListAsync(_ => ids.Contains(_.Id) && _.IsLocked == false);
            var buttons = await _sysButtonService.GetFullListAsync(_ => ids.Contains(_.MenuId));

            await _sysButtonService.DeleteAllAsync(buttons);
            await _sysMenuService.DeleteAllAsync(entity);
            await _sysMenuService.RemoveMenuAsync();
            IEnumerable<string> unDeleteIds = ids.Except(entity.Select(_ => _.Id));
            return Ok(unDeleteIds);
        }

        public async Task<IActionResult> OnPostAddMenuAsync([FromBody] SysMenuDto sysMenu)
        {
            if (string.IsNullOrEmpty(sysMenu.MenuName))
            {
                return Ok(false, "?????????????");
            }
            var entity = await _sysMenuService.CreateOrUpdateAsync(sysMenu);
            return Ok(entity.Id);
        }

        public async Task<IActionResult> OnPostAsync([FromBody] SysMenuDto sysMenuDto)
        {
            if (!ModelState.IsValid)
            {
                return Ok(false, "???????????");
            }

            await _sysMenuService.CreateOrUpdateAsync(sysMenuDto);

            return Ok(new { sysMenuDto });
        }
    }
}