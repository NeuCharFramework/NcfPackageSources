using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Validator;
using System;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    //[IgnoreAntiforgeryToken]
    public class AdminUserInfo_EditModel(IServiceProvider serviceProvider, AdminUserInfoService adminUserInfoService)
        : BaseAdminPageModel(serviceProvider), IValidatorEnvironment
    {
        /// <summary>
        /// Id
        /// </summary>
        //[BindProperty]
        public int Id { get; set; }

        public bool IsEdit { get; set; }


        //[BindProperty]
        public CreateOrUpdate_AdminUserInfoDto AdminUserInfo { get; set; } = new CreateOrUpdate_AdminUserInfoDto();
        //public CreateUpdate_AdminUserInfoDto AdminUserInfo { get; set; }

        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;

        /// <summary>
        /// Handles the initial administrator editor page request.
        /// </summary>
        /// <param name="id">The administrator identifier represented by the page.</param>
        /// <returns>The Razor Page result for the editor.</returns>
        public IActionResult OnGet(int id)
        {
            return Page();
        }

        /// <summary>
        /// Loads one administrator edit DTO for the client-side editor.
        /// </summary>
        /// <param name="id">The administrator identifier to retrieve.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the DTO or an empty DTO.</returns>
        [CustomerResource("admin-edit")]
        public IActionResult OnGetDetail(int id)
        {
            var adminUserInfo = _adminUserInfoService.GetAdminUserInfo(id);
            return Ok(adminUserInfo ?? new CreateOrUpdate_AdminUserInfoDto());
        }

        /// <summary>
        /// Validates and persists the JSON-bound administrator DTO.
        /// Duplicate user names and invalid model state return a failed result.
        /// </summary>
        /// <param name="dto">The administrator values submitted by the editor.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing <see langword="true"/> after persistence.</returns>
        [CustomerResource("admin-add", "admin-edit")]
        public async Task<IActionResult> OnPostSave([FromBody] CreateOrUpdate_AdminUserInfoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return Ok(false);
            }
            bool isExists = this._adminUserInfoService.CheckUserNameExisted(dto.Id, dto.UserName);
            if (isExists)
            {
                return Ok(false);
            }
            if (dto.Id > 0)
            {
                //dto.Id = Id;
                _adminUserInfoService.UpdateAdminUserInfo(dto);
            }
            else
            {
                await _adminUserInfoService.CreateAdminUserInfoAsync(dto);
            }
            return Ok(true);
        }

        /// <summary>
        /// Validates and persists the form-bound administrator DTO, selecting
        /// create or update behavior from the supplied page identifier.
        /// </summary>
        /// <returns>The editor page on validation failure or the administrator list after saving.</returns>
        public async Task<IActionResult> OnPost()
        {
            IsEdit = Id > 0;
            this.Validator(AdminUserInfo.UserName, "用户名", "UserName", false)
                .IsFalse(z => this._adminUserInfoService.CheckUserNameExisted(Id, z), "用户名已存在！", true);
            if (!IsEdit)
            {

                this.Validator(AdminUserInfo.Password, "密码", "Password", false).MinLength(6);
            }
            else
            {
                if (!AdminUserInfo.Password.IsNullOrEmpty())
                {
                    this.Validator(AdminUserInfo.Password, "密码", "Password", false).MinLength(6);
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (IsEdit)
            {
                AdminUserInfo.Id = Id;
                _adminUserInfoService.UpdateAdminUserInfo(AdminUserInfo);
            }
            else
            {
                await _adminUserInfoService.CreateAdminUserInfoAsync(AdminUserInfo);
            }

            base.SetMessager(MessageType.success, $"{(IsEdit ? "修改" : "新增")}成功！");
            return RedirectToPage("./Index");
        }
    }
}
