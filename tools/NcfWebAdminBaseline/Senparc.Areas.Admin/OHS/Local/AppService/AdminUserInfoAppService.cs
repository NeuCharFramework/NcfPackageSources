/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminUserInfoAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260724
    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Remove DatabaseSample and related components from MCP extension

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models.Dto;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.Areas.Admin.OHS.PL;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Ncf.Core.Exceptions;
using System.ComponentModel;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
    /// <summary>
    /// Provides administrator-account management, login, role assignment, and
    /// device-display operations.
    /// </summary>
    public class AdminUserInfoAppService : LocalAppServiceBase
    {
        private readonly AdminUserInfoService _adminUserInfoService;
        private readonly AutoMapper.IMapper _mapper;
        /// <summary>
        /// Initializes the administrator-account application service.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="mapper">The DTO/entity mapper.</param>
        /// <param name="adminUserInfoService">The administrator persistence and authentication service.</param>
        public AdminUserInfoAppService(IServiceProvider serviceProvider, AutoMapper.IMapper mapper, AdminUserInfoService adminUserInfoService) : base(serviceProvider)
        {
            this._adminUserInfoService = adminUserInfoService;
            _mapper = mapper;
        }

        /// <summary>
        /// Returns a paged administrator list without exposing stored passwords.
        /// </summary>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageSize">The maximum records per page.</param>
        /// <returns>A response containing sanitized administrator DTOs and total count.</returns>
        [ApiBind]
        public async Task<AppResponseBase<AdminUserInfo_GetListResponse>> GetList(int pageIndex, int pageSize)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminUserInfo_GetListResponse>, AdminUserInfo_GetListResponse>(async (response, logger) =>
            {
                var list = await _adminUserInfoService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                return new AdminUserInfo_GetListResponse()
                {
                    List = list.Select(z =>
                    {
                        var item = _adminUserInfoService.Mapper.Map<AdminUserInfoDto>(z);
                        item.Password = "";
                        return item;
                    }).ToList(),
                    TotalCount = list.TotalCount
                };
            });
        }

        /// <summary>
        /// Creates an administrator account.
        /// </summary>
        /// <param name="request">The administrator account fields.</param>
        /// <returns>A response containing the new administrator identifier.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminUserInfo_CreateResponse>> Create(AdminUserInfo_CreateOrUpdateRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminUserInfo_CreateResponse>, AdminUserInfo_CreateResponse>(async (response, logger) =>
            {
                var dto = _mapper.Map<CreateOrUpdate_AdminUserInfoDto>(request);
                var adminUserInfo = await _adminUserInfoService.CreateAdminUserInfoAsync(dto);
                return new AdminUserInfo_CreateResponse()
                {
                    AdminUserInfoId = adminUserInfo.Id
                };
            });
        }

        /// <summary>
        /// Updates an existing administrator account.
        /// </summary>
        /// <param name="request">The updated administrator account fields.</param>
        /// <returns>A response containing the updated administrator identifier.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        public async Task<AppResponseBase<AdminUserInfo_CreateResponse>> Update(AdminUserInfo_CreateOrUpdateRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminUserInfo_CreateResponse>, AdminUserInfo_CreateResponse>(async (response, logger) =>
            {
                var dto = _mapper.Map<CreateOrUpdate_AdminUserInfoDto>(request);
                var adminUserInfo = await _adminUserInfoService.UpdateAdminUserInfoAsync(dto);
                return new AdminUserInfo_CreateResponse()
                {
                    AdminUserInfoId = adminUserInfo.Id
                };
            });
        }

        /// <summary>
        /// Authenticates an administrator and returns the account login result.
        /// </summary>
        /// <param name="request">The submitted administrator credentials.</param>
        /// <returns>A response containing the authentication result.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        [FunctionRender("管理员登录", "测试当前管理员登录", typeof(Register))]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<AppResponseBase<AccountLoginResultDto>> LoginAsync([FromBody] AdminUserInfo_LoginRequest request)
        {
            AppResponseBase<AccountLoginResultDto> resultDto = await this.GetResponseAsync<AppResponseBase<AccountLoginResultDto>, AccountLoginResultDto>(async (response, logger) =>
            {
                var result = await _adminUserInfoService.LoginAsync(new AccountLoginDto()
                {
                    UserName = request.UserName,
                    Password = request.Password
                });
                logger.Append("管理员登录：" + request.UserName);
                logger.Append("结果：" + !result.UserName.IsNullOrEmpty());

                return result;
            }, exceptionHandler: (ex, response, logger) =>
            {
                logger.Append(ex.Message);
                logger.Append(ex.StackTrace);

                response.ErrorMessage = ex.Message;
            },
            saveLogAfterFinished: true,
            saveLogName: "管理员登录");
            return resultDto;
        }


        /// <summary>
        /// Sends the requested number to the configured digital display device.
        /// </summary>
        /// <param name="request">The number to display.</param>
        /// <returns>A response containing the device request result.</returns>
        [FunctionRender("设置数字管", "设置数字管显示", typeof(Register), AllowAiInvocation = false)]
        public async Task<AppResponseBase<AdminUserInfo_SetDigitalPipeResponse>> SetDigitalPipeAsync(AdminUserInfo_SetDigitalPipeRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminUserInfo_SetDigitalPipeResponse>, AdminUserInfo_SetDigitalPipeResponse>(async (response, logger) =>
            {
                try{
                logger.Append("设置数字管：" + request.Number);

                var jsonBody = new { number = request.Number }.ToJson();
                logger.Append("数字管请求 JSON：" + jsonBody);

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
                var result = await Senparc.CO2NET.HttpUtility.Post.PostGetJsonAsync<AdminUserInfo_SetDigitalPipeResponse>(
                    ServiceProvider,
                    "http://192.168.1.237:5000/api/DisplayNumber",
                    cookieContainer: null,
                    fileStream: stream,
                    encoding: Encoding.UTF8,
                    contentType: "application/json");

                logger.Append("数字管接口返回：" + result.ToJson());
                return result;
                }
                catch(Exception ex){
                    logger.Append("设置数字管失败：" + ex.Message);
                    logger.Append("设置数字管失败：" + ex.StackTrace);
                    return new AdminUserInfo_SetDigitalPipeResponse()
                    {
                        Success = false,
                        Data = ex.Message
                    };
                }
            });
        }

        /// <summary>
        /// Gets the currently authenticated administrator's profile.
        /// </summary>
        /// <returns>A response containing the current administrator account details.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AccountLoginResultDto>> GetAdminUserInfoAsync()
        {
            AppResponseBase<AccountLoginResultDto> resultDto = await this.GetResponseAsync<AppResponseBase<AccountLoginResultDto>, AccountLoginResultDto>(async (response, logger) =>
            {
                var result = await _adminUserInfoService.GetAdminUserInfoAsync(GetCurrentAdminUserInfoId());
                return result;
            });
            return resultDto;
        }


        /// <summary>
        /// Assigns a role to an administrator account.
        /// </summary>
        /// <param name="request">The administrator and role identifiers.</param>
        /// <returns>A response indicating whether the assignment completed.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        public async Task<AppResponseBase<AdminUserInfo_AddRoleResponse>> AddRoleAsync(AdminUserInfo_AddRoleRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<AdminUserInfo_AddRoleResponse>, AdminUserInfo_AddRoleResponse>(async (response, logger) =>
            {
                await ServiceProvider.GetService<SysRoleAdminUserInfoService>().AddAsync(request.RoleId, request.AccountId);
                return new AdminUserInfo_AddRoleResponse();
            });
            return response;
        }

        /// <summary>
        /// Gets the role identifiers assigned to an administrator account.
        /// </summary>
        /// <param name="request">The administrator identifier.</param>
        /// <returns>A response containing the assigned role identifiers.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        public async Task<AppResponseBase<AdminUserInfo_GetRolesResponse>> GetRolesAsync(AdminUserInfo_AddRoleRequest request)
        {
            //int adminUserInfoId = GetCurrentAdminUserInfoId();
            var response = await this.GetResponseAsync<AppResponseBase<AdminUserInfo_GetRolesResponse>, AdminUserInfo_GetRolesResponse>(async (response, logger) =>
            {
                var roles = await ServiceProvider.GetService<SysRoleAdminUserInfoService>().GetFullListAsync(o => o.AccountId == request.AccountId);
                return new AdminUserInfo_GetRolesResponse()
                {
                    RoleIds = roles.Select(o => o.RoleId)
                };
            });
            return response;
        }

        /// <summary>
        /// Deletes an administrator account, except that the current administrator
        /// cannot delete their own account.
        /// </summary>
        /// <param name="id">The administrator identifier to delete.</param>
        /// <returns>A string response describing the deletion result.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Thrown when the current administrator attempts self-deletion or the
        /// requested account does not exist.
        /// </exception>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteAsync(int id)
        {
            var response = await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                int adminUserInfoId = GetCurrentAdminUserInfoId();

                if (id == adminUserInfoId)
                {
                    throw new NcfExceptionBase("管理员不能删除自己！");
                }

                var adminUserInfo = await _adminUserInfoService.GetObjectAsync(z => z.Id == id);
                if (adminUserInfo == null)
                {
                    throw new NcfExceptionBase("管理员不存在！");
                }

                await _adminUserInfoService.DeleteObjectAsync(adminUserInfo);

                return "删除成功！";
            });
            return response;
        }
    }

    public class AdminUserInfo_SetDigitalPipeRequest:AppRequestBase
    {
        [Description("数字管显示内容")]
        public string Number{get;set;}
    }

    public class AdminUserInfo_SetDigitalPipeResponse
    {
        [Description("是否成功")]
        public bool Success{get;set;}
        [Description("返回数据")]
        public string Data{get;set;}
    }
}
