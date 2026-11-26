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

        /// <summary>
        /// Handles the initial role-assignment page request.
        /// </summary>
        /// <param name="accountId">The administrator account identifier represented by the page.</param>
        public async Task OnGetAsync(int accountId)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves all role assignments currently associated with an administrator account.
        /// </summary>
        /// <param name="accountId">The administrator account identifier whose assignments are requested.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the matching role assignments.</returns>
        /// <summary>
        /// Applies the submitted role assignments to an administrator account.
        /// </summary>
        /// <param name="grantRoleDto">The target account and role identifiers.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing <see langword="true"/> after persistence.</returns>
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
