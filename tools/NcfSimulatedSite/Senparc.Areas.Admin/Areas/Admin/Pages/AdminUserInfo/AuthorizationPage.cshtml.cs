using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class AdminUserInfoAuthorizationPageModel(IServiceProvider serviceProvider, SysRoleAdminUserInfoService sysRoleAdminUserInfoService) 
        : BaseAdminPageModel(serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly SysRoleAdminUserInfoService sysRoleAdminUserInfoService = sysRoleAdminUserInfoService;

        public IEnumerable<SysRoleAdminUserInfoDto> RoleAdminUserInfoDtos { get; set; }

        public async Task OnGetAsync(int accountId)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取用户的角色
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("admin-grant")]
        public async Task<IActionResult> OnGetDetailAsync(int accountId)
        {
            IEnumerable<SysRoleAdminUserInfo> sysRoleAdminUserInfos = await sysRoleAdminUserInfoService.GetFullListAsync(_ => _.AccountId == accountId);
            return Ok(sysRoleAdminUserInfos);
        }

        [Ncf.AreaBase.Admin.Filters.CustomerResource("admin-grant")]
        public async Task<IActionResult> OnPostAsync([FromBody] GrantRoleDto grantRoleDto)
        {
            await sysRoleAdminUserInfoService.AddAsync(grantRoleDto.RoleIds, grantRoleDto.AccountId);
            return Ok(true);
        }
    }

    public class GrantRoleDto
    {
        public IEnumerable<string> RoleIds { get; set; }

        public int AccountId { get; set; }
    }
}