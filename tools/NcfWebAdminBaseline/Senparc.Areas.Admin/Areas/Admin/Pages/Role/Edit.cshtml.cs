/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Edit.cshtml.cs
    File description: Edit.cshtml.cs related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AutoValidateAntiforgeryToken]
    public class RoleEditModel : BaseAdminPageModel
    {
        private readonly SysRoleService _sysRoleService;

        public RoleEditModel(IServiceProvider serviceProvider, SysRoleService sysRoleService)
            : base(serviceProvider)
        {
            CurrentMenu = "Role";
            _sysRoleService = sysRoleService;
        }

        [BindProperty(SupportsGet = true)]
        /// <summary>
        /// Gets or sets the role identifier selected by the editor.
        /// </summary>
        public string Id { get; set; }

        [BindProperty]
        /// <summary>
        /// Gets or sets the role values bound from the editor.
        /// </summary>
        public SysRoleDto SysRoleDto { get; set; }

        /// <summary>
        /// Loads the selected role into the editor when an identifier is present.
        /// </summary>
        /// <returns>A completed task after the bound editor model has been populated.</returns>
        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                var entity = await _sysRoleService.GetObjectAsync(_ => _.Id == Id);
                SysRoleDto = _sysRoleService.Mapper.Map<SysRoleDto>(entity);
            }
        }

        /// <summary>
        /// Retrieves the role entity requested by the client-side editor.
        /// </summary>
        /// <param name="id">The role identifier to retrieve.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the role entity.</returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-edit")]
        public async Task<IActionResult> OnGetDetailAsync(string id)
        {
            var entity = await _sysRoleService.GetObjectAsync(_ => _.Id == id);
            return Ok(entity);
        }

        /// <summary>
        /// Returns enabled roles as select-list items for another administration form.
        /// </summary>
        /// <param name="senparcEntities">The pooled database context used to query roles.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing enabled role options.</returns>
        public async Task<IActionResult> OnGetSelectItemsAsync([FromServices] /*SenparcEntities*/ BasePoolEntities senparcEntities)
        {
            var list = await senparcEntities.Set<SysRole>().Where(_ => _.Enabled)
                .Select(_ => new SelectListItem() { Value = _.Id, Text = _.RoleName })
                .ToListAsync();
            return Ok(list);
        }

        /// <summary>
        /// Creates or updates the JSON-bound role DTO.
        /// </summary>
        /// <param name="sysRoleDto">The role values submitted by the editor.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing <see langword="true"/> after persistence.</returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-add", "role-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] SysRoleDto sysRoleDto)
        {
            await _sysRoleService.CreateOrUpdateAsync(sysRoleDto);
            return Ok(true);
        }

        /// <summary>
        /// Persists the form-bound role model and redirects to the role list.
        /// </summary>
        /// <returns>A redirect to the role index page after persistence.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            //if (!string.IsNullOrEmpty(Id))
            //{
            //    var entity = await _sysRoleService.GetObjectAsync(_ => _.Id == Id);
            //    SysRoleDto = _sysRoleService.Mapper.Map<SysRoleDto>(entity);
            //}
            await _sysRoleService.CreateOrUpdateAsync(SysRoleDto);
            return RedirectToPage("./Index");
        }
    }
}
