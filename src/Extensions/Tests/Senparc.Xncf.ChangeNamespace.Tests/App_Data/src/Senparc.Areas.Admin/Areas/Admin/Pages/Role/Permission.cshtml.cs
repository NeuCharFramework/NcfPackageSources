using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Core.Models.DataBaseModel;
using Senparc.Service;

namespace Senparc.Areas.Admin.Areas.Admin
{
    public class PagesRolePermissionModel : BaseAdminPageModel
    {
        private readonly SysRoleService _sysRoleService;
        private readonly SysPermissionService _sysPermissionService;

        //private readonly SysRoleMenuService _sysRoleMenuService;
        private readonly SysMenuService _sysMenuService;

        public PagesRolePermissionModel(SysMenuService sysMenuService, SysRoleService sysRoleService, SysPermissionService sysPermissionService)
        {
            CurrentMenu = "Role";
            _sysRoleService = sysRoleService;
            _sysPermissionService = sysPermissionService;
            //_sysRoleMenuService = sysRoleMenuService;
            _sysMenuService = sysMenuService;
        }

        /// <summary>
        /// 
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string RoleId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SysRole SysRoleInfo { get; set; }

        public async Task OnGetAsync()
        {
            SysRoleInfo = await _sysRoleService.GetObjectAsync(_ => _.Id == RoleId);            
        }

        public async Task<IActionResult> OnGetRolePermissionAsync(string roleId)
        {
            var data = await _sysPermissionService.GetFullListAsync(_ => _.RoleId == roleId);
            return Ok(data);
        }

        public async Task<IActionResult> OnPostAsync(IEnumerable<SysPermissionDto> sysMenuDto)
        {
            if (!sysMenuDto.Any())
            {
                return Ok(false);
            }
            await _sysPermissionService.AddAsync(sysMenuDto);
            return Ok(true);
        }
    }
}