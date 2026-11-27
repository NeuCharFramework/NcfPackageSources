/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRoleAppService.cs
    File description: SysRoleAppService.cs related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
    /// <summary>
    /// Provides super-administrator APIs for maintaining roles and their permissions.
    /// </summary>
    public class SysRoleAppService : AppServiceBase
    {
        private readonly SysRoleService _sysRoleService;
        private readonly SysRolePermissionService _sysPermissionService;
        private readonly AutoMapper.IMapper _mapper;

        /// <summary>
        /// Initializes the role-management application service.
        /// </summary>
        /// <param name="sysRoleService">The role persistence service.</param>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="mapper">The DTO/entity mapper.</param>
        /// <param name="sysPermissionService">The role-permission persistence service.</param>
        public SysRoleAppService(SysRoleService sysRoleService, IServiceProvider serviceProvider, AutoMapper.IMapper mapper, SysRolePermissionService sysPermissionService) : base(serviceProvider)
        {
            _sysRoleService = sysRoleService;
            _mapper = mapper;
            _sysPermissionService = sysPermissionService;
        }

        /// <summary>
        /// Creates a role or updates an existing role after checking role-code uniqueness.
        /// </summary>
        /// <param name="request">The role fields to create or persist.</param>
        /// <returns>A response containing the persisted role identifier.</returns>
        /// <exception cref="Ncf.Core.Exceptions.NcfExceptionBase">
        /// Thrown when the requested role code is already used by another role.
        /// The standard response pipeline converts it into an error response.
        /// </exception>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        //[Permission(Codes = new string[] { "code" })]
        [Ncf.Core.Authorization.Permission("role-add,role-edit")]
        public async Task<AppResponseBase<SysRole_CreateOrUpdateResponse>> CreateOrUpdateAsync(SysRole_CreateOrUpdateRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_CreateOrUpdateResponse>, SysRole_CreateOrUpdateResponse>(async (response, logger) =>
            {
                var dto = _mapper.Map<SysRoleDto>(request);
                var codeRepeat = await _sysRoleService.GetCountAsync(o => o.RoleCode == request.RoleCode && o.Id != request.Id);
                if (codeRepeat > 0)
                {
                    throw new Ncf.Core.Exceptions.NcfExceptionBase($"{request.RoleCode} 重复");
                }
                await _sysRoleService.CreateOrUpdateAsync(dto);
                var modifyRole = await _sysRoleService.GetObjectAsync(o => o.RoleCode == dto.RoleCode);
                return new SysRole_CreateOrUpdateResponse()
                {
                    Id = modifyRole.Id
                };
            });
            return response;
        }

        /// <summary>
        /// Returns a paged, newest-first list of roles matching the supplied filters.
        /// </summary>
        /// <param name="request">The paging and filtering values from the query string.</param>
        /// <returns>A response containing the total count and mapped role list.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        //[Permission("role.query")]
        public async Task<AppResponseBase<SysRole_GetListResponse>> GetListAsync([FromQuery] SysRole_GetListRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_GetListResponse>, SysRole_GetListResponse>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysRole> helper = new SenparcExpressionHelper<SysRole>();
                var list = await _sysRoleService.GetObjectListAsync(request.PageIndex, request.PageSize, helper.BuildWhereExpression(), _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending, null);
                var dtoItems = _mapper.Map<IEnumerable<Domain.Dto.SysRoleListDto>>(list.AsEnumerable());
                return new SysRole_GetListResponse()
                {
                    TotalCount = list.TotalCount,
                    List = dtoItems
                };
            });
            return response;
        }

        /// <summary>
        /// Gets one role and maps it to the administrator detail DTO.
        /// </summary>
        /// <param name="id">The role identifier to retrieve.</param>
        /// <returns>A response containing the mapped role detail.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        //[Permission("role.detail")]
        public async Task<AppResponseBase<SysRole_GetSingleResponse>> GetSingeAsync(string id)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_GetSingleResponse>, SysRole_GetSingleResponse>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysRole> helper = new SenparcExpressionHelper<SysRole>();
                var entity = await _sysRoleService.GetObjectAsync(o => o.Id == id);
                var item = _mapper.Map<Domain.Dto.SysRoleDetailDto>(entity);
                return new SysRole_GetSingleResponse()
                {
                    Detail = item
                };
            });
            return response;
        }

        /// <summary>
        /// Deletes one role and returns the deleted role detail.
        /// </summary>
        /// <param name="id">The role identifier to delete.</param>
        /// <returns>A response containing the mapped role that was removed.</returns>
        //[Permission("role.delete")]
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Delete)]
        public async Task<AppResponseBase<SysRole_GetSingleResponse>> DeleteSingeAsync(string id)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_GetSingleResponse>, SysRole_GetSingleResponse>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysRole> helper = new SenparcExpressionHelper<SysRole>();
                var entity = await _sysRoleService.GetObjectAsync(o => o.Id == id);
                await _sysRoleService.DeleteObjectAsync(entity);
                var item = _mapper.Map<Domain.Dto.SysRoleDetailDto>(entity);
                return new SysRole_GetSingleResponse()
                {
                    Detail = item
                };
            });
            return response;
        }

        /// <summary>
        /// Replaces or adds the permission assignments represented by the request.
        /// </summary>
        /// <param name="request">
        /// The permission assignment DTOs. A null collection is treated as empty.
        /// </param>
        /// <returns>A response indicating whether the permission update completed.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Put)]
        //[Permission("role.addPermission")]
        public async Task<AppResponseBase<SysRole_Response>> AddPermissionAsync(SysRole_AddPermissionRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_Response>, SysRole_Response>(async (response, logger) =>
            {
                try
                {
                    SenparcExpressionHelper<SysRole> helper = new SenparcExpressionHelper<SysRole>();
                    var dto = _mapper.Map<IEnumerable<SysPermissionDto>>(request.RequestDtos ?? new List<Domain.Dto.PermissionRequestDto>());
                    await _sysPermissionService.AddAsync(dto ?? new List<SysPermissionDto>());
                    return new SysRole_Response()
                    {
                    };
                }
                catch (Exception ex)
                {
                    logger.Append(ex.StackTrace);
                    logger.Append(ex.InnerException?.StackTrace);
                    logger.SaveLogs("修改权限出错");
                    throw;
                }
            });
            return response;
        }

        /// <summary>
        /// Gets the permission identifiers currently assigned to a role.
        /// </summary>
        /// <param name="roleId">The role identifier whose permissions are requested.</param>
        /// <returns>A response containing the assigned permission identifiers.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        //[Permission("role.getPermission")]
        public async Task<AppResponseBase<SysRole_PermissionsResponse>> GetRolePermissionsAsync(string roleId)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysRole_PermissionsResponse>, SysRole_PermissionsResponse>(async (response, logger) =>
            {
                var data = await _sysPermissionService.GetFullListAsync(_ => _.RoleId == roleId);
                return new SysRole_PermissionsResponse()
                {
                    PermissionId = data.Select(x => x.PermissionId).ToArray()
                };
            });
            return response;
        }
    }
}
