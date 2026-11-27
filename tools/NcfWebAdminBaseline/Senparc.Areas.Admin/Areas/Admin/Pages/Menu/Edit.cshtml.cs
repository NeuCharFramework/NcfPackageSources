/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Edit.cshtml.cs
    File description: Senparc.Areas.Admin.Areas.Admin.Pages related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class MenuEditModel : BaseAdminPageModel
    {
        private readonly SysMenuService _sysMenuService;
        private readonly SysButtonService _sysButtonService;

        public MenuEditModel(IServiceProvider serviceProvider, SysMenuService _sysMenuService, SysButtonService _sysButtonService)
            : base(serviceProvider)
        {
            CurrentMenu = "Menu";
            this._sysMenuService = _sysMenuService;
            this._sysButtonService = _sysButtonService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        public SysMenuDto SysMenuDto { get; set; }

        public IEnumerable<SysButton> SysButtons { get; set; }

        /// <summary>
        /// Loads the selected menu and its buttons for editing, or initializes
        /// default values for a new menu.
        /// </summary>
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

        /// <summary>
        /// Loads a menu DTO together with its button definitions for client-side editing.
        /// </summary>
        /// <param name="id">The menu identifier to retrieve.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the menu DTO and buttons.</returns>
        public async Task<IActionResult> OnGetDetailAsync(string id)
        {
            var entity = await _sysMenuService.GetObjectAsync(_ => _.Id == Id);
            var sysMenuDto = _sysMenuService.Mapper.Map<SysMenuDto>(entity);
            var sysButtons = await _sysButtonService.GetFullListAsync(_ => _.MenuId == Id);
            return Ok(new { sysMenuDto, sysButtons });
        }

        /// <summary>
        /// Returns the menu tree currently stored in the database.
        /// </summary>
        /// <returns>An <see cref="OkObjectResult"/> containing menu DTOs.</returns>
        public async Task<IActionResult> OnGetMenuAsync()
        {
            //return Ok(await _sysMenuService.GetMenuDtoByCacheAsync(true));
            return Ok(await _sysMenuService.GetMenuDtoByDbAsync());
        }

        /// <summary>
        /// Returns the button definitions registered for a menu.
        /// </summary>
        /// <param name="menuId">The menu identifier whose buttons are requested.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing menu button DTOs.</returns>
        public async Task<IActionResult> OnGetButtonsAsync(string menuId)
        {
            return Ok(await _sysButtonService.GetSysButtonDtosAsync(menuId));
        }

        /// <summary>
        /// Deletes one menu button when a non-empty button identifier is supplied.
        /// </summary>
        /// <param name="buttonId">The button identifier to delete.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the deletion result.</returns>
        public async Task<IActionResult> OnPostDeleteButtonAsync(string buttonId)
        {
            if (string.IsNullOrEmpty(buttonId))
            {
                return Ok(false);
            }
            await _sysButtonService.DeleteObjectAsync(_ => _.Id == buttonId);
            return Ok(true);
        }

        /// <summary>
        /// Deletes unlocked menus and their buttons, then invalidates the menu cache.
        /// Locked menus are returned as undeletable identifiers.
        /// </summary>
        /// <param name="ids">The menu identifiers requested for deletion.</param>
        /// <returns>The identifiers that could not be deleted.</returns>
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

        /// <summary>
        /// Creates or updates a menu from the supplied DTO and returns its identifier.
        /// </summary>
        /// <param name="sysMenu">The menu values to persist.</param>
        /// <returns>The persisted menu identifier or a validation response.</returns>
        public async Task<IActionResult> OnPostAddMenuAsync([FromBody] SysMenuDto sysMenu)
        {
            if (string.IsNullOrEmpty(sysMenu.MenuName))
            {
                return Ok(false, "菜单名称不能为空");
            }
            var entity = await _sysMenuService.CreateOrUpdateAsync(sysMenu);
            return Ok(entity.Id);
        }

        /// <summary>
        /// Persists a menu DTO after model-state validation.
        /// </summary>
        /// <param name="sysMenuDto">The menu values to persist.</param>
        /// <returns>The submitted DTO or a validation response.</returns>
        public async Task<IActionResult> OnPostAsync([FromBody] SysMenuDto sysMenuDto)
        {
            if (!ModelState.IsValid)
            {
                return Ok(false, "模型验证未通过");
            }

            await _sysMenuService.CreateOrUpdateAsync(sysMenuDto);

            return Ok(new { sysMenuDto });
        }
    }
}
